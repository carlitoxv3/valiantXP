using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.API.Controllers;

/// <summary>Campaigns controller — returns active campaigns with their challenges.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CampaignsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CampaignsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>Returns all active campaigns with their challenges.</summary>
    /// <remarks>
    /// Returns campaigns where IsActive = true and EndDate &gt; now.
    /// Challenges within each campaign have configurationJson included.
    /// Correct answers are NOT exposed (stripped server-side in trivia configs).
    /// </remarks>
    /// <response code="200">List of active campaigns with nested challenges.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveCampaigns(CancellationToken ct)
    {
        var campaigns = await _unitOfWork.Campaigns.GetAllWithChallengesAsync(ct);

        var result = campaigns
            .Where(c => c.IsActive && c.EndDate > DateTime.UtcNow)
            .Select(c => new
            {
                id = c.Id,
                name = c.Name,
                description = (string?)null,   // extend entity if needed
                imageUrl = (string?)null,
                startsAt = c.StartDate,
                endsAt = c.EndDate,
                isActive = c.IsActive,
                challenges = c.Challenges
                    .Where(ch => ch.IsActive)
                    .Select(ch => new
                    {
                        id = ch.Id,
                        name = ch.Name,
                        description = ch.Description,
                        type = ch.Type.ToString(),
                        configurationJson = StripCorrectAnswers(ch.ConfigurationJson, ch.Type.ToString()),
                        isActive = ch.IsActive,
                        campaignId = ch.CampaignId,
                        anonParticipationAllowed = ch.AnonParticipationAllowed
                    })
                    .ToList()
            })
            .ToList();

        return Ok(result);
    }

    /// <summary>Returns a single campaign by ID.</summary>
    /// <response code="200">Campaign found.</response>
    /// <response code="404">Campaign not found.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCampaign(Guid id, CancellationToken ct)
    {
        var campaign = await _unitOfWork.Campaigns.GetWithChallengesAsync(id, ct);
        if (campaign == null) return NotFound(new { error = "Campaign not found." });

        return Ok(new
        {
            id = campaign.Id,
            name = campaign.Name,
            description = (string?)null,
            imageUrl = (string?)null,
            startsAt = campaign.StartDate,
            endsAt = campaign.EndDate,
            isActive = campaign.IsActive,
            challenges = campaign.Challenges
                .Where(ch => ch.IsActive)
                .Select(ch => new
                {
                    id = ch.Id,
                    name = ch.Name,
                    description = ch.Description,
                    type = ch.Type.ToString(),
                    configurationJson = StripCorrectAnswers(ch.ConfigurationJson, ch.Type.ToString()),
                    isActive = ch.IsActive,
                    campaignId = ch.CampaignId,
                    anonParticipationAllowed = ch.AnonParticipationAllowed
                })
                .ToList()
        });
    }

    /// <summary>
    /// Strips correct answer flags from trivia configurationJson before sending to client.
    /// Prevents cheating while keeping the question structure intact.
    /// </summary>
    private static string StripCorrectAnswers(string configJson, string type)
    {
        if (type != "Trivia" || string.IsNullOrEmpty(configJson)) return configJson;

        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(configJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("questions", out _)) return configJson;

            // Rebuild JSON without isCorrect fields
            using var ms = new System.IO.MemoryStream();
            using var writer = new System.Text.Json.Utf8JsonWriter(ms);
            writer.WriteStartObject();

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name == "questions")
                {
                    writer.WritePropertyName("questions");
                    writer.WriteStartArray();
                    foreach (var q in prop.Value.EnumerateArray())
                    {
                        writer.WriteStartObject();
                        foreach (var qProp in q.EnumerateObject())
                        {
                            if (qProp.Name == "options")
                            {
                                writer.WritePropertyName("options");
                                writer.WriteStartArray();
                                foreach (var opt in qProp.Value.EnumerateArray())
                                {
                                    writer.WriteStartObject();
                                    foreach (var optProp in opt.EnumerateObject())
                                    {
                                        // Strip isCorrect from options — sent as letter index only
                                        if (optProp.Name != "isCorrect")
                                            optProp.WriteTo(writer);
                                    }
                                    writer.WriteEndObject();
                                }
                                writer.WriteEndArray();
                            }
                            else
                            {
                                qProp.WriteTo(writer);
                            }
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }
                else
                {
                    prop.WriteTo(writer);
                }
            }

            writer.WriteEndObject();
            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }
        catch
        {
            return configJson; // fallback: return as-is
        }
    }
}
