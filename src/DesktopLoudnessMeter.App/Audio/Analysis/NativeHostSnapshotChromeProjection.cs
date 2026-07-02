using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostSnapshotChromeProjection : IDisposable
{
    private const int SettingsStringThemeId = 0;
    private const int SettingsStringEventOutputFolder = 1;
    private const int SettingsStringDefaultDetailLayoutFile = 2;
    private const int SourceStringSourceKey = 0;
    private const int SourceStringAlias = 1;
    private const int SourceStringInlineCollisionSourceKey = 2;

    private IntPtr _handle;
    private bool _disposed;

    public NativeHostSnapshotChromeProjection()
    {
        var result = NativeMethods.DlmHostSnapshotChromeProjectionCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host snapshot chrome projection could not be initialized (code {result}).");
        }
    }

    public static NativeHostSnapshotChromeProjectionResult Project(
        UserWorkspaceSettings workspace,
        bool showHiddenSources,
        int? selectedSourceIndex,
        int sourceCount)
    {
        using var projection = new NativeHostSnapshotChromeProjection();
        return projection.ProjectSnapshotChrome(workspace, showHiddenSources, selectedSourceIndex, sourceCount);
    }

    public NativeHostSnapshotChromeProjectionResult ProjectSnapshotChrome(
        UserWorkspaceSettings workspace,
        bool showHiddenSources,
        int? selectedSourceIndex,
        int sourceCount)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        workspace ??= new UserWorkspaceSettings();
        ThrowIfFailed(NativeMethods.DlmHostSnapshotChromeProjectionClearExport(_handle), "Native snapshot chrome projection clear failed");
        ThrowIfFailed(
            NativeMethods.DlmHostSnapshotChromeProjectionSetGlobalSettingsExport(
                _handle,
                UserWorkspaceSettings.NormalizeSchemaVersion(workspace.SchemaVersion),
                workspace.ThemeId,
                workspace.MaxRecentEventEntries is null ? 0 : 1,
                workspace.MaxRecentEventEntries ?? 0,
                workspace.EventOutputFolder,
                workspace.LoadDefaultDetailLayoutFromFile ? 1 : 0,
                workspace.DefaultDetailLayoutFile,
                showHiddenSources ? 1 : 0),
            "Native snapshot chrome global settings projection failed");

        foreach (var pair in workspace.Sources)
        {
            AddSourceSettings(pair.Key, pair.Value ?? new SourceWorkspaceSettings());
        }

        ThrowIfFailed(
            NativeMethods.DlmHostSnapshotChromeProjectionProjectExport(
                _handle,
                selectedSourceIndex is null ? 0 : 1,
                selectedSourceIndex ?? -1,
                sourceCount),
            "Native snapshot chrome projection failed");

        return new NativeHostSnapshotChromeProjectionResult(ReadSettings(), ReadCommands());
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_handle != IntPtr.Zero)
        {
            NativeMethods.DlmHostSnapshotChromeProjectionDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void AddSourceSettings(string sourceKey, SourceWorkspaceSettings source)
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSnapshotChromeProjectionAddSourceSettingsExport(
                _handle,
                sourceKey,
                source.Alias,
                source.IsHidden ? 1 : 0,
                source.OrderIndex is null ? 0 : 1,
                source.OrderIndex ?? 0,
                source.TruePeakOversampling is null ? 0 : 1,
                (int)(source.TruePeakOversampling ?? 0),
                source.WaveformDisplayMode is null ? 0 : 1,
                (int)(source.WaveformDisplayMode ?? 0),
                source.SpectrumDisplayMode is null ? 0 : 1,
                (int)(source.SpectrumDisplayMode ?? 0),
                source.InlineStereoVisible is null ? 0 : 1,
                source.InlineStereoVisible == true ? 1 : 0,
                source.InlineWaveformVisible is null ? 0 : 1,
                source.InlineWaveformVisible == true ? 1 : 0,
                source.InlineSpectrumVisible is null ? 0 : 1,
                source.InlineSpectrumVisible == true ? 1 : 0,
                source.InlineLoudnessClockVisible is null ? 0 : 1,
                source.InlineLoudnessClockVisible == true ? 1 : 0,
                source.InlineCollisionVisible is null ? 0 : 1,
                source.InlineCollisionVisible == true ? 1 : 0,
                source.InlineCollisionSourceKey,
                source.CollisionLevelFloorDb is null ? 0 : 1,
                source.CollisionLevelFloorDb ?? 0,
                source.CollisionProminenceThresholdDb is null ? 0 : 1,
                source.CollisionProminenceThresholdDb ?? 0,
                source.CollisionRelativeThreshold is null ? 0 : 1,
                source.CollisionRelativeThreshold ?? 0,
                source.CollisionFrequencyScale is null ? 0 : 1,
                (int)(source.CollisionFrequencyScale ?? 0),
                source.SpectrumDisplayFloorDb is null ? 0 : 1,
                source.SpectrumDisplayFloorDb ?? 0,
                source.SpectrumDisplayCeilingDb is null ? 0 : 1,
                source.SpectrumDisplayCeilingDb ?? 0,
                source.LoudnessClockMinimumDb is null ? 0 : 1,
                source.LoudnessClockMinimumDb ?? 0,
                source.LoudnessClockMaximumDb is null ? 0 : 1,
                source.LoudnessClockMaximumDb ?? 0,
                source.LoudnessClockWarningDb is null ? 0 : 1,
                source.LoudnessClockWarningDb ?? 0,
                source.LoudnessClockCycleSeconds is null ? 0 : 1,
                source.LoudnessClockCycleSeconds ?? 0),
            "Native snapshot chrome source settings projection failed");
    }

    private HostSettingsSnapshot ReadSettings()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSnapshotChromeProjectionGetSettingsExport(_handle, out var settings),
            "Native snapshot chrome settings read failed");
        ThrowIfFailed(
            NativeMethods.DlmHostSnapshotChromeProjectionGetSourceSettingsCountExport(_handle, out var count),
            "Native snapshot chrome source settings count failed");

        var sources = new HostSourceSettingsSnapshot[count];
        for (var index = 0; index < count; index++)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSnapshotChromeProjectionGetSourceSettingsAtExport(_handle, index, out var row),
                "Native snapshot chrome source settings row failed");
            sources[index] = new HostSourceSettingsSnapshot(
                CopySourceString(index, SourceStringSourceKey, row.SourceKeyUtf8ByteCount),
                row.HasAlias == 0 ? null : CopySourceString(index, SourceStringAlias, row.AliasUtf8ByteCount),
                row.IsHidden != 0,
                row.HasOrderIndex == 0 ? null : row.OrderIndex,
                row.HasTruePeakOversampling == 0 ? null : ToTruePeakOversampling(row.TruePeakOversampling),
                row.HasWaveformDisplayMode == 0 ? null : ToWaveformDisplayMode(row.WaveformDisplayMode),
                row.HasSpectrumDisplayMode == 0 ? null : ToSpectrumDisplayMode(row.SpectrumDisplayMode),
                ToNullableBool(row.HasInlineStereoVisible, row.InlineStereoVisible),
                ToNullableBool(row.HasInlineWaveformVisible, row.InlineWaveformVisible),
                ToNullableBool(row.HasInlineSpectrumVisible, row.InlineSpectrumVisible),
                ToNullableBool(row.HasInlineLoudnessClockVisible, row.InlineLoudnessClockVisible),
                ToNullableBool(row.HasInlineCollisionVisible, row.InlineCollisionVisible),
                row.HasInlineCollisionSourceKey == 0 ? null : CopySourceString(index, SourceStringInlineCollisionSourceKey, row.InlineCollisionSourceKeyUtf8ByteCount),
                row.HasCollisionLevelFloorDb == 0 ? null : row.CollisionLevelFloorDb,
                row.HasCollisionProminenceThresholdDb == 0 ? null : row.CollisionProminenceThresholdDb,
                row.HasCollisionRelativeThreshold == 0 ? null : row.CollisionRelativeThreshold,
                row.HasCollisionFrequencyScale == 0 ? null : ToCollisionFrequencyScale(row.CollisionFrequencyScale),
                row.HasSpectrumDisplayFloorDb == 0 ? null : row.SpectrumDisplayFloorDb,
                row.HasSpectrumDisplayCeilingDb == 0 ? null : row.SpectrumDisplayCeilingDb,
                row.HasLoudnessClockMinimumDb == 0 ? null : row.LoudnessClockMinimumDb,
                row.HasLoudnessClockMaximumDb == 0 ? null : row.LoudnessClockMaximumDb,
                row.HasLoudnessClockWarningDb == 0 ? null : row.LoudnessClockWarningDb,
                row.HasLoudnessClockCycleSeconds == 0 ? null : row.LoudnessClockCycleSeconds);
        }

        return new HostSettingsSnapshot(
            settings.SchemaVersion,
            settings.HasThemeId == 0 ? null : CopySettingsString(SettingsStringThemeId, settings.ThemeIdUtf8ByteCount),
            settings.HasMaxRecentEventEntries == 0 ? null : settings.MaxRecentEventEntries,
            settings.HasEventOutputFolder == 0 ? null : CopySettingsString(SettingsStringEventOutputFolder, settings.EventOutputFolderUtf8ByteCount),
            settings.LoadDefaultDetailLayoutFromFile != 0,
            settings.HasDefaultDetailLayoutFile == 0 ? null : CopySettingsString(SettingsStringDefaultDetailLayoutFile, settings.DefaultDetailLayoutFileUtf8ByteCount),
            settings.ShowHiddenSources != 0,
            sources);
    }

    private HostCommandAvailability ReadCommands()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSnapshotChromeProjectionGetCommandAvailabilityExport(_handle, out var commands),
            "Native snapshot chrome command availability read failed");
        return new HostCommandAvailability(
            commands.CanBackToGlobal != 0,
            commands.CanResetAll != 0,
            commands.CanForgetSelectedSource != 0,
            commands.CanMoveSelectedSourceUp != 0,
            commands.CanMoveSelectedSourceDown != 0);
    }

    private string CopySettingsString(int field, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostSnapshotChromeProjectionCopySettingsStringExport(_handle, field, buffer, buffer.Length, out var actualCount),
            "Native snapshot chrome settings string copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private string CopySourceString(int index, int field, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostSnapshotChromeProjectionCopySourceSettingsStringAtExport(_handle, index, field, buffer, buffer.Length, out var actualCount),
            "Native snapshot chrome source settings string copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private static bool? ToNullableBool(int hasValue, int value)
    {
        return hasValue == 0 ? null : value != 0;
    }

    private static TruePeakOversampling ToTruePeakOversampling(int value)
    {
        return Enum.IsDefined(typeof(TruePeakOversampling), value)
            ? (TruePeakOversampling)value
            : TruePeakOversampling.FourX;
    }

    private static WaveformDisplayMode ToWaveformDisplayMode(int value)
    {
        return Enum.IsDefined(typeof(WaveformDisplayMode), value)
            ? (WaveformDisplayMode)value
            : WaveformDisplayMode.Overall;
    }

    private static SpectrumDisplayMode ToSpectrumDisplayMode(int value)
    {
        return Enum.IsDefined(typeof(SpectrumDisplayMode), value)
            ? (SpectrumDisplayMode)value
            : SpectrumDisplayMode.Overall;
    }

    private static CollisionFrequencyScale ToCollisionFrequencyScale(int value)
    {
        return Enum.IsDefined(typeof(CollisionFrequencyScale), value)
            ? (CollisionFrequencyScale)value
            : CollisionFrequencyScale.Logarithmic;
    }

    private static void ThrowIfFailed(int result, string message)
    {
        if (result != 0)
        {
            throw new InvalidOperationException($"{message} with code {result}.");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSettings
    {
        public int SchemaVersion;
        public int HasThemeId;
        public int HasMaxRecentEventEntries;
        public int MaxRecentEventEntries;
        public int HasEventOutputFolder;
        public int LoadDefaultDetailLayoutFromFile;
        public int HasDefaultDetailLayoutFile;
        public int ShowHiddenSources;
        public int ThemeIdUtf8ByteCount;
        public int EventOutputFolderUtf8ByteCount;
        public int DefaultDetailLayoutFileUtf8ByteCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSourceSettingsRow
    {
        public int SourceKeyUtf8ByteCount;
        public int HasAlias;
        public int AliasUtf8ByteCount;
        public int IsHidden;
        public int HasOrderIndex;
        public int OrderIndex;
        public int HasTruePeakOversampling;
        public int TruePeakOversampling;
        public int HasWaveformDisplayMode;
        public int WaveformDisplayMode;
        public int HasSpectrumDisplayMode;
        public int SpectrumDisplayMode;
        public int HasInlineStereoVisible;
        public int InlineStereoVisible;
        public int HasInlineWaveformVisible;
        public int InlineWaveformVisible;
        public int HasInlineSpectrumVisible;
        public int InlineSpectrumVisible;
        public int HasInlineLoudnessClockVisible;
        public int InlineLoudnessClockVisible;
        public int HasInlineCollisionVisible;
        public int InlineCollisionVisible;
        public int HasInlineCollisionSourceKey;
        public int InlineCollisionSourceKeyUtf8ByteCount;
        public int HasCollisionLevelFloorDb;
        public double CollisionLevelFloorDb;
        public int HasCollisionProminenceThresholdDb;
        public double CollisionProminenceThresholdDb;
        public int HasCollisionRelativeThreshold;
        public double CollisionRelativeThreshold;
        public int HasCollisionFrequencyScale;
        public int CollisionFrequencyScale;
        public int HasSpectrumDisplayFloorDb;
        public double SpectrumDisplayFloorDb;
        public int HasSpectrumDisplayCeilingDb;
        public double SpectrumDisplayCeilingDb;
        public int HasLoudnessClockMinimumDb;
        public double LoudnessClockMinimumDb;
        public int HasLoudnessClockMaximumDb;
        public double LoudnessClockMaximumDb;
        public int HasLoudnessClockWarningDb;
        public double LoudnessClockWarningDb;
        public int HasLoudnessClockCycleSeconds;
        public double LoudnessClockCycleSeconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeCommandAvailability
    {
        public int CanBackToGlobal;
        public int CanResetAll;
        public int CanForgetSelectedSource;
        public int CanMoveSelectedSourceUp;
        public int CanMoveSelectedSourceDown;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostSnapshotChromeProjectionDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionClearExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionClearExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionSetGlobalSettingsExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionSetGlobalSettingsExport(
            IntPtr handle,
            int schemaVersion,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? themeId,
            int hasMaxRecentEventEntries,
            int maxRecentEventEntries,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? eventOutputFolder,
            int loadDefaultDetailLayoutFromFile,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? defaultDetailLayoutFile,
            int showHiddenSources);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionAddSourceSettingsExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionAddSourceSettingsExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? alias,
            int isHidden,
            int hasOrderIndex,
            int orderIndex,
            int hasTruePeakOversampling,
            int truePeakOversampling,
            int hasWaveformDisplayMode,
            int waveformDisplayMode,
            int hasSpectrumDisplayMode,
            int spectrumDisplayMode,
            int hasInlineStereoVisible,
            int inlineStereoVisible,
            int hasInlineWaveformVisible,
            int inlineWaveformVisible,
            int hasInlineSpectrumVisible,
            int inlineSpectrumVisible,
            int hasInlineLoudnessClockVisible,
            int inlineLoudnessClockVisible,
            int hasInlineCollisionVisible,
            int inlineCollisionVisible,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? inlineCollisionSourceKey,
            int hasCollisionLevelFloorDb,
            double collisionLevelFloorDb,
            int hasCollisionProminenceThresholdDb,
            double collisionProminenceThresholdDb,
            int hasCollisionRelativeThreshold,
            double collisionRelativeThreshold,
            int hasCollisionFrequencyScale,
            int collisionFrequencyScale,
            int hasSpectrumDisplayFloorDb,
            double spectrumDisplayFloorDb,
            int hasSpectrumDisplayCeilingDb,
            double spectrumDisplayCeilingDb,
            int hasLoudnessClockMinimumDb,
            double loudnessClockMinimumDb,
            int hasLoudnessClockMaximumDb,
            double loudnessClockMaximumDb,
            int hasLoudnessClockWarningDb,
            double loudnessClockWarningDb,
            int hasLoudnessClockCycleSeconds,
            double loudnessClockCycleSeconds);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionProjectExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionProjectExport(
            IntPtr handle,
            int hasSelectedSourceIndex,
            int selectedSourceIndex,
            int sourceCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionGetSettingsExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionGetSettingsExport(IntPtr handle, out NativeSettings settings);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionGetSourceSettingsCountExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionGetSourceSettingsCountExport(IntPtr handle, out int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionGetSourceSettingsAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionGetSourceSettingsAtExport(IntPtr handle, int index, out NativeSourceSettingsRow row);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionGetCommandAvailabilityExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionGetCommandAvailabilityExport(IntPtr handle, out NativeCommandAvailability commands);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionCopySettingsStringExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionCopySettingsStringExport(
            IntPtr handle,
            int field,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSnapshotChromeProjectionCopySourceSettingsStringAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSnapshotChromeProjectionCopySourceSettingsStringAtExport(
            IntPtr handle,
            int index,
            int field,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostSnapshotChromeProjectionResult(
    HostSettingsSnapshot Settings,
    HostCommandAvailability Commands);
