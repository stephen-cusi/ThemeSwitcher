using Windows.ApplicationModel;
using System;
using System.Threading.Tasks;

namespace ThemeSwitcher.Services;

/// <summary>
/// 开机自启管理服务
/// 基于 MSIX StartupTask 扩展
/// </summary>
public class StartupService
{
    private const string TaskId = "ThemeSwitcherStartupTask";

    /// <summary>
    /// 获取当前自启状态
    /// </summary>
    public async Task<StartupTaskState> GetStateAsync()
    {
        try
        {
            var task = await StartupTask.GetAsync(TaskId);
            return task.State;
        }
        catch
        {
            return StartupTaskState.Disabled;
        }
    }

    /// <summary>
    /// 请求启用自启
    /// </summary>
    /// <returns>是否成功启用</returns>
    public async Task<bool> EnableAsync()
    {
        try
        {
            var task = await StartupTask.GetAsync(TaskId);
            var state = await task.RequestEnableAsync();
            return state == StartupTaskState.Enabled;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 禁用自启
    /// </summary>
    public async Task DisableAsync()
    {
        try
        {
            var task = await StartupTask.GetAsync(TaskId);
            task.Disable();
        }
        catch
        {
            // 忽略
        }
    }
}
