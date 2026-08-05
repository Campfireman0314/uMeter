using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.ViewModels;

public sealed class DetailModuleDropPlacementTests
{
    [Fact]
    public void DraggingUpwardInsertsBeforeTargetIncludingAtTop()
    {
        var first = Module(DetailModuleKind.Stereo, 0);
        var last = Module(DetailModuleKind.Spectrum, 4);

        var insertAfter = DetailModuleDropPlacement.ShouldInsertAfter(last, first);

        Assert.False(insertAfter);
    }

    [Fact]
    public void DraggingDownwardInsertsAfterTarget()
    {
        var first = Module(DetailModuleKind.Stereo, 0);
        var last = Module(DetailModuleKind.Spectrum, 4);

        var insertAfter = DetailModuleDropPlacement.ShouldInsertAfter(first, last);

        Assert.True(insertAfter);
    }

    private static DetailModuleViewModel Module(DetailModuleKind kind, int orderIndex)
    {
        return new DetailModuleViewModel(kind, kind.ToString(), isVisible: true)
        {
            OrderIndex = orderIndex
        };
    }
}
