# Read Project Instructions

Please read and understand the project instructions in this order:

1. **Shared Standards** — [shared.md](../instructions/shared.md)
   Naming conventions, component rules, AAA pattern — applied automatically to all files via `applyTo: "**"`

2. **Developer Agent Spec** — [development.instructions.md](../instructions/development.instructions.md)
   CQRS patterns, Fluxor wiring, component conventions

3. **Tester Agent Spec** — [tests.instructions.md](../instructions/tests.instructions.md)
   Unit test, component test, and bUnit patterns — applied automatically to `*Should.cs` files

4. **Debugger Agent Spec** — [debugging.instructions.md](../instructions/debugging.instructions.md)
   Log paths, Redux DevTools, common failure patterns and how to trace them

---

## Invokable Agents

Use these AI workers in Copilot Chat to act in a specific role. Each agent reads its full spec before starting:

| Agent | File | Use for |
|-------|------|---------|
| 📋 Orchestrator | [orchestrator.agent.md](../agents/orchestrator.agent.md) | Multi-step tasks: feature + tests, bug + fix + test, refactors |
| 🛠 Developer | [developer.agent.md](../agents/developer.agent.md) | Features, CQRS, Fluxor, components |
| 🧪 Tester | [tester.agent.md](../agents/tester.agent.md) | Unit tests, component tests |
| 🔍 Debugger | [debugger.agent.md](../agents/debugger.agent.md) | Runtime errors, state issues, log analysis |

**When in doubt, use the orchestrator** — it plans first, then invokes the right specialists in sequence.
