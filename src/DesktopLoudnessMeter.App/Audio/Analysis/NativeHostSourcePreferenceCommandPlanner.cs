using System.Runtime.InteropServices;
using System.Text;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostSourcePreferenceCommandPlanner : IDisposable
{
    private const int StatusSuccess = 0;
    private const int StatusSourceNotFound = 1;
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostSourcePreferenceCommandPlanner()
    {
        var result = NativeMethods.DlmHostSourcePreferenceCommandCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host source preference planner could not be initialized (code {result}).");
        }
    }

    public static NativeHostSourcePreferenceCommandPlan PlanSetAlias(
        IReadOnlyList<string> sourceKeys,
        string sourceKey,
        string? alias)
    {
        using var planner = new NativeHostSourcePreferenceCommandPlanner();
        return planner.PlanAlias(sourceKeys, sourceKey, alias);
    }

    public static NativeHostSourcePreferenceCommandPlan PlanSetHidden(
        IReadOnlyList<string> sourceKeys,
        string sourceKey,
        bool isHidden)
    {
        using var planner = new NativeHostSourcePreferenceCommandPlanner();
        return planner.PlanHidden(sourceKeys, sourceKey, isHidden);
    }

    public static NativeHostSourcePreferenceCommandPlan PlanForget(
        string sourceKey,
        string? selectedSourceKey)
    {
        using var planner = new NativeHostSourcePreferenceCommandPlanner();
        return planner.PlanForgetSource(sourceKey, selectedSourceKey);
    }

    public NativeHostSourcePreferenceCommandPlan PlanAlias(
        IReadOnlyList<string> sourceKeys,
        string sourceKey,
        string? alias)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ResetSources(sourceKeys);
        ThrowIfFailed(
            NativeMethods.DlmHostSourcePreferenceCommandPlanSetAliasExport(_handle, sourceKey, alias),
            "Native host source preference alias planning failed");
        return ReadPlan();
    }

    public NativeHostSourcePreferenceCommandPlan PlanHidden(
        IReadOnlyList<string> sourceKeys,
        string sourceKey,
        bool isHidden)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ResetSources(sourceKeys);
        ThrowIfFailed(
            NativeMethods.DlmHostSourcePreferenceCommandPlanSetHiddenExport(_handle, sourceKey, isHidden ? 1 : 0),
            "Native host source preference hidden planning failed");
        return ReadPlan();
    }

    public NativeHostSourcePreferenceCommandPlan PlanForgetSource(
        string sourceKey,
        string? selectedSourceKey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostSourcePreferenceCommandPlanForgetExport(_handle, sourceKey, selectedSourceKey),
            "Native host source preference forget planning failed");
        return ReadPlan();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_handle != IntPtr.Zero)
        {
            NativeMethods.DlmHostSourcePreferenceCommandDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ResetSources(IReadOnlyList<string> sourceKeys)
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSourcePreferenceCommandClearSourcesExport(_handle),
            "Native host source preference planner clear failed");
        foreach (var sourceKey in sourceKeys)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSourcePreferenceCommandAddSourceExport(_handle, sourceKey),
                "Native host source preference planner add-source failed");
        }
    }

    private NativeHostSourcePreferenceCommandPlan ReadPlan()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSourcePreferenceCommandGetPlanExport(_handle, out var plan),
            "Native host source preference planner plan read failed");
        if (plan.Status == StatusSourceNotFound)
        {
            return NativeHostSourcePreferenceCommandPlan.NotFound;
        }

        if (plan.Status != StatusSuccess)
        {
            throw new InvalidOperationException($"Native host source preference planner returned unknown status {plan.Status}.");
        }

        return new NativeHostSourcePreferenceCommandPlan(
            Success: true,
            Alias: plan.HasAlias == 0 ? null : CopyAlias(plan.AliasUtf8ByteCount),
            IsHidden: plan.IsHidden != 0,
            ShouldForget: plan.ShouldForget != 0,
            ShouldClearSelected: plan.ShouldClearSelected != 0);
    }

    private string CopyAlias(int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostSourcePreferenceCommandCopyAliasExport(
                _handle,
                buffer,
                buffer.Length,
                out var actualCount),
            "Native host source preference alias copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePreferencePlan
    {
        public int Status;
        public int HasAlias;
        public int AliasUtf8ByteCount;
        public int IsHidden;
        public int ShouldForget;
        public int ShouldClearSelected;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourcePreferenceCommandCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourcePreferenceCommandCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourcePreferenceCommandDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostSourcePreferenceCommandDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourcePreferenceCommandClearSourcesExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourcePreferenceCommandClearSourcesExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourcePreferenceCommandAddSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourcePreferenceCommandAddSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourcePreferenceCommandPlanSetAliasExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourcePreferenceCommandPlanSetAliasExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? alias);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourcePreferenceCommandPlanSetHiddenExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourcePreferenceCommandPlanSetHiddenExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            int isHidden);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourcePreferenceCommandPlanForgetExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourcePreferenceCommandPlanForgetExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? selectedSourceKey);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourcePreferenceCommandGetPlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourcePreferenceCommandGetPlanExport(IntPtr handle, out NativePreferencePlan plan);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSourcePreferenceCommandCopyAliasExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSourcePreferenceCommandCopyAliasExport(
            IntPtr handle,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostSourcePreferenceCommandPlan(
    bool Success,
    string? Alias,
    bool IsHidden,
    bool ShouldForget,
    bool ShouldClearSelected)
{
    public static NativeHostSourcePreferenceCommandPlan NotFound { get; } = new(
        Success: false,
        Alias: null,
        IsHidden: false,
        ShouldForget: false,
        ShouldClearSelected: false);
}
