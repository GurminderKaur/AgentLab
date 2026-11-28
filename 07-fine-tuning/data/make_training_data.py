"""Make fictional receipt texts with known answers, for fine-tuning (Step 4.2).

Usage: python make_training_data.py            → writes train.jsonl (2000) and heldout.jsonl (200) next to this script

Each line is one chat example in the format the training notebook expects:
  {"messages": [{"role": "user", "content": <prompt.txt + receipt text>}, {"role": "assistant", "content": <answer JSON>}]}
The user message is built EXACTLY like ReceiptBench builds it at test time (prompt + "\n\nReceipt:\n" + text).

Everything is invented. The 6 test receipts' businesses are never used (no leakage), and every answer is checked:
subtotal + GST/HST + other tax + tip = total, to the cent.
"""
import json, os, random, sys
from datetime import date, timedelta

HERE = os.path.dirname(os.path.abspath(__file__))
PROMPT = open(os.path.join(HERE, "..", "prompt.txt"), encoding="utf-8").read().strip()
rng = random.Random(2026)   # fixed seed: the same data every time

# The test receipts' businesses: never generated.
TEST_VENDORS = {"BOW VALLEY FUEL CO.", "CHINOOK COFFEE HOUSE", "PRAIRIE OFFICE SUPPLY", "FOOTHILLS COURIER LTD.",
                "GRANVILLE PRINT SHOP", "KENSINGTON MARKET GROCERY"}

# Province → (city choices, tax lines). gst = GST or HST amount (as prompt.txt says); other = PST/RST/QST.
PROVINCES = {
    "AB": (["Calgary", "Edmonton", "Red Deer", "Lethbridge", "Canmore", "Airdrie"], [("GST", 0.05, "gst")]),
    "BC": (["Vancouver", "Victoria", "Kelowna", "Surrey"], [("GST", 0.05, "gst"), ("PST", 0.07, "other")]),
    "SK": (["Regina", "Saskatoon"], [("GST", 0.05, "gst"), ("PST", 0.06, "other")]),
    "MB": (["Winnipeg", "Brandon"], [("GST", 0.05, "gst"), ("RST", 0.07, "other")]),
    "ON": (["Toronto", "Ottawa", "Hamilton"], [("HST", 0.13, "gst")]),
    "QC": (["Montreal", "Quebec City"], [("GST", 0.05, "gst"), ("QST", 0.09975, "other")]),
    "NS": (["Halifax"], [("HST", 0.14, "gst")]),
}
WEIGHTS = {"AB": 50, "BC": 12, "SK": 6, "MB": 6, "ON": 14, "QC": 7, "NS": 5}

