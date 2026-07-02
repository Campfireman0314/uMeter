using System.Runtime.InteropServices;
using System.Text;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Audio.Analysis;

internal sealed class NativeHostSelectedDetailSnapshotPlanner : IDisposable
{
    private const int StringFieldComparisonSourceKey = 0;
    private const int StringFieldComparisonDisplayTitle = 1;
    private const int ModuleStringFieldTitle = 0;
    private IntPtr _handle;
    private bool _disposed;

    public NativeHostSelectedDetailSnapshotPlanner()
    {
        var result = NativeMethods.DlmHostSelectedDetailSnapshotCreateExport(out _handle);
        if (result != 0 || _handle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Native host selected-detail snapshot planner could not be initialized (code {result}).");
        }
    }

    public static NativeHostSelectedDetailSnapshotPlan Project(
        IReadOnlyList<NativeHostSelectedDetailSnapshotSourceInput> sources,
        IReadOnlyList<NativeHostDetailModuleLayoutInput> layouts,
        NativeHostDetailModuleVisibility visibility,
        string? selectedSourceKey,
        string? comparisonSourceKey,
        double? levelFloorDb,
        double? prominenceThresholdDb,
        double? relativeThreshold,
        CollisionFrequencyScale? frequencyScale)
    {
        using var planner = new NativeHostSelectedDetailSnapshotPlanner();
        return planner.ProjectSnapshot(
            sources,
            layouts,
            visibility,
            selectedSourceKey,
            comparisonSourceKey,
            levelFloorDb,
            prominenceThresholdDb,
            relativeThreshold,
            frequencyScale);
    }

    public NativeHostSelectedDetailSnapshotPlan ProjectSnapshot(
        IReadOnlyList<NativeHostSelectedDetailSnapshotSourceInput> sources,
        IReadOnlyList<NativeHostDetailModuleLayoutInput> layouts,
        NativeHostDetailModuleVisibility visibility,
        string? selectedSourceKey,
        string? comparisonSourceKey,
        double? levelFloorDb,
        double? prominenceThresholdDb,
        double? relativeThreshold,
        CollisionFrequencyScale? frequencyScale)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfFailed(NativeMethods.DlmHostSelectedDetailSnapshotClearExport(_handle), "Native selected-detail snapshot clear failed");

        foreach (var source in sources)
        {
            var spectrum = ToArray(source.SpectrumBuckets);
            ThrowIfFailed(
                NativeMethods.DlmHostSelectedDetailSnapshotAddSourceExport(
                    _handle,
                    source.SourceKey,
                    source.DisplayTitle,
                    spectrum,
                    spectrum.Length),
                "Native selected-detail snapshot add-source failed");
        }

