namespace DesktopLoudnessMeter.App.ViewModels;

internal static class DetailModuleDropPlacement
{
    public static bool ShouldInsertAfter(DetailModuleViewModel source, DetailModuleViewModel target)
    {
        return source.OrderIndex < target.OrderIndex;
    }
}
