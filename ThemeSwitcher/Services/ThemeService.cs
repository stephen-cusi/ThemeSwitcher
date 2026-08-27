using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ThemeSwitcher.Services;

/// <summary>
/// Windows 主题切换服务
/// 通过修改注册表实现系统级深浅色主题切换
/// </summary>
public class ThemeService
{
    private const string ThemesKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightTheme = "AppsUseLightTheme";
    private const string SystemUsesLightTheme = "SystemUsesLightTheme";

    /// <summary>
    /// 获取当前系统是否为浅色主题
    /// </summary>
    public bool IsLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(ThemesKeyPath);
            var value = key?.GetValue(AppsUseLightTheme);
            return value is int i && i == 1;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// 切换到浅色主题
    /// </summary>
    public void SetLightTheme(bool switchSystem = true, bool switchApp = true)
    {
        SetTheme(true, switchSystem, switchApp);
    }

    /// <summary>
    /// 切换到深色主题
    /// </summary>
    public void SetDarkTheme(bool switchSystem = true, bool switchApp = true)
    {
        SetTheme(false, switchSystem, switchApp);
    }

    /// <summary>
    /// 设置主题
    /// </summary>
    /// <param name="isLight">true=浅色, false=深色</param>
    public void SetTheme(bool isLight, bool switchSystem = true, bool switchApp = true)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(ThemesKeyPath);
            if (key == null) return;

            int value = isLight ? 1 : 0;

            if (switchApp)
                key.SetValue(AppsUseLightTheme, value, RegistryValueKind.DWord);

            if (switchSystem)
                key.SetValue(SystemUsesLightTheme, value, RegistryValueKind.DWord);

            // 广播设置变更，让系统立即响应
            NotifySettingChange();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SetTheme failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换主题（浅色<->深色）
    /// </summary>
    public void Toggle(bool switchSystem = true, bool switchApp = true)
    {
        SetTheme(!IsLightTheme(), switchSystem, switchApp);
    }

    /// <summary>
    /// 广播 WM_SETTINGCHANGE 消息，通知系统主题已变更
    /// </summary>
    private static void NotifySettingChange()
    {
        try
        {
            SendMessageTimeout(
                new IntPtr(0xffff), // HWND_BROADCAST
                0x001A,             // WM_SETTINGCHANGE
                IntPtr.Zero,
                "ImmersiveColorSet",
                0x0002,             // SMTO_ABORTIFHUNG
                2000,
                out _);
        }
        catch
        {
            // 忽略广播失败
        }
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, IntPtr wParam, string lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
}
