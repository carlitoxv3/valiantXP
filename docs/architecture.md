# ValiantXP Architecture

## Overview

ValiantXP is a gamification platform built on **.NET 8** using **Clean Architecture**. It provides a scalable engine for running promotional dynamics (Trivia, Surveys, Promo Codes) with passwordless authentication, an event-driven prize system, and a per-campaign anti-fraud pipeline.

---

## Solution Structure

```
ValiantXP/
├── ValiantXP.Domain/           # Enterprise Business Rules
│   ├── Entities/               # Core entities (User, Campaign, DynamicChallenge, Code, FailedAttempt…)
│   ├── Enums/                  # DynamicType (Trivia, Survey, Code), ChallengeStatus, OtpChannel
│   ├── Interfaces/             # Repository contracts, IUnitOfWork, IDynamicStrategy
│   ├── AntiFraud/              # IAntiFraudRule, AntiFraudContext, AntiFraudCampaignConfig
│   └── Exceptions/             # AntiFraudException (with RuleCode factory methods)
│
├── ValiantXP.Application/      # Application Business Rules
│   ├── Features/               # MediatR Commands & Queries
│   │   ├── Auth/               # RequestOtp, VerifyOtp, VerifyMfa commands
│   │   └── Dynamics/           # GetChallenge query, SubmitChallenge command + handlers
│   ├── AntiFraud/              # IAntiFraudPipeline, AntiFraudPipeline
│   ├── Common/                 # Result<T>, IApplicationDbContext
│   └── DTOs/                   # Request/Response DTOs
│
├── ValiantXP.Infrastructure/   # Interface Adapters & Frameworks
│   ├── Data/                   # ApplicationDbContext, EF Core Configurations, Migrations
│   ├── Identity/               # TokenService, OtpService, MfaService, EmailOtpSender, WhatsAppOtpSender
│   ├── Dynamics/               # TriviaStrategy, SurveyStrategy, CodeStrategy, DynamicService
│   ├── AntiFraud/Rules/        # 8 anti-fraud rules
│   └── Repositories/           # GenericRepository<T>, UnitOfWork, all specific repositories
│
├── ValiantXP.API/              # Entry Point
│   ├── Controllers/            # AuthController, DynamicsController
│   ├── Middleware/             # GlobalExceptionHandlerMiddleware
│   └── Program.cs              # DI composition root, middleware pipeline
│
└── ValiantXP.Tests/            # xUnit unit + integration tests (51 tests)
    ├── Features/Auth/
    ├── Features/Dynamics/
    └── AntiFraud/
```

---

## Authentication Flow

```
Client
  │
  ├─► POST /api/auth/otp/request  { contact, channel: "Email"|"WhatsApp" }
  │       │
  │       └─► OtpService: generate 6-digit OTP, hash, store (10min expiry)
  │           IOtpSender (Email or WhatsApp) → send OTP
  │
  ├─► POST /api/auth/otp/verify  { contact, otp }
  │       │
  │       ├─► Validate OTP hash + expiry
  │       ├─► Auto-register if user doesn't exist
  │       ├─► if IsMfaEnabled = true → return { mfaRequired: true, tempToken }
  │       └─► if IsMfaEnabled = false → return { accessToken, refreshToken }
  │
  ├─► POST /api/auth/mfa/verify  { tempToken, totp }  [if MFA required]
  │       │
  │       └─► Validate TOTP via RFC 6238 → return { accessToken, refreshToken }
  │
  └─► POST /api/auth/refresh  { refreshToken }
          │
          └─► Rotate refresh token → return new { accessToken, refreshToken }
```

---

## Dynamics Engine Flow

```
POST /api/dynamics/{id}/submit  [Authorize]
  │
  ├─ 1. Load DynamicChallenge + Campaign
  ├─ 2. Deserialize AntiFraudCampaignConfig from DynamicChallenge.AntiFraudConfigJson
  ├─ 3. Build AntiFraudContext (userId, challengeId, campaignId, type, remoteIp, inputs, config)
  │
  ├─ 4. AntiFraudPipeline.RunAsync(context)
  │       │
  │       ├─ Order 5:  CampaignActiveWindowRule     [all types]
  │       ├─ Order 10: CodeExistsRule               [Code only]
  │       ├─ Order 20: CodeNotUsedRule              [Code only]
  │       ├─ Order 30: MaxRedemptionsPerUserRule     [Code only]
  │       ├─ Order 30: MaxTriviaAttemptsRule         [Trivia only]
  │       ├─ Order 30: SurveyOncePerUserRule         [Survey only]
  │       ├─ Order 40: MaxAttemptsPerIpRule          [Code only]
  │       └─ Order 50: FailedAttemptsBlockRule       [Code only]
  │           │
  │           └─ AntiFraudException? → record FailedAttempt → return error
  │
  ├─ 5. Resolve IDynamicStrategy by DynamicChallenge.Type
  │       ├─ Trivia  → TriviaStrategy  (score vs passingScore from ConfigurationJson)
  │       ├─ Survey  → SurveyStrategy  (always succeeds)
  │       └─ Code    → CodeStrategy    (atomic mark UsedAt + UserId)
  │
  ├─ 6. Update UserChallengeProgress (Attempts, Score, Status, CompletedAt)
  ├─ 7. SaveChanges
  ├─ 8. [if success] Publish ChallengeCompletedEvent
  │       └─ ChallengeCompletedEventHandler → InstantWin lottery → UserPrize
  │
  └─ 9. Return ChallengeResultDto
          { success, message, payload, awardedPrizes, nextChallengeId }
```

---

## Anti-Fraud Pipeline

| Order | Rule | Dynamic Type | PromoHub Equivalent |
|------:|------|-------------|---------------------|
| 5 | `CampaignActiveWindowRule` | **All** | Campaign date validation |
| 10 | `CodeExistsRule` | Code | `ExchangeCode` SP — check #1 |
| 20 | `CodeNotUsedRule` | Code | `ExchangeCode` SP — check #2 |
| 30 | `MaxRedemptionsPerUserRule` | Code | `ValidateExchangeCode` SP (user) |
| 30 | `MaxTriviaAttemptsRule` | Trivia | — |
| 30 | `SurveyOncePerUserRule` | Survey | — |
| 40 | `MaxAttemptsPerIpRule` | Code | `ValidateExchangeCode` SP (IP) |
| 50 | `FailedAttemptsBlockRule` | Code | `DetectBots` SP |

Per-campaign configuration is stored as JSON in `DynamicChallenge.AntiFraudConfigJson` and deserialized to `AntiFraudCampaignConfig` at runtime. Each module has its own config section (`Code`, `Trivia`, `Survey`).

---

## Technology Decisions

| Decision | Choice | Rationale |
|---|---|---|
| **Mediator** | MediatR | Decouples commands/queries from handlers; enables domain events without tight coupling |
| **ORM** | EF Core 8 + SQL Server | Mature, type-safe, supports migrations and async queries |
| **Dynamic Strategy** | Strategy Pattern via `IDynamicStrategy` | Allows adding new dynamic types without modifying existing code (Open/Closed) |
| **Anti-Fraud** | Rule Pipeline via `IAntiFraudRule` | Rules are independently injectable, orderable, and testable; mirrors PromoHub's Template Method but more composable |
| **Auth** | Passwordless OTP + TOTP MFA | No password storage risk; omnichannel (Email/WhatsApp) via `OtpChannel` enum |
| **Containerization** | Docker + Docker Compose | Dev/prod parity; orchestrated with SQL Server sidecar |
| **CI/CD** | GitHub Actions | Automated build, test, and semantic version tagging on merge to main |
