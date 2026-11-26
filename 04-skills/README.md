# Step 2: skills: reusable instructions for a coding agent

## Two kinds of instructions

| | [`AGENTS.md`](../AGENTS.md) | A skill: [`.agents/skills/add-selftest-check/SKILL.md`](../.agents/skills/add-selftest-check/SKILL.md) |
|---|---|---|
| When it's read | **Every time** the agent works in this repo | **Only when the task matches**: the agent sees just the name and one-line description, and loads the rest on demand |
| What goes in it | House rules: .NET 10, stay local, fictional data, never overwrite, run the self-test before finishing | A step-by-step procedure for one kind of job: adding a self-test check in my style |
| Why it matters for a small model | Short, always-on rules | Keeps the model's limited memory free until the procedure is actually needed |

OpenCode reads skills from `.opencode/skills/`, `.claude/skills/` or `.agents/skills/` ([docs](https://opencode.ai/docs/skills.md)); I use `.agents/skills/` because other coding agents read that location too.

## The experiment (Part 2.3)

The same real bug, fixed twice by OpenCode with a local model (`qwen3:4b`, 32k context) on my laptop:

> In 03-sloth-assistant, search_files("resume") finds nothing, although resume_FINAL.txt is in the practice folder: the underscore stops the whole-word match. Fix it so words in file names separated by _ or - are found, add a self-test check for it, and make sure the self-test passes.

(Confirmed before the experiment: `Search("resume")` returns "No files mention: resume", and all 12 existing checks pass.)

1. **Without** instructions: `AGENTS.md` and `.agents/` temporarily renamed.
2. **With** them.

| What I compare | Without | With |
|---|---|---|
| Fixed the bug (`resume` finds `resume_FINAL.txt`) | | |
| Added a self-test check in the house style (`Check("plain words…", …)`) | | |
| Ran the self-test before finishing | | |
| All checks pass afterwards | | |
| Stayed inside `03-sloth-assistant`, no new NuGet package | | |
| Didn't weaken or delete existing checks | | |
| Time taken | | |

## Run 1 result: without instructions (my laptop, 16 GB RAM, CPU only)

**It didn't produce any code.**

- `ollama launch opencode` loaded `qwen3:4b` with a **64k context** (OpenCode's own instructions need that much), using **12 GB of RAM** (`ollama ps`). My 32k app setting was overridden.
- It "thought" for **24 min 28 s**, then announced `Edit search_files.js`, a **JavaScript** file in a **C#** project. It had guessed from the tool's name instead of looking at the code (the search lives in `Tools.cs`).
- It thought for another **8 min**, then searched for that `.js` file (which doesn't exist).
- The laptop went to sleep; after waking it was still thinking. I stopped it.
- `git status` afterwards: **no files changed** except my renaming of the instruction files.

**What this shows:**
1. A full coding agent is much heavier than my own agents: OpenCode needs ~64k context (about 12 GB with this model), while my Sloth Sort assistant ran in about 4 GB.
2. **Without house rules, a small model guesses the project's basics wrong**, even the language. "C# on .NET 10" is the first line of `AGENTS.md` for exactly this reason.

Run 2 (with instructions) on this laptop would face the same memory and speed limits, so I'm finishing Step 2 differently (below).
