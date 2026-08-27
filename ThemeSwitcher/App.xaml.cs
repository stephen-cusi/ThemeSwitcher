using Microsoft.UI.Xaml;
using System;
using ThemeSwitcher.Services;

namespace ThemeSwitcher;

public partial class App : Application
{
    public static Window MainWindow { get; private set; }
    public static ThemeService ThemeService { get; private set; }
    public static ScheduleService ScheduleService { get; private set; }
    public static SettingsService SettingsService { get; private set; }

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        SettingsService = new SettingsService();
        SettingsService.Load();

        ThemeService = new ThemeService();
        ScheduleService = new ScheduleService(ThemeService, SettingsService);

        MainWindow = new MainWindow();
        MainWindow.Activate();

        // 启动调度服务
        ScheduleService.Start();
    }
}
