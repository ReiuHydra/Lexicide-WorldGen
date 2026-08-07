using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SlideUI.Core;
using SlideUI.UI;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SlideUI.Systems;

/// <summary>
/// 幻灯片系统：每帧驱动 <see cref="SlideShowManager"/> 的转场动画，并注册演示用快捷键。
/// </summary>
public class SlideShowSystem : ModSystem
{
    public static SlideShowSystem Instance => ModContent.GetInstance<SlideShowSystem>();

    /// <summary>下一张幻灯片。</summary>
    public ModKeybind NextSlideKey { get; private set; }

    /// <summary>上一张幻灯片。</summary>
    public ModKeybind PreviousSlideKey { get; private set; }

    /// <summary>加载 Content/scenes.json（重新加载默认版式）。</summary>
    public ModKeybind LoadSlidesKey { get; private set; }

    /// <summary>加载 Content/scenes2.json（备用版式）。</summary>
    public ModKeybind LoadAltSlidesKey { get; private set; }

    /// <summary>卸载当前场景（幻灯片停止）。</summary>
    public ModKeybind UnloadSlidesKey { get; private set; }

    public override void Load()
    {
        if (Main.netMode == NetmodeID.Server) return;

        NextSlideKey = KeybindLoader.RegisterKeybind(Mod, "NextSlide", Keys.Right);
        PreviousSlideKey = KeybindLoader.RegisterKeybind(Mod, "PreviousSlide", Keys.Left);
        LoadSlidesKey = KeybindLoader.RegisterKeybind(Mod, "LoadSlides", Keys.L);
        LoadAltSlidesKey = KeybindLoader.RegisterKeybind(Mod, "LoadAltSlides", Keys.J);
        UnloadSlidesKey = KeybindLoader.RegisterKeybind(Mod, "UnloadSlides", Keys.U);
    }

    public override void Unload()
    {
        NextSlideKey = null;
        PreviousSlideKey = null;
        LoadSlidesKey = null;
        LoadAltSlidesKey = null;
        UnloadSlidesKey = null;
    }

    public override void OnWorldUnload()
    {
        // 世界退出 → 卸载当前场景，避免残留到下一世界
        SlideShowBody.Instance?.UnloadScenes();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        var manager = SlideShowManager.Instance;

        // 每帧推进所有活跃的转场动画
        manager.Update(gameTime);

        // 快捷键切换场景（转场与时长使用目标场景在 JSON 中的配置）
        if (NextSlideKey?.JustPressed == true)
            manager.NextScene();

        if (PreviousSlideKey?.JustPressed == true)
            manager.PreviousScene();

        // 特定事件：加载 / 卸载指定 JSON（内容与代码分离，由事件唤起）
        if (LoadSlidesKey?.JustPressed == true)
            SlideShowBody.Instance?.LoadJson("Content/scenes.json");

        if (LoadAltSlidesKey?.JustPressed == true)
            SlideShowBody.Instance?.LoadJson("Content/scenes2.json");

        if (UnloadSlidesKey?.JustPressed == true)
            SlideShowBody.Instance?.UnloadScenes();
    }
}
