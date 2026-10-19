using System.Text.RegularExpressions;
using HubManagement.Application.Intake.DTOs;

namespace HubManagement.Application.Intake
{
    public static class LabelTextParser
    {
        private static readonly (string Name, Regex Pattern)[] Couriers =
        {
            ("Amazon", new Regex(@"amazon|amzl|\bamz\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("Flipkart", new Regex(@"flipkart|ekart", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("Delhivery", new Regex(@"delhivery", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("DTDC", new Regex(@"\bdtdc\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("BlueDart", new Regex(@"bluedart|blue\s*dart", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("Xpressbees", new Regex(@"xpressbees", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("Shadowfax", new Regex(@"shadowfax", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("Ecom Express", new Regex(@"ecom\s*express", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
            ("Meesho", new Regex(@"meesho", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        };

        private static readonly Regex LabeledAwb = new(
            @"(?:awb|air\s*way\s*bill|airway\s*bill|tracking(?:\s*(?:id|no|number))?|docket|waybill)\s*[:#\-]?\s*([A-Z0-9][A-Z0-9\-]{5,30})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PrefixedAwb = new(
            @"\b((?:TBA|AMZ|FMPP|DL)[A-Z0-9]{8,20})\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TowerThenFlat = new(
            @"tower\s*[-:]?\s*([A-Za-z0-9]{1,4})\s*[,/\-]?\s*flat(?:\s*(?:no\.?|number|#))?\s*[:\-]?\s*([0-9]{2,4})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex FlatThenTower = new(
            @"flat(?:\s*(?:no\.?|number|#))?\s*[:\-]?\s*([0-9]{2,4})\s*[,/\-]?\s*tower\s*[-:]?\s*([A-Za-z0-9]{1,4})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex UnitCode = new(
            @"\b([A-Za-z])\s*[-/]\s*([0-9]{3,4})\b",
            RegexOptions.Compiled);

        private static readonly Regex TowerOnly = new(
            @"\b(?:tower|twr|block|blk)\s*[-:]?\s*([A-Za-z0-9]{1,4})\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex FlatOnly = new(
            @"\bflat(?:\s*(?:no\.?|number|#))?\s*[:\-]?\s*([0-9]{3,4})\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Recipient = new(
            @"(?:deliver(?:y)?\s*to|ship\s*to|recipient|customer|attn)\s*[:\-]?\s*([A-Za-z][^\n\r]{1,40})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static ParsedLabelDto Parse(string? raw)
        {
            var text = (raw ?? "").Replace('\r', '\n');
            if (text.Length > 8000)
                text = text[..8000];

            string? tower = null;
            string? flat = null;

            var towerFlat = TowerThenFlat.Match(text);
            if (towerFlat.Success)
            {
                tower = CleanTower(towerFlat.Groups[1].Value);
                flat = towerFlat.Groups[2].Value;
            }
            else
            {
                var flatTower = FlatThenTower.Match(text);
                if (flatTower.Success)
                {
                    flat = flatTower.Groups[1].Value;
                    tower = CleanTower(flatTower.Groups[2].Value);
                }
                else
                {
                    var unit = UnitCode.Match(text);
                    if (unit.Success)
                    {
                        tower = CleanTower(unit.Groups[1].Value);
                        flat = unit.Groups[2].Value;
                    }
                }
            }

            if (tower == null)
            {
                var only = TowerOnly.Match(text);
                if (only.Success)
                    tower = CleanTower(only.Groups[1].Value);
            }

            if (flat == null)
            {
                var only = FlatOnly.Match(text);
                if (only.Success)
                    flat = only.Groups[1].Value;
            }

            return new ParsedLabelDto
            {
                Courier = FindCourier(text),
                Awb = FindAwb(text),
                RecipientName = FindName(text),
                Tower = tower,
                FlatNumber = flat
            };
        }

        public static string? CleanTower(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var cleaned = Regex.Replace(value.Trim(), @"^(tower|twr|block|blk)\s*", "", RegexOptions.IgnoreCase).Trim().ToUpperInvariant();
            return Regex.IsMatch(cleaned, @"^[A-Z0-9]{1,4}$") ? cleaned : null;
        }

        public static string? CleanFlat(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            var unit = Regex.Match(trimmed, @"^([A-Za-z])\s*[-/]?\s*(\d{3,4})$");
            if (unit.Success)
                return unit.Groups[2].Value;

            var digits = Regex.Match(trimmed, @"\d{2,4}");
            return digits.Success ? digits.Value : null;
        }

        private static string? FindCourier(string text)
        {
            foreach (var (name, pattern) in Couriers)
            {
                if (pattern.IsMatch(text))
                    return name;
            }

            return null;
        }

        private static string? FindAwb(string text)
        {
            var labeled = LabeledAwb.Match(text);
            if (labeled.Success)
                return CleanToken(labeled.Groups[1].Value);

            var prefixed = PrefixedAwb.Match(text);
            if (prefixed.Success)
                return CleanToken(prefixed.Groups[1].Value);

            foreach (var line in text.Split('\n'))
            {
                if (Regex.IsMatch(line, @"pin|pincode|phone|mobile|tel", RegexOptions.IgnoreCase))
                    continue;

                var token = Regex.Match(line, @"\b([A-Z0-9]{10,22})\b", RegexOptions.IgnoreCase);
                if (!token.Success)
                    continue;

                var value = token.Groups[1].Value;
                if (Regex.IsMatch(value, @"^\d{6}$") || Regex.IsMatch(value, @"^\d{10}$"))
                    continue;

                return value.ToUpperInvariant();
            }

            return null;
        }

        private static string? FindName(string text)
        {
            var match = Recipient.Match(text);
            if (!match.Success)
                return null;

            var line = match.Groups[1].Value.Split(',')[0].Trim();
            if (Regex.IsMatch(line, @"^(tower|flat|block|unit)\b", RegexOptions.IgnoreCase))
                return null;

            var words = Regex.Matches(line, @"[A-Za-z][A-Za-z.'-]*");
            if (words.Count == 0)
                return null;

            var name = string.Join(' ', words.Take(4).Select(word => word.Value));
            return name.Length >= 3 ? name : null;
        }

        private static string CleanToken(string value) => value.Trim().Trim('-').ToUpperInvariant();
    }
}
