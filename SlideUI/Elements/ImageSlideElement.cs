using System;
using Microsoft.Xna.Framework;
using SilkyUIFramework.Elements;
using SlideUI.Core;

namespace SlideUI.Elements;

/// <summary>
/// 图片幻灯片元素：包装 SilkyUI 的 <see cref="SUIImage"/>，
/// 可动画位置、尺寸、缩放（<see cref="SlideElementState.Scale"/> 映射到图片缩放）、颜色与透明度。
/// <para>
/// 当 <see cref="FrameRect"/> 被设置时（纹理为动画雪碧图，只显示其中一帧），
/// 会自动补偿 <see cref="SUIImage"/> 按整张纹理居中导致的偏移，让该帧在容器中居中，
/// 且随缩放动画实时保持居中。
/// </para>
/// </summary>
public class ImageSlideElement : SlideElement
{
    /// <summary>被包装的 SilkyUI 图片控件。</summary>
    public SUIImage View { get; }

    /// <summary>
    /// 可选：要显示的帧矩形（如雪碧图的第一帧）。
    /// <see langword="null"/> 表示显示整张纹理。
    /// </summary>
    public Rectangle? FrameRect { get; set; }

    public ImageSlideElement(string id, SUIImage view) : base(id)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
    }

    /// <inheritdoc />
    public override void ApplyState(SlideElementState state)
    {
        var pos = EffectivePosition(state);
        View.SetLeft(pos.X);
        View.SetTop(pos.Y);

        // Size 为零表示保持自然尺寸
        if (state.Size != Vector2.Zero)
            View.SetSize(state.Size.X, state.Size.Y);

        View.ImageColor = state.Color * state.Opacity;

        if (FrameRect is { } frame)
        {
            // 雪碧图单帧：SUIImage 的 ImageAlign 按整张纹理（含所有帧）居中，
            // 这里补偿帧高差，保证当前帧在容器中居中。
            View.SourceRectangle = frame;
            View.ImageOriginPercent = Vector2.Zero;
            View.ImageScale = state.Scale;
            View.ImageAlign = new Vector2(0.5f, 0.5f);
            View.ImageOffset = (View.ImageOriginalSize - new Vector2(frame.Width, frame.Height)) * state.Scale * 0.5f;
        }
        else
        {
            View.ImageScale = state.Scale;
        }
    }

    /// <inheritdoc />
    public override SlideElementState GetCurrentState()
    {
        var color = View.ImageColor;
        return new SlideElementState
        {
            Position = new Vector2(View.Left.Pixels, View.Top.Pixels),
            Opacity = color.A / 255f,
            Scale = View.ImageScale,
            Rotation = 0f,
            Color = new Color(color.R, color.G, color.B),
            Size = new Vector2(View.OuterBounds.Width, View.OuterBounds.Height),
            BorderRadius = View.BorderRadius,
        };
    }

    /// <inheritdoc />
    public override void SetVisible(bool visible) => View.Invalid = !visible;
}
