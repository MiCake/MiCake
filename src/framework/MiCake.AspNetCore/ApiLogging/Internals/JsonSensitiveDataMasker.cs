using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MiCake.AspNetCore.ApiLogging.Internals
{
    /// <summary>
    /// Default implementation of <see cref="ISensitiveDataMasker"/> that masks
    /// sensitive fields in JSON content.
    /// </summary>
    internal sealed class JsonSensitiveDataMasker : ISensitiveDataMasker
    {
        private const string MaskValue = "***";

        public string Mask(string content, IEnumerable<string> sensitiveFields)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            var fields = sensitiveFields?.ToList();
            if (fields == null || fields.Count == 0)
            {
                return content;
            }

            // Try to parse as JSON first
            if (TryMaskJson(content, fields, out var maskedJson))
            {
                return maskedJson;
            }

            // Fall back to regex-based masking for non-JSON content
            return MaskWithRegex(content, fields);
        }

        private static bool TryMaskJson(string content, List<string> sensitiveFields, out string result)
        {
            result = content;

            try
            {
                using var doc = JsonDocument.Parse(content);
                var maskedElement = MaskJsonElement(doc.RootElement, sensitiveFields);
                result = JsonSerializer.Serialize(maskedElement, new JsonSerializerOptions
                {
                    WriteIndented = false
                });
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static object? MaskJsonElement(JsonElement element, List<string> sensitiveFields)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => MaskJsonObject(element, sensitiveFields),
                JsonValueKind.Array => MaskJsonArray(element, sensitiveFields),
                _ => GetJsonValue(element)
            };
        }

        private static Dictionary<string, object?> MaskJsonObject(JsonElement element, List<string> sensitiveFields)
        {
            var result = new Dictionary<string, object?>();

            foreach (var property in element.EnumerateObject())
            {
                var isSensitive = sensitiveFields.Any(f => property.Name.Equals(f, StringComparison.OrdinalIgnoreCase));
                if (isSensitive)
                {
                    result[property.Name] = MaskValue;
                }
                else
                {
                    result[property.Name] = MaskJsonElement(property.Value, sensitiveFields);
                }
            }

            return result;
        }

        private static List<object?> MaskJsonArray(JsonElement element, List<string> sensitiveFields)
        {
            var result = new List<object?>();

            foreach (var item in element.EnumerateArray())
            {
                result.Add(MaskJsonElement(item, sensitiveFields));
            }

            return result;
        }

        private static object? GetJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number when element.TryGetInt64(out var l) => l,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        private static string MaskWithRegex(string content, List<string> sensitiveFields)
        {
            var result = content;

            foreach (var field in sensitiveFields)
            {
                // Match patterns like: "fieldName": "value" or "fieldName":"value"
                var pattern = $@"(""{Regex.Escape(field)}""\s*:\s*"")[^""]*("")";
                result = Regex.Replace(
                    result,
                    pattern,
                    $"$1{MaskValue}$2",
                    RegexOptions.IgnoreCase);

                // Match patterns like: fieldName=value&
                var queryPattern = $@"({Regex.Escape(field)}=)[^&\s]*";
                result = Regex.Replace(
                    result,
                    queryPattern,
                    $"$1{MaskValue}",
                    RegexOptions.IgnoreCase);
            }

            return result;
        }
    }
}
