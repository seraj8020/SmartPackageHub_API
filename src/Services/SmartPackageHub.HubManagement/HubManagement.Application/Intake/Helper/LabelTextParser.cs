using System.Text.RegularExpressions;
using HubManagement.Application.Intake.DTOs;

namespace HubManagement.Application.Intake
{
    public static class LabelTextParser
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

        private static readonly (string Name, Regex Pattern)[] Couriers =
        {
            ("Amazon", new Regex(@"amazon|amzl|\bamz\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, RegexTimeout)),
            ("Flipkart", new Regex(@"flipkart|ekart", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, RegexTimeout)),
            ("Delhivery", new Regex(@"delhivery", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, RegexTimeout)),
            ("DTDC", new Regex(@"\bdtdc\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, RegexTimeout)),
            ("BlueDart", new Regex(@"bluedart|blue\s*dart", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, RegexTimeout)),
            ("Xpressbees", new Regex(@"xpressbees", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, RegexTimeout)),
            ("Shadowfax", new Regex(@"shadowfax", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, RegexTimeout)),
            ("Ecom Express", new Regex(@"ecom\s*express", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, RegexTimeout)),
            ("Meesho", new Regex(@"meesho", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture, RegexTimeout)),
        };

        private static readonly Regex LabeledAwb = new(
            @"(?:awb|air\s*way\s*bill|airway\s*bill|tracking(?:\s*(?:id|no|number))?|docket|waybill)\s*[:#\-]?\s*(?<awb>[A-Z0-9][A-Z0-9\-]{5,30})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

        private static readonly Regex PrefixedAwb = new(
            @"\b(?<awb>(?:TBA|AMZ|FMPP|DL)[A-Z0-9]{8,20})\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

        private static readonly Regex TowerThenFlat = new(
            @"tower\s*[-:]?\s*(?<tower>[A-Za-z0-9]{1,4})\s*[,/\-]?\s*flat(?:\s*(?:no\.?|number|#))?\s*[:\-]?\s*(?<flat>[0-9]{2,4})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

        private static readonly Regex FlatThenTower = new(
            @"flat(?:\s*(?:no\.?|number|#))?\s*[:\-]?\s*(?<flat>[0-9]{2,4})\s*[,/\-]?\s*tower\s*[-:]?\s*(?<tower>[A-Za-z0-9]{1,4})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

        private static readonly Regex UnitCode = new(
            @"\b(?<tower>[A-Za-z])\s*[-/]\s*(?<flat>[0-9]{3,4})\b",
            RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

        private static readonly Regex TowerOnly = new(
            @"\b(?:tower|twr|block|blk)\s*[-:]?\s*(?<tower>[A-Za-z0-9]{1,4})\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

        private static readonly Regex FlatOnly = new(
            @"\bflat(?:\s*(?:no\.?|number|#))?\s*[:\-]?\s*(?<flat>[0-9]{3,4})\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

        private static readonly Regex Recipient = new(
            @"(?:deliver(?:y)?\s*to|ship\s*to|recipient|customer|attn)\s*[:\-]?\s*(?<name>[A-Za-z][^\n\r]{1,40})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            RegexTimeout);

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
                tower = CleanTower(towerFlat.Groups["tower"].Value);
                flat = towerFlat.Groups["flat"].Value;
            }
            else
            {
                var flatTower = FlatThenTower.Match(text);
                if (flatTower.Success)
                {
                    flat = flatTower.Groups["flat"].Value;
                    tower = CleanTower(flatTower.Groups["tower"].Value);
                }
                else
                {
                    var unit = UnitCode.Match(text);
                    if (unit.Success)
                    {
                        tower = CleanTower(unit.Groups["tower"].Value);
                        flat = unit.Groups["flat"].Value;
                    }
                }
            }

            if (tower == null)
            {
                var only = TowerOnly.Match(text);
                if (only.Success)
                    tower = CleanTower(only.Groups["tower"].Value);
            }

            if (flat == null)
            {
                var only = FlatOnly.Match(text);
                if (only.Success)
                    flat = only.Groups["flat"].Value;
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

            var cleaned = Regex.Replace(value.Trim(), @"^(?:tower|twr|block|blk)\s*", "", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, RegexTimeout).Trim().ToUpperInvariant();
            return Regex.IsMatch(cleaned, @"^[A-Z0-9]{1,4}$", RegexOptions.ExplicitCapture, RegexTimeout) ? cleaned : null;
        }

        public static string? CleanFlat(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            var unit = Regex.Match(trimmed, @"^(?<tower>[A-Za-z])\s*[-/]?\s*(?<flat>\d{3,4})$", RegexOptions.ExplicitCapture, RegexTimeout);
            if (unit.Success)
                return unit.Groups["flat"].Value;

            var digits = Regex.Match(trimmed, @"\d{2,4}", RegexOptions.ExplicitCapture, RegexTimeout);
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
                return CleanToken(labeled.Groups["awb"].Value);

            var prefixed = PrefixedAwb.Match(text);
            if (prefixed.Success)
                return CleanToken(prefixed.Groups["awb"].Value);

            foreach (var line in text.Split('\n'))
            {
                if (Regex.IsMatch(line, @"pin|pincode|phone|mobile|tel", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, RegexTimeout))
                    continue;

                var token = Regex.Match(line, @"\b(?<token>[A-Z0-9]{10,22})\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, RegexTimeout);
                if (!token.Success)
                    continue;

                var value = token.Groups["token"].Value;
                if (Regex.IsMatch(value, @"^\d{6}$", RegexOptions.ExplicitCapture, RegexTimeout) || Regex.IsMatch(value, @"^\d{10}$", RegexOptions.ExplicitCapture, RegexTimeout))
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

            var line = match.Groups["name"].Value.Split(',')[0].Trim();
            if (Regex.IsMatch(line, @"^(?:tower|flat|block|unit)\b", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, RegexTimeout))
                return null;

            var words = Regex.Matches(line, @"[A-Za-z][A-Za-z.'-]*", RegexOptions.ExplicitCapture, RegexTimeout);
            if (words.Count == 0)
                return null;

            var name = string.Join(' ', words.Take(4).Select(word => word.Value));
            return name.Length >= 3 ? name : null;
        }

        private static string CleanToken(string value) => value.Trim().Trim('-').ToUpperInvariant();
    }
}
