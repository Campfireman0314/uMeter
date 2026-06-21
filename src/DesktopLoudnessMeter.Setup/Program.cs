using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DesktopLoudnessMeter.Setup;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var options = InstallerOptions.FromArgs(args);
        if (!options.Quiet)
        {
            using var form = new InstallerForm(options);
            return form.ShowDialog() == DialogResult.OK ? 0 : 1;
        }

        try
        {
            if (Installer.RequiresElevation(options))
            {
                using var elevatedProcess = Installer.RestartElevated(options);
                elevatedProcess.WaitForExit();
                return elevatedProcess.ExitCode;
            }

            Installer.Install(options);
            return 0;
        }
        catch (Exception ex)
        {
            Installer.WriteFailureLog(ex);
            return 1;
        }
    }
}

internal sealed record InstallerOptions(
    string InstallRoot,
    bool CreateDesktopShortcut,
    bool CreateStartMenuShortcut,
    bool InstallVstPlugin,
    string VstInstallRoot,
    bool Quiet)
{
    public const string DefaultVstInstallRoot = @"C:\Program Files\VSTPlugins";

    public static InstallerOptions FromArgs(string[] args)
    {
        var options = Default();
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (IsAny(arg, "--quiet", "/quiet", "/silent"))
            {
                options = options with { Quiet = true };
            }
            else if (TryReadValueArg(args, ref index, "--install-dir", "/installDir", out var installRoot))
            {
                options = options with { InstallRoot = installRoot };
            }
            else if (TryReadValueArg(args, ref index, "--vst-dir", "/vstDir", out var vstInstallRoot))
            {
                options = options with
                {
                    InstallVstPlugin = true,
                    VstInstallRoot = vstInstallRoot
                };
            }
            else if (IsAny(arg, "--no-vst", "/noVst"))
            {
                options = options with { InstallVstPlugin = false };
            }
            else if (IsAny(arg, "--no-desktop-shortcut", "/noDesktopShortcut"))
            {
                options = options with { CreateDesktopShortcut = false };
            }
            else if (IsAny(arg, "--no-start-menu-shortcut", "/noStartMenuShortcut"))
            {
                options = options with { CreateStartMenuShortcut = false };
            }
            else if (IsAny(arg, "--no-shortcuts", "/noShortcuts"))
            {
                options = options with
                {
                    CreateDesktopShortcut = false,
                    CreateStartMenuShortcut = false
                };
            }
        }

        return options;
    }

    public static InstallerOptions Default()
    {
        return new InstallerOptions(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "uMeter"),
            CreateDesktopShortcut: true,
            CreateStartMenuShortcut: true,
            InstallVstPlugin: true,
            VstInstallRoot: DefaultVstInstallRoot,
            Quiet: false);
    }

    private static bool TryReadValueArg(
        string[] args,
        ref int index,
        string longName,
        string slashName,
        out string value)
    {
        var arg = args[index];
        foreach (var name in new[] { longName, slashName })
        {
            if (arg.StartsWith($"{name}=", StringComparison.OrdinalIgnoreCase))
            {
                value = arg[(name.Length + 1)..];
                return true;
            }
        }

        if (IsAny(arg, longName, slashName) && index + 1 < args.Length)
        {
            index++;
            value = args[index];
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool IsAny(string value, params string[] options)
    {
        return options.Any(option => string.Equals(value, option, StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class InstallerForm : Form
{
    private readonly TextBox _installPathTextBox = new();
    private readonly TextBox _vstPathTextBox = new();
    private readonly CheckBox _desktopShortcutCheckBox = new();
    private readonly CheckBox _startMenuShortcutCheckBox = new();
    private readonly CheckBox _vstPluginCheckBox = new();
    private readonly Button _browseButton = new();
    private readonly Button _vstBrowseButton = new();
    private readonly Button _installButton = new();
    private readonly Button _cancelButton = new();
    private readonly Label _statusLabel = new();

    public InstallerForm(InstallerOptions options)
    {
        Text = "Install uMeter";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(640, 450);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        BackColor = SystemColors.Window;

        BuildLayout(options);
    }

    private InstallerOptions Options => new(
        _installPathTextBox.Text.Trim(),
        _desktopShortcutCheckBox.Checked,
        _startMenuShortcutCheckBox.Checked,
        _vstPluginCheckBox.Checked,
        _vstPathTextBox.Text.Trim(),
        Quiet: false);

    private void BuildLayout(InstallerOptions options)
    {
        var title = new Label
        {
            AutoSize = true,
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold, GraphicsUnit.Point),
            Location = new Point(24, 22),
            Text = "uMeter Setup"
        };
        Controls.Add(title);

        var description = new Label
        {
            Location = new Point(24, 58),
            Size = new Size(585, 42),
            Text = "Choose where to install the app, shortcuts, and the optional VST3 sender plugin. Installing the plugin under Program Files may request administrator permission."
        };
        Controls.Add(description);

        var pathLabel = new Label
        {
            AutoSize = true,
            Location = new Point(24, 114),
            Text = "App install folder"
        };
        Controls.Add(pathLabel);

        _installPathTextBox.Location = new Point(24, 140);
        _installPathTextBox.Size = new Size(470, 23);
        _installPathTextBox.Text = options.InstallRoot;
        _installPathTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        Controls.Add(_installPathTextBox);

        _browseButton.Location = new Point(506, 139);
        _browseButton.Size = new Size(102, 27);
        _browseButton.Text = "Browse...";
        _browseButton.Click += BrowseButton_Click;
        _browseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Controls.Add(_browseButton);

        var optionsGroup = new GroupBox
        {
            Location = new Point(24, 186),
            Size = new Size(584, 82),
            Text = "Shortcuts"
        };
        Controls.Add(optionsGroup);

        _desktopShortcutCheckBox.Location = new Point(16, 24);
        _desktopShortcutCheckBox.Size = new Size(250, 20);
        _desktopShortcutCheckBox.Checked = options.CreateDesktopShortcut;
        _desktopShortcutCheckBox.Text = "Create Desktop shortcut";
        optionsGroup.Controls.Add(_desktopShortcutCheckBox);

        _startMenuShortcutCheckBox.Location = new Point(16, 50);
        _startMenuShortcutCheckBox.Size = new Size(250, 20);
        _startMenuShortcutCheckBox.Checked = options.CreateStartMenuShortcut;
        _startMenuShortcutCheckBox.Text = "Create Start Menu shortcut";
        optionsGroup.Controls.Add(_startMenuShortcutCheckBox);

        var vstGroup = new GroupBox
        {
            Location = new Point(24, 282),
            Size = new Size(584, 82),
            Text = "VST3 sender"
        };
        Controls.Add(vstGroup);

        _vstPluginCheckBox.Location = new Point(16, 24);
        _vstPluginCheckBox.Size = new Size(260, 20);
        _vstPluginCheckBox.Checked = options.InstallVstPlugin;
        _vstPluginCheckBox.Text = "Install uMeter Sender VST3";
        _vstPluginCheckBox.CheckedChanged += (_, _) => UpdateVstControls();
        vstGroup.Controls.Add(_vstPluginCheckBox);

        _vstPathTextBox.Location = new Point(16, 48);
        _vstPathTextBox.Size = new Size(438, 23);
        _vstPathTextBox.Text = options.VstInstallRoot;
        vstGroup.Controls.Add(_vstPathTextBox);

        _vstBrowseButton.Location = new Point(466, 47);
        _vstBrowseButton.Size = new Size(102, 27);
        _vstBrowseButton.Text = "Browse...";
        _vstBrowseButton.Click += VstBrowseButton_Click;
        vstGroup.Controls.Add(_vstBrowseButton);

        var separator = new Panel
        {
            BackColor = SystemColors.ControlLight,
            Location = new Point(24, 384),
            Size = new Size(584, 1)
        };
        Controls.Add(separator);

        _statusLabel.Location = new Point(24, 405);
        _statusLabel.Size = new Size(370, 28);
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        Controls.Add(_statusLabel);

        _installButton.Location = new Point(406, 402);
        _installButton.Size = new Size(94, 30);
        _installButton.Text = "Install";
        _installButton.Click += InstallButton_Click;
        Controls.Add(_installButton);

        _cancelButton.Location = new Point(514, 402);
        _cancelButton.Size = new Size(94, 30);
        _cancelButton.Text = "Cancel";
        _cancelButton.Click += (_, _) => Close();
        Controls.Add(_cancelButton);

        UpdateVstControls();
        AcceptButton = _installButton;
        CancelButton = _cancelButton;
    }

    private void BrowseButton_Click(object? sender, EventArgs args)
    {
        BrowseFolder(_installPathTextBox, "Choose the app install folder for uMeter");
    }

    private void VstBrowseButton_Click(object? sender, EventArgs args)
    {
        BrowseFolder(_vstPathTextBox, "Choose the VST3 folder for uMeter Sender");
    }

    private void BrowseFolder(TextBox targetTextBox, string description)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = description,
            SelectedPath = targetTextBox.Text,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            targetTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void InstallButton_Click(object? sender, EventArgs args)
    {
        var options = Options;

        try
        {
            if (Installer.RequiresElevation(options))
            {
                var result = MessageBox.Show(
                    this,
                    "Installing the VST3 sender to the selected folder needs administrator permission. Continue?",
                    Text,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                SetInstalling(true);
                _statusLabel.Text = "Requesting administrator permission...";
                Refresh();

                using var elevatedProcess = Installer.RestartElevated(options);
                _statusLabel.Text = "Installing with administrator permission...";
                Refresh();

                await System.Threading.Tasks.Task.Run(() => elevatedProcess.WaitForExit());

                if (elevatedProcess.ExitCode == 0)
                {
                    _statusLabel.Text = "Installed.";
                    MessageBox.Show(this, "uMeter has been installed.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    _statusLabel.Text = "Install failed.";
                    MessageBox.Show(
                        this,
                        $"The elevated installer did not finish successfully.\n\nLog: {Installer.FailureLogPath}",
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    SetInstalling(false);
                }

                return;
            }

            SetInstalling(true);
            _statusLabel.Text = "Installing...";
            Refresh();
            Installer.Install(options);
            _statusLabel.Text = "Installed.";
            MessageBox.Show(this, "uMeter has been installed.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            _statusLabel.Text = "Elevation canceled.";
            SetInstalling(false);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Install failed.";
            MessageBox.Show(this, $"Install failed:\n\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetInstalling(false);
        }
    }

    private void SetInstalling(bool installing)
    {
        _installButton.Enabled = !installing;
        _cancelButton.Enabled = !installing;
        _browseButton.Enabled = !installing;
        _installPathTextBox.Enabled = !installing;
        _desktopShortcutCheckBox.Enabled = !installing;
        _startMenuShortcutCheckBox.Enabled = !installing;
        _vstPluginCheckBox.Enabled = !installing;
        UpdateVstControls();
    }

    private void UpdateVstControls()
    {
        var enabled = _vstPluginCheckBox.Enabled && _vstPluginCheckBox.Checked;
        _vstPathTextBox.Enabled = enabled;
        _vstBrowseButton.Enabled = enabled;
    }
}

internal static class Installer
{
    private const string AppName = "uMeter";
    private const string AppExeName = "uMeter.exe";
    private const string AppProcessName = "uMeter";
    private const string PayloadResourceName = "uMeter.Payload.zip";
    private const string Publisher = "Campfireman Studio";
    private const string Version = "2.2.7";
    private const string InstallMarkerName = ".umeter-install";
    private const string LegacyInstallMarkerName = ".desktop-loudness-meter-install";
    private const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\uMeter";
    private const string LegacyAppName = "Desktop Loudness Meter";
    private const string LegacyAppExeName = "DesktopLoudnessMeter.App.exe";
    private const string LegacyAppProcessName = "DesktopLoudnessMeter.App";
    private const string LegacyUninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\DesktopLoudnessMeter";
    private const string VstPluginDirectoryName = "Plugins";
    private const string Vst3DirectoryName = "VST3";
    private const string VstBundleName = "uMeter Sender.vst3";
    private const string LegacyVstBundleName = "Desktop Loudness Meter Sender.vst3";

    public static string FailureLogPath => Path.Combine(Path.GetTempPath(), "uMeter.Setup.log");

    public static bool RequiresElevation(InstallerOptions options)
    {
        if (IsAdministrator())
        {
            return false;
        }

        var installRoot = NormalizeInstallRoot(options.InstallRoot);
        if (IsProtectedPath(installRoot))
        {
            return true;
        }

        if (!options.InstallVstPlugin)
        {
            return false;
        }

        var vstInstallRoot = NormalizeVstInstallRoot(options.VstInstallRoot);
        return GetVstInstallRoots(vstInstallRoot).Any(IsProtectedPath)
            || GetVstCleanupRoots(vstInstallRoot)
                .Any(root => IsProtectedPath(root) && Directory.Exists(Path.Combine(root, LegacyVstBundleName)));
    }

    public static Process RestartElevated(InstallerOptions options)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Application.ExecutablePath,
            Arguments = BuildQuietArguments(options),
            UseShellExecute = true,
            Verb = "runas"
        };

        var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Could not start the elevated installer.");
        }

        return process;
    }

    public static void Install(InstallerOptions options)
    {
        if (RequiresElevation(options))
        {
            throw new InvalidOperationException("Installing the VST3 sender to the selected folder needs administrator permission.");
        }

        var installRoot = NormalizeInstallRoot(options.InstallRoot);
        var vstInstallRoot = options.InstallVstPlugin
            ? NormalizeVstInstallRoot(options.VstInstallRoot)
            : string.Empty;
        var targetExe = Path.Combine(installRoot, AppExeName);
        var tempRoot = Path.Combine(Path.GetTempPath(), $"uMeterSetup-{Guid.NewGuid():N}");
        var tempZip = Path.Combine(tempRoot, "payload.zip");
        var tempExtract = Path.Combine(tempRoot, "app");

        try
        {
            Directory.CreateDirectory(tempRoot);
            ExtractPayloadResource(tempZip);
            ZipFile.ExtractToDirectory(tempZip, tempExtract);

            StopRunningApp();
            ReplaceInstallDirectory(tempExtract, installRoot);
            InstallVstBundle(installRoot, vstInstallRoot, options.InstallVstPlugin);
            CreateShortcuts(installRoot, targetExe, options);
            WriteUninstallScript(installRoot, vstInstallRoot, options.InstallVstPlugin);
            RegisterUninstaller(installRoot, targetExe, vstInstallRoot, options.InstallVstPlugin);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    public static void WriteFailureLog(Exception exception)
    {
        try
        {
            File.WriteAllText(FailureLogPath, exception.ToString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string NormalizeInstallRoot(string installRoot)
    {
        return NormalizeDirectoryPath(installRoot, "Choose an app install folder.");
    }

    private static string NormalizeVstInstallRoot(string vstInstallRoot)
    {
        return NormalizeDirectoryPath(vstInstallRoot, "Choose a VST3 install folder.");
    }

    private static string NormalizeDirectoryPath(string path, string emptyMessage)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(emptyMessage);
        }

        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrWhiteSpace(root)
            || string.Equals(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Choose a folder, not a drive root.");
        }

        return fullPath;
    }

    private static void ExtractPayloadResource(string destinationPath)
    {
        using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResourceName)
            ?? throw new InvalidOperationException("Installer payload was not embedded. Build with build-installer.ps1.");
        using var output = File.Create(destinationPath);
        payload.CopyTo(output);
    }

    private static void StopRunningApp()
    {
        foreach (var processName in new[] { AppProcessName, LegacyAppProcessName }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    if (!process.CloseMainWindow() || !process.WaitForExit(1500))
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(3000);
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
    }

    private static void ReplaceInstallDirectory(string sourceDirectory, string installRoot)
    {
        var parent = Path.GetDirectoryName(installRoot)
            ?? throw new InvalidOperationException("Could not resolve install directory.");
        Directory.CreateDirectory(parent);

        if (Directory.Exists(installRoot))
        {
            if (!CanReplaceInstallDirectory(installRoot))
            {
                throw new InvalidOperationException(
                    "The selected install folder is not empty and does not look like an existing uMeter install. Choose an empty folder or the existing app folder.");
            }

            Directory.Delete(installRoot, recursive: true);
        }

        if (string.Equals(Path.GetPathRoot(sourceDirectory), Path.GetPathRoot(installRoot), StringComparison.OrdinalIgnoreCase))
        {
            Directory.Move(sourceDirectory, installRoot);
        }
        else
        {
            CopyDirectory(sourceDirectory, installRoot);
            Directory.Delete(sourceDirectory, recursive: true);
        }

        File.WriteAllText(Path.Combine(installRoot, InstallMarkerName), Version);
    }

    private static void InstallVstBundle(string installRoot, string vstInstallRoot, bool installVstPlugin)
    {
        if (!installVstPlugin)
        {
            return;
        }

        var sourceBundle = Path.Combine(installRoot, VstPluginDirectoryName, Vst3DirectoryName, VstBundleName);
        if (!Directory.Exists(sourceBundle))
        {
            throw new InvalidOperationException("The uMeter Sender VST3 bundle is missing from the installer payload. Rebuild with build-installer.ps1.");
        }

        DeleteLegacyVstBundles(vstInstallRoot);

        foreach (var installTargetRoot in GetVstInstallRoots(vstInstallRoot))
        {
            var targetBundle = Path.Combine(installTargetRoot, VstBundleName);
            Directory.CreateDirectory(installTargetRoot);

            if (Directory.Exists(targetBundle))
            {
                Directory.Delete(targetBundle, recursive: true);
            }

            CopyDirectory(sourceBundle, targetBundle);
        }
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);
        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(targetDirectory, Path.GetRelativePath(sourceDirectory, directory));
            Directory.CreateDirectory(target);
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(targetDirectory, Path.GetRelativePath(sourceDirectory, file));
            File.Copy(file, target, overwrite: true);
        }
    }

    private static bool CanReplaceInstallDirectory(string installRoot)
    {
        if (!Directory.EnumerateFileSystemEntries(installRoot).Any())
        {
            return true;
        }

        return File.Exists(Path.Combine(installRoot, InstallMarkerName))
            || File.Exists(Path.Combine(installRoot, LegacyInstallMarkerName))
            || File.Exists(Path.Combine(installRoot, AppExeName))
            || File.Exists(Path.Combine(installRoot, LegacyAppExeName));
    }

    private static void CreateShortcuts(string installRoot, string targetExe, InstallerOptions options)
    {
        var startMenuDir = GetStartMenuDirectory();
        var startMenuShortcut = Path.Combine(startMenuDir, $"{AppName}.lnk");
        var desktopShortcut = GetDesktopShortcutPath();

        TryDeleteFile(startMenuShortcut);
        TryDeleteFile(desktopShortcut);
        TryDeleteDirectory(GetLegacyStartMenuDirectory());
        TryDeleteFile(GetLegacyDesktopShortcutPath());

        if (options.CreateStartMenuShortcut)
        {
            Directory.CreateDirectory(startMenuDir);
            CreateShortcut(
                startMenuShortcut,
                targetExe,
                installRoot,
                "Show per-source desktop loudness meters",
                targetExe);
        }
        else
        {
            TryDeleteDirectoryIfEmpty(startMenuDir);
        }

        if (options.CreateDesktopShortcut)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (!string.IsNullOrWhiteSpace(desktop) && Directory.Exists(desktop))
            {
                CreateShortcut(
                    desktopShortcut,
                    targetExe,
                    installRoot,
                    "Show per-source desktop loudness meters",
                    targetExe);
            }
        }
    }

    private static void CreateShortcut(
        string shortcutPath,
        string targetPath,
        string workingDirectory,
        string description,
        string iconPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is required to create shortcuts.");
        var shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Could not create Windows Script Host shortcut helper.");
        var shortcut = shellType.InvokeMember(
            "CreateShortcut",
            BindingFlags.InvokeMethod,
            binder: null,
            target: shell,
            args: [shortcutPath])
            ?? throw new InvalidOperationException("Could not create shortcut.");
        var shortcutType = shortcut.GetType();

        shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, [targetPath]);
        shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, [workingDirectory]);
        shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, [description]);
        shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, [$"{iconPath},0"]);
        shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, []);
    }

    private static void WriteUninstallScript(string installRoot, string vstInstallRoot, bool installVstPlugin)
    {
        var scriptPath = Path.Combine(installRoot, "Uninstall.ps1");
        var startMenuDir = GetStartMenuDirectory();
        var desktopShortcut = GetDesktopShortcutPath();
        var cleanupScript = $"Start-Sleep -Milliseconds 700; Remove-Item -LiteralPath '{PowerShellQuote(installRoot)}' -Recurse -Force";
        var encodedCleanup = Convert.ToBase64String(Encoding.Unicode.GetBytes(cleanupScript));
        var uninstallNeedsElevation = IsProtectedPath(installRoot)
            || (installVstPlugin && GetVstInstallRoots(vstInstallRoot).Any(IsProtectedPath));
        var script = new StringBuilder();

        script.AppendLine("param([switch]$Elevated)");
        script.AppendLine("$ErrorActionPreference = 'SilentlyContinue'");
        if (uninstallNeedsElevation)
        {
            script.AppendLine("if (-not $Elevated) {");
            script.AppendLine("    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()");
            script.AppendLine("    $principal = [Security.Principal.WindowsPrincipal]::new($identity)");
            script.AppendLine("    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {");
            script.AppendLine("        $escapedPath = $PSCommandPath.Replace(\"'\", \"''\")");
            script.AppendLine("        $command = \"& '$escapedPath' -Elevated\"");
            script.AppendLine("        $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($command))");
            script.AppendLine("        Start-Process powershell.exe -Verb RunAs -ArgumentList \"-NoProfile -ExecutionPolicy Bypass -EncodedCommand $encoded\"");
            script.AppendLine("        exit");
            script.AppendLine("    }");
            script.AppendLine("}");
        }

        script.AppendLine($"Stop-Process -Name '{PowerShellQuote(AppProcessName)}' -Force");
        script.AppendLine($"Stop-Process -Name '{PowerShellQuote(LegacyAppProcessName)}' -Force");
        script.AppendLine($"Remove-Item -LiteralPath '{PowerShellQuote(startMenuDir)}' -Recurse -Force");
        script.AppendLine($"Remove-Item -LiteralPath '{PowerShellQuote(GetLegacyStartMenuDirectory())}' -Recurse -Force");
        script.AppendLine($"Remove-Item -LiteralPath '{PowerShellQuote(desktopShortcut)}' -Force");
        script.AppendLine($"Remove-Item -LiteralPath '{PowerShellQuote(GetLegacyDesktopShortcutPath())}' -Force");

        if (installVstPlugin)
        {
            foreach (var installTargetRoot in GetVstInstallRoots(vstInstallRoot))
            {
                script.AppendLine($"Remove-Item -LiteralPath '{PowerShellQuote(Path.Combine(installTargetRoot, VstBundleName))}' -Recurse -Force");
            }

            foreach (var cleanupRoot in GetVstCleanupRoots(vstInstallRoot))
            {
                script.AppendLine($"Remove-Item -LiteralPath '{PowerShellQuote(Path.Combine(cleanupRoot, LegacyVstBundleName))}' -Recurse -Force");
            }
        }

        script.AppendLine($"Remove-Item -LiteralPath 'HKCU:\\{PowerShellQuote(UninstallKeyPath)}' -Recurse -Force");
        script.AppendLine($"Remove-Item -LiteralPath 'HKCU:\\{PowerShellQuote(LegacyUninstallKeyPath)}' -Recurse -Force");
        script.AppendLine($"Start-Process powershell.exe -WindowStyle Hidden -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-EncodedCommand','{encodedCleanup}'");
        File.WriteAllText(scriptPath, script.ToString());
    }

    private static void RegisterUninstaller(string installRoot, string targetExe, string vstInstallRoot, bool installVstPlugin)
    {
        TryDeleteLegacyUninstaller();

        using var key = Registry.CurrentUser.CreateSubKey(UninstallKeyPath);
        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", Version);
        key.SetValue("Publisher", Publisher);
        key.SetValue("InstallLocation", installRoot);
        key.SetValue("DisplayIcon", $"{targetExe},0");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("Vst3InstallLocation", installVstPlugin ? vstInstallRoot : string.Empty);
        key.SetValue("Vst3StandardMirrorLocation", installVstPlugin ? GetStandardVst3Root() : string.Empty);

        var uninstallScript = Path.Combine(installRoot, "Uninstall.ps1");
        var command = $"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"{uninstallScript}\"";
        key.SetValue("UninstallString", command);
        key.SetValue("QuietUninstallString", command);
    }

    private static string GetStartMenuDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            AppName);
    }

    private static string GetLegacyStartMenuDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            LegacyAppName);
    }

    private static string GetDesktopShortcutPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            $"{AppName}.lnk");
    }

    private static string GetLegacyDesktopShortcutPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            $"{LegacyAppName}.lnk");
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void DeleteLegacyVstBundles(string selectedVstRoot)
    {
        foreach (var cleanupRoot in GetVstCleanupRoots(selectedVstRoot))
        {
            TryDeleteDirectory(Path.Combine(cleanupRoot, LegacyVstBundleName));
        }
    }

    private static IEnumerable<string> GetVstInstallRoots(string selectedVstRoot)
    {
        var roots = new[]
        {
            selectedVstRoot,
            GetStandardVst3Root()
        };

        return NormalizeDistinctRoots(roots);
    }

    private static IEnumerable<string> GetVstCleanupRoots(string selectedVstRoot)
    {
        var roots = new[]
        {
            selectedVstRoot,
            InstallerOptions.DefaultVstInstallRoot,
            GetStandardVst3Root(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Common Files", "VST3")
        };

        return NormalizeDistinctRoots(roots);
    }

    private static string GetStandardVst3Root()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Common Files", "VST3");
    }

    private static IEnumerable<string> NormalizeDistinctRoots(IEnumerable<string> roots)
    {
        return roots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => Path.GetFullPath(Environment.ExpandEnvironmentVariables(root.Trim())))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsProtectedPath(string path)
    {
        var protectedRoots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows)
        };

        return protectedRoots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Any(root => IsPathUnderDirectory(path, root));
    }

    private static bool IsPathUnderDirectory(string path, string directory)
    {
        var normalizedPath = EnsureTrailingSeparator(Path.GetFullPath(path));
        var normalizedDirectory = EnsureTrailingSeparator(Path.GetFullPath(directory));
        return normalizedPath.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
    }

    private static string BuildQuietArguments(InstallerOptions options)
    {
        var args = new List<string>
        {
            "--quiet",
            "--install-dir",
            options.InstallRoot
        };

        if (!options.CreateDesktopShortcut && !options.CreateStartMenuShortcut)
        {
            args.Add("--no-shortcuts");
        }
        else
        {
            if (!options.CreateDesktopShortcut)
            {
                args.Add("--no-desktop-shortcut");
            }

            if (!options.CreateStartMenuShortcut)
            {
                args.Add("--no-start-menu-shortcut");
            }
        }

        if (options.InstallVstPlugin)
        {
            args.Add("--vst-dir");
            args.Add(options.VstInstallRoot);
        }
        else
        {
            args.Add("--no-vst");
        }

        return string.Join(" ", args.Select(QuoteArgument));
    }

    private static string QuoteArgument(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        if (!value.Any(char.IsWhiteSpace) && !value.Contains('"'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
    }

    private static string PowerShellQuote(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static void TryDeleteLegacyUninstaller()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(LegacyUninstallKeyPath, throwOnMissingSubKey: false);
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteDirectoryIfEmpty(string directory)
    {
        try
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
