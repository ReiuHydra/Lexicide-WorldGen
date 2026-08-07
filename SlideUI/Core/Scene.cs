using System;
using System.Collections.Generic;

namespace SlideUI.Core;

/// <summary>
/// 场景（一张"幻灯片"）：包含一组元素及其目标状态快照。
/// 同一 <see cref="SlideElement"/> 可被多个场景共享（按 Id 匹配），
/// 切换场景时由 SlideShowManager 驱动它从当前状态平滑过渡到新场景的目标状态。
/// </summary>
public class Scene
{
    /// <summary>场景唯一标识。</summary>
    public string Id { get; }

    /// <summary>
    /// 切换到本场景时的默认转场算法（可由 JSON 配置）。
    /// 为 <see langword="null"/> 时由 SlideShowManager 回退到线性。
    /// </summary>
    public ITransition Transition { get; set; }

    /// <summary>切换到本场景时的默认时长（秒，可由 JSON 配置）。</summary>
    public float Duration { get; set; } = 1f;

    private readonly List<SlideElement> _elements = new();
    private readonly Dictionary<string, SlideElementState> _states = new();
    private readonly List<Binding> _bindings = new();

    /// <summary>本场景包含的元素（去重，按添加顺序）。</summary>
    public IReadOnlyList<SlideElement> Elements => _elements;

    /// <summary>本场景中每个元素的目标状态（Id → 状态）。</summary>
    public IReadOnlyDictionary<string, SlideElementState> States => _states;

    /// <summary>本场景内的联动绑定（仅当前场景激活时更新）。</summary>
    public IReadOnlyList<Binding> Bindings => _bindings;

    public Scene(string id)
    {
        Id = id;
    }

    /// <summary>
    /// 注册一个元素及其在本场景中的目标状态。
    /// 未指定状态时，以元素当前可视状态作为目标状态。
    /// </summary>
    public Scene AddElement(SlideElement element, SlideElementState state = null)
    {
        if (!_elements.Contains(element))
            _elements.Add(element);

        _states[element.Id] = state?.Clone() ?? element.GetCurrentState();
        return this;
    }

    /// <summary>按元素 Id 移除元素及其状态。</summary>
    public bool RemoveElement(string id)
    {
        _states.Remove(id);
        return _elements.RemoveAll(e => e.Id == id) > 0;
    }

    /// <summary>尝试获取某元素在本场景中的目标状态。</summary>
    public bool TryGetElementState(string id, out SlideElementState state) =>
        _states.TryGetValue(id, out state);

    /// <summary>按 Id 查找本场景内的元素，未找到返回 null。</summary>
    public SlideElement GetElement(string id)
    {
        foreach (var element in _elements)
        {
            if (element.Id == id) return element;
        }
        return null;
    }

    /// <summary>注册一个联动绑定（源/目标元素按 Id 在本场景内解析）。</summary>
    public Scene AddBinding(Binding binding)
    {
        _bindings.Add(binding ?? throw new ArgumentNullException(nameof(binding)));

        // 预初始化绑定的基准源值：此时元素尚未被交互（例如页码按钮未被悬停），
        // 确保"悬停=应用、移开=恢复"的判断正确，而不是把进入场景时的瞬时状态误当基准。
        binding.Initialize(GetElement(binding.SourceElementId));

        return this;
    }

    /// <summary>更新本场景内所有联动绑定（每帧由 SlideShowManager 调用）。</summary>
    public void UpdateBindings()
    {
        foreach (var binding in _bindings)
        {
            binding.Update(GetElement(binding.SourceElementId), GetElement(binding.TargetElementId));
        }
    }

    /// <summary>将本场景的状态无动画地一次性应用到所有元素。</summary>
    public void ApplyState()
    {
        foreach (var element in _elements)
        {
            if (_states.TryGetValue(element.Id, out var state))
                element.ApplyStateImmediately(state);
        }
    }

    /// <summary>获取本场景所有元素当前状态的快照（作为转场的"起始状态"）。</summary>
    public Dictionary<string, SlideElementState> GetState()
    {
        var result = new Dictionary<string, SlideElementState>();
        foreach (var element in _elements)
            result[element.Id] = element.CurrentState?.Clone() ?? element.GetCurrentState();
        return result;
    }
}
