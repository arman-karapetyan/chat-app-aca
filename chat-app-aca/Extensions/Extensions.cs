using System.Security.Cryptography;
using System.Text;

namespace chat_app_aca.Extensions;

public static class Extensions
{
    public static string HashPassword(this string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    public static DateTime FromMs(this long ms) => DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
}