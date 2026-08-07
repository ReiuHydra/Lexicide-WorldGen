using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SlideUI.Content;
using SlideUI.Core;
using SlideUI.Effects;
using SlideUI.Elements;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace SlideUI.UI;

/// <summary>
/// 全屏幻灯片主体：只负责创建视图 / 元素注册表与页码按钮行为；
/// 场景内容由<b>特定事件</b>（进入世界 / 快捷键 / Mod.Call）唤起 <see cref="LoadJson"/> 加载<b>指定</b> JSON。
/// 支持 ← / → 键顺序翻页（不循环），底部页码按钮一键跳转，悬停联动由 JSON 定义。
/// </summary>
[RegisterUI("Vanilla: Mouse Text", "SlideShow Demo")]
public class SlideShowBody : BaseBody
{
    // 只有子元素（页码按钮）拦截鼠标，其余区域穿透到游戏
    protected override bool AvailableItem => true;
    protected override bool AvailableScroll => true;

    /// <summary>当前（最近一次重建的）主体实例，供快捷键 / Mod.Call 唤起加载。</summary>
    public static SlideShowBody Instance { get; private set; }

    /// <summary>元素注册表：代码定义视图与元素类型，布局 / 特效 / 联动数据由 JSON 提供。</summary>
    private readonly Dictionary<string, SlideElement> _elements = new();

    private EffectTextView _titleView;
    private EffectTextView _subtitleView;
    private EffectTextView _hintView;
    private UIElementGroup _cardView;
    private SUIImage _imageView;
    private Rectangle _kingSlimeFrame;

    // 4 个页码跳转按钮
    private readonly UIElementGroup[] _pageViews = new UIElementGroup[4];
    private readonly UITextView[] _pageLabels = new UITextView[4];

    public SlideShowBody()
    {
        Instance = this;

        // 全屏舞台：透明背景、无边框
        BackgroundColor = Color.Transparent;
        Border = 0f;
        IgnoreMouseInteraction = true;

        // 所有文字统一使用高分辨率 DeathText 字体（主菜单同款），保证清晰
        _titleView = CreateText("第一页", 1f, Color.White);
        _subtitleView = CreateText("场景系统 · 多页演示", 0.5f, Color.LightGray);
        _hintView = CreateText("按 ← → 键或点击下方页码跳转", 0.35f, Color.Gray);

        // 文本特效（整体抖动 / 逐字符抖动 / 打字机 / 变色）已全部移入 Content/scenes.json 的根级 "effects"，
        // 这里不再用代码配置（内容与代码分离）。

        // 卡片：圆角面板（作为图片容器）
        _cardView = new UIElementGroup
        {
            Positioning = Positioning.Absolute,
            BackgroundColor = new Color(20, 26, 46, 210),
            Border = 2f,
            BorderColor = Color.White * 0.35f,
            BorderRadius = new Vector4(14f),
        };

        // 图片：史莱姆王（vanilla NPC 纹理默认未加载，需显式请求立即加载）
        var kingSlimeTexture = Main.Assets.Request<Texture2D>(
            TextureAssets.Npc[NPCID.KingSlime].Name, AssetRequestMode.ImmediateLoad);

        // NPC 纹理由多个动画帧纵向堆叠而成，这里只取第一帧显示
        var npcTexture = kingSlimeTexture.Value;
        var frameCount = Main.npcFrameCount[NPCID.KingSlime];
        _kingSlimeFrame = new Rectangle(0, 0, npcTexture.Width, npcTexture.Height / frameCount);

        _imageView = new SUIImage(kingSlimeTexture)
        {
            FitWidth = false,
            FitHeight = false,
            Positioning = Positioning.Absolute,
            ImageColor = Color.White,
            SourceRectangle = _kingSlimeFrame,
        };
        _cardView.AddChild(_imageView);

        // 4 个页码跳转按钮（固定底栏）
        for (int i = 0; i < 4; i++)
        {
            var (view, label) = CreatePageButton((i + 1).ToString());
            _pageViews[i] = view;
            _pageLabels[i] = label;
        }

        AddChild(_titleView);
        AddChild(_subtitleView);
        AddChild(_hintView);
        AddChild(_cardView);
        foreach (var view in _pageViews)
            AddChild(view);
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();

        // 铺满屏幕（UI 缩放后的像素）
        var screen = GraphicsDeviceHelper.GetBackBufferSizeByUIScale();
        SetSize(screen.Width, screen.Height);
        SetLeft(0, 0, 0);
        SetTop(0, 0, 0);

        // 只建立元素注册表与页码按钮行为；场景由特定事件（进入世界 / 快捷键 / Mod.Call）唤起 LoadJson 加载指定 JSON
        BuildElements();

        // 初次进入世界时全部隐藏（不堆积在 (0,0)、不残留默认外观），等 LoadJson 显示
        HideAllElements();
    }

    private static EffectTextView CreateText(string text, float scale, Color color, Vector2? align = null)
    {
        var view = new EffectTextView
        {
            Text = text,
            TextScale = scale,
            TextColor = color,
            TextBorderColor = Color.Black,
            TextAlign = align ?? new Vector2(0f, 0.5f),
            Positioning = Positioning.Absolute,
        };
        // 统一使用高分辨率 DeathText 字体，保证任何尺寸都清晰
        view.UseDeathText();
        return view;
    }

