using System;
using System.Threading;
using System.Threading.Tasks;
using ThemeSwitcher.Models;

namespace ThemeSwitcher.Services;

/// <summary>
/// 主题调度服务
/// 后台定时检查并根据设置自动切换深浅色主题
/// </summary>
public class ScheduleService
{
    private readonly ThemeService _themeService;
    private readonly SettingsService _settingsService;
    private readonly SunriseSunsetService _sunriseSunsetService;
    private readonly LocationService _locationService;

    private Timer _timer;
    private DateTime _lastCheckDate = DateTime.MinValue;
    private (DateTime Sunrise, DateTime Sunset)? _todaySunTimes;
    private readonly SemaphoreSlim _checkLock = new(1, 1);

    /// <summary>
    /// 当前状态信息（用于UI显示）
    /// </summary>
    public string StatusMessage { get; private set; } = "未启动";

    /// <summary>
    /// 下一次切换时间（用于UI显示）
    /// </summary>
    public DateTime? NextSwitchTime { get; private set; }

    /// <summary>
    /// 下一次切换的目标主题
    /// </summary>
    public bool? NextSwitchIsLight { get; private set; }

    public event EventHandler StatusChanged;

    public ScheduleService(ThemeService themeService, SettingsService settingsService)
    {
        _themeService = themeService;
        _settingsService = settingsService;
        _sunriseSunsetService = new SunriseSunsetService();
        _locationService = new LocationService();
    }

