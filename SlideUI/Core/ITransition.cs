namespace SlideUI.Core;

/// <summary>
/// 转场算法接口：输入归一化时间（0..1），输出处理后的进度值（0..1）。
/// 实现此接口即可自定义转场曲线（线性、缓入缓出、贝塞尔等）。
/// </summary>
public interface ITransition
{
    /// <param name="time">归一化时间：0 表示转场开始，1 表示转场结束。</param>
    /// <returns>处理后的进度值，通常落在 0..1 区间。</returns>
    float Calculate(float time);
}
