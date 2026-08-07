using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SlideUI.Effects;

namespace SlideUI.Core;

/// <summary>
/// 所有幻灯片元素的抽象基类。
/// <para>
/// 基类只依赖 <see cref="SlideElementState"/> 这个纯数据类，完全<b>不引用 SilkyUI</b>，
/// 因此 SilkyUI 未来即使重构，核心转场逻辑也几乎不受影响（只需适配具体元素）。
/// </para>
/// <para>
/// 每个元素拥有<b>独立的场景转场时间线</b>：场景切换时由 Manager 统一设置目标状态，
/// 元素各自按自己的 <see cref="ITransition"/> 与时长推进全部属性的动画。
/// 联动（Binding）则通过<b>按属性覆盖（PropertyOverride）</b>只动画单个属性，
/// 其余属性继续沿着场景转场时间线运动，因此联动的快速动画不会打断位置/尺寸的平滑过渡。
/// </para>
/// </summary>
public abstract class SlideElement
{
    /// <summary>元素唯一标识（用于场景间按 Id 匹配，实现元素共享与联动）。</summary>
    public string Id { get; }

    private SlideElementState _currentState;

    /// <summary>
    /// 当前（可视）状态。
    /// <para>
    /// 采用惰性读取：首次访问时才调用 <see cref="GetCurrentState"/> 捕获元素实际状态。
    /// 不能在构造函数中直接捕获——此时派生类字段（如 <see cref="Elements.TextSlideElement.View"/>）
    /// 尚未赋值，调用虚方法会触发 NullReferenceException。
    /// </para>
    /// </summary>
    public SlideElementState CurrentState
    {
        get => _currentState ??= GetCurrentState();
        protected set => _currentState = value;
    }

    /// <summary>
    /// 当前场景分配给元素的基准状态（由 <see cref="SetSceneState"/> 记录）。
    /// 联动（Binding）在源属性回到初始值时，用它恢复目标属性的场景默认值。
    /// </summary>
    public SlideElementState SceneState { get; protected set; }

    /// <summary>目标状态（场景切换时设置）。</summary>
    public SlideElementState TargetState { get; protected set; }

    /// <summary>转场起始状态（开始时捕获当前可视状态）。</summary>
    protected SlideElementState FromState;

    /// <summary>是否正在播放场景转场动画。</summary>
    public bool IsTransitioning { get; protected set; }

    /// <summary>元素级交互状态（子类可重写，例如按钮的悬停状态）。</summary>
    public virtual bool IsHovered => false;

    /// <summary>
    /// 元素级抖动特效：整个元素整体偏移（对所有元素类型生效，包括文本的文本框整体）。
    /// 文本元素还可叠加 <see cref="EffectTextView.Effects"/> 的逐字符抖动，
    /// 二者可同时启用（整体偏移 + 字符内部各自动）。
    /// 为 <see langword="null"/> 时关闭。
    /// </summary>
    public ShakeEffect Shake { get; set; }

    // 场景转场时间线
    private ITransition _transition = LinearTransition.Instance;
    private float _elapsed;
    private float _duration = 1f;

    // 联动属性覆盖：只覆盖单个属性，不干扰场景转场的其他属性
    private readonly Dictionary<string, PropertyOverride> _overrides = new();

    // 元素级抖动状态：整个元素按 ShakeEffect 偏移（文本元素除外——文本用逐字符抖动）
    private float _shakeElapsed;
    private Vector2 _shakeOffset;
    private readonly int _shakeSeed;

    /// <summary>单个属性的联动覆盖：独立的时间线，驱动"该属性"从 FromValue 到 ToValue。</summary>
    private sealed class PropertyOverride
    {
        public string PropertyName;
        public object FromValue;
        public object ToValue;
        public ITransition Transition = EaseInOutTransition.Instance;
        public float Duration = 0.2f;
        public float Elapsed;
    }

