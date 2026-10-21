using System.Collections;
using System.Globalization;
using Newtonsoft.Json;

namespace SmartPackageHub.HubManagement.Api;
internal static class JsonSeedDataLoader
{
    private static readonly JsonSerializerSettings SeedSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        // Keep date strings intact so the UTC converter can honor the offset.
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            new PostgresBoolConverter(),
            new FlexibleStringConverter(),
            new UtcDateTimeConverter()
        }
    };

    public static List<T> LoadSeedData<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Warning: JSON seed data file not found at {filePath}");
            return new List<T>();
        }

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<T>>(jsonContent, SeedSettings) ?? new List<T>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing JSON from {filePath}: {ex.Message}");
            return new List<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred while reading {filePath}: {ex.Message}");
            throw; // Preserves the stack trace and rethrows the exception
        }
    }

    public static IDictionary<string, List<object>> LoadMultipleSeedData(string directoryPath, IDictionary<string, Type> fileToTypeMap)
    {
        var allSeedData = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Warning: Seed data directory not found at {directoryPath}");
            return allSeedData;
        }
        ArgumentNullException.ThrowIfNull(fileToTypeMap);
        foreach (var entry in fileToTypeMap)
        {
            string fileName = entry.Key;
            Type dataType = entry.Value;
            string filePath = Path.Combine(directoryPath, fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Warning: JSON seed data file not found at {filePath}");
                continue;
            }

            try
            {
                string jsonContent = File.ReadAllText(filePath);
                var listType = typeof(List<>).MakeGenericType(dataType);
                if (JsonConvert.DeserializeObject(jsonContent, listType, SeedSettings) is IEnumerable data)
                {
                    var typed = new List<object>();
                    foreach (var item in data)
                    {
                        if (item != null)
                            typed.Add(item);
                    }

                    allSeedData.Add(Path.GetFileNameWithoutExtension(fileName), typed);
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing JSON from {filePath}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error processing seed data file {filePath}: {ex.Message}");
                throw; // Preserves the stack trace and rethrows the exception
            }
        }
        return allSeedData;
    }



    // Seed exports use PostgreSQL boolean literals ("t" / "f") instead of JSON true/false.
    private sealed class PostgresBoolConverter : JsonConverter<bool>
    {
        public override bool ReadJson(JsonReader reader, Type objectType, bool existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Boolean:
                    return Convert.ToBoolean(reader.Value, CultureInfo.InvariantCulture);
                case JsonToken.Integer:
                    return Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture) != 0;
                case JsonToken.String:
                    var text = reader.Value?.ToString()?.Trim();
                    return text is "t" or "T" or "true" or "True" or "1" or "yes" or "Yes";
                case JsonToken.Null:
                    return false;
                default:
                    throw new JsonSerializationException($"Cannot convert token {reader.TokenType} to Boolean.");
            }
        }

        public override void WriteJson(JsonWriter writer, bool value, JsonSerializer serializer) =>
            writer.WriteValue(value);
    }

    // Delivery history exports store pkgNumber as a JSON number; the model property is a string.
    private sealed class FlexibleStringConverter : JsonConverter<string>
    {
        public override string? ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            return Convert.ToString(reader.Value, CultureInfo.InvariantCulture);
        }

        public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer) =>
            writer.WriteValue(value);
    }

    // Npgsql rejects DateTimeKind.Unspecified for timestamp with time zone.
    private sealed class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Date && reader.Value is DateTime dateTime)
                return ToUtc(dateTime);

            var text = reader.Value?.ToString();
            if (string.IsNullOrWhiteSpace(text))
                throw new JsonSerializationException("Date value is missing.");

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var offset))
                return offset.UtcDateTime;

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
                return ToUtc(parsed);

            throw new JsonSerializationException($"Cannot convert '{text}' to DateTime.");
        }

        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer) =>
            writer.WriteValue(value);

        private static DateTime ToUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
    }
}