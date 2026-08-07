using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Helper;

namespace SlideUI.Effects;

/// <summary>
/// 支持逐字符特效与自动换行的文本控件，行为类似 PPT 文本框：
/// <list type="bullet">
/// <item><b>确定边界</b>：设置宽高后（FitWidth=false + WordWrap=true）在指定宽度内自动换行，
/// 高度随内容增长（除非边界装不下，允许超高/超长词溢出）。</item>
/// <item><b>逐字符特效</b>：抖动 / 打字机 / 变色可叠加，可只作用于文本的一部分（见 <see cref="TextEffects"/>）。</item>
/// </list>
/// 继承 SilkyUI 的 <see cref="UITextView"/>：测量 / 定位仍走 UIView 流程，但文本的
/// 布局（换行）与绘制由本类接管，从而获得逐字符控制。
/// </summary>
public class EffectTextView : UITextView
{
    /// <summary>文本特效配置（抖动 / 打字 / 变色）。</summary>
    public TextEffects Effects { get; } = new();

    // 逐字符布局结果：每个字符对应一个字形（含文本下标，供特效按 Text 下标寻址）
    private readonly List<WrappedLine> _lines = new();

    // 特效计时（累计秒）；文本变化时归零（打字机重新开始）
    private float _elapsed;
    private string _lastText = string.Empty;

    /// <summary>
    /// 每帧推进特效计时。由 <see cref="SlideUI.Elements.TextSlideElement.Update"/> 调用，
    /// 与场景转场完全独立。
    /// </summary>
    public void UpdateEffects(float deltaTime)
    {
        if (_lastText != Text)
        {
            _lastText = Text;
            _elapsed = 0f;
        }

        _elapsed += deltaTime;
    }

    #region 布局（逐字符 + 单词换行）

    private struct Glyph
    {
        public char Char;
        public float Width;
        public int TextIndex;
    }

    private sealed class WrappedLine
    {
        public readonly List<Glyph> Glyphs = new();
        public float Width;
    }

    /// <inheritdoc />
    protected override void RecalculateString(float maxWidth)
    {
        _lines.Clear();

        if (string.IsNullOrEmpty(Text))
        {
            TextSize = new Vector2(0f, Font.LineSpacing);
            return;
        }

        // 仅当"固定宽度"（FitWidth=false）时才在给定宽度内换行；否则单行自然排布。
        bool wrap = WordWrap && !FitWidth && maxWidth > 0f && TextScale > 0f;
        float maxW = wrap ? maxWidth / TextScale : float.MaxValue;
        float spacing = Font.CharacterSpacing;

        var currentLine = new WrappedLine();
        _lines.Add(currentLine);

        var word = new WrappedLine();
        float wordAdvance = 0f;
        int wordCount = 0;

        void NewLine()
        {
            currentLine = new WrappedLine();
            _lines.Add(currentLine);
        }

        void FlushWord()
        {
            if (wordCount == 0) return;

            // 单词本身比一行还宽时：独占一行并允许溢出（除非边界装不下）。
            // 否则若当前行放不下 → 换行。
            if (currentLine.Glyphs.Count > 0 &&
                (wordAdvance > maxW || currentLine.Width + spacing + wordAdvance > maxW))
            {
                NewLine();
            }

            foreach (var g in word.Glyphs)
            {
                if (currentLine.Glyphs.Count > 0) currentLine.Width += spacing;
                currentLine.Glyphs.Add(g);
                currentLine.Width += g.Width;
            }

            word.Glyphs.Clear();
            wordAdvance = 0f;
            wordCount = 0;
        }

        for (int i = 0; i < Text.Length; i++)
        {
            char c = Text[i];
            if (c == '\r') continue; // 忽略回车，只按 \n 换行

            float cw = Font.GetCharacterMetrics(c).KernedWidth;

            if (c == '\n')
            {
                FlushWord();
                NewLine();
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                FlushWord();
                // 空白单独放置；放不下则换行
                if (wrap && currentLine.Glyphs.Count > 0 && currentLine.Width + spacing + cw > maxW)
                    NewLine();

                if (currentLine.Glyphs.Count > 0) currentLine.Width += spacing;
                currentLine.Glyphs.Add(new Glyph { Char = c, Width = cw, TextIndex = i });
                currentLine.Width += cw;
                continue;
            }

            // 普通字符 → 累积到当前单词
            if (wordCount > 0) wordAdvance += spacing;
            wordAdvance += cw;
            word.Glyphs.Add(new Glyph { Char = c, Width = cw, TextIndex = i });
            wordCount++;
        }

        FlushWord();

        // 行数限制（可选）
        if (MaxLines > 0 && _lines.Count > MaxLines)
            _lines.RemoveRange(MaxLines, _lines.Count - MaxLines);

        float maxLineWidth = 0f;
        foreach (var line in _lines) maxLineWidth = Math.Max(maxLineWidth, line.Width);
        TextSize = new Vector2(maxLineWidth, _lines.Count * Font.LineSpacing);
    }

