using System;
using Microsoft.Xna.Framework;
using SilkyUIFramework;
using SilkyUIFramework.Elements;
using SlideUI.Core;

namespace SlideUI.Elements;

/// <summary>
/// 按钮幻灯片元素：包装一个 <see cref="UIElementGroup"/> 作为按钮（圆角背景 + 可选文本标签）。
/// <para>
/// 支持鼠标悬停变色与点击事件；位置 / 尺寸 / 圆角 / 背景色 / 透明度可参与场景动画。
/// </para>
/// </summary>
public class ButtonSlideElement : SlideElement
{
    /// <summary>被包装的按钮视图（容器）。</summary>
    public UIElementGroup View { get; }

    /// <summary>按钮上的文本标签（可为 null）。</summary>
    public UITextView Label { get; }

    /// <summary>是否被鼠标悬停（供联动 Binding 监听）。</summary>
    public override bool IsHovered => View.IsMouseHovering;

    /// <summary>按钮被点击时触发。</summary>
    public event Action Clicked;

    /// <summary>按钮基础背景色（由场景状态驱动）。</summary>
    public Color BaseColor { get; private set; }

    /// <summary>悬停时背景色。</summary>
    public Color HoverColor { get; set; } = Color.White;

    /// <summary>标签文本的基础色（构造时捕获，透明度由状态驱动）。</summary>
    private readonly Color _labelColor;

    private float _opacity = 1f;

    public ButtonSlideElement(string id, UIElementGroup view, UITextView label = null) : base(id)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
        Label = label;
        BaseColor = view.BackgroundColor;
        _labelColor = label?.TextColor ?? Color.White;

        View.LeftMouseClick += (_, _) => Clicked?.Invoke();
        View.MouseEnter += (_, _) => View.BackgroundColor = HoverColor * _opacity;
        View.MouseLeave += (_, _) => View.BackgroundColor = BaseColor * _opacity;
    }

    /// <inheritdoc />
    public override void ApplyState(SlideElementState state)
    {
        _opacity = state.Opacity;

        if (state.Text != null && Label != null)
            Label.Text = state.Text;

        // 标签文本随按钮透明度一起淡出/隐藏（否则卸载或淡出时按钮文本会残留）
        if (Label != null)
        {
            Label.TextColor = _labelColor * state.Opacity;
            Label.TextBorderColor = Color.Black * state.Opacity;
        }

        var pos = EffectivePosition(state);
        View.SetLeft(pos.X);
        View.SetTop(pos.Y);

        if (state.Size != Vector2.Zero)
            View.SetSize(state.Size.X, state.Size.Y);

        View.BorderRadius = state.BorderRadius ?? View.BorderRadius;
        if (state.Border >= 0f)
            View.Border = state.Border;
        if (state.BorderColor is { } borderColor)
            View.BorderColor = borderColor * state.Opacity;

        BaseColor = state.Color;
        View.BackgroundColor = BaseColor * state.Opacity;
    }

    /// <inheritdoc />
    public override SlideElementState GetCurrentState()
    {
        var bg = View.BackgroundColor;
        return new SlideElementState
        {
            Position = new Vector2(View.Left.Pixels, View.Top.Pixels),
            Opacity = bg.A / 255f,
            Scale = new Vector2(1f),
            Rotation = 0f,
            Color = new Color(bg.R, bg.G, bg.B),
            Size = new Vector2(View.OuterBounds.Width, View.OuterBounds.Height),
            BorderRadius = View.BorderRadius,
            Border = View.Border,
            BorderColor = View.BorderColor,
            Text = Label?.Text,
        };
    }

    /// <inheritdoc />
    public override void SetVisible(bool visible) => View.Invalid = !visible;
}
