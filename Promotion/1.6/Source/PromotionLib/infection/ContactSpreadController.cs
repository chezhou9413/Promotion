using HarmonyLib;
using PromotionLib.PrLibDefOf;
using PromotionLib.PrLibHediffComp;
using PromotionLib.Utils;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PromotionLib.infection
{
    public class ContactSpreadController
    {
        public const bool isDebug = true;

        // 消毒/防护等抗性系数
        private const float DisinfectionFactor = 0.2f;

        private static void RBDugInfo(string info)
        {
            if (isDebug)
            {
                Log.Message("[PromotionLib] " + info);
            }
        }

        private static readonly List<HediffComp_VirusStrainContainer> tmpStrains = new List<HediffComp_VirusStrainContainer>();

        // 统一的接触传播尝试逻辑：从物品上的VirusStrainComp对pawn进行感染尝试
        private static void TryInfectPawnFromComp(Pawn target, VirusStrainComp comp, bool applyDisinfectionResist)
        {
            if (target == null || comp == null || comp.VirusStrain == null || comp.VirusStrain.Count == 0)
                return;

            tmpStrains.Clear();
            tmpStrains.AddRange(comp.VirusStrain);

            // 不使用 UniqueID 或名称去重；重复感染判断交由 InfectionUtility.IsInfectedWithVirus 处理

            foreach (var vs in tmpStrains)
            {
                if (vs?.virus == null)
                    continue;

                // 仅处理表面携带（接触传播）
                if (vs.virus.SurfacePersistence <= 0)
                    continue;

                // 种族可感染性检查
                if (!InfectionUtility.CheckRaceInfectability(target, vs.virus))
                    continue;

                // 去重
                string uid = vs.virus.UniqueID ?? vs.virus.StrainName ?? "";
                //防护/消毒抵抗
                if (applyDisinfectionResist)
                {
                    float resist = Mathf.Clamp01(target.GetStatValue(ValueDef.Disinfection_level) * DisinfectionFactor);
                    if (Rand.Value < resist)
                        continue;
                }

                // 感染基础概率
                float p = Mathf.Clamp01(vs.virus.Infectivity / 100f);
                if (p <= 0f)
                    continue;

                if (Rand.Value < p)
                {
                    // 注意：IsInfectedWithVirus 返回 true 表示可以感染（未感染）
                    if (InfectionUtility.IsInfectedWithVirus(target, vs.virus))
                    {
                        InfectionUtility.ExecuteVirusTransmission(target, vs.virus);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GenRecipe), "PostProcessProduct")]
        public static class GenRecipe_PostProcessProduct_Patch
        {
            public static void Postfix(Thing product, RecipeDef recipeDef, Pawn worker, Precept_ThingStyle precept, ThingStyleDef style, Nullable<int> overrideGraphicIndex, ref Thing __result)
            {
                Thing finalThing = __result;
                Pawn maker = worker;
                if (maker == null || finalThing == null)
                {
                    RBDugInfo("PostProcessProduct: 物品或者制作者为空");
                    return;
                }

                // 制作过程的“污染被抵消”概率（由制作者的消毒水平决定）
                float cancelProb = Mathf.Clamp01(maker.GetStatValue(ValueDef.Disinfection_level) * DisinfectionFactor);
                if (Rand.Value < cancelProb)
                    return;

                var comp = finalThing.TryGetComp<VirusStrainComp>();
                if (comp == null)
                    return;

                // 从制作者身上获取病毒并附着到物品上
                var makerStrains = VirusStrainUtils.GetAllHediffComp_VirusStrainContainerForPawn(maker);
                if (makerStrains.Count > 0)
                {
                    comp.AddVirusStrainList(makerStrains);
                }
            }
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.SplitOff), new Type[] { typeof(int) })]
        static class Patch_Thing_SplitOff_Min
        {
            // __state = 分割前的物品（就是原实例）
            static void Prefix(Thing __instance, out Thing __state)
                => __state = __instance;

            // __result = 分割后的物品（可能是新实例，也可能仍是原实例）
            static void Postfix(ref Thing __result, Thing __state)
            {
                if (__result == null || __state == null)
                    return;

                var comp = __state.TryGetComp<VirusStrainComp>();
                if (comp == null || comp.VirusStrain == null || comp.VirusStrain.Count == 0)
                    return;

                var newcomp = __result.TryGetComp<VirusStrainComp>();
                if (newcomp == null)
                    return;

                newcomp.AddVirusStrainList(comp.VirusStrain);
            }
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
        public static class Thing_Ingested_Patch
        {
            public static void Postfix(Thing __instance, Pawn ingester, float __result)
            {
                Thing food = __instance;
                if (ingester == null || food == null)
                    return;

                var comp = food.TryGetComp<VirusStrainComp>();
                if (comp == null)
                    return;

                // 食用时尝试感染：应用消毒抵抗
                TryInfectPawnFromComp(ingester, comp, applyDisinfectionResist: true);
            }
        }

        [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentAdded))]
        /// <summary>
        /// 在原始的 Notify_EquipmentAdded 方法执行完毕后运行。
        /// </summary>
        public static class Pawn_EquipmentTracker_AddEquipment_Postfix
        {
            public static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
            {
                Pawn pawn = __instance?.pawn;
                if (pawn == null || eq == null)
                    return;

                var comp = eq.TryGetComp<VirusStrainComp>();
                if (comp == null)
                    return;

                // 穿戴装备时尝试感染：应用消毒抵抗
                TryInfectPawnFromComp(pawn, comp, applyDisinfectionResist: true);
            }
        }

        [HarmonyPatch(typeof(CompUsable), nameof(CompUsable.UsedBy))]
        public static class CompUsable_UsedBy_Patch
        {
            public static void Postfix(CompUsable __instance, Pawn p)
            {
                Thing usableThing = __instance?.parent;
                if (p == null || usableThing == null)
                    return;

                var comp = usableThing.TryGetComp<VirusStrainComp>();
                if (comp == null)
                    return;

                // 使用物品时尝试感染：应用消毒抵抗
                TryInfectPawnFromComp(p, comp, applyDisinfectionResist: true);
            }
        }
    }
}
