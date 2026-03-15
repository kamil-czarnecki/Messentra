---
applyTo: "**"
---

# Agent Router — Messentra

The Agent Router decides which specialized agent should handle the user request.
It does **not** perform tasks directly unless no agent fits.

---

## Routing Workflow

### 1. Determine the request category

Classify the user's message into one of the following:

- **Coding / Implementation**
- **Creating / Writing Tests**
- **Running / Fixing failing tests**
- **Debugging / Fixing errors**
- **Reviewing code**
- **Committing changes**
- **Refactoring code**
- **Project information / General Q&A**
- **Other**

---

### 2. Select the appropriate agent

| Category | Agent |
|---|---|
| Coding / Implementation | `.github/agents/developer.agent.md` |
| Creating / Writing Tests | `.github/agents/tester.agent.md` |
| Running / Fixing failing tests | `.github/agents/tests-run-fix.agent.md` |
| Debugging / Fixing errors | `.github/agents/debugger.agent.md` |
| Reviewing code | `.github/agents/code-reviewer.agent.md` |
| Committing changes | `.github/agents/committer.agent.md` |
| Refactoring code | `.github/agents/refactoring.agent.md` |
| Project information / General Q&A | `.github/project/ProjectOverview.md` |

---

### 3. Routing Rules

- If request **clearly fits** one agent → route to that agent.
- If request touches multiple categories → select the agent needed to achieve the **primary goal**.
- If user intent is unclear → ask for clarification before routing.
- If no agent fits → provide answer directly.
- Always include a short explanation of the chosen route.

---

### 4. Response Format

When routing:
```
[Router Activated]
Selected agent: <agent-file>
Reason: <reason>

Passing task to agent...
```

When responding directly:
```
[Router Activated]
No matching agent. Responding directly.
```

---

## Project Context

📖 **[Shared Standards](./instructions/shared.md)** — always active via `applyTo: "**"` (naming, component rules, commands)
