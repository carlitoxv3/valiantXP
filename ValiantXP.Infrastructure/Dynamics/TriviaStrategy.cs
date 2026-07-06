using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Infrastructure.Dynamics;

public class TriviaStrategy : IDynamicStrategy
{
    public string DynamicType => Domain.Enums.DynamicType.Trivia.ToString();

    public async Task<DynamicResult> ExecuteAsync(DynamicContext context, CancellationToken cancellationToken)
    {
        // 1. Load configuration and user inputs
        var challengeId = context.DynamicId;
        var inputs = context.Inputs ?? new Dictionary<string, string>();

        // We will fetch the challenge configuration in the handler/service and pass it in context.
        // Wait! In the IDynamicStrategy interface:
        // Task<DynamicResult> ExecuteAsync(DynamicContext context, CancellationToken cancellationToken);
        // And DynamicContext has: DynamicId, UserId, Inputs.
        // Wait, how does the strategy read ConfigurationJson?
        // Ah! It only gets DynamicId. So the strategy itself should load the challenge from the database!
        // That makes perfect sense. Let's inject IUnitOfWork in TriviaStrategy so it can fetch the challenge.
        // Wait, let's inject IUnitOfWork. Yes!
        return await ProcessTriviaAsync(context, cancellationToken);
    }

    private readonly IUnitOfWork _unitOfWork;

    public TriviaStrategy(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private async Task<DynamicResult> ProcessTriviaAsync(DynamicContext context, CancellationToken cancellationToken)
    {
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(context.DynamicId, cancellationToken);
        if (challenge == null)
        {
            return new DynamicResult
            {
                Success = false,
                Message = "Trivia challenge not found."
            };
        }

        var configJson = challenge.ConfigurationJson;
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return new DynamicResult
            {
                Success = false,
                Message = "Trivia configuration is empty."
            };
        }

        // Parse JSON
        using var document = JsonDocument.Parse(configJson);
        var root = document.RootElement;

        // Try to read "questions" array, or default to root array if root is an array
        JsonElement questionsElement = default;
        int passingScore = 70; // Default passing score out of 100

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("questions", out var questionsProp))
            {
                questionsElement = questionsProp;
            }
            if (root.TryGetProperty("passingScore", out var passingProp) && passingProp.ValueKind == JsonValueKind.Number)
            {
                passingScore = passingProp.GetInt32();
            }
            else if (root.TryGetProperty("threshold", out var thresholdProp) && thresholdProp.ValueKind == JsonValueKind.Number)
            {
                passingScore = thresholdProp.GetInt32();
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            questionsElement = root;
        }

        if (questionsElement.ValueKind != JsonValueKind.Array)
        {
            return new DynamicResult
            {
                Success = false,
                Message = "Invalid Trivia configuration format."
            };
        }

        int totalPoints = 0;
        int correctPoints = 0;
        int correctCount = 0;
        int questionCount = 0;

        int questionIndex = 0;
        foreach (var questionElement in questionsElement.EnumerateArray())
        {
            questionCount++;

            // --- Determine correct answer ---
            // Priority 1: correctIndex (int) — matches frontend format { q0: "2", q1: "1" }
            // Priority 2: correctAnswer (string) — legacy string match
            int? correctIndex = null;
            string? correctAnswerStr = null;

            if (questionElement.TryGetProperty("correctIndex", out var ciProp) && ciProp.ValueKind == JsonValueKind.Number)
                correctIndex = ciProp.GetInt32();
            else if (questionElement.TryGetProperty("correctAnswer", out var ansProp))
                correctAnswerStr = ansProp.GetString();
            else if (questionElement.TryGetProperty("answer", out var ansProp2))
                correctAnswerStr = ansProp2.GetString();

            // --- Get question points ---
            int questionPoints = 10;
            if (questionElement.TryGetProperty("points", out var pointsProp) && pointsProp.ValueKind == JsonValueKind.Number)
                questionPoints = pointsProp.GetInt32();

            totalPoints += questionPoints;

            // --- Match user answer ---
            // Frontend sends: { "q0": "2", "q1": "1", "q2": "2" } (index as string)
            // Try q{index} key first, then id-based key
            string? userAnswer = null;

            var qKey = $"q{questionIndex}";
            if (context.Inputs.TryGetValue(qKey, out var uaByIndex))
                userAnswer = uaByIndex;
            else if (questionElement.TryGetProperty("id", out var idProp))
            {
                var qId = idProp.GetString() ?? string.Empty;
                context.Inputs.TryGetValue(qId, out userAnswer);
            }

            // --- Check correctness ---
            if (userAnswer != null)
            {
                bool isCorrect = false;

                if (correctIndex.HasValue)
                {
                    // Compare as integer index
                    if (int.TryParse(userAnswer.Trim(), out var userIndex))
                        isCorrect = userIndex == correctIndex.Value;
                }
                else if (!string.IsNullOrEmpty(correctAnswerStr))
                {
                    isCorrect = string.Equals(userAnswer.Trim(), correctAnswerStr.Trim(), StringComparison.OrdinalIgnoreCase);
                }

                if (isCorrect)
                {
                    correctPoints += questionPoints;
                    correctCount++;
                }
            }

            questionIndex++;
        }


        // Calculate score
        // If totalPoints is 0, score is 100
        int finalScore = totalPoints > 0 ? (int)Math.Round((double)correctPoints / totalPoints * 100) : 100;
        bool isSuccess = finalScore >= passingScore;

        var message = isSuccess 
            ? $"Trivia passed! Score: {finalScore} (Passed threshold of {passingScore}). Correct answers: {correctCount}/{questionCount}."
            : $"Trivia failed. Score: {finalScore} (Required: {passingScore}). Correct answers: {correctCount}/{questionCount}.";

        var payload = new Dictionary<string, object>
        {
            { "Score", finalScore },
            { "Total", totalPoints },
            { "CorrectCount", correctCount },
            { "PassingScore", passingScore }
        };

        return new DynamicResult
        {
            Success = isSuccess,
            Message = message,
            Payload = payload
        };
    }
}
