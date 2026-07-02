using System.Runtime.InteropServices;
using System.Text;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostGlobalSettingsCommandPlanner : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostGlobalSettingsCommandPlanner()
    {
        var result = NativeMethods.DlmHostGlobalSettingsCommandCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host global settings planner could not be initialized (code {result}).");
        }
    }

    public static NativeHostGlobalSettingsCommandPlan PlanSetTheme(string? themeId)
    {
        using var planner = new NativeHostGlobalSettingsCommandPlanner();
        return planner.PlanTheme(themeId);
    }

    public static NativeHostGlobalSettingsCommandPlan PlanSetRecentEventLimit(int maxRecentEventEntries)
    {
        using var planner = new NativeHostGlobalSettingsCommandPlanner();
        return planner.PlanRecentEventLimit(maxRecentEventEntries);
    }

    public static NativeHostGlobalSettingsCommandPlan PlanSetEventOutputFolder(string? folder)
    {
        using var planner = new NativeHostGlobalSettingsCommandPlanner();
        return planner.PlanEventOutputFolder(folder);
    }

    public static NativeHostGlobalSettingsCommandPlan PlanSetShowHiddenSources(bool showHiddenSources)
    {
        using var planner = new NativeHostGlobalSettingsCommandPlanner();
        return planner.PlanShowHiddenSources(showHiddenSources);
    }

    public NativeHostGlobalSettingsCommandPlan PlanTheme(string? themeId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostGlobalSettingsCommandPlanSetThemeExport(_handle, themeId),
            "Native host global settings theme planning failed");
        return ReadPlan();
    }

    public NativeHostGlobalSettingsCommandPlan PlanRecentEventLimit(int maxRecentEventEntries)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostGlobalSettingsCommandPlanSetRecentEventLimitExport(_handle, maxRecentEventEntries),
            "Native host global settings recent event limit planning failed");
        return ReadPlan();
    }

    public NativeHostGlobalSettingsCommandPlan PlanEventOutputFolder(string? folder)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostGlobalSettingsCommandPlanSetEventOutputFolderExport(_handle, folder),
            "Native host global settings event output folder planning failed");
        return ReadPlan();
    }

    public NativeHostGlobalSettingsCommandPlan PlanShowHiddenSources(bool showHiddenSources)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(
            NativeMethods.DlmHostGlobalSettingsCommandPlanSetShowHiddenSourcesExport(_handle, showHiddenSources ? 1 : 0),
            "Native host global settings show-hidden planning failed");
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
            NativeMethods.DlmHostGlobalSettingsCommandDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private NativeHostGlobalSettingsCommandPlan ReadPlan()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostGlobalSettingsCommandGetPlanExport(_handle, out var plan),
            "Native host global settings plan read failed");

        return new NativeHostGlobalSettingsCommandPlan(
            ThemeId: plan.HasThemeId == 0 ? null : CopyThemeId(plan.ThemeIdUtf8ByteCount),
            MaxRecentEventEntries: plan.MaxRecentEventEntries,
            EventOutputFolder: plan.HasEventOutputFolder == 0 ? null : CopyEventOutputFolder(plan.EventOutputFolderUtf8ByteCount),
            ShowHiddenSources: plan.ShowHiddenSources != 0);
    }

    private string CopyThemeId(int count)
    {
        return CopyString(count, NativeMethods.DlmHostGlobalSettingsCommandCopyThemeIdExport, "Native host global settings theme copy failed");
    }

    private string CopyEventOutputFolder(int count)
    {
        return CopyString(
            count,
            NativeMethods.DlmHostGlobalSettingsCommandCopyEventOutputFolderExport,
            "Native host global settings event output folder copy failed");
    }

    private string CopyString(int count, CopyStringExport copy, string message)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(copy(_handle, buffer, buffer.Length, out var actualCount), message);
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    private delegate int CopyStringExport(IntPtr handle, byte[] buffer, int capacity, out int actualByteCount);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeGlobalSettingsPlan
    {
        public int HasThemeId;
        public int ThemeIdUtf8ByteCount;
        public int MaxRecentEventEntries;
        public int HasEventOutputFolder;
        public int EventOutputFolderUtf8ByteCount;
        public int ShowHiddenSources;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostGlobalSettingsCommandCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostGlobalSettingsCommandCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostGlobalSettingsCommandDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostGlobalSettingsCommandDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostGlobalSettingsCommandPlanSetThemeExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostGlobalSettingsCommandPlanSetThemeExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? themeId);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostGlobalSettingsCommandPlanSetRecentEventLimitExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostGlobalSettingsCommandPlanSetRecentEventLimitExport(
            IntPtr handle,
            int maxRecentEventEntries);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostGlobalSettingsCommandPlanSetEventOutputFolderExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostGlobalSettingsCommandPlanSetEventOutputFolderExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? folder);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostGlobalSettingsCommandPlanSetShowHiddenSourcesExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostGlobalSettingsCommandPlanSetShowHiddenSourcesExport(
            IntPtr handle,
            int showHiddenSources);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostGlobalSettingsCommandGetPlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostGlobalSettingsCommandGetPlanExport(IntPtr handle, out NativeGlobalSettingsPlan plan);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostGlobalSettingsCommandCopyThemeIdExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostGlobalSettingsCommandCopyThemeIdExport(
            IntPtr handle,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostGlobalSettingsCommandCopyEventOutputFolderExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostGlobalSettingsCommandCopyEventOutputFolderExport(
            IntPtr handle,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostGlobalSettingsCommandPlan(
    string? ThemeId,
    int MaxRecentEventEntries,
    string? EventOutputFolder,
    bool ShowHiddenSources);
