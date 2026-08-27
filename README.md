# 主题切换器 (ThemeSwitcher)

基于 WinUI 3 的 Windows 深浅色主题自动切换工具，支持 Windows 10 1809+ / Windows 11，x64 与 ARM64 架构。

## 功能特性

- **手动切换**：一键切换系统深浅色主题
- **定时切换**：自定义浅色/深色开始时间
- **日出日落自动切换**：基于系统地理位置，日出后切浅色、日落后切深色
- **切换范围可控**：可分别控制系统主题（任务栏/资源管理器）和应用主题
- **开机自启**：支持 MSIX StartupTask 开机自启动
- **跨架构**：支持 x64 和 ARM64（WOA 设备如 Surface Pro X 等）

## 系统要求

- Windows 10 版本 1809 (build 17763) 或更高 / Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) 17.8+，需安装以下工作负载：
  - **使用 .NET 的桌面开发**
  - **通用 Windows 平台开发**（UWP 工具集）
  - **Windows 应用 SDK C# 开发工具**（单项目 MSIX 打包支持）

## 构建步骤

### 1. 还原依赖

```bash
dotnet restore ThemeSwitcher.sln
```

### 2. 构建 x64 版本

```bash
# 调试版
dotnet build ThemeSwitcher.sln -c Debug -p:Platform=x64

# 发布版
dotnet build ThemeSwitcher.sln -c Release -p:Platform=x64
```

### 3. 构建 ARM64 版本

```bash
dotnet build ThemeSwitcher.sln -c Release -p:Platform=ARM64
```

### 4. 生成 MSIX 安装包

在 Visual Studio 中：

1. 右键 `ThemeSwitcher.Package` 项目 → **发布** → **创建应用程序包**
2. 选择旁加载 (Sideloading)
3. 选择架构：勾选 **x64** 和 **ARM64**
4. 选择或创建签名证书（测试用可自动生成临时证书）
5. 点击创建，输出 MSIX 包到 `AppPackages` 目录

命令行方式（需先配置签名证书）：

```bash
msbuild ThemeSwitcher.Package/ThemeSwitcher.Package.wapproj /p:Configuration=Release /p:Platform=x64 /t:Publish /p:AppxPackageDir=..\AppPackages\x64\
msbuild ThemeSwitcher.Package/ThemeSwitcher.Package.wapproj /p:Configuration=Release /p:Platform=ARM64 /t:Publish /p:AppxPackageDir=..\AppPackages\ARM64\
```

### 5. 安装 MSIX

1. 双击生成的 `.msix` 文件
2. 点击「安装」
3. 首次运行时系统会请求位置权限（用于日出日落模式），请允许

> 注意：如果使用自签名证书，需要先将证书安装到「受信任的根证书颁发机构」。

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
│   │   └── StartupService.cs          # 开机自启管理
│   ├── Properties/PublishProfiles/    # 发布配置 (x64/ARM64)
│   └── Assets/                        # 应用资源
└── ThemeSwitcher.Package/             # MSIX 打包项目
    ├── ThemeSwitcher.Package.wapproj
    ├── Package.appxmanifest           # 应用清单（含位置权限+自启）
    └── Images/                        # 包图标资源
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

后台 `Timer` 每 30 秒检查一次当前时间，根据设置的模式判断是否需要切换主题，避免重复切换。

## 自定义

- 修改 `Package.appxmanifest` 中的 `Publisher` 和应用名称
- 替换 `ThemeSwitcher.Package/Images/` 下的图标为自己的设计
- 在 `ThemeSwitcher.csproj` 中调整 `Version` 版本号

## 许可证

MIT
