// The assistant's tools, as plain C# with no AI packages, so they can be tested without a model.
// The model can SEARCH and READ, and SUGGEST a move. Only the person at the keyboard can approve a move.

namespace SlothAssistant;

public sealed class Downloads
{
    public static readonly string[] Folders =
        ["Bills & Receipts", "Banking & Taxes", "Work", "School", "Travel", "Medical", "Home & Car", "IDs & Personal", "Photos", "Other"];

    public string Root { get; }
    public HashSet<string> Read { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(string File, string Folder, string Reason)> Suggestions { get; } = new();

    public Downloads(string root) { Root = root; }

    private string? Safe(string name) =>
        name.Length > 0 && name == Path.GetFileName(name) && File.Exists(Path.Combine(Root, name)) ? Path.Combine(Root, name) : null;

    /// <summary>Search INSIDE every file (and its name) with plain code, so the model doesn't have to read 50 files.</summary>
    public string Search(string query)
    {
        var words = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                         .Where(w => w.Length > 2).Distinct().ToList();
        if (words.Count == 0) return "Give at least one word to search for.";
        var hits = new List<(string Name, int Score, string Line)>();
        foreach (var path in Directory.GetFiles(Root))
        {
            var name = Path.GetFileName(path);
            var text = File.ReadAllText(path);
            var hay = (name + "\n" + text).ToLowerInvariant();
            int score = words.Count(w => hay.Contains(w));
            if (score == 0) continue;
            // the first line that matches, so the model sees WHY it matched
            var line = text.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => words.Any(w => l.ToLowerInvariant().Contains(w))) ?? "";
            hits.Add((name, score, line.Length > 90 ? line[..90] + "…" : line));
        }
        if (hits.Count == 0) return $"No files mention: {string.Join(", ", words)}. Try other words (a company name, 'statement', 'due').";
        return $"{hits.Count} file(s) match (best first, up to 8). Read a file before deciding about it.\n" +
               string.Join("\n", hits.OrderByDescending(h => h.Score).ThenBy(h => h.Name).Take(8)
                                     .Select(h => $"- {h.Name}  (matches {h.Score}/{words.Count} words): \"{h.Line}\""));
    }

    public string ReadFile(string name)
    {
        if (Safe(name) is not { } path) return $"Error: no file named \"{name}\". Use the exact name from search results.";
        Read.Add(name);
        var text = File.ReadAllText(path);
        return text.Length > 1500 ? text[..1500] + "…" : text;
    }

    /// <summary>Tool-level guardrail: the model may only suggest moving a file it has actually read.</summary>
    public string Suggest(string name, string folder, string reason)
    {
        if (Safe(name) == null) return $"Error: no file named \"{name}\".";
        if (!Read.Contains(name)) return $"Not allowed yet: read {name} first, then suggest.";
        var f = Folders.FirstOrDefault(x => x.Equals(folder, StringComparison.OrdinalIgnoreCase));
        if (f == null) return $"Error: folder must be one of: {string.Join(", ", Folders)}.";
        Suggestions.RemoveAll(s => s.File.Equals(name, StringComparison.OrdinalIgnoreCase));
        Suggestions.Add((name, f, reason));
        return $"Suggestion recorded: {name} → {f}. Nothing has moved; the user will approve or reject it.";
    }

