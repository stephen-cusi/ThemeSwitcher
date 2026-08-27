using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ThemeSwitcher.Services;

/// <summary>
/// 开机自启管理服务
/// 非打包应用使用注册表方式实现开机自启
/// </summary>
public class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ThemeSwitcher";

    /// <summary>
    /// 获取当前自启状态
    /// </summary>
    public Task<bool> IsEnabledAsync()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            var value = key?.GetValue(AppName);
            return Task.FromResult(value != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 启用开机自启（通过注册表 Run 键）
    /// </summary>
    public Task<bool> EnableAsync()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                return Task.FromResult(false);

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null)
                return Task.FromResult(false);

            // 使用引号包裹路径，避免路径含空格时出错
            key.SetValue(AppName, $"\"{exePath}\"", RegistryValueKind.String);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 禁用开机自启
    /// </summary>
    public Task DisableAsync()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            key?.DeleteValue(AppName, false);
        }
        catch
        {
            // 忽略
        }
        return Task.CompletedTask;
    }
}
