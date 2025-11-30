using HarmonyLib;
using PromotionLib.Utils;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace PromotionLib.infection
{
    public class ContactSpreadController
    {
        public const bool isDebug = true;

        public static void RBDugInfo(string info)
        {
            if (isDebug)
            {
                Log.Message("[PromotionLib] " + info);
            }
        }
        private static List<HediffComp_VirusStrainContainer> virusStrains = new List<HediffComp_VirusStrainContainer>();

        [HarmonyPatch(typeof(GenRecipe), "PostProcessProduct")]
        public static class GenRecipe_PostProcessProduct_Patch
        {
            public static void Postfix(Thing product, RecipeDef recipeDef, Pawn worker, Precept_ThingStyle precept, ThingStyleDef style, Nullable<int> overrideGraphicIndex, ref Thing __result)
            {
                Thing finalThing = __result;
                Pawn makepawn = worker;
                if (makepawn == null || finalThing == null)
                {
                    RBDugInfo("物品或者制作者为空");
                    return;
                }
                float pop = makepawn.GetStatValue(ValueDef.Disinfection_level) * 0.2f;
                if (Rand.Value < pop)
                {
                    return;
                }
                virusStrains = VirusStrainUtils.GetAllHediffComp_VirusStrainContainerForPawn(makepawn);
                VirusStrainComp comp = finalThing.TryGetComp<VirusStrainComp>();
                if (comp != null && virusStrains.Count > 0)
                {
                    comp.AddVirusStrainList(virusStrains);
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
                if (__result != null && __state != null)
                {
                    VirusStrainComp comp = __state.TryGetComp<VirusStrainComp>();
                    if (comp != null)
                    {
                        VirusStrainComp newcomp = __result.TryGetComp<VirusStrainComp>();
                        if (newcomp != null)
                        {
                            newcomp.AddVirusStrainList(comp.VirusStrain);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Thing), nameof(Thing.Ingested))]
        public static class Thing_Ingested_Patch
        {
            public static void Postfix(Thing __instance, Pawn ingester, float __result)
            {
                // `__instance` 在这里就是被吃掉的那个物品 (Thing)
                Thing food = __instance;

                // 安全检查
                if (ingester == null || food == null)
                {
                    return;
                }
                VirusStrainComp comp = food.TryGetComp<VirusStrainComp>();
                if (comp == null || comp.VirusStrain.Count == 0)
                {
                    return;
                }
                virusStrains = comp.VirusStrain;
                foreach (HediffComp_VirusStrainContainer vs in virusStrains)
                {
                    if (vs.virus.SurfacePersistence > 0)
                    {
                        if (!InfectionUtility.CheckRaceInfectability(ingester, vs.virus))
                        {
                            continue;
                        }
                        if (Rand.Value < (vs.virus.Infectivity / 100f))
                        {
                            if (InfectionUtility.IsInfectedWithVirus(ingester, vs.virus))
                            {
                                InfectionUtility.ExecuteVirusTransmission(ingester, vs.virus);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentAdded))]
        /// <summary>
        /// 在原始的 Notify_EquipmentAdded 方法执行完毕后运行。
        /// </summary>
        /// <param name="__instance">Pawn_EquipmentTracker 的实例</param>
        /// <param name="eq">刚刚被装备上的物品</param>
        public static class Pawn_EquipmentTracker_AddEquipment_Postfix
        {
            public static void Postfix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
            {
                Pawn pawn = __instance.pawn;
                if (pawn == null || eq == null)
                {
                    return; // 安全检查
                }
                VirusStrainComp comp = eq.TryGetComp<VirusStrainComp>();
                virusStrains = comp.VirusStrain;
                foreach (HediffComp_VirusStrainContainer vs in virusStrains)
                {
                    if (vs.virus.SurfacePersistence > 0)
                    {
                        if (!InfectionUtility.CheckRaceInfectability(pawn, vs.virus))
                        {
                            continue;
                        }
                        float pop = pawn.GetStatValue(ValueDef.Disinfection_level) * 0.2f;
                        if (Rand.Value < pop)
                        {
                            continue;
                        }
                        if (Rand.Value < (vs.virus.Infectivity / 100f))
                        {
                            if (InfectionUtility.IsInfectedWithVirus(pawn, vs.virus))
                            {
                                InfectionUtility.ExecuteVirusTransmission(pawn, vs.virus);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CompUsable), nameof(CompUsable.UsedBy))]
        public static class CompUsable_UsedBy_Patch
        {
            public static void Postfix(CompUsable __instance, Pawn p)
            {
                Thing usableThing = __instance.parent;
                if (p == null || usableThing == null)
                {
                    return;
                }
                VirusStrainComp comp = usableThing.TryGetComp<VirusStrainComp>();
                if (comp == null || comp.VirusStrain.Count == 0)
                {
                    return;
                }
                virusStrains = comp.VirusStrain;
                foreach (HediffComp_VirusStrainContainer vs in virusStrains)
                {
                    if (vs.virus.SurfacePersistence > 0)
                    {
                        if (!InfectionUtility.CheckRaceInfectability(p, vs.virus))
                        {
                            continue;
                        }
                        float pop = p.GetStatValue(ValueDef.Disinfection_level) * 0.2f;
                        if (Rand.Value < pop)
                        {
                            continue;
                        }
                        if (Rand.Value < (vs.virus.Infectivity / 100f))
                        {
                            if (InfectionUtility.IsInfectedWithVirus(p, vs.virus))
                            {
                                InfectionUtility.ExecuteVirusTransmission(p, vs.virus);
                            }
                        }
                    }
                }
            }
        }
    }
}