    /// <summary>Runs only after the person says yes. Never overwrites: adds " (2)" if the name is taken.</summary>
    public List<string> Apply()
    {
        var done = new List<string>();
        foreach (var (file, folder, _) in Suggestions)
        {
            var src = Path.Combine(Root, file);
            if (!File.Exists(src)) continue;
            var dir = Path.Combine(Root, "Sorted", folder);
            Directory.CreateDirectory(dir);
            var target = Path.Combine(dir, file);
            for (int i = 2; File.Exists(target); i++) target = Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(file)} ({i}){Path.GetExtension(file)}");
            File.Move(src, target, overwrite: false);
            done.Add($"{file} → Sorted/{folder}/{Path.GetFileName(target)}");
        }
        Suggestions.Clear();
        return done;
    }

    /// <summary>A messy, fictional Downloads folder: two phone bills a month apart, a September power bill, and lots of noise.</summary>
    public static void MakePractice(string root)
    {
        Directory.CreateDirectory(root);
        void W(string n, string t) => File.WriteAllText(Path.Combine(root, n), "SAMPLE - FICTIONAL\n" + t);
        W("document(3).txt", "Chinook Mobile\nYour bill\nAccount 5550-1234\nStatement date: Sep 14, 2026\nWireless service, 20 GB plan\nAmount due: $82.40\nDue by: Oct 5, 2026");
        W("document(7).txt", "Chinook Mobile\nYour bill\nAccount 5550-1234\nStatement date: Oct 14, 2026\nWireless service, 20 GB plan\nAmount due: $85.12\nDue by: Nov 4, 2026");
        W("enmax_statement.txt", "Prairie Power Co.\nYour bill\nStatement date: Sep 20, 2026\nElectricity and natural gas\nAmount due: $112.33");
        W("statement_0923.txt", "Prairie Credit Union\nChequing account statement\nSep 1 - Sep 30, 2026\nClosing balance: $2,140.55");
        W("notes.txt", "COMP 2510 - Assignment 3 due Nov 2. Remember to email the TA.");
        W("recipe.txt", "Pan de muerto: flour, butter, eggs, orange zest, anise.");
        W("Untitled.txt", "Residential tenancy agreement\nStart date: Nov 1, 2026\nMonthly rent: $1,450");
        W("receipt_0917.txt", "Chinook Coffee House\nSep 17, 2026\n2 lattes, 1 muffin\nTotal: $16.96");
        W("booking.txt", "Aurora Air\nBooking confirmation\nCalgary to Vancouver, Nov 20, 2026");
        W("resume_FINAL.txt", "Alex Example\nSoftware developer\nC#, .NET, SQL");
        for (int i = 1; i <= 30; i++)
            W($"download ({i}).txt", $"Saved web page {i}: {(i % 3 == 0 ? "10 tips for fall gardening in Calgary" : i % 3 == 1 ? "How to winterize your bike" : "Best hikes near Canmore this September")}.");
    }

    /// <summary>Checks for the parts that must never go wrong (run: dotnet run -- selftest). No model needed.</summary>
    public static int SelfTest()
    {
        int fail = 0;
        void Check(string what, bool ok) { Console.WriteLine($"{(ok ? "  ✓" : "  ✗")} {what}"); if (!ok) fail++; }
        var root = Path.Combine(Path.GetTempPath(), "sloth-assistant-test-" + Guid.NewGuid().ToString("N")[..6]);
        try
        {
            MakePractice(root);
            var d = new Downloads(root);
            var s = d.Search("phone bill");
            Check("search finds both Chinook bills by content", s.Contains("document(3).txt") && s.Contains("document(7).txt"));
            Check("search shows why a file matched", s.Contains("Your bill"));
            Check("search caps results at 8", s.Split('\n').Count(l => l.StartsWith("- ")) <= 8);
            Check("search with no hits gives a hint", d.Search("zebra").StartsWith("No files mention"));
            Check("read refuses paths", d.ReadFile("../secret.txt").StartsWith("Error"));
            Check("suggest refuses a file not yet read", d.Suggest("document(3).txt", "Bills & Receipts", "x").StartsWith("Not allowed"));
            d.ReadFile("document(3).txt");
            Check("suggest refuses an unknown folder", d.Suggest("document(3).txt", "Phone Stuff", "x").StartsWith("Error: folder"));
            Check("suggest works after reading", d.Suggest("document(3).txt", "bills & receipts", "Sept phone bill").StartsWith("Suggestion recorded"));
            Check("nothing moved before approval", File.Exists(Path.Combine(root, "document(3).txt")));
            Directory.CreateDirectory(Path.Combine(root, "Sorted", "Bills & Receipts"));
            File.WriteAllText(Path.Combine(root, "Sorted", "Bills & Receipts", "document(3).txt"), "older file");
            var moved = d.Apply();
            Check("apply never overwrites (adds (2))", moved.Single().EndsWith("document(3) (2).txt")
                && File.ReadAllText(Path.Combine(root, "Sorted", "Bills & Receipts", "document(3).txt")) == "older file");
        }
        finally { try { Directory.Delete(root, true); } catch { } }
        Console.WriteLine(fail == 0 ? "All checks passed." : $"{fail} check(s) failed.");
        return fail == 0 ? 0 : 1;
    }
}
