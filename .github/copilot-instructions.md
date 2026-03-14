---
name: orchestrator
description: Routes tasks to specialized agents (developer, tester, or debugger)
---

Main orchestrator for Messentra — analyze the request and embody the appropriate agent role.

## Available Agents

### Orchestrator (`orchestrator`)
🤖 **[Agent →](./agents/orchestrator.agent.md)**

**Use for:** Any multi-step task — new features with tests, bug fix + regression test, refactors. Plans work first, then executes step by step using the right specialist agents.

### Developer Agent (`developer`)
🤖 **[Agent →](./agents/developer.agent.md)** · 📖 **[Full spec →](./instructions/development.instructions.md)**

**Use for:** Features, CQRS operations, UI components, Fluxor state, validators, database operations, refactoring, bug fixes

### Tester Agent (`tester`)
🤖 **[Agent →](./agents/tester.agent.md)** · 📖 **[Full spec →](./instructions/tests.instructions.md)**

**Use for:** Unit tests, component tests, test coverage, fixing failing tests

### Debugger Agent (`debugger`)
🤖 **[Agent →](./agents/debugger.agent.md)** · 📖 **[Full spec →](./instructions/debugging.instructions.md)**

**Use for:** Diagnosing runtime errors, Fluxor state issues, Azure SDK failures, EF Core errors, component rendering bugs

## Decision Process
1. **Multi-step task** (feature + tests, bug + fix + test, refactor)? → use **orchestrator**
2. **Single concern** (implement only, test only, diagnose only)? → use the specialist directly
3. **Ambiguous?** → default to **orchestrator**

## Project Context
📖 **[Shared Standards](./instructions/shared.md)** — always active via `applyTo: "**"` (naming, component rules, commands)