    /// <summary>
    /// 启动调度服务
    /// </summary>
    public void Start()
    {
        // 每30秒检查一次
        _timer = new Timer(_ => _ = OnTimerTickAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// 停止调度服务
    /// </summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// 立即执行一次检查（从 UI 线程安全调用）
    /// </summary>
    public void ForceCheck()
    {
        _ = OnTimerTickAsync();
    }

    private async Task OnTimerTickAsync()
    {
        // 防止并发执行（Timer线程 + UI线程同时调用）
        if (!await _checkLock.WaitAsync(0))
            return;

        try
        {
            var settings = _settingsService.Settings;
            var now = DateTime.Now;

            // 日期变更时重新计算日出日落
            if (now.Date != _lastCheckDate)
            {
                _lastCheckDate = now.Date;
                _todaySunTimes = null; // 清除缓存，下次需要时重新计算
            }

            bool? targetIsLight = settings.Mode switch
            {
                SwitchMode.Manual => null, // 手动模式不自动切换
                SwitchMode.Scheduled => GetScheduledTarget(now, settings),
                SwitchMode.SunriseSunset => await GetSunriseSunsetTarget(now, settings),
                _ => null
            };

            if (targetIsLight.HasValue)
            {
                var currentIsLight = _themeService.IsLightTheme();
                if (currentIsLight != targetIsLight.Value)
                {
                    _themeService.SetTheme(
                        targetIsLight.Value,
                        settings.SwitchSystemTheme,
                        settings.SwitchAppTheme);
                    settings.LastIsLight = targetIsLight.Value;
                    _settingsService.Save();
                }
            }

            UpdateStatus(settings, now, targetIsLight);
        }
        catch (Exception ex)
        {
            StatusMessage = $"调度异常: {ex.Message}";
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _checkLock.Release();
        }
    }

    /// <summary>
    /// 定时模式：根据设置的时间判断目标主题
    /// </summary>
    private bool GetScheduledTarget(DateTime now, AppSettings settings)
    {
        var lightTime = ParseTime(settings.LightTime);
        var darkTime = ParseTime(settings.DarkTime);

        var nowMinutes = now.Hour * 60 + now.Minute;

        if (lightTime == darkTime)
            return true; // 相同时间默认浅色

        if (lightTime < darkTime)
        {
            // 浅色时间段在白天（如 7:00-19:00）
            return nowMinutes >= lightTime && nowMinutes < darkTime;
        }
        else
        {
            // 浅色时间段跨天（如 19:00-次日7:00，这种情况较少）
            return nowMinutes >= lightTime || nowMinutes < darkTime;
        }
    }

    /// <summary>
    /// 日出日落模式：根据日出日落时间判断目标主题
    /// 日出后切浅色，日落后切深色
    /// </summary>
    private async Task<bool?> GetSunriseSunsetTarget(DateTime now, AppSettings settings)
    {
        var sunTimes = await GetTodaySunTimes(settings);
        if (!sunTimes.HasValue)
            return null;

        var sunrise = sunTimes.Value.Sunrise.AddMinutes(settings.SunriseOffsetMinutes);
        var sunset = sunTimes.Value.Sunset.AddMinutes(settings.SunsetOffsetMinutes);

        // 日出后到日落前为浅色
        return now >= sunrise && now < sunset;
    }

    /// <summary>
    /// 获取今天的日出日落时间（带缓存）
    /// </summary>
    private async Task<(DateTime Sunrise, DateTime Sunset)?> GetTodaySunTimes(AppSettings settings)
    {
        if (_todaySunTimes.HasValue)
            return _todaySunTimes.Value;

        double lat, lng;

        if (settings.Latitude.HasValue && settings.Longitude.HasValue)
        {
            lat = settings.Latitude.Value;
            lng = settings.Longitude.Value;
        }
        else
        {
            // 自动获取位置
            var location = await _locationService.GetCurrentLocationAsync();
            if (!location.HasValue)
                return null;

            lat = location.Value.Latitude;
            lng = location.Value.Longitude;
            settings.Latitude = lat;
            settings.Longitude = lng;
            settings.LocationLastUpdated = DateTime.Now;
            _settingsService.Save();
        }

        _todaySunTimes = _sunriseSunsetService.Calculate(DateTime.Today, lat, lng);
        return _todaySunTimes;
    }

    /// <summary>
    /// 更新状态信息
    /// </summary>
    private void UpdateStatus(AppSettings settings, DateTime now, bool? currentTarget)
    {
        NextSwitchTime = null;
        NextSwitchIsLight = null;

        switch (settings.Mode)
        {
            case SwitchMode.Manual:
                StatusMessage = "手动模式";
                break;

            case SwitchMode.Scheduled:
                {
                    var lightMin = ParseTime(settings.LightTime);
                    var darkMin = ParseTime(settings.DarkTime);
                    var lightToday = todayAt(lightMin);
                    var darkToday = todayAt(darkMin);

                    // 找出下一个到来的时间点及其对应主题
                    if (now < lightToday && now < darkToday)
                    {
                        if (lightToday <= darkToday)
                        { NextSwitchTime = lightToday; NextSwitchIsLight = true; }
                        else
                        { NextSwitchTime = darkToday; NextSwitchIsLight = false; }
                    }
                    else if (now < lightToday)
                    { NextSwitchTime = lightToday; NextSwitchIsLight = true; }
                    else if (now < darkToday)
                    { NextSwitchTime = darkToday; NextSwitchIsLight = false; }
                    else
                    {
                        var lightTom = lightToday.AddDays(1);
                        var darkTom = darkToday.AddDays(1);
                        if (lightTom <= darkTom)
                        { NextSwitchTime = lightTom; NextSwitchIsLight = true; }
                        else
                        { NextSwitchTime = darkTom; NextSwitchIsLight = false; }
                    }

                    StatusMessage = $"定时模式 · 浅色 {settings.LightTime} · 深色 {settings.DarkTime}";
                    break;
                }

            case SwitchMode.SunriseSunset:
                {
                    if (_todaySunTimes.HasValue)
                    {
                        var sunrise = _todaySunTimes.Value.Sunrise.AddMinutes(settings.SunriseOffsetMinutes);
                        var sunset = _todaySunTimes.Value.Sunset.AddMinutes(settings.SunsetOffsetMinutes);

                        if (now < sunrise)
                        {
                            NextSwitchTime = sunrise;
                            NextSwitchIsLight = true;
                        }
                        else if (now < sunset)
                        {
                            NextSwitchTime = sunset;
                            NextSwitchIsLight = false;
                        }
                        else
                        {
                            // 明天的日出
                            var tomorrow = _sunriseSunsetService.Calculate(
                                DateTime.Today.AddDays(1),
                                settings.Latitude ?? 0,
                                settings.Longitude ?? 0);
                            NextSwitchTime = tomorrow.Sunrise.AddMinutes(settings.SunriseOffsetMinutes);
                            NextSwitchIsLight = true;
                        }

                        StatusMessage = $"日出日落 · 日出 {sunrise:HH:mm} · 日落 {sunset:HH:mm}";
                    }
                    else
                    {
                        StatusMessage = "日出日落模式 · 正在获取位置...";
                    }
                    break;
                }
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);

        DateTime todayAt(int minutes) =>
            DateTime.Today.AddMinutes(minutes);
    }

    private static int ParseTime(string timeStr)
    {
        if (TimeSpan.TryParse(timeStr, out var ts))
            return (int)ts.TotalMinutes;
        return 7 * 60; // 默认 7:00
    }
}
