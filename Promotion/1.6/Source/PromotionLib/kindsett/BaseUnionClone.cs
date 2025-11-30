using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 联邦克隆人基础设置类
/// 包含所有克隆人类型的通用逻辑
/// </summary>
public abstract class BaseUnionClone
{
    // ==============================
    // 抽象属性 - 子类必须实现
    // ==============================
    
    /// <summary>
    /// 目标 PawnKind 常量集合（生效的兵种）
    /// </summary>
    protected abstract string[] TargetPawnKinds { get; }
    
    /// <summary>
    /// 可选武器常量集合（可初始赋值）
    /// </summary>
    protected abstract string[] WeaponDefNames { get; }
    
    /// <summary>
    /// 护甲定义名称
    /// </summary>
    protected abstract string ArmorDefName { get; }
    
    /// <summary>
    /// 头盔定义名称
    /// </summary>
    protected abstract string HelmetDefName { get; }
    
    /// <summary>
    /// 克隆人类型名称（用于日志和命名）
    /// </summary>
    protected abstract string CloneTypeName { get; }
    
    /// <summary>
    /// 克隆人品质标签
    /// </summary>
    protected abstract string QualityLabel { get; }
    
    /// <summary>
    /// 基础心情值
    /// </summary>
    protected virtual float BaseMoodLevel => 0.8f;
    
    /// <summary>
    /// 射击技能等级
    /// </summary>
    protected virtual int ShootingSkillLevel => 15;
    
    /// <summary>
    /// 近战技能等级
    /// </summary>
    protected virtual int MeleeSkillLevel => 10;
    
    /// <summary>
    /// 医疗技能等级
    /// </summary>
    protected virtual int MedicineSkillLevel => 10;
    
    /// <summary>
    /// 建造技能等级
    /// </summary>
    protected virtual int ConstructionSkillLevel => 0;
    
    /// <summary>
    /// 额外特征列表
    /// </summary>
    protected virtual string[] AdditionalTraits => new string[0];

    // ==============================
    // 共享常量
    // ==============================
    
    // 随机数（不每次 new，避免种子重复）
    protected static readonly System.Random s_random = new System.Random();

    // ==============================
    // 主要处理方法
    // ==============================
    
    /// <summary>
    /// 处理克隆人设置的主要方法
    /// </summary>
    public void ProcessClone(Pawn pawn)
    {
        // 基本空值检查
        if (pawn == null)
        {
            return;
        }
        if (pawn.kindDef == null || pawn.kindDef.defName == null)
        {
            return;
        }
        if (pawn.story == null || pawn.skills == null)
        {
            return;
        }

        // 仅对人类并且是指定的 Kind 生效
        if (pawn.def != ThingDefOf.Human)
        {
            return;
        }
        if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
        {
            return;
        }
        if (!IsKindInTargetList(pawn.kindDef.defName))
        {
            return;
        }

        Log.Message("[PromotionLib] Customizing " + pawn.kindDef.defName + " traits and skills");

        // ====== 外观设定 ======
        SetupAppearance(pawn);

        // ====== 特征（Traits）设定 ======
        SetupTraits(pawn);

        // ====== 心情设定（降低精神崩溃概率） ======
        SetupMood(pawn);

        // ====== 技能设定 ======
        SetupSkills(pawn);

        // ====== 装备生成（护甲、头盔、主武器） ======
        GenerateEquipment(pawn);

        // ====== 名字与背景 ======
        SetupNameAndBackground(pawn);

        // 刷新渲染
        if (pawn.Drawer != null && pawn.Drawer.renderer != null)
        {
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }
    }

    // ==============================
    // 私有辅助方法
    // ==============================
    
