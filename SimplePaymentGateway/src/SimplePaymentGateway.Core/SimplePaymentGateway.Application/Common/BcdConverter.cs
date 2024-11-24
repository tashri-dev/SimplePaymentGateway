using System.Text;

namespace SimplePaymentGateway.Application.Common;
public static class BcdConverter
{
    public static byte[] StringToBcd(string data)
    {
        if (string.IsNullOrEmpty(data) || data.Length % 2 != 0)
            data = data.PadLeft((data.Length + 1) / 2 * 2, '0');

        var bytes = new byte[data.Length / 2];
        for (int i = 0; i < data.Length; i += 2)
        {
            bytes[i / 2] = (byte)((GetHexValue(data[i]) << 4) | GetHexValue(data[i + 1]));
        }
        return bytes;
    }

    public static string BcdToString(byte[] bcd)
    {
        var result = new StringBuilder(bcd.Length * 2);
        foreach (byte b in bcd)
        {
            result.Append(((b >> 4) & 0x0F).ToString("X"));
            result.Append((b & 0x0F).ToString("X"));
        }
        return result.ToString();
    }

    private static int GetHexValue(char hex)
    {
        return hex switch
        {
            >= '0' and <= '9' => hex - '0',
            >= 'A' and <= 'F' => hex - 'A' + 10,
            >= 'a' and <= 'f' => hex - 'a' + 10,
            _ => throw new ArgumentException($"Invalid hex character: {hex}")
        };
    }
}
