using System;
using Microsoft.Xna.Framework;
using SilkyUIFramework.Elements;
using SlideUI.Core;

namespace SlideUI.Elements;

/// <summary>
/// 形状幻灯片元素：包装一个普通的 <see cref="UIView"/>（矩形 / 圆角面板），
/// 可动画位置、尺寸、圆角、背景色与透明度。
/// </summary>
public class ShapeSlideElement : SlideElement
{
    /// <summary>被包装的 SilkyUI 视图（作为形状的面板）。</summary>
    public UIView View { get; }

    public ShapeSlideElement(string id, UIView view) : base(id)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
    }

    /// <inheritdoc />
    public override void ApplyState(SlideElementState state)
    {
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

        View.BackgroundColor = state.Color * state.Opacity;
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
        };
    }

    /// <inheritdoc />
    public override void SetVisible(bool visible) => View.Invalid = !visible;
}
