using System;

namespace SlideUI.Core;

/// <summary>
/// 三次贝塞尔曲线转场（CSS cubic-bezier 风格）。
/// 通过两个控制点 (X1,Y1) 与 (X2,Y2) 定义缓动曲线，X1、X2 应位于 [0,1]。
/// 实现方式：把 time 作为曲线 X 轴目标值，用牛顿迭代解出参数 t，再求 Y 值。
/// </summary>
public sealed class BezierTransition : ITransition
{
    // 常用预设（与 CSS 缓动函数一致）
    public static readonly BezierTransition Ease = new(0.25f, 0.1f, 0.25f, 1f);
    public static readonly BezierTransition EaseIn = new(0.42f, 0f, 1f, 1f);
    public static readonly BezierTransition EaseOut = new(0f, 0f, 0.58f, 1f);
    public static readonly BezierTransition EaseInOut = new(0.42f, 0f, 0.58f, 1f);

    public float X1 { get; }
    public float Y1 { get; }
    public float X2 { get; }
    public float Y2 { get; }

    public BezierTransition(float x1, float y1, float x2, float y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }

    public float Calculate(float time)
    {
        if (time <= 0f) return 0f;
        if (time >= 1f) return 1f;

        // 牛顿迭代解 x(t) = time，得到参数 t，再求 y(t)
        float t = time;
        for (int i = 0; i < 8; i++)
        {
            float x = SampleX(t);
            float dx = DerivativeX(t);
            if (MathF.Abs(dx) < 1e-6f) break;
            t -= (x - time) / dx;
            t = Math.Clamp(t, 0f, 1f);
        }

        return SampleY(t);
    }

    // 三次贝塞尔：B(t) = 3(1-t)²t·P1 + 3(1-t)t²·P2 + t³（P0=(0,0)，P3=(1,1)）
    private float SampleX(float t) => CubicBezier(t, X1, X2);
    private float SampleY(float t) => CubicBezier(t, Y1, Y2);
    private float DerivativeX(float t) => CubicBezierDerivative(t, X1, X2);

    private static float CubicBezier(float t, float p1, float p2)
    {
        float mt = 1f - t;
        return 3f * mt * mt * t * p1 + 3f * mt * t * t * p2 + t * t * t;
    }

    private static float CubicBezierDerivative(float t, float p1, float p2)
    {
        float mt = 1f - t;
        return 3f * mt * mt * p1 + 6f * mt * t * (p2 - p1) + 3f * t * t * (1f - p2);
    }
}
