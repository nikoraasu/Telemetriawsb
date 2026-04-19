using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Telemetry.Contracts;

public static class ChecksumHelper
{
    public static string ComputeChecksum(InfluxPayload payload, string secretKey)
    {
        var canonical = string.Join("|",
            payload.Measurement?.Trim() ?? string.Empty,
            payload.Location?.Trim() ?? string.Empty,
            payload.Value.ToString("0.######", CultureInfo.InvariantCulture),
            payload.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToBase64String(hash);
    }

    public static bool VerifyChecksum(InfluxPayload payload, string secretKey)
        => string.Equals(payload.Checksum, ComputeChecksum(payload, secretKey), StringComparison.Ordinal);
}
