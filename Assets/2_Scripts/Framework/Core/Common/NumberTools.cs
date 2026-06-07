using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 数值格式化工具 —— 将大数转换为带单位的简写形式。
/// 示例：1234 → "1.2K"，1234567 → "1.2M"
/// </summary>
public class NumberTools
{
    private static readonly string[] Units = { "", "K", "M", "T", "P", "E", "Z" };

    /// <summary>
    /// 将数值转换为带单位的简写字符串（保留 1 位小数）。
    /// </summary>
    /// <param name="value">原始数值</param>
    /// <returns>格式化后的字符串，如 "1.2K"、"3.5M"</returns>
    public static string GetNumberConversion(long value)
    {
        if (value < 1000)
            return value.ToString();

        // 计算单位层级：每 1000 一级
        int unitIndex = 0;
        double scaled = value;
        while (scaled >= 1000 && unitIndex < Units.Length - 1)
        {
            scaled /= 1000;
            unitIndex++;
        }

        // 保留 1 位小数，去掉末尾的 .0
        string result = scaled.ToString("0.0") + Units[unitIndex];
        return result.Replace(".0", "");
    }

    /// <summary>
    /// 将数值转换为带单位的简写字符串（保留指定小数位数）。
    /// </summary>
    /// <param name="value">原始数值</param>
    /// <param name="decimalPlaces">保留小数位数（默认 1）</param>
    /// <returns>格式化后的字符串</returns>
    public static string GetNumberConversion(long value, int decimalPlaces)
    {
        if (value < 1000)
            return value.ToString();

        int unitIndex = 0;
        double scaled = value;
        while (scaled >= 1000 && unitIndex < Units.Length - 1)
        {
            scaled /= 1000;
            unitIndex++;
        }

        string format = "0." + new string('0', decimalPlaces);
        string result = scaled.ToString(format) + Units[unitIndex];
        // 去掉末尾的无效零（如 "1.00K" → "1K"）
        result = result.TrimEnd('0').TrimEnd('.');
        return result + Units[unitIndex];
    }
}
