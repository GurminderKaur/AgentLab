# Step 2: skills: reusable instructions for a coding agent

## Two kinds of instructions

| | [`AGENTS.md`](../AGENTS.md) | A skill: [`.agents/skills/add-selftest-check/SKILL.md`](../.agents/skills/add-selftest-check/SKILL.md) |
|---|---|---|
| When it's read | **Every time** the agent works in this repo | **Only when the task matches**: the agent sees just the name and one-line description, and loads the rest on demand |
| What goes in it | House rules: .NET 10, stay local, fictional data, never overwrite, run the self-test before finishing | A step-by-step procedure for one kind of job: adding a self-test check in my style |
| Why it matters for a small model | Short, always-on rules | Keeps the model's limited memory free until the procedure is actually needed |

OpenCode reads skills from `.opencode/skills/`, `.claude/skills/` or `.agents/skills/` ([docs](https://opencode.ai/docs/skills.md)); I use `.agents/skills/` because other coding agents read that location too.

## The experiment (Part 2.3)

The same small task, run twice with OpenCode and a local model on my laptop:

> In `03-sloth-assistant`, make `search_files` ignore upper/lower case in file names too, and add a self-test check for it.

1. **Without** the instructions (AGENTS.md and the skill temporarily renamed).
2. **With** them.

What I'll compare: did it build, did it add a check in the right style, did it run the self-test, did it add a NuGet package or touch other folders, and how long it took.

Results: *not run yet.*
