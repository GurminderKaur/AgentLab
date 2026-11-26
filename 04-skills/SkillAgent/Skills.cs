// Skills, the way coding agents do it: the model always sees only each skill's NAME and one-line DESCRIPTION.
// It calls load_skill(name) to read the full recipe only when the task needs it, so the small context stays free.

namespace SkillAgent;

public sealed record Skill(string Name, string Description, string Body);

public static class Skills
{
    /// <summary>Reads every skills/&lt;name&gt;/SKILL.md (front matter: name + description, then the recipe).</summary>
    public static List<Skill> LoadAll(string folder)
    {
        var list = new List<Skill>();
        if (!Directory.Exists(folder)) return list;
        foreach (var dir in Directory.GetDirectories(folder).OrderBy(d => d))
        {
            var file = Path.Combine(dir, "SKILL.md");
            if (!File.Exists(file)) continue;
            var lines = File.ReadAllLines(file);
            if (lines.Length < 3 || lines[0].Trim() != "---") continue;
            int end = Array.FindIndex(lines, 1, l => l.Trim() == "---");
            if (end < 0) continue;
            string Field(string key) => lines[1..end].Select(l => l.Trim())
                .Where(l => l.StartsWith(key + ":")).Select(l => l[(key.Length + 1)..].Trim()).FirstOrDefault() ?? "";
            var name = Field("name");
            // Same rule as OpenCode: the name must match its folder, so the index can't lie about what loads.
            if (name.Length == 0 || name != Path.GetFileName(dir)) continue;
            list.Add(new Skill(name, Field("description"), string.Join("\n", lines[(end + 1)..]).Trim()));
        }
        return list;
    }

    /// <summary>The short list that goes into the system prompt: names and descriptions only.</summary>
    public static string Index(List<Skill> skills) =>
        string.Join("\n", skills.Select(s => $"- {s.Name}: {s.Description}"));

    public static string Load(List<Skill> skills, string name) =>
        skills.FirstOrDefault(s => s.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)) is { } s
            ? s.Body
            : $"Error: no skill named \"{name}\". Available: {string.Join(", ", skills.Select(x => x.Name))}.";

    public static int SelfTest(string folder)
    {
        int fail = 0;
        void Check(string what, bool ok) { Console.WriteLine($"{(ok ? "  ✓" : "  ✗")} {what}"); if (!ok) fail++; }
        var all = LoadAll(folder);
        Check("skills: both skills found", all.Select(s => s.Name).SequenceEqual(new[] { "filing-bills", "filing-travel" }));
        Check("skills: index has names and descriptions only (no recipe text)",
            Index(all).Contains("filing-bills:") && !Index(all).Contains("Home & Car"));
        Check("skills: loading returns the full recipe", Load(all, "filing-bills").Contains("Home & Car"));
        Check("skills: unknown skill gives a helpful error", Load(all, "taxes").StartsWith("Error") && Load(all, "taxes").Contains("filing-bills"));
        var tmp = Path.Combine(Path.GetTempPath(), "skill-test-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(Path.Combine(tmp, "wrong-folder"));
        File.WriteAllText(Path.Combine(tmp, "wrong-folder", "SKILL.md"), "---\nname: other-name\ndescription: x\n---\nbody");
        Check("skills: a skill whose name doesn't match its folder is ignored", LoadAll(tmp).Count == 0);
        Directory.Delete(tmp, true);
        int tools = SlothAssistant.Downloads.SelfTest();
        Console.WriteLine(fail == 0 && tools == 0 ? "All skill checks passed." : "Some checks failed.");
        return fail == 0 && tools == 0 ? 0 : 1;
    }
}
