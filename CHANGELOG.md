# CHANGELOG

All notable changes to ValiantXP will be documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) | Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html)

---

## [0.4.0] - 2026-06-30
### Changed
- **English Nomenclature Refactor** — renamed all Spanish identifiers to English throughout the codebase:
  - `DynamicType.Codigo` → `DynamicType.Code`
  - `DynamicType.Encuesta` → `DynamicType.Survey`
  - `class CodigoStrategy` → `class CodeStrategy`
  - `class EncuestaStrategy` → `class SurveyStrategy`
  - `CodigoAntiFraudConfig` → `CodeAntiFraudConfig`
  - `EncuestaAntiFraudConfig` → `SurveyAntiFraudConfig`
  - `AntiFraudCampaignConfig.Codigo` → `AntiFraudCampaignConfig.Code`
  - `AntiFraudCampaignConfig.Encuesta` → `AntiFraudCampaignConfig.Survey`
  - All anti-fraud rule `ApplicableType` references updated
  - All test classes updated: `CodigoStrategyTests` → `CodeStrategyTests`, `EncuestaStrategyTests` → `SurveyStrategyTests`
- 51 unit tests passing, 0 failures.

---

## [0.3.0] - 2026-06-29
### Added
- **Anti-Fraud Layer (Sprint 2.5)** — cross-cutting concern integrated into the Dynamics Pattern.
  - `IAntiFraudRule` — ordered, per-dynamic-type rule contract with `ApplicableType` (null = all) and `Order` (multiples of 10).
  - `AntiFraudPipeline` — executes rules in ascending Order, filters by dynamic type, short-circuits on first `AntiFraudException`.
  - `AntiFraudContext` — immutable per-request context (userId, challengeId, campaignId, challengeType, remoteIp, inputs, config, campaign dates).
  - `AntiFraudCampaignConfig` — per-campaign nested config with module sections:
    - `Code`: MaxRedemptionsPerUserPerDay, MaxAttemptsPerIpPerHour, TrackFailedAttempts, MaxFailedAttemptsBeforeBlock, FailedAttemptWindowMinutes, ExternalValidatorId
    - `Trivia`: MaxAttemptsPerUser, CooldownBetweenAttemptsSeconds, TrackIpAttempts, MaxIpAttemptsPerHour
    - `Survey`: OncePerUser, EnforceIpUniqueness, MaxSubmissionsPerIp
  - `AntiFraudException` — strongly typed with `RuleCode` factory methods (`CODE_NOT_FOUND`, `CODE_ALREADY_USED`, `DAILY_LIMIT_EXCEEDED`, `IP_LIMIT_EXCEEDED`, `USER_BLOCKED`, `TRIVIA_ATTEMPTS_EXCEEDED`, `SURVEY_ALREADY_ANSWERED`, `CAMPAIGN_NOT_ACTIVE`).
  - `FailedAttempt` entity + `IFailedAttemptRepository` + EF Core config with composite indexes for rolling-window COUNT queries.
  - **8 anti-fraud rules** (in execution order):
    - `CampaignActiveWindowRule` (Order 5, all types)
    - `CodeExistsRule` (Order 10, Code) — mirrors PromoHub ExchangeCode check #1
    - `CodeNotUsedRule` (Order 20, Code) — mirrors PromoHub ExchangeCode check #2
    - `MaxRedemptionsPerUserRule` (Order 30, Code) — mirrors PromoHub ValidateExchangeCode (user)
    - `MaxTriviaAttemptsRule` (Order 30, Trivia)
    - `SurveyOncePerUserRule` (Order 30, Survey)
    - `MaxAttemptsPerIpRule` (Order 40, Code) — mirrors PromoHub ValidateExchangeCode (IP)
    - `FailedAttemptsBlockRule` (Order 50, Code) — mirrors PromoHub DetectBots SP
  - `SubmitChallengeCommandHandler` updated: pipeline runs before strategy execution, failed attempts recorded on rejection, `RemoteIp` propagated from `DynamicsController` through `SubmitChallengeCommand`.
  - `DynamicChallenge.AntiFraudConfigJson` — nullable JSON field for per-challenge anti-fraud overrides.
  - `IUnitOfWork.FailedAttempts` — exposed through the Unit of Work pattern.
  - 6 unit tests: `AntiFraudPipelineTests` (empty pipeline, all pass, exception propagation, type-mismatch skip, execution order, short-circuit).
- Total: **51 tests passing**.

---

## [0.2.0] - 2026-06-29
### Added
- **Code Module (Promo Code Redemption)**: New `CodeStrategy` dynamic that validates and consumes promo codes. Validates code exists, is unused, then marks it consumed (`UsedAt`, `UserId`). Prize assignment delegated to `ChallengeCompletedEvent` (PromoHub pattern).
- **`Code` entity** with: `CodeNumber`, `CampaignId`, `UserId`, `UsedAt`, `RemoteIP`.
- **`CodeRepository`** implementing `ICodeRepository` with `GetByCodeNumberAsync` and `BulkInsertAsync` (stub).
- **`CodeConfiguration`** (EF Core Fluent API): table `Codes`, unique index on `CodeNumber`, FK to `Campaign` (Restrict) and `User` (SetNull).
- **`DbSet<Code> Codes`** added to `ApplicationDbContext`.
- **`ICodeRepository Codes`** added to `IUnitOfWork`, implemented in `UnitOfWork` with lazy initialization.
- **Challenge Chaining**: `SubmitChallengeCommandHandler` returns `NextChallengeId` in `ChallengeResultDto` when challenge succeeds and `DynamicChallenge.NextChallengeId` is configured.
- **`ChallengeResultDto.NextChallengeId`** (`Guid?`): new field in response DTO.
- **DI registration**: `CodeStrategy` and `ICodeRepository` registered in `DependencyInjection.cs`.
- **Unit tests** (`CodeStrategyTests.cs`): 6 cases — null code, empty code, invalid code, already-used code, valid code, code with whitespace trimming.
- **Integration tests** (`ChainingIntegrationTests.cs`): 3 cases — `NextChallengeId` propagated on success, null on failure, null when no chain configured.
- Total: **45 tests passing**.

---

## [0.1.0] - 2026-06-29
### Added
- Initial .NET 8 solution with Clean Architecture (Domain / Application / Infrastructure / API / Tests).
- Passwordless authentication module: OTP omnichannel (Email + WhatsApp) via `OtpChannel` enum, TOTP MFA (Otp.NET), JWT (access + refresh rotation).
- Entities: `User`, `RefreshToken`, `OtpCode`.
- Generic `IRepository<T>` + specific repositories: `UserRepository`, `RefreshTokenRepository`, `OtpCodeRepository`.
- JWT claims: sub, email, unique_name, jti, role. Refresh token rotation with sliding expiry.
- API endpoints: `POST /api/auth/otp/request`, `POST /api/auth/otp/verify`, `POST /api/auth/mfa/setup`, `POST /api/auth/mfa/enable`, `POST /api/auth/mfa/verify`, `POST /api/auth/refresh`, `GET /api/users/me`.
- Rate limiting, Azure Key Vault config, Docker multi-stage build, Docker Compose, GitHub Actions CI/CD pipeline.
