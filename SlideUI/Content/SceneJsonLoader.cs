using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using SlideUI.Core;
using SlideUI.Effects;
using SlideUI.Elements;

namespace SlideUI.Content;

/// <summary>
/// 从 JSON 定义加载场景，实现<b>内容与代码分离</b>。
/// <para>
/// JSON 格式：
/// <code>
/// {
///   "scenes": [
///     {
///       "id": "intro",
///       "transition": "easeout",          // 本场景默认转场（linear/easeinout/ease/easein/easeout/instant）
///       "duration": 1.2,                  // 本场景默认转场时长（秒）
///       "elements": [
///         { "id": "title", "position": ["10%", "14%"], "scale": 1.0, "color": "#FFFFFF", "text": "第一页" },
///         { "id": "card",  "position": ["10%", "38%"], "size": ["52%", "44%"], "color": "#141A2E",
///           "borderRadius": 14, "border": 2, "borderColor": "#FFFFFF" }
///       ],
///       "bindings": [                     // 联动：源（通常为按钮 IsHovered）→ 目标属性
///         { "source": "page1", "target": "title", "targetProperty": "Color",
///           "value": "#FF4500", "duration": 0.15 }
///       ]
///     }
///   ],
///   "effects": {                          // 根级特效：元素 Id → 特效配置（与场景无关，全局生效）
///     "title": {
///       "shake":     { "amplitude": 4, "period": 0.24, "style": "jitter" },   // 整体抖动（所有元素）
///       "charShake": { "amplitude": 12, "period": 1, "style": "bounce" }      // 逐字符抖动（仅文本）
///     },
///     "subtitle": { "typewriter": { "interval": 0.14 } },
///     "hint":     { "colorCycle": { "colors": ["#FFD700", "#87CEFA", "#FF4500"], "period": 4.8 } }
///   }
/// }
/// </code>
/// 坐标 / 尺寸支持 <c>"10%"</c>（相对容器）或 <c>"192"</c> / <c>"192px"</c>（绝对像素）。
/// scale / borderRadius 可以是单个数字（均匀）或数组 [x, y] / [tl, tr, br, bl]。
/// </para>
/// </summary>
public static class SceneJsonLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        // JSON 使用 camelCase，DTO 使用 PascalCase，需忽略大小写匹配
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// 解析 JSON 并构建场景。只包含 JSON 中出现的元素（按 Id 从 <paramref name="elements"/> 查找）。
    /// 解析失败会抛出异常，由调用方记录日志。
    /// </summary>
    /// <param name="json">JSON 文本。</param>
    /// <param name="screenSize">屏幕尺寸（X=宽，Y=高），用于解析百分比。</param>
    /// <param name="elements">元素注册表：Id → SlideElement（由代码创建视图与元素类型）。</param>
    public static List<Scene> LoadScenes(string json, Vector2 screenSize, IReadOnlyDictionary<string, SlideElement> elements)
    {
        var result = new List<Scene>();
        if (string.IsNullOrWhiteSpace(json)) return result;

        var root = JsonSerializer.Deserialize<JsonRoot>(json, Options)
            ?? throw new InvalidOperationException("scenes.json 反序列化结果为空");

        // 根级特效（独立于场景切换）：按元素 Id 应用
        ApplyRootEffects(root, elements);

        if (root.Scenes == null) return result;

        foreach (var js in root.Scenes)
        {
            var scene = new Scene(js.Id ?? "scene");

            // 场景默认转场与时长
            scene.Transition = ResolveTransition(js.Transition);
            if (js.Duration is { } dur && dur > 0f)
                scene.Duration = dur;

            if (js.Elements != null)
            {
                foreach (var je in js.Elements)
                {
                    if (je.Id == null || !elements.TryGetValue(je.Id, out var element)) continue;

                    scene.AddElement(element, new SlideElementState
                    {
                        Position = Resolve(je.Position, screenSize),
                        Size = Resolve(je.Size, screenSize),
                        Scale = ParseVec2(je.Scale, Vector2.One),
                        Color = ParseColor(je.Color),
                        Opacity = je.Opacity ?? 1f,
                        Text = je.Text,
                        BorderRadius = ParseRadius(je.BorderRadius),
                        Border = je.Border ?? -1f,
                        BorderColor = je.BorderColor != null ? ParseColor(je.BorderColor) : (Color?)null,
                    });
                }
            }

            // 场景内联动（在代码中解析为 Binding 并注册）
            if (js.Bindings != null)
            {
                foreach (var jb in js.Bindings)
                {
                    if (jb.Source == null || jb.Target == null) continue;
                    var prop = jb.TargetProperty ?? "Color";
                    var binding = new Binding(
                        jb.Source, jb.SourceProperty ?? "IsHovered", jb.Target, prop,
                        _ => ParseBindingValue(jb.Value, prop))
                    {
                        Duration = jb.Duration ?? 0.2f,
                    };
                    scene.AddBinding(binding);
                }
            }

            result.Add(scene);
        }

        return result;
    }

    /// <summary>解析 ["10%", "14%"] 形式的坐标 / 尺寸（x 相对宽，y 相对高）。</summary>
    private static Vector2 Resolve(string[] xy, Vector2 container)
    {
        if (xy == null || xy.Length < 2) return Vector2.Zero;
        return new Vector2(ResolveAxis(xy[0], container.X), ResolveAxis(xy[1], container.Y));
    }

    private static float ResolveAxis(string value, float container)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0f;

        value = value.Trim();
        if (value.EndsWith('%'))
            return container * (float.Parse(value[..^1]) / 100f);
        if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            return float.Parse(value[..^2]);
        return float.Parse(value);
    }

    /// <summary>解析 scale：数字（均匀）或 [x, y] 数组。</summary>
    private static Vector2 ParseVec2(JsonElement j, Vector2 fallback)
    {
        if (j.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return fallback;
        if (j.ValueKind == JsonValueKind.Array && j.GetArrayLength() >= 2)
            return new Vector2(j[0].GetSingle(), j[1].GetSingle());
        if (j.ValueKind == JsonValueKind.Number)
            return new Vector2(j.GetSingle());
        return fallback;
    }

    /// <summary>解析 borderRadius：数字（均匀）或 [tl, tr, br, bl] 数组。</summary>
    private static Vector4? ParseRadius(JsonElement j)
    {
        if (j.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return null;
        if (j.ValueKind == JsonValueKind.Array && j.GetArrayLength() >= 4)
            return new Vector4(j[0].GetSingle(), j[1].GetSingle(), j[2].GetSingle(), j[3].GetSingle());
        if (j.ValueKind == JsonValueKind.Number)
            return new Vector4(j.GetSingle());
        return null;
    }

    /// <summary>解析 "#RRGGBB" 或 "#RRGGBBAA" 颜色。</summary>
    private static Color ParseColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Color.White;

        hex = hex.Trim().TrimStart('#');
        if (hex.Length < 6) return Color.White;

        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
        int a = hex.Length >= 8 ? Convert.ToInt32(hex.Substring(6, 2), 16) : 255;
        return new Color(r, g, b, a);
    }

    /// <summary>把 JSON 根级特效（元素 Id → 配置）应用到元素注册表。</summary>
    private static void ApplyRootEffects(JsonRoot root, IReadOnlyDictionary<string, SlideElement> elements)
    {
        if (root.Effects == null) return;

        foreach (var pair in root.Effects)
        {
            if (pair.Key == null || pair.Value == null || !elements.TryGetValue(pair.Key, out var element)) continue;
            ApplyEffects(element, pair.Value);
        }
    }

    /// <summary>把单个元素的 JSON 特效配置应用到元素上。整体抖动对所有元素；逐字符/打字机/变色仅文本元素。</summary>
    private static void ApplyEffects(SlideElement element, JsonElementEffects je)
    {
        if (je.Shake != null)
            element.Shake = ParseShake(je.Shake);

        if (element is TextSlideElement text && text.Effects != null)
        {
            if (je.CharShake != null) text.Effects.CharShake = ParseShake(je.CharShake);
            if (je.Typewriter != null) text.Effects.Typewriter = ParseTypewriter(je.Typewriter);
            if (je.ColorCycle != null) text.Effects.ColorCycle = ParseColorCycle(je.ColorCycle);
        }
    }

    private static ShakeEffect ParseShake(JsonShake j)
    {
        var effect = new ShakeEffect();
        if (j.Enabled is { } enabled) effect.Enabled = enabled;
        if (j.Amplitude is { } amplitude) effect.Amplitude = amplitude;
        if (j.Period is { } period) effect.Period = period;
        if (j.CharacterOffset is { } offset) effect.CharacterOffset = offset;
        if (j.Start is { } start) effect.Start = start;
        if (j.Length is { } length) effect.Length = length;
        if (j.Style is { } style) effect.Style = style.Trim().ToLowerInvariant() switch
        {
            "bounce" => ShakeStyle.Bounce,
            _ => ShakeStyle.Jitter,
        };
        return effect;
    }

    private static TypewriterEffect ParseTypewriter(JsonTypewriter j)
    {
        var effect = new TypewriterEffect();
        if (j.Enabled is { } enabled) effect.Enabled = enabled;
        if (j.Interval is { } interval) effect.Interval = interval;
        if (j.Start is { } start) effect.Start = start;
        if (j.Length is { } length) effect.Length = length;
        return effect;
    }

    private static ColorCycleEffect ParseColorCycle(JsonColorCycle j)
    {
        var effect = new ColorCycleEffect();
        if (j.Enabled is { } enabled) effect.Enabled = enabled;
        if (j.Colors is { Length: > 0 }) effect.Colors = Array.ConvertAll(j.Colors, ParseColor);
        if (j.Period is { } period) effect.Period = period;
        if (j.Smooth is { } smooth) effect.Smooth = smooth;
        if (j.CharacterOffset is { } offset) effect.CharacterOffset = offset;
        if (j.Start is { } start) effect.Start = start;
        if (j.Length is { } length) effect.Length = length;
        return effect;
    }

    /// <summary>
    /// 把 JSON 中转场名字解析为 <see cref="ITransition"/>。
    /// 支持：linear / easeinout / ease / easein / easeout / instant；未知名字返回 null（用默认线性）。
    /// </summary>
    private static ITransition ResolveTransition(string name)
    {
        switch (name?.Trim().ToLowerInvariant())
        {
            case "linear": return LinearTransition.Instance;
            case "easeinout": return EaseInOutTransition.Instance;
            case "ease": return BezierTransition.Ease;
            case "easein": return BezierTransition.EaseIn;
            case "easeout": return BezierTransition.EaseOut;
            case "instant": return InstantTransition.Instance;
            default: return null;
        }
    }

    /// <summary>
    /// 把 JSON 中的联动目标值按目标属性类型解析为强类型值。
    /// 目标属性为颜色时解析字符串 "#RRGGBB"；为向量时解析数组；为标量时解析数字。
    /// </summary>
    private static object ParseBindingValue(JsonElement value, string targetProperty)
    {
        switch (targetProperty)
        {
            case "Color":
            case "BorderColor":
                if (value.ValueKind == JsonValueKind.String)
                    return ParseColor(value.GetString());
                break;
            case "Position":
            case "Size":
            case "Scale":
                if (value.ValueKind == JsonValueKind.Array)
                    return ParseVec2(value, Vector2.One);
                if (value.ValueKind == JsonValueKind.Number)
                    return new Vector2(value.GetSingle());
                break;
            case "BorderRadius":
                if (value.ValueKind == JsonValueKind.Array || value.ValueKind == JsonValueKind.Number)
                    return ParseRadius(value);
                break;
            case "Opacity":
            case "Rotation":
            case "Border":
                if (value.ValueKind == JsonValueKind.Number)
                    return value.GetSingle();
                break;
        }
        return null;
    }

    // ---- JSON DTO ----
    public class JsonRoot
    {
        public List<JsonScene> Scenes { get; set; }

        /// <summary>根级特效：元素 Id → 特效配置（整体抖动 / 逐字符抖动 / 打字机 / 变色，与场景无关）。</summary>
        public Dictionary<string, JsonElementEffects> Effects { get; set; }
    }

    public class JsonScene
    {
        public string Id { get; set; }

        /// <summary>默认转场名（linear/easeinout/ease/easein/easeout/instant）。</summary>
        public string Transition { get; set; }

        /// <summary>默认转场时长（秒）。</summary>
        public float? Duration { get; set; }

        public List<JsonElementState> Elements { get; set; }

        public List<JsonBinding> Bindings { get; set; }
    }

    public class JsonElementState
    {
        public string Id { get; set; }
        public string[] Position { get; set; }
        public string[] Size { get; set; }
        public JsonElement Scale { get; set; }
        public string Color { get; set; }
        public float? Opacity { get; set; }
        public string Text { get; set; }
        public JsonElement BorderRadius { get; set; }

        /// <summary>边框宽度（像素）；负数表示不改变。</summary>
        public float? Border { get; set; }

        /// <summary>边框颜色（"#RRGGBB" / "#RRGGBBAA"）；为空表示不改变。</summary>
        public string BorderColor { get; set; }
    }

    /// <summary>JSON 中的联动定义：源（通常为按钮的 IsHovered）→ 目标元素属性。</summary>
    public class JsonBinding
    {
        /// <summary>源元素 Id。</summary>
        public string Source { get; set; }

        /// <summary>源属性名（默认 IsHovered）。</summary>
        public string SourceProperty { get; set; }

        /// <summary>目标元素 Id。</summary>
        public string Target { get; set; }

        /// <summary>目标属性名（Color/Scale/Opacity/...）。</summary>
        public string TargetProperty { get; set; }

        /// <summary>源属性激活时的目标值（颜色为 "#RRGGBB"，向量为数组，标量为数字）。</summary>
        public JsonElement Value { get; set; }

        /// <summary>联动转场时长（秒）。</summary>
        public float? Duration { get; set; }
    }

    /// <summary>一个元素的 JSON 特效配置（根级，按元素 Id 引用）。</summary>
    public class JsonElementEffects
    {
        /// <summary>整体抖动（作用于元素基类，任意元素类型）。</summary>
        public JsonShake Shake { get; set; }

        /// <summary>逐字符抖动（仅文本元素）。</summary>
        public JsonShake CharShake { get; set; }

        /// <summary>打字机（仅文本元素）。</summary>
        public JsonTypewriter Typewriter { get; set; }

        /// <summary>变色（仅文本元素）。</summary>
        public JsonColorCycle ColorCycle { get; set; }
    }

    /// <summary>JSON 抖动配置（整体 / 逐字符共用）。</summary>
    public class JsonShake
    {
        public bool? Enabled { get; set; }
        public float? Amplitude { get; set; }
        public float? Period { get; set; }

        /// <summary>"jitter"（随机颤抖）或 "bounce"（上下跳动）。</summary>
        public string Style { get; set; }

        public float? CharacterOffset { get; set; }
        public int? Start { get; set; }
        public int? Length { get; set; }
    }

    /// <summary>JSON 打字机配置（仅文本）。</summary>
    public class JsonTypewriter
    {
        public bool? Enabled { get; set; }
        public float? Interval { get; set; }
        public int? Start { get; set; }
        public int? Length { get; set; }
    }

    /// <summary>JSON 变色配置（仅文本）。</summary>
    public class JsonColorCycle
    {
        public bool? Enabled { get; set; }
        public string[] Colors { get; set; }
        public float? Period { get; set; }
        public bool? Smooth { get; set; }
        public float? CharacterOffset { get; set; }
        public int? Start { get; set; }
        public int? Length { get; set; }
    }
}
