// Scores a model's receipt reading against the known answers, field by field. No AI packages, so it's testable without a model.
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReceiptBench;

public sealed record Expected(string File, string Vendor, string Date, double Subtotal, double Gst, double Other, double Tip, double Total);
public sealed record Reading(string Vendor, string Date, double Subtotal, double Gst, double OtherTax, double Tip, double Total, bool Parsed);

public static class Score
{
    public static readonly string[] Fields = ["vendor", "date", "subtotal", "gst", "other_tax", "tip", "total"];

    public static List<Expected> LoadExpected(string path) =>
        JsonDocument.Parse(File.ReadAllText(path)).RootElement.EnumerateArray().Select(e => new Expected(
            e.GetProperty("file").GetString()!, e.GetProperty("vendor").GetString()!, e.GetProperty("date").GetString()!,
            e.GetProperty("subtotal").GetDouble(), e.GetProperty("gst").GetDouble(), e.GetProperty("other").GetDouble(),
            e.GetProperty("tip").GetDouble(), e.GetProperty("total").GetDouble())).ToList();

    /// <summary>Lenient: finds the JSON object even inside chatter or ```json fences; numbers may be strings like "$1.25".</summary>
    public static Reading Parse(string reply)
    {
        int a = reply.IndexOf('{'), b = reply.LastIndexOf('}');
        if (a < 0 || b <= a) return new("", "", 0, 0, 0, 0, 0, false);
        try
        {
            var r = JsonDocument.Parse(reply[a..(b + 1)]).RootElement;
            string S(string k) => r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : "";
            double N(string k)
            {
                if (!r.TryGetProperty(k, out var v)) return 0;
                if (v.ValueKind == JsonValueKind.Number) return v.GetDouble();
                var s = Regex.Replace(v.ToString(), @"[^0-9.\-]", "");
                return double.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;
            }
            return new(S("vendor"), S("date"), N("subtotal"), N("gst"), N("other_tax"), N("tip"), N("total"), true);
        }
        catch (JsonException) { return new("", "", 0, 0, 0, 0, 0, false); }
    }

    private static string Norm(string s) => Regex.Replace(s.ToLowerInvariant(), "[^a-z0-9]", "");

    /// <summary>Same rules as Shoebox's score.py: vendor by name match, date exact, amounts to the cent.</summary>
    public static Dictionary<string, bool> Check(Expected e, Reading r) => new()
    {
        ["vendor"] = Norm(r.Vendor).Length > 4 && (Norm(e.Vendor).Contains(Norm(r.Vendor)) || Norm(r.Vendor).Contains(Norm(e.Vendor))),
        ["date"] = r.Date == e.Date,
        ["subtotal"] = Math.Abs(r.Subtotal - e.Subtotal) < 0.005,
        ["gst"] = Math.Abs(r.Gst - e.Gst) < 0.005,
        ["other_tax"] = Math.Abs(r.OtherTax - e.Other) < 0.005,
        ["tip"] = Math.Abs(r.Tip - e.Tip) < 0.005,
        ["total"] = Math.Abs(r.Total - e.Total) < 0.005,
    };

    public static int SelfTest()
    {
        int fail = 0;
        void Ck(string what, bool ok) { Console.WriteLine($"{(ok ? "  ✓" : "  ✗")} {what}"); if (!ok) fail++; }
        var e = new Expected("04_courier.jpg", "Foothills Courier Ltd.", "2026-10-09", 25.00, 1.25, 0, 0, 26.25);
        var good = Parse("Sure:\n```json\n{\"vendor\":\"FOOTHILLS COURIER LTD\",\"date\":\"2026-10-09\",\"subtotal\":25,\"gst\":\"$1.25\",\"other_tax\":0,\"tip\":0,\"total\":26.25}\n```");
        Ck("parse: JSON found inside chatter and code fences; \"$1.25\" read as 1.25", good.Parsed && good.Gst == 1.25);
        Ck("score: every field right", Check(e, good).Values.All(x => x));
        var copied = good with { Gst = 25.00 };
        Ck("score: GST copied from the subtotal is wrong (Shoebox's real mistake)", !Check(e, copied)["gst"] && Check(e, copied)["subtotal"]);
        Ck("parse: no JSON at all is reported, not crashed", !Parse("I can't read this").Parsed);
        Console.WriteLine(fail == 0 ? "All scoring checks passed." : $"{fail} scoring check(s) failed.");
        return fail;
    }
}
