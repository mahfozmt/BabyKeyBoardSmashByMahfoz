using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace BabyKeyBoardSmash.Installer;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        bool isUninstall = false;
        foreach (var arg in e.Args)
        {
            if (arg.Equals("--uninstall", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("/uninstall", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-u", StringComparison.OrdinalIgnoreCase))
            {
                isUninstall = true;
                break;
            }
        }

        string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
        if (Path.GetFileNameWithoutExtension(currentExe).Equals("Uninstall", StringComparison.OrdinalIgnoreCase))
        {
            isUninstall = true;
        }

        if (isUninstall)
        {
            PerformUninstall();
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private void PerformUninstall()
    {
        var result = MessageBox.Show(
            "আপনি কি নিশ্চিত যে আপনার কম্পিউটার থেকে বেবি কীবোর্ড স্ম্যাশ (BabyKeyBoardSmash) আনইনস্টল করতে চান?\n\nAre you sure you want to uninstall BabyKeyBoardSmash?",
            "বেবি কীবোর্ড স্ম্যাশ আনইনস্টল (Uninstall)",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            // 1. Delete Desktop shortcut
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string desktopShortcut = Path.Combine(desktopDir, "BabyKeyBoardSmash.lnk");
            if (File.Exists(desktopShortcut))
            {
                File.Delete(desktopShortcut);
            }

            // 2. Delete Start Menu shortcut and folder
            string startMenuPrograms = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "BabyKeyBoardSmashByMahfoz");
            if (Directory.Exists(startMenuPrograms))
            {
                Directory.Delete(startMenuPrograms, true);
            }

            // 3. Remove Registry entry
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\BabyKeyBoardSmashByMahfoz", false);
            }
            catch { }

            // 4. Delete install directory after exit via background cmd
            string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c timeout /t 2 /nobreak > nul & rmdir /s /q \"{appDir}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            MessageBox.Show(
                "বেবি কীবোর্ড স্ম্যাশ সফলভাবে আনইনস্টল করা হয়েছে।\n\nBabyKeyBoardSmash was uninstalled successfully.",
                "আনইনস্টল সম্পন্ন (Completed)",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"আনইনস্টল করার সময় ত্রুটি হয়েছে:\n{ex.Message}",
                "ত্রুটি (Error)",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
