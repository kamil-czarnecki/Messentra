# Messentra — Project Overview

Messentra is a **cross-platform Azure Service Bus desktop explorer** built with **Blazor Server + Electron.NET**. It lets developers browse namespaces, queues, topics, and subscriptions; send and peek messages; and manage saved connections — all from a native desktop window on Windows and macOS.

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| **Runtime** | .NET 10, Blazor Server, Electron.NET |
| **UI** | MudBlazor |
| **State management** | Fluxor (Redux pattern) |
| **CQRS** | Mediator source-generator (`Mediator.SourceGenerator`) |
| **Validation** | FluentValidation |
| **Persistence** | Entity Framework Core + SQLite (local connections store) |
| **Azure SDK** | `Azure.Messaging.ServiceBus`, `Azure.Identity` |
| **Testing** | xUnit v3, Shouldly, AutoFixture, Moq, bUnit |

---

## Solution Structure

```
Messentra.slnx
├── src/
│   └── Messentra/                  # Single application project
│       ├── Features/               # Vertical slices (UI + state + CQRS)
│       │   ├── Explorer/           # Resource tree, message viewer
│       │   ├── Settings/           # Connection management
│       │   ├── Layout/             # Shell, activity log, navigation
│       │   └── Jobs/               # Background job state
│       ├── Infrastructure/         # Azure SDK wrappers + DI registration
│       │   └── AzureServiceBus/
│       ├── Domain/                 # Connection entity + ConnectionConfig
│       ├── Migrations/             # EF Core SQLite migrations
│       └── wwwroot/
└── tests/
    ├── Messentra.UnitTests/        # Handler, validator, reducer, effect tests
    └── Messentra.ComponentTests/   # bUnit Blazor component tests
```

---

## Architecture

```
UI (Razor components)
  → dispatches Fluxor Actions
    → Effects call Mediator (ICommand / IQuery)
      → Handlers call Infrastructure providers
        → Azure SDK  (Azure.Messaging.ServiceBus, Azure.Identity)
        └─ EF Core / SQLite  (connection config only)
```

- **No direct Azure SDK calls outside `Infrastructure/`**
- **No `ConnectionConfig` passed to infrastructure** — always converted to `ConnectionInfo` (discriminated union) inside the handler
- **No cross-slice imports** — each feature owns its slice end-to-end

---

## Key Features

- **Connection management** — save named connections using either a connection string or Entra ID (managed identity / service principal)
- **Resource explorer** — browse Service Bus namespaces, queues, topics, and subscriptions in a tree; search with a DSL (`namespace:`, `has:dlq`)
- **Message operations** — send, peek, receive, dead-letter, and resubmit messages
- **Activity log** — in-app panel showing real-time operation results (success / error surfaced from every Effect)

---

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Command | `[Action][Entity]Command` | `CreateConnectionCommand` |
| Query | `Get[Entity/Entities]Query` | `GetConnectionsQuery` |
| Handler | `[Command/Query]Handler` | `CreateConnectionCommandHandler` |
| Validator | `[Command]Validator` | `CreateConnectionCommandValidator` |
| Fluxor action | `[Action][Entity]Action` | `FetchConnectionsAction` |
| State | `[Feature]State` | `ConnectionState` |
| DTO | `[Entity]Dto` | `ConnectionDto` |
| Component | `[Feature]Component` / `[Feature]Dialog` | `ConnectionsComponent` |
| Page | `[Feature]Page` | `SettingsPage` |
| Test class | `[ClassName]Should` | `CreateConnectionCommandHandlerShould` |

---

## Vertical Slice Layout

Each feature lives entirely under `Features/<Feature>/` and owns its slice end-to-end:

```
Features/Settings/Connections/
  CreateConnection/
    CreateConnectionCommand.cs           # ICommand record
    CreateConnectionCommandHandler.cs    # ICommandHandler<T>
    CreateConnectionCommandValidator.cs  # AbstractValidator<T>
    ConnectionConfigDto.cs               # slice-local DTO
  ConnectionActions.cs    # all Fluxor action records
  ConnectionEffects.cs    # [EffectMethod] → Mediator → *SuccessAction / *FailureAction
  ConnectionReducers.cs   # [ReducerMethod] pure state transforms
  ConnectionState.cs      # [FeatureState] record with private no-arg ctor
  ConnectionSelector.cs   # IStateSelection<> projections (when needed)
```

State files may also live in a `State/` subfolder (e.g. `Features/Layout/State/`).

---

## Design Patterns