    /// <summary>
    /// 设置外观
    /// </summary>
    private void SetupAppearance(Pawn pawn)
    {
        pawn.story.bodyType = BodyTypeDefOf.Male;
        HairDef shaved = DefDatabase<HairDef>.GetNamed("Shaved", false);
        if (shaved != null)
        {
            pawn.story.hairDef = shaved;
        }
        pawn.story.skinColorOverride = new Color(1.0f, 0.93f, 0.6f);
    }

    /// <summary>
    /// 设置特征
    /// </summary>
    private void SetupTraits(Pawn pawn)
    {
        if (pawn.story.traits != null && pawn.story.traits.allTraits != null)
        {
            pawn.story.traits.allTraits.Clear();

            // 基础特征
            TraitDef toughDef = TraitDef.Named("Tough");
            if (toughDef != null)
            {
                pawn.story.traits.GainTrait(new Trait(toughDef));
            }

            TraitDef braveDef = DefDatabase<TraitDef>.GetNamed("Brave", false);
            if (braveDef != null)
            {
                pawn.story.traits.GainTrait(new Trait(braveDef));
            }

            // 确保移除"Coward"（懦弱）
            TraitDef cowardDef = DefDatabase<TraitDef>.GetNamed("Coward", false);
            if (cowardDef != null && pawn.story.traits.HasTrait(cowardDef))
            {
                Trait t = pawn.story.traits.GetTrait(cowardDef);
                if (t != null)
                {
                    pawn.story.traits.RemoveTrait(t);
                }
            }

            // 添加额外特征
            foreach (string traitName in AdditionalTraits)
            {
                TraitDef traitDef = DefDatabase<TraitDef>.GetNamed(traitName, false);
                if (traitDef != null)
                {
                    pawn.story.traits.GainTrait(new Trait(traitDef));
                }
            }
        }
    }

    /// <summary>
    /// 设置心情
    /// </summary>
    private void SetupMood(Pawn pawn)
    {
        if (pawn.needs != null && pawn.needs.mood != null)
        {
            pawn.needs.mood.CurLevel = BaseMoodLevel;
        }
    }

