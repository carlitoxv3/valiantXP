# Anti-Fraud Layer

## Overview
The anti-fraud layer is a **cross-cutting concern** integrated into the Dynamics Pattern. It executes as a pipeline of ordered, injectable rules before any dynamic strategy runs. Each rule is independently testable and configurable per campaign.

The design mirrors PromoHub's stored procedure chain (`ExchangeCode` → `ValidateExchangeCode` → `DetectBots`) but reimplements it as a clean, composable pipeline.

---

## Architecture

```
POST /api/dynamics/{id}/submit
  │
  ▼
SubmitChallengeCommandHandler
  │
  ├─ Deserialize AntiFraudCampaignConfig from DynamicChallenge.AntiFraudConfigJson
  ├─ Build AntiFraudContext (immutable, built once per request)
  │
  ▼
AntiFraudPipeline.RunAsync(AntiFraudContext)
  │
  ├─ Iterates IAntiFraudRule[] sorted by Rule.Order (ascending)
  ├─ Skips rules where ApplicableType != context.ChallengeType
  └─ Throws AntiFraudException on first rule violation
       │
       └─► [if Code type] Record FailedAttempt to DB → return error result
  │
  ▼
IDynamicStrategy.ExecuteAsync()  (only reached if all rules pass)
```

---

## Rules Reference

| Order | Rule Class | Applies To | Config Key | PromoHub Equivalent |
|------:|------------|-----------|-----------|---------------------|
| 5 | `CampaignActiveWindowRule` | **All** | `EnforceCampaignDateWindow` | Campaign date check |
| 10 | `CodeExistsRule` | Code | — | `ExchangeCode` SP check #1 |
| 20 | `CodeNotUsedRule` | Code | — | `ExchangeCode` SP check #2 |
| 30 | `MaxRedemptionsPerUserRule` | Code | `Code.MaxRedemptionsPerUserPerDay` | `ValidateExchangeCode` (user) |
| 30 | `MaxTriviaAttemptsRule` | Trivia | `Trivia.MaxAttemptsPerUser` | — |
| 30 | `SurveyOncePerUserRule` | Survey | `Survey.OncePerUser` | — |
| 40 | `MaxAttemptsPerIpRule` | Code | `Code.MaxAttemptsPerIpPerHour` | `ValidateExchangeCode` (IP) |
| 50 | `FailedAttemptsBlockRule` | Code | `Code.MaxFailedAttemptsBeforeBlock` + `FailedAttemptWindowMinutes` | `DetectBots` SP |

---

## Per-Campaign Configuration

Each `DynamicChallenge` stores its anti-fraud config in `AntiFraudConfigJson`. Missing values fall back to sensible defaults.

```json
{
  "EnforceCampaignDateWindow": true,
  "Code": {
    "MaxRedemptionsPerUserPerDay": 1,
    "MaxAttemptsPerIpPerHour": 5,
    "TrackFailedAttempts": true,
    "MaxFailedAttemptsBeforeBlock": 3,
    "FailedAttemptWindowMinutes": 60,
    "ExternalValidatorId": null
  },
  "Trivia": {
    "MaxAttemptsPerUser": 3,
    "CooldownBetweenAttemptsSeconds": 0,
    "TrackIpAttempts": false,
    "MaxIpAttemptsPerHour": 10
  },
  "Survey": {
    "OncePerUser": true,
    "EnforceIpUniqueness": false,
    "MaxSubmissionsPerIp": 1
  }
}
```

---

## Error Codes

When a rule fires, `AntiFraudException` is thrown with a machine-readable `RuleCode`. The `GlobalExceptionHandlerMiddleware` maps these to the correct HTTP status:

| `RuleCode` | HTTP Status | Meaning |
|---|---|---|
| `CODE_NOT_FOUND` | 422 Unprocessable Entity | Code does not exist in DB |
| `CODE_ALREADY_USED` | 422 Unprocessable Entity | Code was already redeemed |
| `DAILY_LIMIT_EXCEEDED` | 429 Too Many Requests | Daily redemption cap reached |
| `IP_LIMIT_EXCEEDED` | 429 Too Many Requests | Too many requests from this IP |
| `USER_BLOCKED` | 429 Too Many Requests | User blocked after N failed attempts |
| `TRIVIA_ATTEMPTS_EXCEEDED` | 422 Unprocessable Entity | Max trivia attempts reached |
| `SURVEY_ALREADY_ANSWERED` | 422 Unprocessable Entity | Survey already submitted |
| `CAMPAIGN_NOT_ACTIVE` | 422 Unprocessable Entity | Outside campaign date window |

---

## Failed Attempt Tracking

When a Code submission is rejected by an anti-fraud rule, a `FailedAttempt` record is written to the database by `SubmitChallengeCommandHandler` (not by the rule itself — separation of concerns):

```csharp
// FailedAttempt entity
{
  UserId, ChallengeId, CampaignId,
  SubmittedValue,   // the code that was submitted
  RemoteIp,         // from HttpContext
  RuleCode,         // which rule fired
  Reason,           // human-readable message
  AttemptedAt       // UTC timestamp
}
```

The `FailedAttempts` table has **composite indexes** optimized for the rolling-window COUNT queries used by `FailedAttemptsBlockRule` and `MaxAttemptsPerIpRule`:
- `(UserId, ChallengeId, AttemptedAt)`
- `(RemoteIp, ChallengeId, AttemptedAt)`

---

## Adding a Custom Rule

1. Create a class in `ValiantXP.Infrastructure/AntiFraud/Rules/`
2. Implement `IAntiFraudRule`:

```csharp
public sealed class MyCustomRule : IAntiFraudRule
{
    // null = applies to all types; or set DynamicType.Code / .Trivia / .Survey
    public DynamicType? ApplicableType => DynamicType.Code;

    // Use multiples of 10; existing rules use 5–50
    public int Order => 60;

    public async Task ValidateAsync(AntiFraudContext context, CancellationToken ct)
    {
        // Access per-campaign config via context.Config.Code.*
        // Throw AntiFraudException to reject
        // Return normally to pass
    }
}
```

3. Register in DI (`DependencyInjection.cs`):
```csharp
services.AddScoped<IAntiFraudRule, MyCustomRule>();
```

The pipeline picks it up automatically — no other changes required.
