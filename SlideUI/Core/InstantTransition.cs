namespace SlideUI.Core;

/// <summary>
/// 瞬间转场：progress 始终为 1，即切换<b>无过渡</b>、直接"闪现"到目标状态。
/// 用于需要无动画切换场景的场合（如初始显示、快速跳页）。
/// </summary>
public sealed class InstantTransition : ITransition
{
    /// <summary>共享实例（无状态，可复用）。</summary>
    public static readonly InstantTransition Instance = new();

    public float Calculate(float time) => 1f;
}
