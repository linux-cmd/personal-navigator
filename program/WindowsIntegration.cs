using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PersonalNavigator;

public static class WindowsIntegration
{
    public const int HotKeyId = 0x504E;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint VkSpace = 0x20;
    private const int WmHotKey = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static bool RegisterGlobalHotKey(Window window, Action toggle)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (HwndSource.FromHwnd(handle) is HwndSource source)
        {
            source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (msg == WmHotKey && wParam.ToInt32() == HotKeyId)
                {
                    toggle();
                    handled = true;
                }
                return IntPtr.Zero;
            });
        }
        return RegisterHotKey(handle, HotKeyId, ModControl | ModAlt, VkSpace);
    }

    public static void UnregisterGlobalHotKey(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero) UnregisterHotKey(handle, HotKeyId);
    }

    public static void ApplyDarkWindow(Window window)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        int enabled = 1;
        DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int));
        int backdrop = 2;
        DwmSetWindowAttribute(handle, 38, ref backdrop, sizeof(int));
    }

    public static void InstallShellIntegration(string executablePath, bool startWithWindows)
    {
        string command = $"\"{executablePath}\" \"%V\"";
        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\Background\shell\PersonalNavigator"))
        {
            key.SetValue("MUIVerb", "Open Personal Map");
            key.SetValue("Icon", executablePath);
        }
        using (var commandKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\Background\shell\PersonalNavigator\command"))
            commandKey.SetValue(string.Empty, command);

        command = $"\"{executablePath}\" \"%1\"";
        using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell\PersonalNavigator"))
        {
            key.SetValue("MUIVerb", "Open Personal Map");
            key.SetValue("Icon", executablePath);
        }
        using (var commandKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\Directory\shell\PersonalNavigator\command"))
            commandKey.SetValue(string.Empty, command);

        using var run = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        if (startWithWindows) run?.SetValue("PersonalNavigator", $"\"{executablePath}\" --background");
        else run?.DeleteValue("PersonalNavigator", false);
    }

    public static void OpenPath(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not open item", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    public static void RevealPath(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Could not show item", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
