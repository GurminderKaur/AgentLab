# Step 2b: skills inside my own agent

The [Sloth Sort assistant](../../03-sloth-assistant) from Step 1.3, plus **skills**: short recipes I write in plain Markdown (`skills/<name>/SKILL.md`), loaded **only when needed**.

## How it works

1. At start-up, the agent reads every `SKILL.md` and puts **only the name and one-line description** of each into the model's instructions:
   ```
   - filing-bills: How I file bills and statements (phone, power, gas, …). Use whenever filing a bill, invoice or statement.
   - filing-travel: How I file trips: flight bookings, boarding passes, … Use when filing anything travel-related.
   ```
2. The model gets one extra tool, `load_skill(name)`. When the task matches a skill, it calls the tool and **only then** sees the full recipe.
3. The output shows `📚 load_skill("…")` when that happens.

This is the same idea coding agents use (OpenCode, Claude Code): a short index always visible, the details loaded on demand, so a small model's limited context stays free.

## The experiment

The `filing-bills` skill contains a **personal rule the model can't guess**: phone and credit-card bills go to *Bills & Receipts*, but **power, gas, water and rent go to *Home & Car***.

Task: **"File my September power bill."** (the answer is `enmax_statement.txt`, Prairie Power Co., Sep 20). Same model, same fresh practice folder, twice:

```powershell
cd 04-skills/SkillAgent
dotnet run -- selftest
dotnet run -- qwen3:4b "File my September power bill." --no-skills
dotnet run -- qwen3:4b "File my September power bill."
```

## First real runs (my laptop, `qwen3:4b`, CPU only)

| | Without skills | With skills |
|---|---|---|
| Loaded the skill | (not available) | ✅ `filing-bills`, **before** searching |
| Right file (`enmax_statement.txt`) | ✅ | ✅ |
| Folder it chose (in its answer) | **Bills & Receipts**: the obvious guess | **Home & Car** ✅ my rule, and it quoted it ("utility bills go into Home & Car") |
| Time | 174 s | 260 s (loading the recipe adds a turn) |
| Recorded the suggestion with `suggest_move` | ❌ | ❌ |

**The skill changed the decision exactly as intended**: same model, same files, and only the run with the recipe followed my personal rule. The cost was about 90 extra seconds.

**But neither run actually called `suggest_move`**, so the report said "No moves suggested":

- Without skills, the model wrote the call **as text** in its answer, under the name `_Main_g_suggest_move_3`. That was my bug: I hadn't given the tools explicit names, so the framework used C#'s compiled names for local functions. Odd names make small models fumble the call format.
- With skills, it **described** its choice ("I suggest moving…") without calling the tool.

Fixes: every tool now has a plain name (`AIFunctionFactory.Create(suggest_move, "suggest_move")`, also fixed in Step 1.3), and a new guardrail pushes back once if the model read files but recorded no suggestion.

## After the fixes

Results: *not run yet.*