    protected SlideElement(string id)
    {
        Id = id;
        _shakeSeed = StableHash(id);
    }

    /// <summary>应用状态时应使用的位置（基准位置 + 元素级抖动偏移）。对所有元素类型生效。</summary>
    protected Vector2 EffectivePosition(SlideElementState state) => state.Position + _shakeOffset;

    /// <summary>记录场景分配给该元素的基准状态（供联动恢复用），并确保元素可见。</summary>
    public void SetSceneState(SlideElementState sceneState)
    {
        SceneState = sceneState?.Clone();
        SetVisible(true);
    }

    /// <summary>
    /// 开始一次到目标状态的场景转场。自动捕获当前可视状态作为起始状态，
    /// 并用指定的 <see cref="ITransition"/> 与时长推进全部属性。
    /// 会清除所有联动的属性覆盖（新场景接管控制权）。
    /// </summary>
    public void BeginTransition(SlideElementState target, ITransition transition, float duration)
    {
        _overrides.Clear();
        FromState = GetCurrentState();
        TargetState = target?.Clone() ?? FromState.Clone();
        _transition = transition ?? LinearTransition.Instance;
        _duration = Math.Max(duration, 0.01f);
        _elapsed = 0f;
        IsTransitioning = true;
    }

    /// <summary>以默认参数（线性、1 秒）开始一次转场。等价于 <see cref="BeginTransition"/> 的简写。</summary>
    public void SetTargetState(SlideElementState targetState)
        => BeginTransition(targetState, LinearTransition.Instance, 1f);

    /// <summary>
    /// 每帧推进该元素自身的动画（由 SlideShowManager 在主循环调用）。
    /// <para>
    /// 性能优化：当元素既无场景转场、又无联动覆盖时（完全静止），
    /// 直接返回而不重复 Set* / MarkLayoutDirty，避免每帧触发不必要的布局重算。
    /// </para>
    /// <para>子类可重写：在调用 base 之前先推进自身的独立逻辑（如文本特效的计时）。</para>
    /// </summary>
    public virtual void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // 0) 元素级抖动：每帧推进计时并计算当前偏移（独立于场景转场）
        _shakeElapsed += dt;
        _shakeOffset = Shake is { Enabled: true } && Shake.Amplitude > 0f
            ? Shake.Offset(_shakeElapsed, _shakeSeed)
            : Vector2.Zero;

        // 1) 场景转场插值
        if (IsTransitioning && FromState != null && TargetState != null)
        {
            _elapsed += dt;
            float t = MathHelper.Clamp(_elapsed / _duration, 0f, 1f);
            float progress = _transition.Calculate(t);
            CurrentState = SlideElementState.Lerp(FromState, TargetState, progress);

            // 时间耗尽或转场曲线已到达终点（如 InstantTransition 第一帧即为 1）→ 立即完成
            if (t >= 1f || progress >= 1f)
                FinishTransition();

            // 2) 应用联动属性覆盖并更新到实际元素
            ApplyOverrides(gameTime);
            ApplyState(CurrentState);
            return;
        }

        // 无场景转场但有联动覆盖：以场景基准为底，套用覆盖
        if (_overrides.Count > 0)
        {
            CurrentState = SceneState?.Clone() ?? CurrentState;
            ApplyOverrides(gameTime);
            ApplyState(CurrentState);
            return;
        }

