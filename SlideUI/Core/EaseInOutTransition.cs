using System;

namespace SlideUI.Core;

/// <summary>
/// 缓入缓出转场：使用 smoothstep 曲线（中间快、两端缓），
/// 是最常用的"自然"转场效果。
/// </summary>
public sealed class EaseInOutTransition : ITransition
{
    /// <summary>共享实例（无状态，可复用）。</summary>
    public static readonly EaseInOutTransition Instance = new();

    public float Calculate(float time)
    {
        time = Math.Clamp(time, 0f, 1f);
        return time * time * (3f - 2f * time);
    }
}
