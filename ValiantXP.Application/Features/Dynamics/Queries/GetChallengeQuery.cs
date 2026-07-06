using MediatR;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Application.Common;
using ValiantXP.Application.DTOs;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.Application.Features.Dynamics.Queries;

public record GetChallengeQuery(Guid Id) : IRequest<Result<ChallengeDto>>;

public class GetChallengeQueryHandler : IRequestHandler<GetChallengeQuery, Result<ChallengeDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetChallengeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChallengeDto>> Handle(GetChallengeQuery request, CancellationToken cancellationToken)
    {
        var challenge = await _unitOfWork.DynamicChallenges.GetAsync(request.Id, cancellationToken);
        if (challenge == null)
        {
            return Result<ChallengeDto>.Failure("Challenge not found.");
        }

        if (!challenge.IsActive)
        {
            return Result<ChallengeDto>.Failure("Challenge is inactive.");
        }

        var sanitizedJson = SanitizeConfigurationJson(challenge.ConfigurationJson);

        var dto = new ChallengeDto
        {
            Id = challenge.Id,
            CampaignId = challenge.CampaignId,
            Type = challenge.Type.ToString(),
            Name = challenge.Name,
            Description = challenge.Description,
            IsActive = challenge.IsActive,
            AnonParticipationAllowed = challenge.AnonParticipationAllowed,
            ConfigurationJson = sanitizedJson
        };

        return Result<ChallengeDto>.Success(dto);
    }

    private string SanitizeConfigurationJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        try
        {
            var node = JsonNode.Parse(json);
            SanitizeNode(node);
            return node?.ToJsonString() ?? json;
        }
        catch
        {
            return json;
        }
    }

    private void SanitizeNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var keysToRemove = new List<string>();
            foreach (var property in obj)
            {
                var keyLower = property.Key.ToLowerInvariant();
                if (keyLower.Contains("correct") || keyLower.Contains("answer"))
                {
                    keysToRemove.Add(property.Key);
                }
                else
                {
                    SanitizeNode(property.Value);
                }
            }
            foreach (var key in keysToRemove)
            {
                obj.Remove(key);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var child in arr)
            {
                SanitizeNode(child);
            }
        }
    }
}
