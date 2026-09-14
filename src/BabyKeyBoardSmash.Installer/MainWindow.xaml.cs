using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace BabyKeyBoardSmash.Installer;

public partial class MainWindow : Window
{
    private bool _isCompleted = false;

    public MainWindow()
    {
        InitializeComponent();
        string defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "BabyKeyBoardSmashByMahfoz"
        );
        TxtInstallPath.Text = defaultPath;
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "ইনস্টলেশন ফোল্ডার নির্বাচন করুন (Select Destination Folder)",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        };

        if (dialog.ShowDialog() == true)
        {
            string selected = dialog.FolderName;
            if (!selected.EndsWith("BabyKeyBoardSmashByMahfoz", StringComparison.OrdinalIgnoreCase))
            {
                selected = Path.Combine(selected, "BabyKeyBoardSmashByMahfoz");
            }
            TxtInstallPath.Text = selected;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void BtnInstall_Click(object sender, RoutedEventArgs e)
    {
        if (_isCompleted)
        {
            if (ChkLaunchAfter.IsChecked == true)
            {
                LaunchApp(TxtInstallPath.Text);
            }
            Close();
            return;
        }

        string targetDir = TxtInstallPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(targetDir))
        {
            MessageBox.Show("দয়া করে একটি সঠিক ফোল্ডার নির্বাচন করুন।", "সতর্কতা", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool createDesktop = ChkDesktopShortcut.IsChecked == true;
        bool createStartMenu = ChkStartMenuShortcut.IsChecked == true;

        // UI locking
        BtnInstall.IsEnabled = false;
        BtnCancel.IsEnabled = false;
        BtnBrowse.IsEnabled = false;
        TxtInstallPath.IsEnabled = false;
        ChkDesktopShortcut.IsEnabled = false;
        ChkStartMenuShortcut.IsEnabled = false;
        ChkLaunchAfter.IsEnabled = false;

        try
        {
            await Task.Run(() => PerformInstallation(targetDir, createDesktop, createStartMenu));

            _isCompleted = true;
            PbProgress.Value = 100;
            TxtStatus.Text = "ইনস্টলেশন সফলভাবে সম্পন্ন হয়েছে! (Installation Completed)";
            BtnInstall.Content = "চালু করুন (Launch)";
            BtnInstall.IsEnabled = true;
            BtnCancel.Content = "সমাপ্ত (Finish)";
            BtnCancel.IsEnabled = true;
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"ত্রুটি: {ex.Message}";
            MessageBox.Show($"ইনস্টলেশন সম্পন্ন করা সম্ভব হয়নি:\n{ex.Message}", "ত্রুটি (Error)", MessageBoxButton.OK, MessageBoxImage.Error);
            BtnInstall.IsEnabled = true;
            BtnCancel.IsEnabled = true;
            BtnBrowse.IsEnabled = true;
            TxtInstallPath.IsEnabled = true;
        }
    }

    private void PerformInstallation(string targetDir, bool createDesktop, bool createStartMenu)
    {
        Directory.CreateDirectory(targetDir);

        // 1. Locate embedded payload zip
        var assembly = Assembly.GetExecutingAssembly();
        string resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith("app_payload.zip", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("অ্যাপ প্যাকেজ (app_payload.zip) খুঁজে পাওয়া যায়নি।");

        using Stream? stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("প্যাকেজ স্ট্রিম খুলতে ব্যর্থ হয়েছে।");

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        int totalEntries = archive.Entries.Count;
        int processed = 0;

        foreach (var entry in archive.Entries)
        {
            string destinationPath = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));

            if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
            {
                Directory.CreateDirectory(destinationPath);
            }
            else
            {
                string? parent = Path.GetDirectoryName(destinationPath);
                if (parent != null) Directory.CreateDirectory(parent);
                entry.ExtractToFile(destinationPath, overwrite: true);
            }

            processed++;
            int progress = (int)((double)processed / totalEntries * 85);
            Dispatcher.Invoke(() =>
            {
                PbProgress.Value = progress;
                TxtStatus.Text = $"ফাইল আনপ্যাক করা হচ্ছে... ({processed}/{totalEntries})";
            });
        }

        // 2. Drop uninstaller binary
        Dispatcher.Invoke(() => TxtStatus.Text = "আনইনস্টলার তৈরি করা হচ্ছে...");
        string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        string uninstallExe = Path.Combine(targetDir, "Uninstall.exe");
        if (File.Exists(currentExe))
        {
            try
            {
                File.Copy(currentExe, uninstallExe, true);
            }
            catch { }
        }

        string appExe = Path.Combine(targetDir, "BabyKeyBoardSmash.exe");

        // 3. Create Shortcuts
        if (createDesktop)
        {
            Dispatcher.Invoke(() => TxtStatus.Text = "ডেস্কটপ শর্টকাট তৈরি করা হচ্ছে...");
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string desktopShortcut = Path.Combine(desktopDir, "BabyKeyBoardSmash.lnk");
            CreateShortcut(desktopShortcut, appExe, targetDir);
        }

        if (createStartMenu)
        {
            Dispatcher.Invoke(() => TxtStatus.Text = "স্টার্ট মেনু শর্টকাট তৈরি করা হচ্ছে...");
            string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "BabyKeyBoardSmashByMahfoz");
            Directory.CreateDirectory(startMenuDir);
            string startMenuShortcut = Path.Combine(startMenuDir, "BabyKeyBoardSmash.lnk");
            CreateShortcut(startMenuShortcut, appExe, targetDir);
        }

        // 4. Windows Registry registration (Add/Remove Programs)
        Dispatcher.Invoke(() => TxtStatus.Text = "উইন্ডোজ অ্যাপ তালিকায় যুক্ত করা হচ্ছে...");
        RegisterInWindowsUninstall(targetDir, appExe, uninstallExe);
    }

    private static void CreateShortcut(string shortcutPath, string targetExePath, string workingDir)
    {
        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetExePath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.Description = "BabyKeyBoardSmash by Mahfoz - ছোটদের বাংলা কীবোর্ড স্ম্যাশ গেম";
                shortcut.Save();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to create shortcut: {ex.Message}");
        }
    }

    private static void RegisterInWindowsUninstall(string installDir, string exePath, string uninstallExe)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\BabyKeyBoardSmashByMahfoz");
            if (key != null)
            {
                key.SetValue("DisplayName", "BabyKeyBoardSmash by Mahfoz");
                key.SetValue("DisplayVersion", "1.0.0");
                key.SetValue("Publisher", "Mahfoz");
                key.SetValue("InstallLocation", installDir);
                key.SetValue("DisplayIcon", exePath);
                key.SetValue("UninstallString", $"\"{uninstallExe}\"");
                key.SetValue("QuietUninstallString", $"\"{uninstallExe}\" --uninstall");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                key.SetValue("EstimatedSize", 185000, RegistryValueKind.DWord);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to register in Windows Registry: {ex.Message}");
        }
    }

    private static void LaunchApp(string targetDir)
    {
        string exePath = Path.Combine(targetDir, "BabyKeyBoardSmash.exe");
        if (File.Exists(exePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = targetDir,
                UseShellExecute = true
            });
        }
    }
}
