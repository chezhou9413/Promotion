using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine; // 用于 Color
using System;      // 用于 Random
using System.Collections.Generic;

/// <summary>
/// 联邦克隆人火力兵设置类
/// 继承自BaseUnionClone，实现火力兵特有的配置
/// </summary>
[HarmonyPatch(typeof(PawnGenerator), "TryGenerateNewPawnInternal")]
public static class UnionCloneHeavy
{
    // 火力兵配置类实例
    private static readonly UnionCloneHeavyConfig config = new UnionCloneHeavyConfig();

    [HarmonyPostfix]
    public static void Postfix(Pawn __result)
    {
        // 调用父类的处理方法
        config.ProcessClone(__result);
    }
}

/// <summary>
/// 联邦克隆人火力兵配置类
/// 继承自BaseUnionClone，定义火力兵特有的属性
/// </summary>
public class UnionCloneHeavyConfig : BaseUnionClone
{
    // ==============================
    // 抽象属性实现
    // ==============================
    
    /// <summary>
    /// 目标 PawnKind 常量集合（生效的兵种）
    /// </summary>
    protected override string[] TargetPawnKinds => new string[]
    {
        "UnionCloneHeavy"
    };

    /// <summary>
    /// 可选武器常量集合（可初始赋值）
    /// </summary>
    protected override string[] WeaponDefNames => new string[]
    {
        "Type6_gale_emg"
    };

    /// <summary>
    /// 护甲定义名称
    /// </summary>
    protected override string ArmorDefName => "CloneAlliHeavy_Armor";

    /// <summary>
    /// 头盔定义名称
    /// </summary>
    protected override string HelmetDefName => "CloneAlliHeavy_Helmet";

    /// <summary>
    /// 克隆人类型名称（用于日志和命名）
    /// </summary>
    protected override string CloneTypeName => "联邦克隆人火力兵";

    /// <summary>
    /// 克隆人品质标签
    /// </summary>
    protected override string QualityLabel => "优秀品";

    // ==============================
    // 重写属性（火力兵特有配置）
    // ==============================
    
    /// <summary>
    /// 基础心情值（火力兵心情略高）
    /// </summary>
    protected override float BaseMoodLevel => 0.85f;
    
    /// <summary>
    /// 射击技能等级（火力兵射击技能更高）
    /// </summary>
    protected override int ShootingSkillLevel => 18;
    
    /// <summary>
    /// 近战技能等级（火力兵近战技能略低）
    /// </summary>
    protected override int MeleeSkillLevel => 8;
    
    /// <summary>
    /// 医疗技能等级
    /// </summary>
    protected override int MedicineSkillLevel => 8;
    
    /// <summary>
    /// 建造技能等级（用于掩体）
    /// </summary>
    protected override int ConstructionSkillLevel => 5;
    
    /// <summary>
    /// 额外特征列表（火力兵特有特征：坚韧不拔）
    /// </summary>
    protected override string[] AdditionalTraits => new string[]
    {
        "Steadfast"
    };
} 