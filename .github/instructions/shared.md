---
applyTo: "**"
---
# Shared Project Standards

This document contains common patterns and standards used across development and testing.

## Tech Stack
- **.NET:** 10.0, Blazor Server, Electron.NET
- **UI:** MudBlazor
- **State:** Fluxor (Redux pattern)
- **CQRS:** Mediator.SourceGenerator
- **Validation:** FluentValidation
- **Database:** Entity Framework Core with SQLite
- **Testing:** xUnit, Shouldly, AutoFixture, Moq, bUnit

## Naming Conventions
- **Commands:** `[Action][Entity]Command` → `CreateConnectionCommand`
- **Queries:** `Get[Entity/Entities]Query` → `GetConnectionsQuery`
- **Handlers:** `[Command/Query]Handler` → `CreateConnectionCommandHandler`
- **Validators:** `[Command]Validator` → `CreateConnectionCommandValidator`
- **Actions:** `[Action][Entity]Action` → `LoadConnectionsAction`
- **State:** `[Feature]State` → `ConnectionState`
- **DTOs:** `[Entity]Dto` → `ConnectionDto`
- **Components:** `[Feature]Component` or `[Feature]Dialog` → `ConnectionsComponent`
- **Pages:** `[Feature]Page` → `SettingsPage`
- **Tests:** `[ClassName]Should` → `CreateConnectionCommandHandlerShould`

## Component File Structure
Each Blazor component consists of 1-3 files:

1. **`[Component].razor`** (Required) - Markup only, no `@inherits` needed
2. **`[Component].razor.cs`** (Optional) - Logic, dependencies, lifecycle
3. **`[Component].razor.css`** (Optional) - Scoped styles

### Critical Rules
- ❌ **DON'T** add `@inherits FluxorComponent` - inherited via `_Imports.razor`
- ❌ **DON'T** use `Style` attributes or `<style>` tags - use `.razor.css`
- ⚠️ **MUST** wrap component in single root element for isolated CSS to work

## AAA Testing Pattern
```csharp
[Fact]
public async Task MethodName()
{
    // Arrange
    // ... setup
    
    // Act
    // ... execute
    
    // Assert
    // ... verify
}
```

## Common Tools
- **Build:** `dotnet build`
- **Run:** `dotnet run`
- **Test:** `dotnet test`
- **Filter tests:** `dotnet test --filter "FullyQualifiedName~ClassName"`
