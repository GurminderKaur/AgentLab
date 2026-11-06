---
name: add-selftest-check
description: How to add a self-test check to a step in this repo (the `selftest` command that runs without a model). Use whenever you change or add behaviour in a tool, a guardrail, or file handling.
---

# Add a self-test check

Every step that has tools has a `SelfTest()` method that runs with `dotnet run -- selftest`, **without Ollama**. It checks the parts that must never go wrong. When you change behaviour, add a check for it in the same style.

## Steps

1. **Find the self-test.** Search the step folder for `SelfTest(`. In `03-sloth-assistant` it is in `Tools.cs`, at the bottom of the `Downloads` class.
2. **Write one check per behaviour**, using the existing helper:
   ```csharp
   Check("plain words saying what must be true", condition);
   ```
   - The text describes the **behaviour**, not the code: `"search: 'phone' finds 'Mobile'"`, not `"test Aliases dictionary"`.
   - The condition is one boolean expression. If you need setup, do it in a line before.
3. **Use the practice data, never real files.** Work inside the temporary folder the self-test already creates (`root`), made by `MakePractice(root)`. If you need a new sample file, add it to `MakePractice` and start its text with `SAMPLE - FICTIONAL`.
4. **Test the safety rules too**, not just the happy path: a refused path (`../x`), a refused folder name, a file that must not move before approval.
5. **Run it:** `dotnet run -- selftest` inside the step folder. Every line must show `✓`, ending with `All checks passed.`
6. **Report** the new check names and the final line of the output in your summary.

## Example

Adding "search ignores case" to `03-sloth-assistant`:

```csharp
Check("search ignores upper/lower case", d.Search("CHINOOK").Contains("document(7).txt"));
```

## Don't
- Don't delete or weaken an existing check to make a change pass. If a check fails, the change is wrong, or the check needs updating **with a reason in the commit message**.
- Don't add a test framework or NuGet package: the self-test is plain C# on purpose.