# Kinds of business: (name parts, items with price ranges, can be zero-rated, takes tips)
KINDS = {
    "fuel": (["Prairie Sky Gas", "Rocky Ridge Fuels", "North Star Petro", "Elbow River Gas Bar", "Summit Fuel Stop"],
             [("REGULAR UNLEADED {l}L @ {p}", None), ("PREMIUM {l}L @ {p}", None), ("CAR WASH DELUXE", (9, 18)), ("WIPER FLUID", (4, 9))], False, False),
    "coffee": (["Maple Bean Cafe", "Aspen Grind Coffee", "Riverside Roasters", "Little Prairie Cafe", "Bluebird Espresso"],
               [("Latte 16oz", (4.5, 6.5)), ("Americano", (3, 4.5)), ("Croissant", (3, 4.75)), ("Banana Bread", (3, 4.5)), ("Chai Latte", (4.5, 6))], False, True),
    "restaurant": (["Golden Wheat Bistro", "Cedar & Stone Grill", "Northern Lights Diner", "Harvest Table"],
                   [("Club Sandwich", (14, 19)), ("Soup of the Day", (6, 9)), ("Caesar Salad", (11, 16)), ("Burger & Fries", (16, 22)), ("Iced Tea", (3, 4.5))], False, True),
    "office": (["Westgate Office Depot", "Paperclip & Co.", "Northline Stationers", "Desk Works Supply"],
               [("PRINTER PAPER 500 SH", (8, 14)), ("TONER CARTRIDGE", (45, 120)), ("STAPLER", (12, 25)), ("NOTEBOOKS 3PK", (7, 13)), ("USB DRIVE 64GB", (12, 22))], False, False),
    "courier": (["Chinook Express Delivery", "Swift Parcel Co.", "Bow Line Couriers", "Metro Rush Delivery"],
                [("Same-day delivery - {n} pkgs", (15, 60)), ("Next-day envelope", (9, 20)), ("Signature required", (2, 5))], False, False),
    "print": (["Pixel Press Printing", "Quick Ink Print Shop", "Signal Print & Copy"],
              [("Business cards x{n}00", (40, 120)), ("Flyers x250", (60, 150)), ("Poster 24x36", (25, 60)), ("Banner 3x6 ft", (70, 160))], False, False),
    "grocery": (["Sunrise Fresh Market", "Valley Harvest Foods", "Corner Basket Grocery", "Greenfield Market"],
                [("MILK 4L", (5, 7)), ("BREAD", (3, 5)), ("EGGS DOZEN", (4, 7)), ("APPLES {k}KG", (4, 10)), ("RICE 8KG", (14, 22)), ("CHICKEN THIGHS", (9, 20))], True, False),
    "hardware": (["Timberline Hardware", "Homestead Building Supply", "Northfield Tools"],
                 [("DRILL BITS SET", (15, 40)), ("PAINT 3.78L", (35, 70)), ("EXTENSION CORD", (12, 30)), ("WOOD SCREWS BOX", (6, 14))], False, False),
}
KIND_WEIGHTS = {"fuel": 14, "coffee": 14, "restaurant": 12, "office": 14, "courier": 10, "print": 8, "grocery": 18, "hardware": 10}

STREETS = ["Example Trail NW", "Sample St SW", "Demo Blvd", "Test Rd SE", "Placeholder Way", "Fictional Ave NW", "Model Cres", "Practice Dr"]
MONTHS = ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"]
MONTH_NAMES = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]

def money(x): return f"{x:.2f}"
def cents(x): return round(x + 1e-9, 2)

def fmt_date(d: date):
    """The six styles seen in real transcripts. MM/DD/YYYY only (never DD/MM), to match the test receipts."""
    style = rng.randrange(6)
    t = f" {rng.randint(7, 21):02d}:{rng.randint(0, 59):02d}" if rng.random() < 0.5 else ""
    return [f"{d.month:02d}/{d.day:02d}/{d.year}{t}",
            f"{MONTH_NAMES[d.month-1]} {d.day}, {d.year}" + (f" {rng.randint(7,11)}:{rng.randint(0,59):02d} AM" if rng.random() < 0.4 else ""),
            f"{d.isoformat()}{t}",
            f"{d.day:02d} {MONTHS[d.month-1]} {d.year}",
            f"{MONTH_NAMES[d.month-1]} {d.day} {d.year}",
            f"{d.year}/{d.month:02d}/{d.day:02d}"][style]

