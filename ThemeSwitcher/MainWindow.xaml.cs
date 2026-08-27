using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using ThemeSwitcher.Models;
using ThemeSwitcher.Services;

namespace ThemeSwitcher;

public sealed partial class MainWindow : Window
{
    private readonly StartupService _startupService = new();
    private bool _isLoading = true;

    public MainWindow()
    {
        InitializeComponent();
        Title = "主题切换器";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        InitializeUI();
        LoadSettings();
        UpdateStatusDisplay();

        App.ScheduleService.StatusChanged += ScheduleService_StatusChanged;

        // 异步加载自启状态（_isLoading 仍为 true，不会触发 Toggled 事件）
        _ = LoadAutoStartState();

        _isLoading = false;
    }

    private void InitializeUI()
    {
        // 模式选项在 XAML 中直接定义
    }

    private void LoadSettings()
    {
        var s = App.SettingsService.Settings;

        // 模式
        ModeRadio.SelectedIndex = (int)s.Mode;
        UpdateModePanels(s.Mode);

        // 时间
        if (TimeSpan.TryParse(s.LightTime, out var lt))
            LightTimePicker.Time = lt;
        if (TimeSpan.TryParse(s.DarkTime, out var dt))
            DarkTimePicker.Time = dt;

        // 切换范围
        SwitchSystemToggle.IsOn = s.SwitchSystemTheme;
        SwitchAppToggle.IsOn = s.SwitchAppTheme;

        // 位置
        if (s.Latitude.HasValue && s.Longitude.HasValue)
            LocationText.Text = $"纬度 {s.Latitude:F4}, 经度 {s.Longitude:F4}";

        SunriseOffsetBox.Value = s.SunriseOffsetMinutes;
        SunsetOffsetBox.Value = s.SunsetOffsetMinutes;
    }

    private async System.Threading.Tasks.Task LoadAutoStartState()
    {
        var enabled = await _startupService.IsEnabledAsync();
        // 先设 _isLoading 防止 Toggled 事件触发写注册表
        _isLoading = true;
        AutoStartToggle.IsOn = enabled;
        _isLoading = false;
    }

    private void UpdateModePanels(SwitchMode mode)
    {
        ScheduledPanel.Visibility = mode == SwitchMode.Scheduled ? Visibility.Visible : Visibility.Collapsed;
        SunrisePanel.Visibility = mode == SwitchMode.SunriseSunset ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateStatusDisplay()
    {
        var isLight = App.ThemeService.IsLightTheme();
        CurrentThemeText.Text = isLight ? "☀️ 浅色主题" : "🌙 深色主题";

        StatusText.Text = App.ScheduleService.StatusMessage;

        if (App.ScheduleService.NextSwitchTime.HasValue)
        {
            var target = App.ScheduleService.NextSwitchIsLight == true ? "浅色" : "深色";
            NextSwitchText.Text = $"下次切换：{App.ScheduleService.NextSwitchTime:MM-dd HH:mm} → {target}";
        }
        else
        {
            NextSwitchText.Text = "下次切换：--";
        }
    }

    private void ScheduleService_StatusChanged(object sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(UpdateStatusDisplay);
    }

    // === 事件处理 ===

    private void ToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        var s = App.SettingsService.Settings;
        App.ThemeService.Toggle(s.SwitchSystemTheme, s.SwitchAppTheme);
        UpdateStatusDisplay();
    }

    private void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        App.ScheduleService.ForceCheck();
        UpdateStatusDisplay();
    }

    private void ModeRadio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        var mode = (SwitchMode)ModeRadio.SelectedIndex;
        App.SettingsService.Settings.Mode = mode;
        App.SettingsService.Save();
        UpdateModePanels(mode);
        App.ScheduleService.ForceCheck();
    }

    private void LightTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        if (_isLoading) return;
        App.SettingsService.Settings.LightTime = LightTimePicker.Time.ToString(@"HH\:mm");
        App.SettingsService.Save();
        App.ScheduleService.ForceCheck();
    }

    private void DarkTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
    {
        if (_isLoading) return;
        App.SettingsService.Settings.DarkTime = DarkTimePicker.Time.ToString(@"HH\:mm");
        App.SettingsService.Save();
        App.ScheduleService.ForceCheck();
    }

    private async void GetLocationBtn_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        btn.IsEnabled = false;
        LocationText.Text = "正在获取...";

        var locService = new LocationService();
        var loc = await locService.GetCurrentLocationAsync();

        if (loc.HasValue)
        {
            App.SettingsService.Settings.Latitude = loc.Value.Latitude;
            App.SettingsService.Settings.Longitude = loc.Value.Longitude;
            App.SettingsService.Settings.LocationLastUpdated = DateTime.Now;
            App.SettingsService.Save();
            LocationText.Text = $"纬度 {loc.Value.Latitude:F4}, 经度 {loc.Value.Longitude:F4}";
            App.ScheduleService.ForceCheck();
        }
        else
        {
            LocationText.Text = "获取失败，请检查位置权限";
        }

        btn.IsEnabled = true;
    }

    private void SunriseOffsetBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isLoading) return;
        App.SettingsService.Settings.SunriseOffsetMinutes = (int)SunriseOffsetBox.Value;
        App.SettingsService.Save();
    }

    private void SunsetOffsetBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_isLoading) return;
        App.SettingsService.Settings.SunsetOffsetMinutes = (int)SunsetOffsetBox.Value;
        App.SettingsService.Save();
    }

    private void SwitchSystemToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        App.SettingsService.Settings.SwitchSystemTheme = SwitchSystemToggle.IsOn;
        App.SettingsService.Save();
    }

    private void SwitchAppToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        App.SettingsService.Settings.SwitchAppTheme = SwitchAppToggle.IsOn;
        App.SettingsService.Save();
    }

    private async void AutoStartToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        if (AutoStartToggle.IsOn)
        {
            var ok = await _startupService.EnableAsync();
            if (!ok)
            {
                AutoStartToggle.IsOn = false;
                var dlg = new ContentDialog
                {
                    Title = "无法启用自启",
                    Content = "写入注册表失败，请以管理员身份运行后重试。",
                    CloseButtonText = "确定",
                    XamlRoot = Content.XamlRoot
                };
                await dlg.ShowAsync();
            }
        }
        else
        {
            await _startupService.DisableAsync();
        }
    }
}
