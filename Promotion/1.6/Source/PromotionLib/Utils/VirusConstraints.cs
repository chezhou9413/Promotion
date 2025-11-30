using PromotionLib;
using System;
using System.Collections.Generic;
using static PromotionLib.VirusStrain;

/// <summary>
/// 每个病毒类型的统一数值约束逻辑
/// 修改病毒属性或变异后，调用 Apply() 自动校正
/// </summary>
public static class VirusConstraints
{
    public static void VirusStrainApply(ref VirusStrain v)
    {
        switch (v.Type)
        {
            case VirusCategory.PandemicVirus:
                // 流行病毒类
                v.MaxIncubationPeriod = Math.Min(v.MaxIncubationPeriod, 300000);
                //限制潜伏期不超过 300000 tick
                v.Infectivity = Math.Max(v.Infectivity, 50);
                // 最小传播力为 50
                v.Symptoms = CleanAndTrim(v.Symptoms, 3);
                // 症状列表最多保留 3 个
                v.StrainGene = CleanAndTrim(v.StrainGene, 3);
                // 病毒基因列表最多保留 3 个
                break;

        }
        // 保证最小潜伏期 ≤ 最大潜伏期
        if (v.MinIncubationPeriod > v.MaxIncubationPeriod)
            v.MaxIncubationPeriod = v.MinIncubationPeriod;
    }

    /// <summary>
    /// 清理集合中 null 对象，去重并优化容量，最后截断到指定长度。
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    /// <param name="list">要处理的集合（会在原地修改）</param>
    /// <param name="maxCount">保留的最大数量</param>
    public static List<T> CleanAndTrim<T>(List<T> list, int maxCount)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));

        // 1. 排除 null 对象（仅对引用类型有效）
        list.RemoveAll(item => item == null);

        // 2. 去重（保持原顺序）
        var seen = new HashSet<T>();
        list.RemoveAll(item => !seen.Add(item));

        // 3. 优化容量（减少内存占用）
        list.TrimExcess();

        // 4. 截断到指定长度
        if (maxCount >= 0 && list.Count > maxCount)
        {
            list.RemoveRange(maxCount, list.Count - maxCount);
        }

        return list;
    }
}
