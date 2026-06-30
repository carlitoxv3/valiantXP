# Dynamics Engine

## Overview
The Dynamics Engine processes gamification challenges using the **Strategy Pattern**. Each dynamic type has its own `IDynamicStrategy` implementation resolved at runtime by `DynamicType`. Strategies plug into an event-driven prize system via MediatR domain events.

---

## Dynamic Types

| `DynamicType` | Strategy | Description |
|---|---|---|
| `Trivia` | `TriviaStrategy` | Multiple-choice quiz. Score calculated against correct answers in `ConfigurationJson`. Passes if `score >= passingScore`. |
| `Survey` | `SurveyStrategy` | Opinion survey. Always succeeds on submission. Answers stored for analytics. |
| `Code` | `CodeStrategy` | Promo code redemption. Validates code exists + unused, then atomically marks it as used (`UsedAt`, `UserId`). |

---

## Submit API

```
POST /api/dynamics/{id}/submit
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "inputs": {
    "code": "PROMO2026"          // for Code dynamic
    "q1": "B", "q2": "A"        // for Trivia dynamic
    "opinion": "Very good"       // for Survey dynamic
  }
}
```

### Response
```json
{
  "success": true,
  "message": "Code 'PROMO2026' successfully redeemed!",
  "payload": { "CodeNumber": "PROMO2026", "CampaignId": "..." },
  "awardedPrizeNames": ["Free Coffee (Code: PRIZE-ABC123)"],
  "nextChallengeId": "33333333-3333-3333-3333-333333333333"
}
```

---

## Processing Flow

```
POST /api/dynamics/{id}/submit
  │
  1. Load DynamicChallenge + Campaign (validate IsActive)
  2. Deserialize AntiFraudCampaignConfig from AntiFraudConfigJson
  3. Build AntiFraudContext
  │
  4. ─── AntiFraudPipeline ───────────────────────────────────
  │   Runs 8 ordered rules. Throws AntiFraudException on fail.
  │   Failed attempt recorded to FailedAttempts table.
  │   ───────────────────────────────────────────────────────
  │
  5. Resolve IDynamicStrategy by challenge.Type
  6. strategy.ExecuteAsync(context) → DynamicResult
  7. Create/update UserChallengeProgress (Attempts, Score, Status)
  8. SaveChangesAsync
  │
  9. [if success] IPublisher.Publish(ChallengeCompletedEvent)
  │     └─ ChallengeCompletedEventHandler
  │           → InstantWin lottery (check Prize.RemainingQuantity)
  │           → Create UserPrize with generated code
  │           → SaveChangesAsync
  │
  10. Return ChallengeResultDto
```

---

## Challenge Configuration

Each `DynamicChallenge` stores its type-specific settings in `ConfigurationJson`.

### Trivia — `ConfigurationJson` schema
```json
{
  "questions": [
    {
      "id": "q1",
      "text": "What is the capital of France?",
      "options": { "A": "Berlin", "B": "Paris", "C": "Madrid" },
      "correctAnswer": "B"
    }
  ],
  "passingScore": 60
}
```

### Survey — `ConfigurationJson` schema
```json
{
  "questions": [
    { "id": "q1", "text": "How would you rate our product?" }
  ]
}
```

### Code — No `ConfigurationJson` needed
The `code` input key is the only requirement:
```json
{ "inputs": { "code": "PROMO2026" } }
```

---

## Challenge Chaining

When `DynamicChallenge.NextChallengeId` is set, the response includes `nextChallengeId` on successful completion. Clients navigate to the next challenge automatically. Chaining **only occurs on success**.

```
Challenge A (Trivia) ──success──► Challenge B (Code) ──success──► Challenge C (Survey)
    nextChallengeId = B.Id           nextChallengeId = C.Id           nextChallengeId = null
```

---

## InstantWin Prize System

On `ChallengeCompletedEvent`, the `ChallengeCompletedEventHandler` runs a lottery:

1. Loads all prizes for the completed challenge
2. For each prize with `RemainingQuantity > 0`, evaluates the win probability
3. If the user wins: decrements `RemainingQuantity`, creates a `UserPrize` with a unique code
4. The awarded prize code is returned in `awardedPrizeNames`

Prize types: `Coupon`, `Points`, `PhysicalPrize`, `DigitalVoucher`
