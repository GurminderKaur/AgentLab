# Step 2: Skills (reusable instructions for a coding agent)

A **skill** is a folder with a `SKILL.md`: a name, a one-line description, and instructions. A coding agent sees only the name and description, and loads the full instructions when a task matches. OpenCode reads project skills from `.agents/skills/<name>/SKILL.md` (also `.opencode/skills/` and `.claude/skills/`).

My skill: [`.agents/skills/local-first-dotnet-ai/SKILL.md`](../.agents/skills/local-first-dotnet-ai/SKILL.md). Every rule in it comes from something that actually went wrong in October or in Step 1:

| Rule in the skill | Where I learned it |
|---|---|
| Long prose as plain text, not JSON | Marigold: forced JSON made Gemma 3 4B run past its length limit, twice |
| Timeouts of at least 10 minutes | Step 1.3: OllamaSharp's 100 s default timed out while qwen3 was thinking |
| "Read before you answer" guardrails | Step 1.1: two models answered about files they never opened |
| Tools do the heavy work and say why something matched | Step 1.3: a noisy search made the model give up |
| The model only suggests; a person approves; never overwrite | Sloth Sort, Step 1.3 |
| Fictional sample data only | All projects |

## The experiment (Part 2.3)

Same task, same local model, in OpenCode: **without** the skill, then **with** it.

> Task: *"In 03-sloth-assistant, add a `file_info` tool that returns a file's size and last-modified date."*

What the skill should change: logic goes in `Tools.cs` (testable without a model), the tool refuses paths, a self-test check is added, no new packages, and the agent runs the self-test before saying it's done.

Results: *to be filled in from my laptop runs.*
