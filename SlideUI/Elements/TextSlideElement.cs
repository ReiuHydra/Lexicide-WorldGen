using System;
using Microsoft.Xna.Framework;
using SilkyUIFramework.Elements;
using SlideUI.Core;
using SlideUI.Effects;

namespace SlideUI.Elements;

/// <summary>
/// 文本幻灯片元素：包装 <see cref="EffectTextView"/>（SilkyUI <see cref="UITextView"/> 的扩展），
/// 把 <see cref="SlideElementState"/> 映射到文本框的位置 / 缩放 / 旋转 / 颜色 / 文本。
/// <para>
/// 文本特效（抖动 / 打字机 / 变色）与场景转场独立，由本元素每帧推进
/// （见 <see cref="Update"/> 与 <see cref="Effects"/>）。
/// </para>
/// </summary>
public class TextSlideElement : SlideElement
{
    /// <summary>被包装的文本控件。</summary>
    public UITextView View { get; }

    /// <summary>文本特效配置（为 null 时表示包装的不是 <see cref="EffectTextView"/>，无特效可用）。</summary>
    public TextEffects Effects => (View as EffectTextView)?.Effects;

    public TextSlideElement(string id, UITextView view) : base(id)
    {
        View = view ?? throw new ArgumentNullException(nameof(view));
    }

    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        // 先推进文本特效计时（与场景转场无关），再处理转场/联动动画
        if (View is EffectTextView effectView)
            effectView.UpdateEffects((float)gameTime.ElapsedGameTime.TotalSeconds);

        base.Update(gameTime);
    }

    /// <inheritdoc />
    public override void ApplyState(SlideElementState state)
    {
        // 文本是非动画属性，每次设置时 UITextView.Text 内部会做相等判断，不变则无开销。
        if (state.Text != null)
            View.Text = state.Text;

        // 位置：整体抖动偏移来自基类 SlideElement.Shake（对整个文本框生效）；
        // 逐字符抖动由 EffectTextView 在绘制时内部处理，二者可叠加。
        var pos = EffectivePosition(state);
        View.SetLeft(pos.X);
        View.SetTop(pos.Y);
        View.TextScale = state.Scale.X;
        View.TextRotation = state.Rotation;

        // 文本框边界：给定宽高时用"固定宽度 + 自动换行"（PPT 文本框式，高度随内容增长）；
        // 未指定时自然适配内容（单行）。
        if (state.Size != Vector2.Zero)
        {
            View.FitWidth = false;
            View.WordWrap = true;
            View.SetSize(state.Size.X, state.Size.Y);
        }
        else
        {
            View.FitWidth = true;
            View.WordWrap = false;
        }

        // 透明度通过颜色 Alpha 通道表达。
        View.TextColor = state.Color * state.Opacity;
        View.TextBorderColor = Color.Black * state.Opacity;
    }

    /// <inheritdoc />
    public override SlideElementState GetCurrentState()
    {
        var color = View.TextColor;
        return new SlideElementState
        {
            Position = new Vector2(View.Left.Pixels, View.Top.Pixels),
            Opacity = color.A / 255f,
            Scale = new Vector2(View.TextScale),
            Rotation = View.TextRotation,
            Color = new Color(color.R, color.G, color.B),
            Text = View.Text,
        };
    }

    /// <inheritdoc />
    public override void SetVisible(bool visible) => View.Invalid = !visible;
}
