namespace Api.Application.Common.Security;

using System.Text.Json;
using System.Text.Json.Nodes;

public static class PiiMasking
{
    public static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return email;
        }

        var trimmed = email.Trim();
        var separatorIndex = trimmed.IndexOf('@');
        if (separatorIndex <= 0 || separatorIndex == trimmed.Length - 1)
        {
            return "***";
        }

        var localPart = trimmed[..separatorIndex];
        var domain = trimmed[separatorIndex..];

        if (localPart.Length <= 2)
        {
            return $"{localPart[0]}*@{domain[1..]}";
        }

        return $"{localPart[0]}***{localPart[^1]}{domain}";
    }

    public static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return phone;
        }

        var trimmed = phone.Trim();
        if (trimmed.Length <= 4)
        {
            return new string('*', trimmed.Length);
        }

        return $"{new string('*', trimmed.Length - 4)}{trimmed[^4..]}";
    }

    public static string MaskJsonPayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return payloadJson;
        }

        try
        {
            var node = JsonNode.Parse(payloadJson);
            if (node is null)
            {
                return payloadJson;
            }

            MaskNode(node);
            return node.ToJsonString();
        }
        catch (JsonException)
        {
            return payloadJson;
        }
    }

    private static void MaskNode(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (property.Value is null)
                {
                    continue;
                }

                if (property.Value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue))
                {
                    if (property.Key.Contains("email", StringComparison.OrdinalIgnoreCase))
                    {
                        jsonObject[property.Key] = MaskEmail(stringValue);
                        continue;
                    }

                    if (property.Key.Contains("phone", StringComparison.OrdinalIgnoreCase))
                    {
                        jsonObject[property.Key] = MaskPhone(stringValue);
                        continue;
                    }
                }

                MaskNode(property.Value);
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is not null)
                {
                    MaskNode(item);
                }
            }
        }
    }
}