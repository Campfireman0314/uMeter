using DesktopLoudnessMeter.Protocol;

namespace DesktopLoudnessMeter.SenderKit;

public static class SenderIdentityStore
{
    public static string LoadOrCreate(string path, string prefix = "sender")
    {
        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path).Trim();
            if (LocalLoopbackProtocol.IsValidHandle(existing))
            {
                return existing;
            }
        }

        var handle = $"{NormalizePrefix(prefix)}-{Guid.NewGuid():N}";
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, handle);
        return handle;
    }

    private static string NormalizePrefix(string prefix)
    {
        var normalized = new string(prefix
            .Trim()
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-')
            .Where(character => character != '\0')
            .ToArray())
            .Trim('-');

        return string.IsNullOrWhiteSpace(normalized) ? "sender" : normalized;
    }
}
