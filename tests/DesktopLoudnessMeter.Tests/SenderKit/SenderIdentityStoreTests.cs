using DesktopLoudnessMeter.SenderKit;

namespace DesktopLoudnessMeter.Tests.SenderKit;

public sealed class SenderIdentityStoreTests
{
    [Fact]
    public void GeneratedHandlesPersistAndReload()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dlm-sender-{Guid.NewGuid():N}.identity");

        try
        {
            var first = SenderIdentityStore.LoadOrCreate(path, "probe");
            var second = SenderIdentityStore.LoadOrCreate(path, "probe");

            Assert.Equal(first, second);
            Assert.StartsWith("probe-", first, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
