using System.Security.Claims;
using System.Text.Json;

namespace BuildingManager.Blazor.Services;

/// <summary>
/// Decodes a JWT payload into claims for UI purposes (the API still validates
/// the signature on every call; this is only for driving the client UI).
/// </summary>
public static class JwtParser
{
    public static IReadOnlyList<Claim> ParseClaims(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            throw new FormatException("Not a JWT token.");
        }

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = (payload.Length % 4) switch
        {
            2 => payload + "==",
            3 => payload + "=",
            _ => payload
        };

        var claims = new List<Claim>();
        using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
        foreach (var property in document.RootElement.EnumerateObject())
        {
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.String:
                    claims.Add(new Claim(property.Name, property.Value.GetString() ?? string.Empty));
                    break;
                case JsonValueKind.Number:
                    // Keep raw text (e.g. exp as unix seconds) without locale formatting.
                    claims.Add(new Claim(property.Name, property.Value.GetRawText()));
                    break;
                case JsonValueKind.Array:
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            claims.Add(new Claim(property.Name, item.GetString() ?? string.Empty));
                        }
                    }
                    break;
            }
        }

        return claims;
    }

    public static bool IsExpired(IEnumerable<Claim> claims)
    {
        var exp = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        return long.TryParse(exp, out var unixSeconds)
            && DateTimeOffset.FromUnixTimeSeconds(unixSeconds) <= DateTimeOffset.UtcNow;
    }
}
