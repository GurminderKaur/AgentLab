// Turns any file in the Downloads folder into text the agent's tools can search and read:
//   PDF / Word / Excel / PowerPoint → their real text (TextExtract, no packages)
//   photos (JPG/PNG)                → a short description written by a vision model (Gemma 3), read ONCE and remembered
//   scanned PDFs (no text inside)   → an honest note saying so, instead of nothing
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BillsAgent;

public sealed class Library
{
    public static readonly HashSet<string> PhotoExt = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };
    private readonly string _notesFile;
    private readonly Dictionary<string, string> _notes;   // SHA-256 of the photo → what the vision model saw

    public Library(string notesFile)
    {
        _notesFile = notesFile;
        try { _notes = File.Exists(notesFile) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(notesFile)) ?? new() : new(); }
        catch { _notes = new(); }
    }

    public static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))[..16];

    /// <summary>The text the agent sees for a file. Plugged into the shared tools via Downloads.ReadText.</summary>
    public string Read(string path)
    {
        var ext = Path.GetExtension(path);
        if (PhotoExt.Contains(ext))
            return _notes.TryGetValue(Hash(path), out var note)
                ? "[Photo. Description written by a vision model; it may contain mistakes.]\n" + note
                : "[Photo that hasn't been looked at yet. Nothing is known about it.]";
        var text = TextExtract.Excerpt(path);
        if (text.Length == 0 && ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return "[This PDF has no readable text. It's probably a scanned image, which this assistant can't read yet.]";
        return text;
    }

    public bool NeedsLook(string path) => PhotoExt.Contains(Path.GetExtension(path)) && !_notes.ContainsKey(Hash(path));

    /// <summary>Ask the vision model about each photo it hasn't seen before, and remember the answer.</summary>
    public async Task LookAtPhotosAsync(string folder, Func<byte[], string, Task<string>> describe)
    {
        foreach (var path in Directory.GetFiles(folder).Where(NeedsLook).Order())
        {
            Console.Write($"  🖼  looking at {Path.GetFileName(path)}… ");
            var started = DateTime.Now;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var note = (await describe(File.ReadAllBytes(path), ext == ".png" ? "image/png" : "image/jpeg")).Trim();
            Remember(path, note);
            Console.WriteLine($"{(DateTime.Now - started).TotalSeconds:F0} s");
            Console.WriteLine("     " + note.Replace("\n", "\n     "));
        }
    }

    public void Remember(string path, string note)
    {
        _notes[Hash(path)] = note;
        File.WriteAllText(_notesFile, JsonSerializer.Serialize(_notes, new JsonSerializerOptions { WriteIndented = true }));
    }

    public const string PhotoPrompt = """
        This is a photo of a document from someone's Downloads folder.
        Write 3 to 6 short lines with only what you can read in the photo:
        - the company or store name
        - what kind of document it is (receipt, bill, statement, ticket, letter…)
        - every date you see, written like "Oct 6, 2026"
        - the total or amount due
        No guesses, no commentary.
        """;

    // ── A practice Downloads folder in real formats (all fictional) ──────────

    public static void MakePractice(string root, byte[] receiptPhoto)
    {
        Directory.CreateDirectory(root);
        void Pdf(string n, params string[] lines) => File.WriteAllBytes(Path.Combine(root, n), MakePdf(lines));
        void Txt(string n, string t) => File.WriteAllText(Path.Combine(root, n), "SAMPLE - FICTIONAL\n" + t);
        Pdf("document(3).pdf", "SAMPLE - FICTIONAL", "Chinook Mobile", "Your bill", "Account 5550-1234", "Statement date: Sep 14, 2026", "Wireless service, 20 GB plan", "Amount due: $82.40", "Due by: Oct 5, 2026");
        Pdf("document(7).pdf", "SAMPLE - FICTIONAL", "Chinook Mobile", "Your bill", "Account 5550-1234", "Statement date: Oct 14, 2026", "Wireless service, 20 GB plan", "Amount due: $85.12", "Due by: Nov 4, 2026");
        Pdf("enmax_statement.pdf", "SAMPLE - FICTIONAL", "Prairie Power Co.", "Your bill", "Statement date: Sep 20, 2026", "Electricity and natural gas", "Amount due: $112.33");
        Pdf("statement_0923.pdf", "SAMPLE - FICTIONAL", "Prairie Credit Union", "Chequing account statement", "Sep 1 - Sep 30, 2026", "Closing balance: $2,140.55");
        Pdf("scan0001.pdf");   // a "scanned" PDF: no text inside, like a photo saved as PDF
        MakeDocx(Path.Combine(root, "Untitled document.docx"), "SAMPLE - FICTIONAL", "Residential tenancy agreement", "Start date: Nov 1, 2026", "Monthly rent: $1,450");
        File.WriteAllBytes(Path.Combine(root, "IMG_2207.jpg"), receiptPhoto);   // a photo of a coffee receipt
        Txt("booking.txt", "Aurora Air\nBooking confirmation\nCalgary to Vancouver, Nov 20, 2026");
        Txt("notes.txt", "COMP 2510 - Assignment 3 due Nov 2. Remember to email the TA.");
        for (int i = 1; i <= 30; i++)
            Txt($"download ({i}).txt", $"Saved web page {i}: {(i % 3 == 0 ? "10 tips for fall gardening in Calgary" : i % 3 == 1 ? "How to winterize your bike" : "Best hikes near Canmore this September")}.");
    }

    /// <summary>A small valid PDF with one compressed text stream, one line per entry (like a real e-bill).</summary>
    public static byte[] MakePdf(params string[] lines)
    {
        var content = new StringBuilder("BT /F1 12 Tf 72 720 Td 14 TL\n");
        foreach (var l in lines) content.Append('(').Append(l.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")).Append(") Tj T*\n");
        content.Append("ET");
        using var zms = new MemoryStream();
        using (var z = new ZLibStream(zms, CompressionLevel.Optimal, leaveOpen: true)) z.Write(Encoding.Latin1.GetBytes(content.ToString()));
        var stream = zms.ToArray();
        var ms = new MemoryStream();
        void W(string s) => ms.Write(Encoding.Latin1.GetBytes(s));
        W("%PDF-1.4\n1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj\n");
        W("3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >> endobj\n");
        W($"4 0 obj << /Length {stream.Length} /Filter /FlateDecode >>\nstream\n");
        ms.Write(stream);
        W("\nendstream\nendobj\ntrailer << /Root 1 0 R >>\n%%EOF\n");
        return ms.ToArray();
    }

    public static void MakeDocx(string path, params string[] paragraphs)
    {
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        using var w = new StreamWriter(zip.CreateEntry("word/document.xml").Open());
        w.Write("<?xml version=\"1.0\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>");
        foreach (var p in paragraphs) w.Write($"<w:p><w:r><w:t>{System.Security.SecurityElement.Escape(p)}</w:t></w:r></w:p>");
        w.Write("</w:body></w:document>");
    }

    // ── Checks that need no model (dotnet run -- selftest) ──────────────────

    public static int SelfTest(byte[] photo)
    {
        int fail = 0;
        void Check(string what, bool ok) { Console.WriteLine($"{(ok ? "  ✓" : "  ✗")} {what}"); if (!ok) fail++; }
        var tmp = Path.Combine(Path.GetTempPath(), "bills-test-" + Guid.NewGuid().ToString("N")[..6]);
        try
        {
            var root = Path.Combine(tmp, "downloads");
            MakePractice(root, photo);
            var lib = new Library(Path.Combine(tmp, "photo-notes.json"));
            var d = new SlothAssistant.Downloads(root) { ReadText = lib.Read };

            Check("pdf: text read from a compressed PDF bill, line by line", lib.Read(Path.Combine(root, "document(3).pdf")).Contains("Chinook Mobile\nYour bill"));
            var first = d.Search("phone bill September").Split('\n').First(l => l.StartsWith("- "));
            Check("search: finds the September phone bill INSIDE a PDF, ranked first", first.Contains("document(3).pdf") && first.Contains("3 of 3"));
            Check("word: the lease in a .docx is found by 'rent'", d.Search("rent").Contains("Untitled document.docx"));
            Check("scanned pdf: says honestly that it can't be read", lib.Read(Path.Combine(root, "scan0001.pdf")).Contains("no readable text"));
            Check("photo: before looking, nothing is claimed about it", lib.Read(Path.Combine(root, "IMG_2207.jpg")).Contains("hasn't been looked at"));
            Check("photo: needs a look the first time", lib.NeedsLook(Path.Combine(root, "IMG_2207.jpg")));
            lib.Remember(Path.Combine(root, "IMG_2207.jpg"), "Chinook Coffee House\nReceipt\nOct 6, 2026\nTotal $16.96");
            Check("photo: once described, search finds it by its content", d.Search("coffee receipt October").Contains("IMG_2207.jpg"));
            Check("photo: the description is labelled as written by a model", d.ReadFile("IMG_2207.jpg").StartsWith("[Photo. Description written by a vision model"));
            Check("photo: remembered across runs (no second look)", !new Library(Path.Combine(tmp, "photo-notes.json")).NeedsLook(Path.Combine(root, "IMG_2207.jpg")));
        }
        finally { try { Directory.Delete(tmp, true); } catch { } }
        Console.WriteLine(fail == 0 ? "All real-bill checks passed." : $"{fail} check(s) failed.");
        return fail;
    }
}