### CQRS (Mediator source-generator)
- `ICommand` / `ICommand<TResult>` for write operations
- `IQuery<TResult>` for read operations
- Registered automatically via `builder.Services.AddMediator(...)`
- Every handler calls `validator.ValidateAndThrowAsync()` before any business logic

### Fluxor (Redux)
- `[FeatureState]` record with a `private` no-arg constructor that sets defaults
- Every Effect dispatches `*SuccessAction` **or** `*FailureAction` — never swallows exceptions
- `LogActivityAction` dispatched in every effect (both paths) to surface results in the Activity Log
- `@inherits FluxorComponent` is inherited globally via `_Imports.razor` — **never** add it to individual components

### Blazor Components
- Split: `.razor` (markup) + `.razor.cs` (code-behind) + `.razor.css` (scoped styles)
- Constructor injection in `.razor.cs` — no `[Inject]` properties
- Single root element in every `.razor` file — required for scoped CSS
- No `Style="..."` attributes or `<style>` tags

### Result types
`OneOf` source-generator for discriminated command results (e.g. `SendMessageResult : OneOfBase<Success, SendMessageError>`).

### Infrastructure auth contract
`ConnectionInfo` (discriminated union) — convert `Domain.ConnectionConfig` → `ConnectionInfo` inside every handler:
```csharp
config.ConnectionType switch {
    ConnectionType.ConnectionString => new ConnectionInfo.ConnectionString(...),
    ConnectionType.EntraId          => new ConnectionInfo.ManagedIdentity(...),
    _ => throw new InvalidOperationException(...)
};
```

---

## Developer Workflows

```bash
dotnet electronize start      # hot-reload dev mode (Electron window)
dotnet build                  # build only
dotnet test                   # all tests
dotnet test tests/Messentra.UnitTests
dotnet test tests/Messentra.ComponentTests
dotnet test --filter "FullyQualifiedName~ClassName"

dotnet ef migrations add <Name> --project src/Messentra
dotnet ef database update     --project src/Messentra
```

Live SQLite DB: `~/.local/share/Messentra/Messentra.db` (macOS) · `%LOCALAPPDATA%\Messentra\Messentra.db` (Windows)

---

## Testing Strategy

All tests use **xUnit v3**, **Shouldly** for assertions, **AutoFixture** for data, and **Moq** for mocking.

| Type | Project | Base class | Covers |
|------|---------|-----------|--------|
| Unit | `Messentra.UnitTests` | `InMemoryDbTestBase` (for EF) or plain `sealed class` | Handlers, validators, reducers, effects |
| Component | `Messentra.ComponentTests` | `ComponentTestBase` (bUnit + MudBlazor) | Blazor component rendering, state integration, user interactions |

- Test classes: `sealed`, named `<Subject>Should`
- Test methods: plain English sentences
- **Always** `// Arrange`, `// Act`, `// Assert` — no other comments

---

## Extending the Solution

When adding new functionality:

1. **Start in the CQRS slice** — add `Command`/`Query` + `Handler` + `Validator` under `Features/<Feature>/<Action>/`
2. **Wire Fluxor** — add actions in `*Actions.cs`, effects in `*Effects.cs`, reducers in `*Reducers.cs`; every effect must dispatch `*SuccessAction` or `*FailureAction` and call `LogActivityAction`
3. **Build the UI** — `.razor` + `.razor.cs` + `.razor.css`; constructor injection; single root element
4. **Add infrastructure** — new provider interface + implementation in `Infrastructure/AzureServiceBus/`; register in `Infrastructure/Extensions.cs`
5. **Cover with tests** — handler (happy path + validation + edge cases), validator (all rules), reducer (each transition), effect (mediator calls + failure dispatch), component (rendering + interactions)

---

## Key Files Reference

| Path | Purpose |
|------|---------|
| `src/Messentra/Infrastructure/Extensions.cs` | DI registration for all infrastructure services |
| `src/Messentra/Features/_Imports.razor` | Global Razor imports incl. `@inherits FluxorComponent` |
| `src/Messentra/Infrastructure/AzureServiceBus/ConnectionInfo.cs` | Infrastructure auth union type |
| `src/Messentra/Features/Explorer/Resources/ResourceState.cs` | Tree-node type hierarchy |
| `src/Messentra/Features/Explorer/Resources/SearchQuery.cs` | Search DSL parser (`namespace:`, `has:dlq`) |
| `src/Messentra/Features/Explorer/Resources/ResourceTreeFilter.cs` | Tree filtering logic |
| `tests/Messentra.UnitTests/InMemoryDbTestBase.cs` | EF Core in-memory test base |
| `tests/Messentra.ComponentTests/ComponentTestBase.cs` | bUnit base class |

