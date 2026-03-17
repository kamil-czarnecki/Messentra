# AGENTS.md — Messentra

Cross-platform Azure Service Bus desktop explorer built with **Blazor Server + Electron.NET**, using **Fluxor** for state management, **MudBlazor** for UI, **Mediator** (source-generator CQRS), **FluentValidation**, and **EF Core + SQLite** for local persistence.

## Agent Specifications
Detailed per-agent instructions live in `.github/instructions/`. Invokable agent workers live in `.github/agents/`:

| Role | Agent file | Full spec |
|------|-----------|-----------|
| 🛠 Developer | [developer.agent.md](/.github/agents/developer.agent.md) | [development.instructions.md](/.github/instructions/development.instructions.md) |
| 🧪 Tester | [tester.agent.md](/.github/agents/tester.agent.md) | [tests.instructions.md](/.github/instructions/tests.instructions.md) |
| 🔧 Tests Run & Fix | [tests-run-fix.agent.md](/.github/agents/tests-run-fix.agent.md) | Run suite, triage failures, fix, confirm green |
| 🔍 Debugger | [debugger.agent.md](/.github/agents/debugger.agent.md) | [debugging.instructions.md](/.github/instructions/debugging.instructions.md) |
| 🔎 Code Reviewer | [code-reviewer.agent.md](/.github/agents/code-reviewer.agent.md) | Convention & architecture quality audit |
| ✂️ Refactoring | [refactoring.agent.md](/.github/agents/refactoring.agent.md) | Behaviour-preserving code improvements |
| 📦 Committer | [committer.agent.md](/.github/agents/committer.agent.md) | Branch creation, code review gate, conventional commits |

## Handoff Integrity Rules (All Agents)

- Report completion only after claimed file edits are actually applied in workspace diff.
- `Created`/`Modified` lists must match real file changes; never claim modified files with no diff.
- Run task-relevant validation before handoff (for example `dotnet build`/`dotnet test`) and report result.
- If validation cannot run, return a blocked status with explicit blocker details and remaining verification work.
- Do not return control with provisional completion states (for example "not verified").

## Architecture Overview

```
UI (Razor components)
  → dispatches Fluxor Actions
    → Effects call Mediator (ICommand / IQuery)
      → Handlers call Infrastructure providers
        → Azure SDK (Azure.Messaging.ServiceBus, Azure.Identity)
        └─ EF Core / SQLite (connection config only)
```

Single project (`src/Messentra`). Two test projects under `tests/`:
- `Messentra.UnitTests` — handler/reducer/effect tests, uses `InMemoryDbTestBase` with SQLite in-memory
- `Messentra.ComponentTests` — bUnit component tests, uses `ComponentTestBase` with MudBlazor providers

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Command | `[Action][Entity]Command` | `CreateConnectionCommand` |
| Query | `Get[Entity/Entities]Query` | `GetConnectionsQuery` |
| Handler | `[Command/Query]Handler` | `CreateConnectionCommandHandler` |
| Validator | `[Command]Validator` | `CreateConnectionCommandValidator` |
| Action | `[Action][Entity]Action` | `FetchConnectionsAction` |
| State | `[Feature]State` | `ConnectionState` |
| DTO | `[Entity]Dto` | `ConnectionDto` |
| Component | `[Feature]Component` / `[Feature]Dialog` | `ConnectionsComponent` |
| Page | `[Feature]Page` | `SettingsPage` |
| Test class | `[ClassName]Should` | `CreateConnectionCommandHandlerShould` |

## Key Structural Conventions

### Vertical Slice layout
Each feature lives in `Features/<Feature>/` and owns its slice end-to-end:
```
Features/Settings/Connections/
  CreateConnection/
    CreateConnectionCommand.cs          # ICommand record
    CreateConnectionCommandHandler.cs   # ICommandHandler<T>
    CreateConnectionCommandValidator.cs # AbstractValidator<T>
    ConnectionConfigDto.cs              # slice-local DTO
  ConnectionActions.cs   # all Fluxor action records for this slice
  ConnectionEffects.cs   # [EffectMethod] calling Mediator, dispatching success/failure
  ConnectionReducers.cs  # [ReducerMethod] pure state transforms
  ConnectionState.cs     # [FeatureState] record with private no-arg constructor
  ConnectionSelector.cs  # IStateSelection<> projections (when needed)
```

State files may also live in a `State/` subfolder when the feature warrants it (e.g. `Features/Layout/State/`).


### Mediator (not MediatR)
Uses `Mediator` source-generator package (`Mediator.SourceGenerator`). Implement `IQueryHandler<TQuery, TResult>` / `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResult>`. Registered automatically via `builder.Services.AddMediator(...)`.

### Fluxor State
- Declare `[FeatureState]` on the state record; always include a `private` no-arg constructor that sets defaults.
- Actions: plain `sealed record` types, co-located in `*Actions.cs`.
- Effects: `[EffectMethod]` or `[EffectMethod(typeof(ActionType))]`; always dispatch a `*SuccessAction` or `*FailureAction`.
- Reducers: `[ReducerMethod]` static methods in a `static class *Reducers`.
- Projections (derived/computed state): use `IStateSelection<TState, TProjection>` in a `*Selector` service registered as `Scoped`.
- **`@inherits FluxorComponent` is already in `_Imports.razor`** — never add it manually to individual components.