    /// <summary>创建页码按钮（圆角容器 + 居中文本标签）。</summary>
    private static (UIElementGroup View, UITextView Label) CreatePageButton(string text)
    {
        var view = new UIElementGroup
        {
            Positioning = Positioning.Absolute,
            BackgroundColor = new Color(56, 92, 168),
            Border = 1f,
            BorderColor = Color.White * 0.6f,
            BorderRadius = new Vector4(8f),
        };
        var label = CreateText(text, 0.42f, Color.White, new Vector2(0.5f, 0.5f));
        label.SetLeft(0f, 0f, 0.5f);
        label.SetTop(0f, 0f, 0.5f);
        view.AddChild(label);
        return (view, label);
    }

    /// <summary>建立元素注册表与页码按钮行为（视图在构造函数创建，本方法只做包装与接线）。</summary>
    private void BuildElements()
    {
        // 页码按钮（代码只负责创建视图与包装元素；位置 / 样式 / 标签文本全部由 JSON 决定）
        var pageElements = new ButtonSlideElement[4];
        for (int i = 0; i < 4; i++)
            pageElements[i] = new ButtonSlideElement($"page{i + 1}", _pageViews[i], _pageLabels[i]);

        _elements.Clear();
        _elements["title"] = new TextSlideElement("title", _titleView);
        _elements["subtitle"] = new TextSlideElement("subtitle", _subtitleView);
        _elements["hint"] = new TextSlideElement("hint", _hintView);
        _elements["card"] = new ShapeSlideElement("card", _cardView);
        _elements["image"] = new ImageSlideElement("image", _imageView) { FrameRect = _kingSlimeFrame };
        _elements["page1"] = pageElements[0];
        _elements["page2"] = pageElements[1];
        _elements["page3"] = pageElements[2];
        _elements["page4"] = pageElements[3];

        // 页码按钮行为：按注册顺序一键跳到第 i 个场景（转场与时长用目标场景的 JSON 默认配置）。
        // 每次加载 JSON 后按钮自动指向新加载的场景，无需改代码。
        var manager = SlideShowManager.Instance;
        for (int i = 0; i < pageElements.Length; i++)
        {
            int index = i;
            pageElements[i].Clicked += () =>
            {
                var target = manager.GetSceneIdByIndex(index);
                if (target != null) manager.SwitchTo(target);
            };
        }
    }

    /// <summary>
    /// 由特定事件（进入世界 / 快捷键 / Mod.Call）唤起：加载<b>指定</b> JSON 的场景定义，
    /// 替换当前所有场景并显示第一页。可重复调用（用于切换版式）。
    /// </summary>
    public void LoadJson(string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath)) return;

        var screen = GraphicsDeviceHelper.GetBackBufferSizeByUIScale();
        var scenes = ParseJson(jsonPath, new Vector2(screen.Width, screen.Height));
        if (scenes.Count == 0)
        {
            SlideUIMod.Instance?.Logger.Warn($"「{jsonPath}」未加载到任何场景，保持当前场景不变");
            return;
        }

        var manager = SlideShowManager.Instance;

        // 先卸载旧场景并隐藏所有元素（避免旧版式中未出现在新 JSON 的元素残留），
        // 再注册新场景并显示第一页（SetSceneState 会让场景内元素重新可见）
        manager.UnloadAll();
        HideAllElements();
        foreach (var scene in scenes)
            manager.RegisterScene(scene.Id, scene);

        manager.ShowScene(scenes[0].Id);
    }

    /// <summary>
    /// 卸载：移除所有已加载的场景（幻灯片停止，视图整体隐藏）。
    /// 由特定事件（快捷键 / Mod.Call / 世界退出）唤起。可重复调用（幂等）。
    /// </summary>
    public void UnloadScenes()
    {
        SlideShowManager.Instance.UnloadAll();
        HideAllElements();
    }

    /// <summary>
    /// 把所有元素整体隐藏（SetVisible(false)）：SilkyUI 会把 <see cref="UIView.Invalid"/> 的视图
    /// 从布局 / 更新 / 绘制 / 鼠标命中缓存中剔除，整棵子树（含按钮标签）一个开关即彻底隐藏，
    /// 不会残留边框、文字等子属性。
    /// </summary>
    private void HideAllElements()
    {
        foreach (var element in _elements.Values)
            element.SetVisible(false);
    }

    /// <summary>解析指定 JSON 为场景列表（读取文件 + 反序列化 + 记录日志）。</summary>
    private List<Scene> ParseJson(string jsonPath, Vector2 screen)
    {
        var scenes = new List<Scene>();
        var jsonBytes = SlideUIMod.Instance?.GetFileBytes(jsonPath);
        if (jsonBytes is { Length: > 0 })
        {
            var json = Encoding.UTF8.GetString(jsonBytes);
            try
            {
                scenes.AddRange(SceneJsonLoader.LoadScenes(json, screen, _elements));
            }
            catch (Exception ex)
            {
                SlideUIMod.Instance?.Logger.Error($"解析 {jsonPath} 失败", ex);
            }
        }
        else
        {
            SlideUIMod.Instance?.Logger.Warn($"未找到 {jsonPath}（文件未打包？）");
        }

        return scenes;
    }
}
