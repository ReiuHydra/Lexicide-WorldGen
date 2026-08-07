using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SlideUI.Core;

/// <summary>
/// 幻灯片总控制器（单例）：注册场景、切换场景、并在每一帧驱动所有活跃的转场动画与联动绑定。
/// <para>
/// 本类不依赖 SilkyUI，只操作 <see cref="Scene"/> 与 <see cref="SlideElement"/>。
/// 在游戏主循环（例如 ModSystem.UpdateUI）中调用 <see cref="Update"/> 即可推进动画。
/// </para>
/// <para>
/// 每个元素拥有独立的转场时间线：场景切换由 Manager 统一设置各元素的目标状态，
/// 联动（Binding）可在任意时刻触发单个元素的属性转场，二者共用同一驱动循环。
/// </para>
/// </summary>
public class SlideShowManager
{
    /// <summary>全局单例。</summary>
    public static SlideShowManager Instance { get; } = new();

    private readonly Dictionary<string, Scene> _scenes = new();
    private readonly List<string> _sceneOrder = new();

    /// <summary>当前场景。</summary>
    public Scene CurrentScene { get; private set; }

    /// <summary>是否仍有元素正在播放转场动画。</summary>
    public bool IsTransitioning { get; private set; }

    /// <summary>最近一次场景切换使用的转场算法。</summary>
    public ITransition CurrentTransition { get; private set; }

    /// <summary>最近一次场景切换的时长（秒）。</summary>
    public float TransitionDuration { get; private set; }

    private SlideShowManager() { }

    #region 场景注册

    /// <summary>注册一个场景。</summary>
    public void RegisterScene(string id, Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);

        if (!_scenes.ContainsKey(id))
            _sceneOrder.Add(id);

        _scenes[id] = scene;
    }

    /// <summary>注销一个场景。</summary>
    public bool UnregisterScene(string id)
    {
        _sceneOrder.Remove(id);
        return _scenes.Remove(id);
    }

    /// <summary>尝试获取场景。</summary>
    public bool TryGetScene(string id, out Scene scene) => _scenes.TryGetValue(id, out scene);

    /// <summary>已注册场景的 Id 列表（按注册顺序）。注意：返回内部列表，遍历时勿直接修改。</summary>
    public IReadOnlyList<string> GetRegisteredSceneIds() => _sceneOrder;

    /// <summary>按注册顺序获取第 index 个场景的 Id；越界返回 null。</summary>
    public string GetSceneIdByIndex(int index)
        => index >= 0 && index < _sceneOrder.Count ? _sceneOrder[index] : null;

    /// <summary>卸载所有场景并清空当前场景（幻灯片停止）。</summary>
    public void UnloadAll()
    {
        _scenes.Clear();
        _sceneOrder.Clear();
        CurrentScene = null;
        IsTransitioning = false;
    }

    #endregion

    #region 场景切换

    /// <summary>无动画地立即显示一个场景（用于初始显示）。</summary>
    public void ShowScene(string sceneId)
    {
        if (!_scenes.TryGetValue(sceneId, out var scene)) return;

        foreach (var element in scene.Elements)
        {
            if (scene.TryGetElementState(element.Id, out var target))
            {
                element.SetSceneState(target);
                element.ApplyStateImmediately(target);
            }
        }

        CurrentScene = scene;
        IsTransitioning = false;
    }

    /// <summary>
    /// 切换到目标场景。为每个元素记录场景基准状态、设置目标状态，
    /// 然后由各元素用转场算法与时长独立推进平滑过渡。
    /// <para>
    /// <paramref name="transition"/> 为 null 时使用目标场景的默认转场
    /// （<see cref="Scene.Transition"/>，未配置则线性）；<paramref name="duration"/> 小于 0 时
    /// 使用目标场景的默认时长（<see cref="Scene.Duration"/>）。
    /// </para>
    /// </summary>
    public void SwitchTo(string sceneId, ITransition transition = null, float duration = -1f)
    {
        if (!_scenes.TryGetValue(sceneId, out var scene)) return;
        if (scene == CurrentScene && !IsTransitioning) return;

        var tr = transition ?? scene.Transition ?? LinearTransition.Instance;
        var dur = duration >= 0f ? duration : scene.Duration;
        CurrentTransition = tr;
        TransitionDuration = dur;

        foreach (var element in scene.Elements)
        {
            if (scene.TryGetElementState(element.Id, out var target))
            {
                element.SetSceneState(target);
                element.BeginTransition(target, tr, dur);
            }
        }

        CurrentScene = scene;
        IsTransitioning = true;
    }

    /// <summary>切换到下一个场景（按注册顺序；使用目标场景的默认转场，不循环）。</summary>
    public void NextScene(ITransition transition = null, float duration = -1f)
    {
        int next = GetRelativeSceneIndex(1);
        if (next >= 0)
            SwitchTo(_sceneOrder[next], transition, duration);
    }

    /// <summary>切换到上一个场景（按注册顺序；使用目标场景的默认转场，不循环）。</summary>
    public void PreviousScene(ITransition transition = null, float duration = -1f)
    {
        int prev = GetRelativeSceneIndex(-1);
        if (prev >= 0)
            SwitchTo(_sceneOrder[prev], transition, duration);
    }

    /// <summary>
    /// 计算相对当前场景偏移 delta（±1）的场景下标；
    /// 超出首尾则返回 -1（不循环，即第一页按 ←、最后一页按 → 都不切换）。
    /// </summary>
    private int GetRelativeSceneIndex(int delta)
    {
        if (_sceneOrder.Count == 0) return -1;

        int index = CurrentScene != null ? _sceneOrder.IndexOf(CurrentScene.Id) : -1;
        if (index < 0) index = 0;

        int next = index + delta;
        return next >= 0 && next < _sceneOrder.Count ? next : -1;
    }

    #endregion

    #region 每帧驱动

    /// <summary>
    /// 在游戏主循环中调用（例如 ModSystem.UpdateUI 里）：
    /// 先更新当前场景的联动绑定（可能触发新的元素转场），
    /// 再推进当前场景所有元素的转场动画。
    /// </summary>
    public void Update(GameTime gameTime)
    {
        var scene = CurrentScene;
        if (scene == null) return;

        // 1. 联动：源属性变化 → 触发目标元素属性转场
        scene.UpdateBindings();

        // 2. 推进每个元素自身的转场动画
        bool any = false;
        foreach (var element in scene.Elements)
        {
            element.Update(gameTime);
            any |= element.IsTransitioning;
        }

        IsTransitioning = any;
    }

    #endregion
}
