using System.Text;

namespace Bilreg.Application.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Opaque keyset cursor over (SortAt DESC, JourneyId DESC).
/// Format: Base64Url of "ticks|journeyId".
/// </summary>
public static class JourneyListCursor
{
    public static string Encode(DateTime sortAt, string journeyId)
    {
        var payload = $"{sortAt.Ticks}|{journeyId}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(string? cursor, out DateTime sortAt, out string journeyId)
    {
        sortAt = default;
        journeyId = string.Empty;
        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            var sep = raw.IndexOf('|');
            if (sep <= 0 || sep >= raw.Length - 1)
                return false;

            if (!long.TryParse(raw[..sep], out var ticks))
                return false;

            sortAt = new DateTime(ticks, DateTimeKind.Unspecified);
            journeyId = raw[(sep + 1)..];
            return journeyId.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
