using System;

namespace SlideUI.Core;

/// <summary>
/// 元素联动（Binding）：监听<b>源元素</b>的某个属性（如按钮的 IsHovered），
/// 当其变化时通过 <see cref="Transform"/> 映射为<b>目标元素</b>某个属性的值，
/// 并用 <see cref="Transition"/> / <see cref="Duration"/> 驱动目标平滑转场。
/// <para>
/// 当源属性回到<b>初始值</b>（例如按钮取消悬停）时，目标会自动恢复到
/// 当前场景分配给它的基准状态值（<see cref="SlideElement.SceneState"/>），
/// 从而避免写死"恢复值"与场景基础状态不一致的问题。
/// </para>
/// </summary>
public class Binding
{
    /// <summary>源元素 Id（在所属场景内解析）。</summary>
    public string SourceElementId { get; }

    /// <summary>源属性名（如 "IsHovered"，或任意状态属性名）。</summary>
    public string SourceProperty { get; }

    /// <summary>目标元素 Id（在所属场景内解析）。</summary>
    public string TargetElementId { get; }

    /// <summary>目标属性名（如 "Color"、"Scale"、"Opacity"）。</summary>
    public string TargetProperty { get; }

    /// <summary>把源属性值映射为目标属性值的委托；为 null 时直接透传源值。</summary>
    public Func<object, object> Transform { get; }

    /// <summary>联动触发的转场曲线。</summary>
    public ITransition Transition { get; set; } = EaseInOutTransition.Instance;

    /// <summary>联动转场的时长（秒）。</summary>
    public float Duration { get; set; } = 0.2f;

    private object _initialSourceValue;
    private object _lastSourceValue;
    private bool _initialized;

    public Binding(
        string sourceElementId,
        string sourceProperty,
        string targetElementId,
        string targetProperty,
        Func<object, object> transform = null)
    {
        SourceElementId = sourceElementId ?? throw new ArgumentNullException(nameof(sourceElementId));
        SourceProperty = sourceProperty ?? throw new ArgumentNullException(nameof(sourceProperty));
        TargetElementId = targetElementId ?? throw new ArgumentNullException(nameof(targetElementId));
        TargetProperty = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));
        Transform = transform;
    }

    /// <summary>
    /// 预初始化：在场景构建（元素尚未被交互、源属性处于静止值）时捕获基准源值。
    /// <para>
    /// 由 <see cref="Scene.AddBinding"/> 在绑定注册时调用。否则若推迟到进入场景后的第一帧才捕获，
    /// 用户点击页码按钮切页时鼠标正悬停在按钮上，会把"激活值"误记为基准值，
    /// 导致悬停 / 移开（应用 / 恢复）的逻辑整体颠倒。
    /// </para>
    /// </summary>
    public void Initialize(SlideElement source)
    {
        if (source == null) return;

        _initialSourceValue = source.GetProperty(SourceProperty);
        _lastSourceValue = _initialSourceValue;
        _initialized = true;
    }

    /// <summary>
    /// 每帧调用：读取源属性，若与上一次不同则驱动目标属性转场。
    /// 首次调用仅记录初始源值（不触发），用于后续"恢复基准状态"判断。
    /// </summary>
    public void Update(SlideElement source, SlideElement target)
    {
        if (source == null || target == null) return;

        var value = source.GetProperty(SourceProperty);

        if (!_initialized)
        {
            _initialized = true;
            _initialSourceValue = value;
            _lastSourceValue = value;
            return;
        }

        if (Equals(_lastSourceValue, value)) return;
        _lastSourceValue = value;

        // 源属性回到初始值 → 恢复目标元素的场景基准值；否则用 Transform 映射
        object transformed = Equals(value, _initialSourceValue)
            ? target.SceneState?.GetProperty(TargetProperty)
            : Transform?.Invoke(value) ?? value;

        target.SetTargetProperty(TargetProperty, transformed, Transition, Duration);
    }
}