        foreach (var layout in layouts)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSelectedDetailSnapshotAddModuleLayoutExport(
                    _handle,
                    (int)layout.Kind,
                    layout.OrderIndex,
                    layout.Height is null ? 0 : 1,
                    layout.Height ?? 0),
                "Native selected-detail snapshot add-module-layout failed");
        }

        ThrowIfFailed(
            NativeMethods.DlmHostSelectedDetailSnapshotProjectExport(
                _handle,
                selectedSourceKey,
                comparisonSourceKey,
                visibility.Stereo is null ? 0 : 1,
                visibility.Stereo == true ? 1 : 0,
                visibility.Collision is null ? 0 : 1,
                visibility.Collision == true ? 1 : 0,
                visibility.LoudnessClock is null ? 0 : 1,
                visibility.LoudnessClock == true ? 1 : 0,
                visibility.Waveform is null ? 0 : 1,
                visibility.Waveform == true ? 1 : 0,
                visibility.Spectrum is null ? 0 : 1,
                visibility.Spectrum == true ? 1 : 0,
                levelFloorDb.HasValue ? 1 : 0,
                levelFloorDb.GetValueOrDefault(),
                prominenceThresholdDb.HasValue ? 1 : 0,
                prominenceThresholdDb.GetValueOrDefault(),
                relativeThreshold.HasValue ? 1 : 0,
                relativeThreshold.GetValueOrDefault(),
                frequencyScale.HasValue ? 1 : 0,
                (int)(frequencyScale ?? CollisionFrequencyScale.Logarithmic)),
            "Native selected-detail snapshot project failed");

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
            NativeMethods.DlmHostSelectedDetailSnapshotDestroyExport(_handle);
        }

        _handle = IntPtr.Zero;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private NativeHostSelectedDetailSnapshotPlan ReadPlan()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedDetailSnapshotGetPlanExport(_handle, out var plan),
            "Native selected-detail snapshot plan read failed");

        var modules = ReadModules();
        var bands = ReadBands();
        var collision = new NativeHostSelectedDetailCollisionPlan(
            plan.ComparisonSourceKeyUtf8ByteCount <= 0 ? null : CopyString(StringFieldComparisonSourceKey, plan.ComparisonSourceKeyUtf8ByteCount),
            plan.ComparisonDisplayTitleUtf8ByteCount <= 0 ? null : CopyString(StringFieldComparisonDisplayTitle, plan.ComparisonDisplayTitleUtf8ByteCount),
            plan.LevelFloorDb,
            plan.ProminenceThresholdDb,
            plan.RelativeThreshold,
            ToFrequencyScale(plan.FrequencyScale),
            bands);

        return new NativeHostSelectedDetailSnapshotPlan(
            plan.HasSelectedSource != 0,
            plan.HasSelectedSource == 0 ? null : plan.SelectedSourceIndex,
            modules,
            plan.IsWaveformVisible != 0,
            plan.IsSpectrumVisible != 0,
            plan.IsStereoVisible != 0,
            plan.IsLoudnessClockVisible != 0,
            plan.IsCollisionVisible != 0,
            collision);
    }

    private NativeHostSelectedDetailModulePlanRow[] ReadModules()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedDetailSnapshotGetModuleCountExport(_handle, out var count),
            "Native selected-detail snapshot module count read failed");

        var modules = new NativeHostSelectedDetailModulePlanRow[count];
        for (var index = 0; index < count; index++)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSelectedDetailSnapshotGetModuleAtExport(_handle, index, out var row),
                "Native selected-detail snapshot module read failed");
            modules[index] = new NativeHostSelectedDetailModulePlanRow(
                ToDetailModuleKind(row.Kind),
                CopyModuleString(index, ModuleStringFieldTitle, row.TitleUtf8ByteCount),
                row.OrderIndex,
                row.IsVisible != 0,
                row.Height);
        }

        return modules;
    }

    private FrequencyCollisionBand[] ReadBands()
    {
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedDetailSnapshotGetBandCountExport(_handle, out var count),
            "Native selected-detail snapshot band count read failed");

        var bands = new FrequencyCollisionBand[count];
        for (var index = 0; index < count; index++)
        {
            ThrowIfFailed(
                NativeMethods.DlmHostSelectedDetailSnapshotGetBandAtExport(_handle, index, out var band),
                "Native selected-detail snapshot band read failed");
            bands[index] = new FrequencyCollisionBand(band.StartRatio, band.EndRatio, band.Intensity);
        }

        return bands;
    }

    private string CopyString(int field, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedDetailSnapshotCopyStringExport(_handle, field, buffer, buffer.Length, out var actualCount),
            "Native selected-detail snapshot string copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private string CopyModuleString(int index, int field, int count)
    {
        if (count <= 0)
        {
            return string.Empty;
        }

        var buffer = new byte[count];
        ThrowIfFailed(
            NativeMethods.DlmHostSelectedDetailSnapshotCopyModuleStringAtExport(_handle, index, field, buffer, buffer.Length, out var actualCount),
            "Native selected-detail snapshot module string copy failed");
        return Encoding.UTF8.GetString(buffer, 0, actualCount);
    }

    private static double[] ToArray(IReadOnlyList<double>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        if (values is double[] array)
        {
            return array;
        }

        return values.ToArray();
    }

    private static DetailModuleKind ToDetailModuleKind(int value)
    {
        return Enum.IsDefined(typeof(DetailModuleKind), value)
            ? (DetailModuleKind)value
            : DetailModuleKind.Stereo;
    }

    private static CollisionFrequencyScale ToFrequencyScale(int value)
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
    private struct NativeSelectedDetailSnapshotPlan
    {
        public int HasSelectedSource;
        public int SelectedSourceIndex;
        public int IsWaveformVisible;
        public int IsSpectrumVisible;
        public int IsStereoVisible;
        public int IsLoudnessClockVisible;
        public int IsCollisionVisible;
        public int ComparisonSourceKeyUtf8ByteCount;
        public int ComparisonDisplayTitleUtf8ByteCount;
        public double LevelFloorDb;
        public double ProminenceThresholdDb;
        public double RelativeThreshold;
        public int FrequencyScale;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeModuleRow
    {
        public int Kind;
        public int OrderIndex;
        public int IsVisible;
        public double Height;
        public int TitleUtf8ByteCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBand
    {
        public double StartRatio;
        public double EndRatio;
        public double Intensity;
    }

    private static partial class NativeMethods
    {
        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotCreateExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotCreateExport(out IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotDestroyExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DlmHostSelectedDetailSnapshotDestroyExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotClearExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotClearExport(IntPtr handle);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotAddSourceExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotAddSourceExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string sourceKey,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string displayTitle,
            [In] double[] spectrumValues,
            int spectrumCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotAddModuleLayoutExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotAddModuleLayoutExport(
            IntPtr handle,
            int kind,
            int orderIndex,
            int hasHeight,
            double height);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotProjectExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotProjectExport(
            IntPtr handle,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? selectedSourceKey,
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            string? comparisonSourceKey,
            int hasStereoVisible,
            int stereoVisible,
            int hasCollisionVisible,
            int collisionVisible,
            int hasLoudnessClockVisible,
            int loudnessClockVisible,
            int hasWaveformVisible,
            int waveformVisible,
            int hasSpectrumVisible,
            int spectrumVisible,
            int hasLevelFloorDb,
            double levelFloorDb,
            int hasProminenceThresholdDb,
            double prominenceThresholdDb,
            int hasRelativeThreshold,
            double relativeThreshold,
            int hasFrequencyScale,
            int frequencyScale);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotGetPlanExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotGetPlanExport(
            IntPtr handle,
            out NativeSelectedDetailSnapshotPlan plan);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotGetModuleCountExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotGetModuleCountExport(IntPtr handle, out int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotGetModuleAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotGetModuleAtExport(IntPtr handle, int index, out NativeModuleRow row);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotCopyModuleStringAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotCopyModuleStringAtExport(
            IntPtr handle,
            int index,
            int field,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotGetBandCountExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotGetBandCountExport(IntPtr handle, out int count);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotGetBandAtExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotGetBandAtExport(IntPtr handle, int index, out NativeBand band);

        [DllImport("DesktopLoudnessMeter.Native", EntryPoint = "DlmHostSelectedDetailSnapshotCopyStringExport", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int DlmHostSelectedDetailSnapshotCopyStringExport(
            IntPtr handle,
            int field,
            [Out] byte[] buffer,
            int capacity,
            out int actualByteCount);
    }
}

internal sealed record NativeHostSelectedDetailSnapshotSourceInput(
    string SourceKey,
    string DisplayTitle,
    IReadOnlyList<double>? SpectrumBuckets);

internal sealed record NativeHostSelectedDetailSnapshotPlan(
    bool HasSelectedSource,
    int? SelectedSourceIndex,
    IReadOnlyList<NativeHostSelectedDetailModulePlanRow> Modules,
    bool IsWaveformVisible,
    bool IsSpectrumVisible,
    bool IsStereoVisible,
    bool IsLoudnessClockVisible,
    bool IsCollisionVisible,
    NativeHostSelectedDetailCollisionPlan Collision);

internal sealed record NativeHostSelectedDetailModulePlanRow(
    DetailModuleKind Kind,
    string Title,
    int OrderIndex,
    bool IsVisible,
    double Height);

internal sealed record NativeHostSelectedDetailCollisionPlan(
    string? ComparisonSourceKey,
    string? ComparisonDisplayName,
    double LevelFloorDb,
    double ProminenceThresholdDb,
    double RelativeThreshold,
    CollisionFrequencyScale FrequencyScale,
    IReadOnlyList<FrequencyCollisionBand> Bands);