        // 完全静止：若启用了元素级抖动，需每帧重新应用（位置随抖动变化）；否则无需任何操作
        if (_shakeOffset != Vector2.Zero)
            ApplyState(CurrentState);
    }

    /// <summary>把 Id 稳定哈希为整数，用作抖动的相位种子（让不同元素错峰抖动）。</summary>
    private static int StableHash(string s)
    {
        unchecked
        {
            int hash = 17;
            if (s != null)
                foreach (var c in s)
                    hash = hash * 31 + c;
            return hash;
        }
    }

    /// <summary>结束场景转场，把元素锁定到目标状态。</summary>
    public void FinishTransition()
    {
        if (TargetState != null)
        {
            CurrentState = TargetState.Clone();
            ApplyState(CurrentState);
        }

        TargetState = null;
        FromState = null;
        IsTransitioning = false;
    }

    /// <summary>无动画地立即应用一个状态（用于场景初始显示）。</summary>
    public void ApplyStateImmediately(SlideElementState state)
    {
        _overrides.Clear();
        CurrentState = state.Clone();
        ApplyState(state);
        TargetState = null;
        FromState = null;
        IsTransitioning = false;
    }

    /// <summary>按名称读取属性（状态属性 + 元素级属性如 IsHovered）。供联动 Binding 使用。</summary>
    public virtual object GetProperty(string name) => name switch
    {
        "IsHovered" => IsHovered,
        _ => CurrentState.GetProperty(name),
    };

    /// <summary>
    /// 联动：仅覆盖<b>一个属性</b>并启动该属性的快速动画。
    /// 其余属性继续沿着场景转场时间线运动，互不干扰。
    /// </summary>
    public void SetTargetProperty(string name, object value, ITransition transition, float duration)
    {
        object from;
        try
        {
            from = CurrentState.GetProperty(name);
        }
        catch (ArgumentException)
        {
            // 当前状态没有该属性时，以场景基准值兜底
            from = SceneState?.GetProperty(name) ?? value;
        }

        _overrides[name] = new PropertyOverride
        {
            PropertyName = name,
            FromValue = from,
            ToValue = value,
            Transition = transition ?? EaseInOutTransition.Instance,
            Duration = Math.Max(duration, 0.01f),
            Elapsed = 0f,
        };
    }

    /// <summary>推进所有联动属性覆盖并写入当前状态。</summary>
    private void ApplyOverrides(GameTime gameTime)
    {
        if (_overrides.Count == 0) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        foreach (var pair in _overrides)
        {
            var o = pair.Value;
            o.Elapsed += dt;

            object value = o.Elapsed >= o.Duration
                ? o.ToValue // 覆盖完成：保持最终值，直到被替换或场景接管
                : InterpolateProperty(o.PropertyName, o.FromValue, o.ToValue,
                    o.Transition.Calculate(o.Elapsed / o.Duration));

            CurrentState.SetProperty(o.PropertyName, value);
        }
    }

    /// <summary>按属性类型对 from/to 插值。</summary>
    private static object InterpolateProperty(string name, object from, object to, float t)
    {
        switch (name)
        {
            case "Position": return Vector2.Lerp((Vector2)from, (Vector2)to, t);
            case "Opacity": return MathHelper.Lerp((float)from, (float)to, t);
            case "Scale": return Vector2.Lerp((Vector2)from, (Vector2)to, t);
            case "Rotation": return MathHelper.Lerp((float)from, (float)to, t);
            case "Color": return Color.Lerp((Color)from, (Color)to, t);
            case "Size": return Vector2.Lerp((Vector2)from, (Vector2)to, t);
            case "BorderRadius":
                return from is Vector4 f && to is Vector4 t2 ? Vector4.Lerp(f, t2, t) : to;
            default: return t >= 0.5f ? to : from;
        }
    }

    /// <summary>把状态应用到实际元素上（具体元素在此集成 SilkyUI 控件）。</summary>
    public abstract void ApplyState(SlideElementState state);

    /// <summary>读取元素当前可视状态，用于生成转场起始状态快照。</summary>
    public abstract SlideElementState GetCurrentState();

    /// <summary>
    /// 显示或隐藏整个元素（含所有子视图，如按钮标签）。
    /// 隐藏后该元素不再参与布局 / 绘制 / 鼠标交互，用于"未加载 / 卸载"时的干净隐藏，
    /// 避免逐个清空背景、边框、文字等属性造成残留。
    /// </summary>
    public abstract void SetVisible(bool visible);
}
