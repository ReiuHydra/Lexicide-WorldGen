using System;
using Microsoft.Xna.Framework;

namespace SlideUI.Effects;

/// <summary>
/// 变色特效：让文本沿一个颜色数组循环，可平滑渐变或突变切换。
/// 仅作用范围内的字符参与变色，范围外保持基础色。
/// </summary>
public sealed class ColorCycleEffect : TextEffect
{
    /// <summary>颜色数组（至少 1 项）。</summary>
    public Color[] Colors = [Color.White];

    /// <summary>走完整个颜色数组所需时间（秒）。</summary>
    public float Period = 1f;

    /// <summary>true=平滑渐变；false=突变切换。</summary>
    public bool Smooth = true;

    /// <summary>相邻字符的相位偏移（相对一个周期），用于彩虹波浪；默认 0。</summary>
    public float CharacterOffset = 0f;

    /// <summary>字符 <paramref name="charIndex"/> 在累计时间 <paramref name="elapsed"/> 处的颜色（色调，叠加在基础色上）。</summary>
    public Color ColorAt(float elapsed, int charIndex)
    {
        if (Colors == null || Colors.Length == 0) return Color.White;
        if (Colors.Length == 1) return Colors[0];

        var phase = charIndex * CharacterOffset;
        float p = CycleProgress(elapsed, Period, phase) * Colors.Length;

        if (Smooth)
        {
            int i = (int)p % Colors.Length;
            float t = p - MathF.Floor(p);
            return Color.Lerp(Colors[i], Colors[(i + 1) % Colors.Length], t);
        }

        return Colors[((int)MathF.Floor(p)) % Colors.Length];
    }
}