    /// <summary>
    /// 设置技能
    /// </summary>
    private void SetupSkills(Pawn pawn)
    {
        if (pawn.skills != null && pawn.skills.skills != null)
        {
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                if (skill != null && skill.def != null && skill.def.defName != null)
                {
                    switch (skill.def.defName)
                    {
                        case "Shooting":
                            skill.Level = ShootingSkillLevel;
                            break;
                        case "Melee":
                            skill.Level = MeleeSkillLevel;
                            break;
                        case "Medicine":
                            skill.Level = MedicineSkillLevel;
                            break;
                        case "Construction":
                            skill.Level = ConstructionSkillLevel;
                            break;
                        default:
                            skill.Level = 0;
                            break;
                    }

                    // 禁止热情 & 经验增长
                    skill.passion = Passion.None;
                    skill.xpSinceLastLevel = 0f;
                    skill.xpSinceMidnight = 0f;
                }
            }
        }
    }

    /// <summary>
    /// 设置名字和背景
    /// </summary>
    private void SetupNameAndBackground(Pawn pawn)
    {
        string id = GenerateCloneName();
        pawn.Name = new NameTriple(CloneTypeName, id, QualityLabel);

        BackstoryDef child = DefDatabase<BackstoryDef>.GetNamed("UnionClone_Cadet", false);
        if (child != null)
        {
            pawn.story.Childhood = child;
        }
        BackstoryDef adult = DefDatabase<BackstoryDef>.GetNamed("UnionClone_Soldier", false);
        if (adult != null)
        {
            pawn.story.Adulthood = adult;
        }
    }

    /// <summary>
    /// 检查 Kind 是否在目标集合
    /// </summary>
    private bool IsKindInTargetList(string kindDefName)
    {
        if (kindDefName == null)
        {
            return false;
        }
        for (int i = 0; i < TargetPawnKinds.Length; i++)
        {
            if (TargetPawnKinds[i] == kindDefName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 生成装备
    /// </summary>
    private void GenerateEquipment(Pawn pawn)
    {
        if (pawn == null)
        {
            return;
        }

        // ====== 护甲 ======
        if (pawn.apparel != null)
        {
            if (!HasApparel(pawn, ArmorDefName))
            {
                ThingDef armorDef = DefDatabase<ThingDef>.GetNamed(ArmorDefName, false);
                if (armorDef != null)
                {
                    Apparel armor = ThingMaker.MakeThing(armorDef) as Apparel;
                    if (armor != null)
                    {
                        pawn.apparel.Wear(armor, false);
                    }
                }
            }

            // ====== 头盔 ======
            if (!HasApparel(pawn, HelmetDefName))
            {
                ThingDef helmetDef = DefDatabase<ThingDef>.GetNamed(HelmetDefName, false);
                if (helmetDef != null)
                {
                    Apparel helmet = ThingMaker.MakeThing(helmetDef) as Apparel;
                    if (helmet != null)
                    {
                        pawn.apparel.Wear(helmet, false);
                    }
                }
            }
        }

        // ====== 主武器：从武器常量集合随机选择 ======
        if (pawn.equipment == null)
        {
            return;
        }

        ThingDef randomWeaponDef = GetRandomWeaponDefFromPool();

        if (randomWeaponDef != null)
        {
            if (pawn.equipment.Primary == null)
            {
                ThingWithComps newWeaponA = ThingMaker.MakeThing(randomWeaponDef) as ThingWithComps;
                if (newWeaponA != null)
                {
                    pawn.equipment.AddEquipment(newWeaponA);
                }
            }
            else
            {
                if (!IsDefNameInArray(pawn.equipment.Primary.def.defName, WeaponDefNames))
                {
                    ThingWithComps cur = pawn.equipment.Primary;
                    if (cur != null)
                    {
                        pawn.equipment.Remove(cur);
                    }
                    ThingWithComps newWeaponB = ThingMaker.MakeThing(randomWeaponDef) as ThingWithComps;
                    if (newWeaponB != null)
                    {
                        pawn.equipment.AddEquipment(newWeaponB);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 从武器池随机取一个 ThingDef
    /// </summary>
    private ThingDef GetRandomWeaponDefFromPool()
    {
        if (WeaponDefNames == null || WeaponDefNames.Length == 0)
        {
            return null;
        }
        int idx = s_random.Next(WeaponDefNames.Length);
        string defName = WeaponDefNames[idx];

        if (defName == null)
        {
            return null;
        }
        ThingDef weaponDef = DefDatabase<ThingDef>.GetNamed(defName, false);
        return weaponDef;
    }

    /// <summary>
    /// 判断 Pawn 是否已穿戴指定 defName 的衣物
    /// </summary>
    private bool HasApparel(Pawn pawn, string apparelDefName)
    {
        if (pawn == null || pawn.apparel == null || pawn.apparel.WornApparel == null || apparelDefName == null)
        {
            return false;
        }

        List<Apparel> worn = pawn.apparel.WornApparel;
        for (int i = 0; i < worn.Count; i++)
        {
            Apparel a = worn[i];
            if (a != null && a.def != null && a.def.defName == apparelDefName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 在字符串数组中查找指定 defName
    /// </summary>
    private bool IsDefNameInArray(string defName, string[] pool)
    {
        if (defName == null || pool == null)
        {
            return false;
        }
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] == defName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 生成克隆名：两个大写字母 + 四个数字
    /// </summary>
    private string GenerateCloneName()
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char[] name = new char[6];

        // 前两位字母
        int i0 = s_random.Next(letters.Length);
        int i1 = s_random.Next(letters.Length);
        name[0] = letters[i0];
        name[1] = letters[i1];

        // 后四位数字
        for (int i = 2; i < 6; i++)
        {
            int d = s_random.Next(10);
            name[i] = (char)('0' + d);
        }

        return new string(name);
    }
} 