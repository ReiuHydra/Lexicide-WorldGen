namespace SlideUI.Core;

/// <summary>
/// 线性插值转场：progress == time，元素匀速过渡。
/// </summary>
public sealed class LinearTransition : ITransition
{
    /// <summary>共享实例（线性转场无状态，可复用）。</summary>
    public static readonly LinearTransition Instance = new();

    public float Calculate(float time) => time;
}
