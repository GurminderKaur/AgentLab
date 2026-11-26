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

Results: *not run yet.*
