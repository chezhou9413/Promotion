using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine; // 用于 Color
using System;      // 用于 Random
using System.Collections.Generic;

/// <summary>
/// 联邦克隆人指挥官设置类
/// 继承自BaseUnionClone，实现指挥官特有的配置
/// </summary>
[HarmonyPatch(typeof(PawnGenerator), "TryGenerateNewPawnInternal")]
public static class UnionCommanderSett
{
    // 指挥官配置类实例
    private static readonly UnionCommanderConfig config = new UnionCommanderConfig();

    [HarmonyPostfix]
    public static void Postfix(Pawn __result)
    {
        // 调用父类的处理方法
        config.ProcessClone(__result);
    }
}

/// <summary>
/// 联邦克隆人指挥官配置类
/// 继承自BaseUnionClone，定义指挥官特有的属性
/// </summary>
public class UnionCommanderConfig : BaseUnionClone
{
    // ==============================
    // 抽象属性实现
    // ==============================
    
    /// <summary>
    /// 目标 PawnKind 常量集合（生效的兵种）
    /// </summary>
    protected override string[] TargetPawnKinds => new string[]
    {
        "UnionCommander"
    };

    /// <summary>
    /// 可选武器常量集合（可初始赋值）
    /// </summary>
    protected override string[] WeaponDefNames => new string[]
    {
        "Gun_Type5EM_CQC"
    };

    /// <summary>
    /// 护甲定义名称
    /// </summary>
    protected override string ArmorDefName => "CloneSoldier_Armor";

    /// <summary>
    /// 头盔定义名称
    /// </summary>
    protected override string HelmetDefName => "CloneSoldier_Helmet";

    /// <summary>
    /// 克隆人类型名称（用于日志和命名）
    /// </summary>
    protected override string CloneTypeName => "联邦克隆人指挥官";

    /// <summary>
    /// 克隆人品质标签
    /// </summary>
    protected override string QualityLabel => "合格品";

    // ==============================
    // 可选重写属性（使用默认值）
    // ==============================
    
    // 基础心情值：使用默认值 0.8f
    // 射击技能等级：使用默认值 15
    // 近战技能等级：使用默认值 10
    // 医疗技能等级：使用默认值 10
    // 建造技能等级：使用默认值 0
    // 额外特征列表：使用默认值（空数组）
}
