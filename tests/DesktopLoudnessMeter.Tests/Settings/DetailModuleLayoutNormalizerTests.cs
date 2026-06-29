using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Settings;

public sealed class DetailModuleLayoutNormalizerTests
{
    [Fact]
    public void CollisionModuleKeepsOriginalCompactHeight()
    {
        var module = new DetailModuleViewModel(DetailModuleKind.Collision, "Frequency collision", isVisible: true);

        Assert.Equal(112, module.Height);
        Assert.Equal(112, module.DisplayHeight);
        Assert.Equal(112, module.DisplayMinimumHeight);

        module.Height = 420;
        module.DisplayHeight = 260;

        Assert.Equal(112, module.Height);
        Assert.Equal(112, module.DisplayHeight);
        Assert.Equal(112, module.DisplayMinimumHeight);
    }

    [Fact]
    public void CollisionLayoutImportIgnoresStoredHeight()
    {
        var normalized = DetailModuleLayoutNormalizer.Normalize(
            [
                new DetailModuleLayoutSettings
                {
                    Kind = DetailModuleKind.Collision,
                    OrderIndex = 0,
                    Height = 420
                }
            ]);

        Assert.Equal(112, normalized.Single(module => module.Kind == DetailModuleKind.Collision).Height);
    }
}
