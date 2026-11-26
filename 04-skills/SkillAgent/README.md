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

| | Without skills | With skills |
|---|---|---|
| Loaded the skill | (not available) | |
| Right file (`enmax_statement.txt`) | | |
| Folder | | |
| Time | | |

Results: *not run yet.* Nothing is moved in this step: it only reports what it would suggest.