    #endregion

    #region 绘制（逐字符特效）

    /// <inheritdoc />
    protected override void DrawSnippets(SpriteBatch spriteBatch)
    {
        var innerSize = (Vector2)InnerBounds.Size;
        var textSize = TextSize * TextScale;
        var textPosition = InnerBounds.Position + TextOffset + TextPercentOffset * innerSize
            + TextAlign * (innerSize - textSize);
        var textOrigin = TextPercentOrigin * textSize;
        textPosition.Y += TextScale * TextDrawingHelper.GetFontOffset(Font);

        float spacing = Font.CharacterSpacing;
        var baseColor = TextColor;
        var charScale = new Vector2(TextScale);

        float y = textPosition.Y;
        foreach (var line in _lines)
        {
            float x = textPosition.X;
            foreach (var g in line.Glyphs)
            {
                int idx = g.TextIndex;
                if (IsVisible(idx))
                {
                    var color = baseColor;

                    if (!IgnoreTextColor && Effects.ColorCycle is { Enabled: true } cc && cc.InRange(idx, Text.Length))
                    {
                        color = Color.FromNonPremultiplied(
                            cc.ColorAt(_elapsed, idx).ToVector4() * baseColor.ToVector4());
                    }

                    var pos = new Vector2(x, y);
                    if (Effects.CharShake is { Enabled: true } sh && sh.InRange(idx, Text.Length))
                        pos += sh.Offset(_elapsed, idx);

                    if (TextBorder > 0f && TextBorderColor.A > 0)
                        DrawShadow(spriteBatch, g.Char, pos, TextBorderColor, TextRotation, textOrigin, charScale, TextBorder);

                    spriteBatch.DrawString(Font, new string(g.Char, 1), pos, color,
                        TextRotation, textOrigin, charScale, SpriteEffects.None, 0f);
                }

                x += spacing * TextScale + g.Width * TextScale;
            }

            y += Font.LineSpacing * TextScale;
        }
    }

    /// <summary>字符是否在打字机特效下当前可见。</summary>
    private bool IsVisible(int index)
    {
        if (Effects.Typewriter is not { Enabled: true } tw) return true;
        if (index < tw.Start) return true;

        int end = tw.Length < 0 ? Text.Length : Math.Min(Text.Length, tw.Start + tw.Length);
        if (index >= end) return true;

        float typed = _elapsed / Math.Max(tw.Interval, 1e-4f);
        return index - tw.Start < (int)typed;
    }

    private static readonly Vector2[] ShadowOffsets =
    {
        new(-1f, 0f), new(1f, 0f), new(0f, -1f), new(0f, 1f),
    };

    private void DrawShadow(SpriteBatch spriteBatch, char c, Vector2 pos, Color color,
        float rotation, Vector2 origin, Vector2 scale, float spread)
    {
        foreach (var offset in ShadowOffsets)
        {
            spriteBatch.DrawString(Font, new string(c, 1), pos + offset * spread, color,
                rotation, origin, scale, SpriteEffects.None, 0f);
        }
    }

    #endregion
}