### Infrastructure Layer
`Infrastructure/` holds Azure SDK wrappers only. Each Azure resource type has its own provider interface + implementation (e.g. `IAzureServiceBusQueueProvider`, `IAzureServiceBusQueueMessagesProvider`). Register via `services.AddInfrastructure()` (extension in `Infrastructure/Extensions.cs`).

`ConnectionInfo` (discriminated union) is the internal infrastructure contract — convert `Domain.ConnectionConfig` → `ConnectionInfo` inside every query/command handler using the same pattern:
```csharp
config.ConnectionType switch {
    ConnectionType.ConnectionString => new ConnectionInfo.ConnectionString(config.ConnectionStringConfig!.ConnectionString),
    ConnectionType.EntraId          => new ConnectionInfo.ManagedIdentity(ns, tenantId, clientId),
    _ => throw new InvalidOperationException(...)
};
```

### Result types
Use `OneOf` source-generator for discriminated results (e.g. `SendMessageResult : OneOfBase<Success, SendMessageError>`). Decorate partial class with `[GenerateOneOf]`.

### Domain persistence
`Connection` entity stores `ConnectionConfig` as a **JSON column** in SQLite (see `ConnectionConfiguration.cs`). Migrations live in `src/Messentra/Migrations/`. Add migrations with:
```bash
dotnet ef migrations add <Name> --project src/Messentra
```

## Component Conventions

- Split: `.razor` (markup) + `.razor.cs` (code-behind `partial class`) + `.razor.css` (scoped styles).
- **Never** use inline `Style="..."` attributes or `<style>` tags — all styles go in `.razor.css`.
- **Wrap each component in a single root element** so isolated CSS selectors work.
- Inject services via **primary constructor** in the `.razor.cs` file (not `[Inject]` properties).
- Use `[Parameter, EditorRequired]` for required parameters.

## Developer Workflows

### Build & run (Electron)
```bash
dotnet electronize start     # hot-reload dev mode (Electron window)
dotnet build                 # build only
```

### Run tests
```bash
dotnet test                  # all tests
dotnet test tests/Messentra.UnitTests
dotnet test tests/Messentra.ComponentTests
dotnet test --filter "FullyQualifiedName~ClassName"   # filter by class
```

### Database migrations
```bash
dotnet ef migrations add <Name> --project src/Messentra
dotnet ef database update    --project src/Messentra
```
The live SQLite DB is stored at `$LOCALAPPDATA/Messentra/Messentra.db` (macOS: `~/.local/share/Messentra/`).

## Test Patterns

### Unit tests (`InMemoryDbTestBase`)
Extend `InMemoryDbTestBase` for handler tests requiring EF Core. Use `AutoFixture` for data, `Moq` for interfaces, `Shouldly` for assertions, `xunit.v3` for test runner.

### Component tests (`ComponentTestBase`)
Extend `ComponentTestBase` (bUnit). It pre-wires MudBlazor, Fluxor mocks (`MockDispatcher`, `IState<T>` via `TestState<T>`), and `JSInterop.Mode = Loose`. Use `MudPopover` component to interact with menu/popover content:
```csharp
cut.Find(".mud-menu button").Click();
MudPopover.Find(".mud-menu-item").Click();
```
Get/set Fluxor state: `GetState<TState>().SetState(newState)`.

Additional component test conventions:
- Prefer async variants of bUnit APIs when available (`ClickAsync`, `WaitForAssertionAsync`, etc.).
- Treat component tests as black-box tests. Do **not** read or mutate internal/private fields, properties, or methods (including via reflection).
- Pass inputs via public component parameters in `Render(...)` and verify outcomes through rendered UI and user interactions only.

### Naming
Test classes: `<Subject>Should` (e.g. `ConnectionEffectsShould`). Test methods: plain English sentences (`HandleFetchConnectionsActionWhenSuccess`).

### AAA Pattern
**Always** include `// Arrange`, `// Act`, `// Assert` comments in every test — minimize other comments.

### Test coverage
- **Unit tests:** handlers (happy path + validation + edge cases), validators (all rules), reducers (each state transition), effects (mediator calls + failure dispatch)
- **Component tests:** initial/loading/error rendering, state integration, user interactions, action dispatching, dialogs

## Key Files Reference

| Path | Purpose |
|------|---------|
| `src/Messentra/Infrastructure/Extensions.cs` | DI registration for all infrastructure services |
| `src/Messentra/Features/_Imports.razor` | Global Razor imports incl. `@inherits FluxorComponent` |
| `src/Messentra/Features/Explorer/Resources/ResourceState.cs` | Tree-node type hierarchy |
| `src/Messentra/Features/Explorer/Resources/SearchQuery.cs` | Search DSL parser (`namespace:`, `has:dlq`) |
| `src/Messentra/Features/Explorer/Resources/ResourceTreeFilter.cs` | Tree filtering logic |
| `src/Messentra/Infrastructure/AzureServiceBus/ConnectionInfo.cs` | Infrastructure auth union type |
| `tests/Messentra.ComponentTests/ComponentTestBase.cs` | bUnit base class |
| `tests/Messentra.UnitTests/InMemoryDbTestBase.cs` | EF Core in-memory test base |

