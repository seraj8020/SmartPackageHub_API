namespace HubManagement.Application.HubResidents.Services
{
    internal static class ResidentCsvParser
    {
        public const int MaxDataRows = 1000;

        private sealed record HeaderMap(int FullName, int Phone, int TowerWing, int FlatNumber, int? IsMember);

        public static (IReadOnlyList<ResidentCsvRow> Rows, string? Error) Parse(string csvContent)
        {
            if (string.IsNullOrWhiteSpace(csvContent))
                return ([], "CSV file is empty.");

            var rawLines = csvContent
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n');

            var firstNonEmpty = Array.FindIndex(rawLines, static l => !string.IsNullOrWhiteSpace(l));
            if (firstNonEmpty < 0)
                return ([], "CSV file is empty.");

            var headerCells = SplitCsvLine(rawLines[firstNonEmpty]);
            if (!TryMapHeader(headerCells, out var map, out var headerError))
                return ([], headerError);

            var rows = new List<ResidentCsvRow>();
            for (var i = firstNonEmpty + 1; i < rawLines.Length; i++)
            {
                var line = rawLines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (rows.Count >= MaxDataRows)
                    return ([], $"CSV exceeds the maximum of {MaxDataRows} data rows.");

                var cells = SplitCsvLine(line);
                rows.Add(new ResidentCsvRow(
                    Line: i + 1,
                    FullName: Cell(cells, map.FullName),
                    Phone: Cell(cells, map.Phone),
                    TowerWing: Cell(cells, map.TowerWing),
                    FlatNumber: Cell(cells, map.FlatNumber),
                    IsMember: ParseBool(Cell(cells, map.IsMember))));
            }

            if (rows.Count == 0)
                return ([], "CSV has a header but no data rows.");

            return (rows, null);
        }

        private static bool TryMapHeader(IReadOnlyList<string> cells, out HeaderMap map, out string? error)
        {
            map = new HeaderMap(-1, -1, -1, -1, null);
            error = null;
            var fullName = IndexOf(cells, "FULLNAME", "NAME");
            var phone = IndexOf(cells, "PHONE", "PHONENO", "MOBILE");
            var tower = IndexOf(cells, "TOWERWING", "TOWER", "WING");
            var flat = IndexOf(cells, "FLATNUMBER", "FLAT", "UNIT", "UNITNUMBER");
            var member = IndexOf(cells, "ISMEMBER", "MEMBER");

            if (fullName < 0 || phone < 0 || tower < 0 || flat < 0)
            {
                error = "CSV header must include fullName, phone, towerWing, and flatNumber.";
                return false;
            }

            map = new HeaderMap(fullName, phone, tower, flat, member >= 0 ? member : null);
            return true;
        }

        private static int IndexOf(IReadOnlyList<string> cells, params string[] aliases)
        {
            var aliasSet = new HashSet<string>(aliases, StringComparer.Ordinal);
            for (var i = 0; i < cells.Count; i++)
            {
                if (aliasSet.Contains(NormalizeHeader(cells[i])))
                    return i;
            }

            return -1;
        }

        private static string NormalizeHeader(string value) =>
            new string(value.Trim().Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        private static string Cell(IReadOnlyList<string> cells, int? index)
        {
            if (index is null || index.Value < 0 || index.Value >= cells.Count)
                return string.Empty;
            return cells[index.Value].Trim();
        }

        private static bool ParseBool(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            return value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("y", StringComparison.OrdinalIgnoreCase);
        }

        internal static IReadOnlyList<string> SplitCsvLine(string line)
        {
            var cells = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;
            var i = 0;
            while (i < line.Length)
            {
                var ch = line[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i += 2;
                            continue;
                        }

                        inQuotes = false;
                        i++;
                        continue;
                    }

                    current.Append(ch);
                    i++;
                    continue;
                }

                if (ch == '"')
                {
                    inQuotes = true;
                    i++;
                    continue;
                }

                if (ch == ',')
                {
                    cells.Add(current.ToString());
                    current.Clear();
                    i++;
                    continue;
                }

                current.Append(ch);
                i++;
            }

            cells.Add(current.ToString());
            return cells;
        }
    }
}
