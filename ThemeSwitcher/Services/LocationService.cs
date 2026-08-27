using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace ThemeSwitcher.Services;

/// <summary>
/// 地理位置服务
/// 获取系统当前位置（经纬度）用于日出日落计算
/// </summary>
public class LocationService
{
    private readonly Geolocator _geolocator;

    public LocationService()
    {
        _geolocator = new Geolocator
        {
            DesiredAccuracy = PositionAccuracy.Default,
            MovementThreshold = 5000 // 5公里变化才触发更新
        };
    }

    /// <summary>
    /// 获取当前位置
    /// </summary>
    /// <returns>纬度和经度，失败返回null</returns>
    public async Task<(double Latitude, double Longitude)?> GetCurrentLocationAsync()
    {
        try
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            if (accessStatus != GeolocationAccessStatus.Allowed)
                return null;

            var position = await _geolocator.GetGeopositionAsync();
            var coord = position.Coordinate;

            return (coord.Point.Position.Latitude, coord.Point.Position.Longitude);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetLocation failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 检查位置权限状态
    /// </summary>
    public async Task<bool> HasPermissionAsync()
    {
        try
        {
            var status = await Geolocator.RequestAccessAsync();
            return status == GeolocationAccessStatus.Allowed;
        }
        catch
        {
            return false;
        }
    }
}
