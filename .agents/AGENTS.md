# ValiantXP — Agent Team Protocol

The ValiantXP project uses a **permanent team of 5 specialized agents** with strict file-scope lanes.
Never create one-shot sprint agents. Always reuse this team by sending messages to running agents.

## Commit Policy (ONE rule, no exceptions)

> **Only `tester` commits. Only after `dotnet test` is 100% GREEN.**

```
dev    → NEVER commits
infra  → NEVER commits
api    → NEVER commits
tester → ONE atomic commit per sprint, after all tests pass
analyst→ NEVER commits code (Parent commits docs if needed)
```

### Commit format
```
feat(<module>): <description> (Sprint N)

Examples:
  feat(giftcard): implement GiftCard pool delivery with atomic SQL (Sprint 10)
  feat(identity): implement identity federation with OAuth and guest sessions (Sprint 9)
  test(giftcard): add pool assignment, out-of-stock, and entity tests
```

### Why only tester?
- tester is last in the chain — confirms the FULL system works together
- A commit before `dotnet test` passes = broken commit
- Single commit per sprint = clean, bisectable history
- If tester finds a bug in src → reports to Parent → Parent fixes → tester retests → then commits

## The Team

| Agent | Role | Scope | Activate when |
|---|---|---|---|
| `dev` | Domain & Application Developer | `Domain/`, `Application/` | New entity, interface, command, query, strategy |
| `infra` | Infrastructure & Data Engineer | `Infrastructure/` | New repo, EF config, migration, infra service |
| `api` | API Layer Developer | `API/Controllers/`, `API/Program.cs` | New endpoint, controller, middleware |
| `tester` | QA Engineer | `Tests/` only | Build is clean, needs tests written or fixed |
| `analyst` | Read-only Research Agent | Artifacts `.md` only | PromoHub analysis, architecture research |

## Sprint Workflow (always in this order)

```
1. analyst  →  research (if needed) → produces design .md
2. dev      →  Domain + Application (interfaces first)
3. infra ‖ api  →  parallel (both read Domain interfaces, write to their own lanes)
4. tester   →  tests (after build is confirmed clean)
5. Parent   →  reviews → approves git commit
```

**Max 2 agents running in parallel.** Never run dev + infra + api all at once.

## Lane Rules (no exceptions)

| Agent | ✅ CAN write | ❌ NEVER touches |
|---|---|---|
| `dev` | `Domain/**`, `Application/**` | `Infrastructure/`, `API/`, `Tests/` |
| `infra` | `Infrastructure/**` | `Domain/`, `Application/`, `API/`, `Tests/` |
| `api` | `API/Controllers/**`, `API/Program.cs` | `Domain/`, `Infrastructure/`, `Tests/` |
| `tester` | `Tests/**` | ANY `src/` file |
| `analyst` | `artifacts/*.md` | ANY `.cs` file |

### The DI Registration Rule
`DependencyInjection.cs` belongs to `infra`.
When `api` needs to register something → api tells Parent → Parent tells infra → infra adds it.

## Conflict Prevention

When running infra + api in parallel:
1. `dev` MUST commit first (interfaces stable before others start)
2. Both agents create **new files only** (never edit the same file simultaneously)
3. `DependencyInjection.cs` → infra handles it last (after api is done)

## Commit Convention

```
feat(domain):  ...   ← dev commits
feat(infra):   ...   ← infra commits
feat(api):     ...   ← api commits
test(...):     ...   ← tester commits
docs:          ...   ← analyst produces, Parent commits
```

## Sprint Kickoff Checklist

1. Update `task.md` with new sprint tasks
2. Update `implementation_plan.md` with the design
3. Send `analyst` their research task (if needed)
4. Send `dev` their Domain/Application task
5. When dev reports done → send `infra` and `api` their tasks in parallel
6. When both report done → send `tester` to run tests + commit
7. Review → approve

## Agent Locations

All agents: `c:\Users\ag_dw\Documents\Massive\valiantXP\.agents\agents\<name>\agent.json`
