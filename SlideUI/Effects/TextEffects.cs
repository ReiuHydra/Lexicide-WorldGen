using System;
using Microsoft.Xna.Framework;

namespace SlideUI.Effects;

/// <summary>抖动方式。</summary>
public enum ShakeStyle
{
    /// <summary>随机抖动（颤抖）：每个周期在幅度内取一次随机偏移。</summary>
    Jitter,

    /// <summary>上下跳动（y = |sin x|）：字符周期性地向上弹跳再落回。</summary>
    Bounce,
}

/// <summary>
/// 文本特效配置的集合，挂在 <see cref="EffectTextView.Effects"/> 上。
/// 每个特效都可为 <see langword="null"/>（关闭）。特效不依赖场景切换，由文本元素逐帧驱动。
/// </summary>
public sealed class TextEffects
{
    /// <summary>逐字符抖动特效（为 null 表示关闭）。与元素基类 <see cref="Core.SlideElement.Shake"/> 的整体抖动可叠加。</summary>
    public ShakeEffect CharShake;

    /// <summary>打字机特效（为 null 表示关闭）。</summary>
    public TypewriterEffect Typewriter;

    /// <summary>变色特效（为 null 表示关闭）。</summary>
    public ColorCycleEffect ColorCycle;
}

/// <summary>
/// 文本特效基类：每个特效只作用于文本中的一段（<see cref="Start"/> / <see cref="Length"/>），
/// 默认作用于整段文本。
/// </summary>
public abstract class TextEffect
{
    /// <summary>是否启用。</summary>
    public bool Enabled = true;

    /// <summary>作用范围的起始字符下标（含，相对整段文本）。</summary>
    public int Start;

    /// <summary>作用范围的字符数；负数表示到文本末尾。</summary>
    public int Length = -1;

    /// <summary>字符 <paramref name="index"/> 是否落在作用范围内。</summary>
    public bool InRange(int index, int textLength)
        => index >= Start && index < textLength && (Length < 0 || index < Start + Length);

    /// <summary>把"累计时间 + 相位偏移"归一到单个周期内的进度（0..1）。</summary>
    protected static float CycleProgress(float elapsed, float period, float phase)
    {
        if (period <= 0f) return 0f;
        float p = elapsed / period + phase;
        p -= (float)Math.Floor(p);
        return p;
    }
}

/// <summary>抖动特效：随机颤抖或上下跳动。</summary>
public sealed class ShakeEffect : TextEffect
{
    /// <summary>抖动幅度（像素）。</summary>
    public float Amplitude = 4f;

    /// <summary>每次抖动的持续时间（秒）。随机抖动每周期重新取一次随机偏移；跳动每周期完成一次起落。</summary>
    public float Period = 0.1f;

    /// <summary>抖动方式（随机颤抖 / 上下跳动）。</summary>
    public ShakeStyle Style = ShakeStyle.Jitter;

    /// <summary>相邻字符的相位偏移（相对一个周期），用于错峰 / 波浪效果；默认 0.1 让字符错开。</summary>
    public float CharacterOffset = 0.1f;

    /// <summary>计算字符 <paramref name="charIndex"/> 在累计时间 <paramref name="elapsed"/> 处的偏移（像素）。</summary>
    public Vector2 Offset(float elapsed, int charIndex)
    {
        if (Amplitude <= 0f) return Vector2.Zero;
        // 相位只取 [0,1) 的小数部分：元素级抖动会传入很大的 Id 哈希作为 charIndex，
        // 若不归一化，与小的 elapsed/period 相加会因浮点精度丢失导致相位冻结（元素几乎不动）。
        var phase = (charIndex * CharacterOffset) % 1f;

        return Style switch
        {
            ShakeStyle.Bounce => new Vector2(0f,
                -Amplitude * Math.Abs(MathF.Sin(MathHelper.TwoPi * CycleProgress(elapsed, Period, phase)))),
            _ => JitterOffset(elapsed, Period, phase) * Amplitude,
        };
    }

    private static Vector2 JitterOffset(float elapsed, float period, float phase)
    {
        // 每个周期（含相位）取一次稳定的伪随机偏移，避免每帧高频闪烁。
        int cycle = (int)MathF.Floor(elapsed / Math.Max(period, 1e-4f) + phase);
        return new Vector2(Hash(cycle, 0), Hash(cycle, 1)) * 2f - Vector2.One;
    }

    /// <summary>把整数种子哈希到 [0,1) 的确定性伪随机数。</summary>
    private static float Hash(int a, int b)
    {
        uint x = (uint)(a * 374761393 + b * 668265263);
        x = (x ^ (x >> 13)) * 1274126177u;
        x ^= x >> 16;
        return (x & 0xFFFFFF) / (float)0x1000000;
    }
}

/// <summary>
/// 打字机特效：按间隔逐字显示。仅作用范围内的字符会"打字"出现；
/// 范围外的字符始终可见。文本变化时自动从头开始。
/// </summary>
public sealed class TypewriterEffect : TextEffect
{
    /// <summary>放置下一个字符（含空格）前等待的时间（秒）。</summary>
    public float Interval = 0.06f;
}
