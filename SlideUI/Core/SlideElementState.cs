using System;
using Microsoft.Xna.Framework;

namespace SlideUI.Core;

/// <summary>
/// 幻灯片元素的可动画状态快照。
/// 这是与 SilkyUI 完全解耦的纯数据类，只描述"元素应该处于什么状态"，
/// 由具体元素（如 <see cref="Elements.TextSlideElement"/>）负责映射到实际控件。
/// </summary>
public sealed class SlideElementState
{
    /// <summary>元素位置（像素偏移，具体语义由元素实现决定，文本元素为 Left/Top 锚点）。</summary>
    public Vector2 Position;

    /// <summary>透明度，0..1。</summary>
    public float Opacity = 1f;

    /// <summary>缩放（目前文本元素使用 X 分量作为字号缩放）。</summary>
    public Vector2 Scale = Vector2.One;

    /// <summary>旋转（弧度）。</summary>
    public float Rotation;

    /// <summary>颜色（RGB 通道；透明度请使用 <see cref="Opacity"/>）。</summary>
    public Color Color = Color.White;

    /// <summary>
    /// 非动画属性：文本内容。切换场景时立即应用，不参与插值。
    /// </summary>
    public string Text;

    /// <summary>
    /// 元素宽高（像素）。<see cref="Vector2.Zero"/> 表示"保持元素自然尺寸"（不改变）。
    /// 形状 / 图片元素使用；文本元素自动适配字号，忽略此字段。
    /// </summary>
    public Vector2 Size;

    /// <summary>
    /// 圆角半径（左上、右上、右下、左下）。<see langword="null"/> 表示不改变。
    /// 形状 / 按钮元素使用。
    /// </summary>
    public Vector4? BorderRadius;

    /// <summary>
    /// 边框宽度（像素）。负数（如 -1）表示不改变。<see cref="Elements.ShapeSlideElement"/> 使用。
    /// </summary>
    public float Border = -1f;

    /// <summary>
    /// 边框颜色。<see langword="null"/> 表示不改变。<see cref="Elements.ShapeSlideElement"/> 使用。
    /// </summary>
    public Color? BorderColor;

    /// <summary>深度复制当前状态。</summary>
    public SlideElementState Clone() => new()
    {
        Position = Position,
        Opacity = Opacity,
        Scale = Scale,
        Rotation = Rotation,
        Color = Color,
        Text = Text,
        Size = Size,
        BorderRadius = BorderRadius,
        Border = Border,
        BorderColor = BorderColor,
    };

    /// <summary>按名称读取属性（用于联动 Binding）。</summary>
    public object GetProperty(string name) => name switch
    {
        "Position" => Position,
        "Opacity" => Opacity,
        "Scale" => Scale,
        "Rotation" => Rotation,
        "Color" => Color,
        "Text" => Text,
        "Size" => Size,
        "BorderRadius" => BorderRadius,
        "Border" => Border,
        "BorderColor" => BorderColor,
        _ => throw new ArgumentException($"未知状态属性: {name}"),
    };

    /// <summary>按名称设置属性（用于联动 Binding）。</summary>
    public void SetProperty(string name, object value)
    {
        switch (name)
        {
            case "Position": Position = (Vector2)value; break;
            case "Opacity": Opacity = (float)value; break;
            case "Scale": Scale = (Vector2)value; break;
            case "Rotation": Rotation = (float)value; break;
            case "Color": Color = (Color)value; break;
            case "Text": Text = (string)value; break;
            case "Size": Size = (Vector2)value; break;
            case "BorderRadius": BorderRadius = (Vector4?)value; break;
            case "Border": Border = (float)value; break;
            case "BorderColor": BorderColor = (Color?)value; break;
            default: throw new ArgumentException($"未知状态属性: {name}");
        }
    }

    /// <summary>
    /// 在 <paramref name="from"/> 与 <paramref name="to"/> 之间按 <paramref name="t"/>（0..1）插值。
    /// <para>文本取目标状态（若目标为 null 则沿用起始文本），保证切换时文本立即变化。</para>
    /// <para><see cref="Size"/> 在目标为 <see cref="Vector2.Zero"/>（未指定）时沿用起始尺寸，否则插值。</para>
    /// <para><see cref="BorderRadius"/> 在目标为 null（未指定）时沿用起始圆角，否则插值。</para>
    /// <para><see cref="Border"/> 在目标为负数（未指定）时沿用起始值，否则插值；<see cref="BorderColor"/> 同理。</para>
    /// </summary>
    public static SlideElementState Lerp(SlideElementState from, SlideElementState to, float t)
    {
        return new SlideElementState
        {
            Position = Vector2.Lerp(from.Position, to.Position, t),
            Opacity = MathHelper.Lerp(from.Opacity, to.Opacity, t),
            Scale = Vector2.Lerp(from.Scale, to.Scale, t),
            Rotation = MathHelper.Lerp(from.Rotation, to.Rotation, t),
            Color = Color.Lerp(from.Color, to.Color, t),
            Text = to.Text ?? from.Text,
            Size = to.Size == Vector2.Zero ? from.Size : Vector2.Lerp(from.Size, to.Size, t),
            BorderRadius = to.BorderRadius is { } target
                ? (from.BorderRadius is { } source ? Vector4.Lerp(source, target, t) : target)
                : from.BorderRadius,
            Border = to.Border >= 0f ? MathHelper.Lerp(from.Border, to.Border, t) : from.Border,
            BorderColor = to.BorderColor is { } toBc
                ? (from.BorderColor is { } fromBc ? Color.Lerp(fromBc, toBc, t) : toBc)
                : from.BorderColor,
        };
    }
}