def make_receipt():
    prov = rng.choices(list(WEIGHTS), weights=WEIGHTS.values())[0]
    kind = rng.choices(list(KIND_WEIGHTS), weights=KIND_WEIGHTS.values())[0]
    names, items, zero_rated_kind, tips = KINDS[kind]
    vendor = rng.choice(names)
    if rng.random() < 0.5: vendor = vendor.upper()
    assert vendor.upper() not in TEST_VENDORS
    city = rng.choice(PROVINCES[prov][0])
    d = date(2026, 1, 1) + timedelta(days=rng.randrange(330))

    lines, subtotal = [], 0.0
    for name, rng_price in rng.sample(items, k=rng.randint(1, min(4, len(items)))):
        if rng_price is None:   # fuel: litres × price per litre
            l, p = round(rng.uniform(20, 70), 1), round(rng.uniform(1.30, 1.75), 3)
            price = cents(l * p); label = name.format(l=l, p=p)
        else:
            price = cents(rng.uniform(*rng_price)); label = name.format(n=rng.randint(2, 9), k=round(rng.uniform(1, 3), 1))
        qty = 2 if (kind in ("coffee", "restaurant") and rng.random() < 0.25) else 1
        for _ in range(qty): lines.append(f"{label} {money(price)}"); subtotal += price
    subtotal = cents(subtotal)

    # Taxes. Groceries are zero-rated: GST is 0.00 (printed as 0.00, or no tax line at all).
    gst = other = 0.0
    tax_lines = []
    zero = zero_rated_kind
    for label, rate, field in PROVINCES[prov][1]:
        amount = 0.0 if zero else cents(subtotal * rate)
        if field == "gst": gst = amount
        else: other = amount
        if zero and rng.random() < 0.4: continue   # sometimes no tax line printed at all
        if zero and field == "other": continue
        shown = rng.choice([f"{label} {round(rate*100, 3):g}%", label, f"{'.'.join(label)}." if label == "GST" else label, "GST/HST" if field == "gst" else label])
        tax_lines.append(f"{shown} {money(amount)}")
    tip = cents(rng.choice([1, 2, 3, subtotal * 0.15, subtotal * 0.18])) if (tips and rng.random() < 0.6) else 0.0
    total = cents(subtotal + gst + other + tip)

    text = [vendor, f"{rng.randint(1, 999)} {rng.choice(STREETS)}", f"{city}, {prov}"]
    if rng.random() < 0.8: text.append(f"GST# 00000 0000 RT{rng.randint(1, 9):04d}")
    if rng.random() < 0.2: text.append("")
    text.append(fmt_date(d))
    text += lines
    text.append(rng.choice(["SUBTOTAL", "Subtotal", "SUB-TOTAL", "Sub Total"]) + f" {money(subtotal)}")
    text += tax_lines
    if tip: text.append(rng.choice(["TIP", "Tip", "GRATUITY"]) + f" {money(tip)}")
    text.append(rng.choice(["TOTAL", "Total", "TOTAL DUE", "AMOUNT DUE"]) + f" ${money(total)}")
    text.append(rng.choice(["VISA", "MASTERCARD", "DEBIT", "AMEX"]) + f" ****{rng.randint(1000, 9999)} APPROVED")
    if rng.random() < 0.7: text.append(rng.choice(["Thank you!", "THANK YOU", "Thanks for shopping!", "See you again!"]))
    text.append("*** SAMPLE RECEIPT - FOR TESTING ***")
    text.append("fictional business")

    answer = {"vendor": vendor, "date": d.isoformat(), "subtotal": subtotal, "gst": gst, "other_tax": other, "tip": tip, "total": total}
    # Self-check: the numbers must add up exactly.
    assert abs(subtotal + gst + other + tip - total) < 0.005, answer
    return "\n".join(text), answer

def example(text, answer):
    a = {k: (round(v, 2) if isinstance(v, float) else v) for k, v in answer.items()}
    return {"messages": [{"role": "user", "content": PROMPT + "\n\nReceipt:\n" + text},
                         {"role": "assistant", "content": json.dumps(a, ensure_ascii=False)}]}

def main():
    seen, rows = set(), []
    while len(rows) < 2200:
        text, ans = make_receipt()
        if text in seen: continue
        seen.add(text); rows.append(example(text, ans))
    train, heldout = rows[:2000], rows[2000:]
    for name, data in (("train.jsonl", train), ("heldout.jsonl", heldout)):
        with open(os.path.join(HERE, name), "w", encoding="utf-8") as f:
            for r in data: f.write(json.dumps(r, ensure_ascii=False) + "\n")
    # Summary, so the mix is visible.
    answers = [json.loads(r["messages"][1]["content"]) for r in rows]
    print(f"train {len(train)} · held out {len(heldout)}")
    print(f"zero GST: {sum(a['gst'] == 0 for a in answers)} · with tip: {sum(a['tip'] > 0 for a in answers)} · with other tax: {sum(a['other_tax'] > 0 for a in answers)}")
    print("\nExample:\n" + train[0]["messages"][0]["content"].split("Receipt:\n")[1] + "\n→ " + train[0]["messages"][1]["content"])

if __name__ == "__main__":
    main()
