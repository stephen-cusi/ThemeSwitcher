using System;

namespace ThemeSwitcher.Services;

/// <summary>
/// 日出日落时间计算服务
/// 基于 NOAA 太阳位置算法的简化实现
/// </summary>
public class SunriseSunsetService
{
    /// <summary>
    /// 计算指定日期和位置的日出日落时间（本地时间）
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="latitude">纬度（正值为北纬）</param>
    /// <param name="longitude">经度（正值为东经）</param>
    /// <returns>日出和日落的本地时间</returns>
    public (DateTime Sunrise, DateTime Sunset) Calculate(DateTime date, double latitude, double longitude)
    {
        // 使用 UTC 日期计算
        var utcDate = date.Date.ToUniversalTime();
        int dayOfYear = utcDate.DayOfYear;

        // 计算太阳赤纬
        double declination = CalculateDeclination(dayOfYear);

        // 计算时差（分钟）
        double equationOfTime = CalculateEquationOfTime(dayOfYear);

        // 计算日出日落时角（度）
        double latRad = ToRadians(latitude);
        double decRad = ToRadians(declination);

        double cosHourAngle = -Math.Tan(latRad) * Math.Tan(decRad);
        // 极昼/极夜处理
        if (cosHourAngle > 1) cosHourAngle = 1;
        if (cosHourAngle < -1) cosHourAngle = -1;

        double hourAngle = ToDegrees(Math.Acos(cosHourAngle));

        // 太阳正午（UTC，分钟）
        double solarNoon = 720 - 4 * longitude - equationOfTime;

        // 日出日落（UTC，分钟）
        double sunriseUTC = solarNoon - 4 * hourAngle;
        double sunsetUTC = solarNoon + 4 * hourAngle;

        // 转换为本地时间
        var sunrise = utcDate.Date.AddMinutes(sunriseUTC).ToLocalTime();
        var sunset = utcDate.Date.AddMinutes(sunsetUTC).ToLocalTime();

        // 如果计算结果跨天，修正到目标日期
        if (sunrise.Date != date.Date)
            sunrise = sunrise.Date == date.Date.AddDays(-1)
                ? sunrise.AddDays(1) : sunrise.AddDays(-1);
        if (sunset.Date != date.Date)
            sunset = sunset.Date == date.Date.AddDays(-1)
                ? sunset.AddDays(1) : sunset.AddDays(-1);

        return (sunrise, sunset);
    }

    /// <summary>
    /// 计算太阳赤纬角（度）
    /// </summary>
    private static double CalculateDeclination(int dayOfYear)
    {
        // 近似公式：赤纬 = 23.45 * sin(360/365 * (284 + N))
        double angle = 360.0 / 365.0 * (284 + dayOfYear);
        return 23.45 * Math.Sin(ToRadians(angle));
    }

    /// <summary>
    /// 计算时差（分钟）
    /// </summary>
    private static double CalculateEquationOfTime(int dayOfYear)
    {
        double B = ToRadians(360.0 / 365.0 * (dayOfYear - 81));
        return 9.87 * Math.Sin(2 * B) - 7.53 * Math.Cos(B) - 1.5 * Math.Sin(B);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    private static double ToDegrees(double radians) => radians * 180.0 / Math.PI;
}
