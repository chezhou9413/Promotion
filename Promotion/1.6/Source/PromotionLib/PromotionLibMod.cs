using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PromotionLib
{
    [StaticConstructorOnStartup]
    public static class PromotionLibStartup
    {
        static Harmony harmony;

        static PromotionLibStartup()
        {
            Log.Message("[PromotionLib] 游戏启动，开始初始化 Harmony 和添加组件");

            harmony = new Harmony("PromotionLib.core");
            harmony.PatchAll(typeof(PromotionLibStartup).Assembly);

            // 调用添加组件到所有物品的函数
            AddCompToAllItems();
        }

        public static void AddCompToAllItems()
        {
            // 遍历所有物品定义
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
            {
                // 确保物品不是抽象的，并且属于物品类（包括武器）
                if (!thingDef.thingClass.IsAbstract && thingDef.category == ThingCategory.Item)
                {
                    // 如果物品是装备，也应该检查它是否具有物品属性（包括武器）
                    if (thingDef.comps == null)
                        thingDef.comps = new List<CompProperties>();

                    // 确保没有重复添加组件
                    if (thingDef.comps.All(comp => comp.GetType() != typeof(VirusStrainComp)))
                    {
                        // 为物品或装备添加 VirusStrainComp 组件
                        thingDef.comps.Add(new CompProperties_VirusStrain());
                    }
                }
            }
        }
    }
}
