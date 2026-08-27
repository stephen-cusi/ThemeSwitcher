# 主题切换器 (ThemeSwitcher)

基于 WinUI 3 的 Windows 深浅色主题自动切换工具，支持 Windows 10 1809+ / Windows 11，x64 与 ARM64 架构。

> 本项目为**第二个主题切换器**，由**豆包（Doubao）**编写，MiMo（我）负责上传到 GitHub 并编写 CI/CD 构建流程、修复自启和界面等 bug。

## 功能特性

- **手动切换**：一键切换系统深浅色主题
- **定时切换**：自定义浅色/深色开始时间
- **日出日落自动切换**：基于系统地理位置，日出后切浅色、日落后切深色
- **切换范围可控**：可分别控制系统主题（任务栏/资源管理器）和应用主题
- **开机自启**：通过注册表实现，支持非打包应用
- **跨架构**：支持 x64 和 ARM64（WOA 设备如 Surface Pro X 等）

## 系统要求

- Windows 10 版本 1809 (build 17763) 或更高 / Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 17.8+，需安装以下工作负载：
  - **使用 .NET 的桌面开发**
  - **通用 Windows 平台开发**（UWP 工具集）
  - **Windows 应用 SDK C# 开发工具**（单项目 MSIX 打包支持）

## 快速开始

1. 从 [GitHub Actions](https://github.com/stephen-cusi/ThemeSwitcher/actions) 下载最新构建产物
2. 解压后直接运行 `ThemeSwitcher.exe`
3. 首次运行需要安装 [Windows App Runtime](https://github.com/microsoft/WindowsAppSDK/releases)

## 构建步骤

### 1. 还原依赖

```bash
dotnet restore ThemeSwitcher/ThemeSwitcher.csproj
```

### 2. 构建 x64 版本

```bash
# 调试版
dotnet build ThemeSwitcher/ThemeSwitcher.csproj -c Debug -p:Platform=x64

# 发布版
dotnet build ThemeSwitcher/ThemeSwitcher.csproj -c Release -p:Platform=x64
```

### 3. 构建 ARM64 版本

```bash
dotnet build ThemeSwitcher/ThemeSwitcher.csproj -c Release -p:Platform=ARM64
```

## 项目结构

```
ThemeSwitcher/
├── ThemeSwitcher.sln                  # 解决方案
├── ThemeSwitcher/                     # 主应用项目 (WinUI 3)
│   ├── ThemeSwitcher.csproj
│   ├── App.xaml / App.xaml.cs         # 应用入口
│   ├── MainWindow.xaml / .cs          # 主窗口
│   ├── Models/
│   │   └── AppSettings.cs             # 设置模型
│   ├── Services/
│   │   ├── ThemeService.cs            # 主题切换（注册表）
│   │   ├── ScheduleService.cs         # 定时调度
│   │   ├── SunriseSunsetService.cs    # 日出日落计算
│   │   ├── LocationService.cs         # 地理位置
│   │   ├── SettingsService.cs         # 设置持久化
│   │   └── StartupService.cs          # 开机自启管理（注册表方式）
│   ├── Properties/PublishProfiles/    # 发布配置 (x64/ARM64)
│   └── Assets/                        # 应用资源
├── ThemeSwitcher.Package/             # MSIX 打包项目（可选）
│   ├── ThemeSwitcher.Package.wapproj
│   └── Images/                        # 包图标资源
└── .github/workflows/build.yml        # CI 构建验证
```

## 实现原理

### 主题切换

通过修改注册表实现系统级主题切换：

```
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize
  ├── AppsUseLightTheme    (DWORD: 1=浅色, 0=深色)  应用主题
  └── SystemUsesLightTheme (DWORD: 1=浅色, 0=深色)  系统主题
```

修改后广播 `WM_SETTINGCHANGE` 消息通知系统立即生效。

### 日出日落计算

基于 NOAA 太阳位置算法的简化实现，输入经纬度和日期即可计算当地日出日落时间，无需联网。

### 定时调度

后台 `Timer` 每 30 秒检查一次当前时间，根据设置的模式判断是否需要切换主题，使用 `SemaphoreSlim` 防止并发竞态。

## 许可证

MIT
