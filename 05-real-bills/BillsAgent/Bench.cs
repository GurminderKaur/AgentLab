// Step 3: choosing a model. Fixed tasks with known right answers, the same practice folder for every model,
// and a plain scoring rule, so models can be compared fairly. No AI packages here, so it can be tested without a model.
using System.Text;

namespace BillsAgent;

public sealed record BenchTask(string Id, string Request, string File, string Folder, string Skill);

public sealed class RunResult
{
    public string Model = "", TaskId = "";
    public List<(string File, string Folder, string Reason)> Suggestions = new();
    public List<string> Loaded = new();
    public double Seconds;
    public int Pushbacks, ToolCalls;
    public string? Error;
}

public static class Bench
{
    public static readonly BenchTask[] Tasks =
    [
        new("phone-bill", "File my September phone bill.", "document(3).pdf", "Bills & Receipts", "filing-bills"),
        new("power-bill", "File my September power bill.", "enmax_statement.pdf", "Home & Car", "filing-bills"),
        new("coffee-photo", "File my coffee receipt from October.", "IMG_2207.jpg", "Bills & Receipts", "filing-receipts"),
    ];

    /// <summary>Right = exactly the expected file in the expected folder, and nothing else suggested.</summary>
    public static (int Points, string Label) Score(BenchTask t, RunResult r)
    {
        if (r.Error != null) return (0, "❌ error");
        if (r.Suggestions.Count == 0) return (0, "❌ no suggestion");
        bool fileOk = r.Suggestions.Any(s => s.File.Equals(t.File, StringComparison.OrdinalIgnoreCase));
        bool exact = r.Suggestions.Count == 1 && fileOk && r.Suggestions[0].Folder == t.Folder;
        if (exact) return (2, "✅ right");
        if (fileOk && r.Suggestions.Any(s => s.File.Equals(t.File, StringComparison.OrdinalIgnoreCase) && s.Folder == t.Folder))
            return (1, "◐ right, plus extra suggestions");
        if (fileOk) return (1, $"◐ right file, wrong folder ({r.Suggestions.First(s => s.File.Equals(t.File, StringComparison.OrdinalIgnoreCase)).Folder})");
        return (0, $"❌ wrong file ({r.Suggestions[0].File})");
    }

    /// <summary>The report: one summary row per model, then every run in detail.</summary>
    public static string Report(List<RunResult> runs, Dictionary<string, (double RamGb, string Licence)> info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Model | Right (of 3) | Score | Avg time | RAM in Ollama | Push-backs | Skill loaded when needed | Licence |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var g in runs.GroupBy(r => r.Model))
        {
            var scored = g.Select(r => (r, s: Score(Tasks.First(t => t.Id == r.TaskId), r))).ToList();
            int right = scored.Count(x => x.s.Points == 2), pts = scored.Sum(x => x.s.Points);
            int skillOk = scored.Count(x => x.r.Loaded.Contains(Tasks.First(t => t.Id == x.r.TaskId).Skill));
            var ok = g.Where(r => r.Error == null).ToList();
            var (ram, lic) = info.GetValueOrDefault(g.Key, (0, "?"));
            sb.AppendLine($"| `{g.Key}` | {right}/{g.Count()} | {pts}/{2 * g.Count()} | {(ok.Count == 0 ? "–" : $"{ok.Average(r => r.Seconds):F0} s")} | " +
                          $"{(ram > 0 ? $"{ram:F1} GB" : "?")} | {g.Sum(r => r.Pushbacks)} | {skillOk}/{g.Count()} | {lic} |");
        }
        sb.AppendLine();
        sb.AppendLine("| Model | Task | Result | Suggested | Skills loaded | Tool calls | Time |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var r in runs)
        {
            var t = Tasks.First(x => x.Id == r.TaskId);
            var sug = r.Error ?? (r.Suggestions.Count == 0 ? "–" : string.Join("; ", r.Suggestions.Select(s => $"{s.File} → {s.Folder}")));
            sb.AppendLine($"| `{r.Model}` | {t.Id} | {Score(t, r).Label} | {sug.Replace("|", "/")} | {(r.Loaded.Count == 0 ? "none" : string.Join(", ", r.Loaded))} | {r.ToolCalls} | {r.Seconds:F0} s |");
        }
        return sb.ToString();
    }

    /// <summary>First meaningful line of a licence text, e.g. "Apache License Version 2.0".</summary>
    public static string LicenceName(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "not stated";
        var line = text.Split('\n').Select(l => l.Trim().Trim('#', '*', ' ')).FirstOrDefault(l => l.Length > 3) ?? "not stated";
        return line.Length > 60 ? line[..60] + "…" : line;
    }

    public static int SelfTest()
    {
        int fail = 0;
        void Check(string what, bool ok) { Console.WriteLine($"{(ok ? "  ✓" : "  ✗")} {what}"); if (!ok) fail++; }
        var power = Tasks[1];
        RunResult R(params (string, string)[] s) => new() { Model = "m", TaskId = power.Id, Suggestions = s.Select(x => (x.Item1, x.Item2, "")).ToList() };
        Check("bench: right file + right folder = right", Score(power, R(("enmax_statement.pdf", "Home & Car"))).Points == 2);
        Check("bench: right file, wrong folder = half", Score(power, R(("enmax_statement.pdf", "Bills & Receipts"))).Label.StartsWith("◐ right file, wrong folder"));
        Check("bench: extra suggestions aren't full marks", Score(power, R(("enmax_statement.pdf", "Home & Car"), ("document(3).pdf", "Bills & Receipts"))).Points == 1);
        Check("bench: wrong file = zero", Score(power, R(("document(3).pdf", "Home & Car"))).Points == 0);
        Check("bench: no suggestion = zero", Score(power, R()).Label == "❌ no suggestion");
        Check("bench: errors are reported, not hidden", Score(power, new RunResult { TaskId = power.Id, Error = "timeout" }).Label == "❌ error");
        Check("bench: licence name from licence text", LicenceName("\n  Apache License\n Version 2.0, January 2004") == "Apache License");
        var report = Report([R(("enmax_statement.pdf", "Home & Car"))], new() { ["m"] = (3.2, "Apache License") });
        Check("bench: report has a summary row per model", report.Contains("| `m` | 1/1 | 2/2 |") && report.Contains("3.2 GB") && report.Contains("Apache License"));
        Console.WriteLine(fail == 0 ? "All bench checks passed." : $"{fail} bench check(s) failed.");
        return fail;
    }
}
