using System;
using System.Text.Json.Serialization;

namespace ThemeSwitcher.Models;

/// <summary>
/// 切换模式
/// </summary>
public enum SwitchMode
{
    /// <summary>手动切换</summary>
    Manual,
    /// <summary>定时切换</summary>
    Scheduled,
    /// <summary>日出日落自动切换</summary>
    SunriseSunset
}

/// <summary>
/// 应用设置
/// </summary>
public class AppSettings
{
    /// <summary>当前切换模式</summary>
    public SwitchMode Mode { get; set; } = SwitchMode.Scheduled;

    /// <summary>浅色模式开始时间（小时:分钟）</summary>
    public string LightTime { get; set; } = "07:00";

    /// <summary>深色模式开始时间（小时:分钟）</summary>
    public string DarkTime { get; set; } = "19:00";

    /// <summary>是否同时切换系统主题（任务栏/资源管理器）</summary>
    public bool SwitchSystemTheme { get; set; } = true;

    /// <summary>是否同时切换应用主题</summary>
    public bool SwitchAppTheme { get; set; } = true;

    /// <summary>是否开机自启</summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>是否最小化到托盘</summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>手动设置的纬度（用于日出日落计算，为空则自动获取）</summary>
    public double? Latitude { get; set; }

    /// <summary>手动设置的经度</summary>
    public double? Longitude { get; set; }

    /// <summary>日出日落切换时的偏移分钟数（正数=延后，负数=提前）</summary>
    public int SunriseOffsetMinutes { get; set; } = 0;

    public int SunsetOffsetMinutes { get; set; } = 0;

    /// <summary>上次已知的主题状态，避免重复切换</summary>
    [JsonIgnore]
    public bool? LastIsLight { get; set; }

    /// <summary>位置最后更新时间</summary>
    public DateTime? LocationLastUpdated { get; set; }

    /// <summary>位置缓存有效期（小时）</summary>
    public int LocationCacheHours { get; set; } = 24;
}
