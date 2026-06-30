# ValiantXP

A production-grade **gamification platform API** built with .NET 8 and Clean Architecture. Provides passwordless authentication, a multi-type dynamics engine (Trivia, Survey, Promo Codes), an event-driven InstantWin prize system, and a per-campaign anti-fraud pipeline.

---

## Features

- 🔐 **Passwordless Auth** — OTP via Email or WhatsApp + optional TOTP MFA (no passwords stored)
- 🎮 **Dynamics Engine** — Strategy pattern: Trivia, Survey, Code redemption with challenge chaining
- 🏆 **InstantWin Prizes** — Event-driven prize lottery triggered by challenge completion
- 🛡️ **Anti-Fraud Pipeline** — 8 configurable rules per campaign (rate limiting, bot detection, IP blocking)
- 📦 **Clean Architecture** — Domain / Application / Infrastructure / API with MediatR + EF Core
- 🐳 **Docker Ready** — Multi-stage Dockerfile + Docker Compose for local development
- ⚙️ **CI/CD** — GitHub Actions with automated testing and semantic versioning

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│  ValiantXP.API          (Controllers, Middleware)   │
├─────────────────────────────────────────────────────┤
│  ValiantXP.Application  (MediatR Commands/Queries)  │
├─────────────────────────────────────────────────────┤
│  ValiantXP.Infrastructure (EF Core, Strategies,    │
│                            Anti-Fraud Rules, DI)    │
├─────────────────────────────────────────────────────┤
│  ValiantXP.Domain       (Entities, Interfaces,     │
│                          Enums, Exceptions)         │
└─────────────────────────────────────────────────────┘
```

See [docs/architecture.md](docs/architecture.md) for detailed flow diagrams.

---

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| Docker Desktop | Latest |
| SQL Server | 2019+ (or via Docker) |
| dotnet-ef tool | 8.x (`dotnet tool install -g dotnet-ef`) |

---

## Quick Start

### Option A — Docker Compose (recommended)

```bash
git clone https://github.com/carlitoxv3/valiantXP.git
cd valiantXP
docker-compose up --build
```

API available at `http://localhost:5000` | Swagger UI at `http://localhost:5000/swagger`

### Option B — Local (dotnet run)

```bash
# 1. Configure connection string
cp ValiantXP.API/appsettings.Development.json.example ValiantXP.API/appsettings.Development.json
# Edit DefaultConnection in appsettings.Development.json

# 2. Apply database migrations
dotnet ef database update \
  --project ValiantXP.Infrastructure \
  --startup-project ValiantXP.API

# 3. Run the API
dotnet run --project ValiantXP.API
```

---

## Environment Variables

| Variable | Description | Example |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | `Server=localhost;Database=ValiantXP_Dev;...` |
| `JwtSettings__Secret` | JWT signing key (min 32 chars) | `your-super-secret-key-min-32-chars` |
| `JwtSettings__Issuer` | JWT issuer | `ValiantXP` |
| `JwtSettings__Audience` | JWT audience | `ValiantXP-Client` |
| `JwtSettings__AccessTokenExpiryMinutes` | Access token TTL | `60` |
| `JwtSettings__RefreshTokenExpiryDays` | Refresh token TTL | `30` |
| `OtpSettings__ExpiryMinutes` | OTP expiry | `10` |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` / `Production` |

---

## API Endpoints

### Authentication

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| POST | `/api/auth/otp/request` | Request OTP (Email or WhatsApp) | — |
| POST | `/api/auth/otp/verify` | Verify OTP → JWT or MFA required | — |
| POST | `/api/auth/mfa/setup` | Get TOTP secret + QR URI | Bearer |
| POST | `/api/auth/mfa/enable` | Enable MFA with first TOTP | Bearer |
| POST | `/api/auth/mfa/verify` | Verify TOTP during login | tempToken |
| POST | `/api/auth/refresh` | Rotate refresh token | — |
| GET | `/api/users/me` | Get current user profile | Bearer |

### Dynamics

| Method | Endpoint | Description | Auth |
|---|---|---|---|
| GET | `/api/dynamics/{id}` | Get challenge details (answers hidden) | Bearer |
| POST | `/api/dynamics/{id}/submit` | Submit challenge (Trivia/Survey/Code) | Bearer |

---

## Running Tests

```bash
dotnet test ValiantXP.sln
```

```
✅ 51 tests passed, 0 failed
   Features/Auth: 15 tests
   Features/Dynamics: 30 tests
   AntiFraud: 6 tests
```

---

## Documentation

| Document | Description |
|---|---|
| [docs/architecture.md](docs/architecture.md) | Solution structure, flows, tech decisions |
| [docs/modules/auth.md](docs/modules/auth.md) | OTP + MFA authentication reference |
| [docs/modules/dynamics.md](docs/modules/dynamics.md) | Dynamics engine + challenge config schemas |
| [docs/modules/antifraud.md](docs/modules/antifraud.md) | Anti-fraud pipeline, rule reference, error codes |
| [CHANGELOG.md](CHANGELOG.md) | Version history |

---

## Project Structure

```
ValiantXP/
├── ValiantXP.Domain/          # Entities, Enums, Interfaces, AntiFraud contracts
├── ValiantXP.Application/     # MediatR commands/queries, DTOs, pipeline
├── ValiantXP.Infrastructure/  # EF Core, strategies, rules, repositories, identity
├── ValiantXP.API/             # Controllers, middleware, Program.cs
├── ValiantXP.Tests/           # xUnit tests (51 tests)
├── docs/                      # Documentation
├── docker-compose.yml         # Local dev orchestration
├── Dockerfile                 # Multi-stage production build
└── .github/workflows/         # CI/CD pipelines
```

---

## Contributing

This project uses an **agentic development methodology** — features are implemented by specialized AI subagents (Architect, Developer, Tester, DevOps) orchestrated by a Scrum Master agent. Each sprint results in a clean build, passing tests, and a commit.

See [docs/CONTRIBUTING_AGENTS.md](docs/CONTRIBUTING_AGENTS.md) for the agent workflow.

Manual contributions follow standard GitHub Flow: feature branch → PR → CI passes → merge.
