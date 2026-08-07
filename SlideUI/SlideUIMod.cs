using SlideUI.Core;
using SlideUI.UI;
using Terraria.ModLoader;

namespace SlideUI;

/// <summary>
/// SlideUI 模组主类。
/// 基于 SilkyUI（SilkyUIFramework）的"PPT 式"场景管理器：
/// 场景注册 / 多种转场 / 元素状态动画 / 联动绑定 / JSON 场景定义。
/// </summary>
public class SlideUIMod : Mod
{
    public static SlideUIMod Instance => ModContent.GetInstance<SlideUIMod>();

    /// <summary>
    /// 对外 API（供其他模组通过 <c>ModLoader.GetMod("SlideUI").Call(...)</c> 调用）：
    /// <list type="bullet">
    /// <item><c>"SwitchTo", string sceneId, [ITransition transition], [float duration]</c> → bool：切换场景（带动画）</item>
    /// <item><c>"ShowScene", string sceneId</c> → bool：无动画立即显示</item>
    /// <item><c>"NextScene"</c> → bool：下一张（顺序，不循环）</item>
    /// <item><c>"PreviousScene"</c> → bool：上一张（顺序，不循环）</item>
    /// <item><c>"LoadJson", string jsonPath</c> → bool：由事件唤起，加载指定 JSON（替换当前场景）</item>
    /// <item><c>"UnloadScenes"</c>（或 "Unload"）→ bool：卸载全部场景（幻灯片停止）</item>
    /// <item><c>"GetCurrentScene"</c> → string：当前场景 Id（无则 null）</item>
    /// <item><c>"IsTransitioning"</c> → bool：是否正在转场</item>
    /// </list>
    /// 示例：<c>ModLoader.GetMod("SlideUI")?.Call("LoadJson", "Content/scenes2.json")</c>
    /// </summary>
    public override object Call(params object[] args)
    {
        if (args is null || args.Length == 0 || args[0] is not string method)
            return null;

        var manager = SlideShowManager.Instance;
        switch (method)
        {
            case "SwitchTo":
            case "SwitchScene":
                if (args.Length < 2 || args[1] is not string sceneId) return false;
                var transition = args.Length >= 3 && args[2] is ITransition tr ? tr : null;
                var duration = args.Length >= 4 && args[3] is float d ? d : -1f;
                manager.SwitchTo(sceneId, transition, duration);
                return true;

            case "ShowScene":
                if (args.Length < 2 || args[1] is not string showId) return false;
                manager.ShowScene(showId);
                return true;

            case "NextScene":
                manager.NextScene();
                return true;

            case "PreviousScene":
                manager.PreviousScene();
                return true;

            case "GetCurrentScene":
                return manager.CurrentScene?.Id;

            case "IsTransitioning":
                return manager.IsTransitioning;

            case "LoadJson":
                if (args.Length < 2 || args[1] is not string loadPath) return false;
                SlideShowBody.Instance?.LoadJson(loadPath);
                return true;

            case "UnloadScenes":
            case "Unload":
                SlideShowBody.Instance?.UnloadScenes();
                return true;

            default:
                return null;
        }
    }
}
