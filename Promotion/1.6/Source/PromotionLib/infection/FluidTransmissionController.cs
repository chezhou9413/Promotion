using HarmonyLib;
using PromotionLib.PrLibHediffComp;
using PromotionLib.Utils;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace PromotionLib.infection
{
    public class FluidTransmissionController
    {
        public const bool isDebug = true;

        private static void RBDugInfo(string info)
        {
            if (isDebug)
            {
                Log.Message("[PromotionLib] " + info);
            }
        }

        private static readonly List<HediffComp_VirusStrainContainer> tmpStrains = new List<HediffComp_VirusStrainContainer>();

        [HarmonyPatch(typeof(RecipeWorker), nameof(RecipeWorker.ApplyOnPawn))]
        public static class Surgery_Patch
        {
            public static void Postfix(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
            {
                // 仅对手术生效（兼容 Recipe_Surgery 派生类）
                if (bill?.recipe?.Worker is Recipe_Surgery)
                {
                    if (billDoer != null && pawn != null)
                    {
                        // 双向体液传播，内部已使用 IsInfectedWithVirus 判断重复，并在 Execute 中深拷贝病毒
                        InfectionUtility.PawnFluidTransmissionInfectedPawn(billDoer, pawn, 1f);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TendUtility), nameof(TendUtility.DoTend))]
        public static class Tend_Patch
        {
            public static void Postfix(Pawn doctor, Pawn patient)
            {
                if (doctor != null && patient != null)
                {
                    InfectionUtility.PawnFluidTransmissionInfectedPawn(doctor, patient, 1f);
                }
            }
        }

        [HarmonyPatch(typeof(JobDriver_Lovin), "MakeNewToils")]
        public static class MakeNewToils_Patch
        {
            public static void Postfix(JobDriver_Lovin __instance)
            {
                Pawn initiator = __instance.pawn;

                // 通过属性或字段获取 partner
                PropertyInfo partnerProperty = typeof(JobDriver_Lovin).GetProperty("Partner", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                Pawn partner = partnerProperty?.GetValue(__instance) as Pawn;
                if (partner == null)
                {
                    FieldInfo partnerField = typeof(JobDriver_Lovin).GetField("partner", BindingFlags.NonPublic | BindingFlags.Instance);
                    partner = partnerField?.GetValue(__instance) as Pawn;
                }

                if (initiator != null && partner != null)
                {
                    InfectionUtility.PawnFluidTransmissionInfectedPawn(initiator, partner, 2f);
                }
            }
        }

        [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
        public static class MeleeAttack_Patch
        {
            public static void Postfix(Verb_MeleeAttack __instance)
            {
                Pawn attacker = __instance.CasterPawn;
                Pawn victim = __instance.CurrentTarget.Pawn;

                if (attacker == null || victim == null)
                    return;

                // 收集攻击者身上的可体液传播病毒（避免每次分配新表）
                tmpStrains.Clear();
                tmpStrains.AddRange(VirusStrainUtils.GetAllHediffComp_VirusStrainContainerForPawn(attacker));

                foreach (var vs in tmpStrains)
                {
                    if (vs?.virus == null)
                        continue;

                    // 仅处理“体液传播”病毒，且该开关未被禁用
                    if (!vs.virus.FluidTransmission)
                        continue;
                    if (vs.isFluidTransmissionEnabled) // 被禁用则跳过（命名沿用原逻辑）
                        continue;

                    // 可感染性检查（仅当 true 时继续）
                    if (!InfectionUtility.CheckRaceInfectability(victim, vs.virus))
                        continue;

                    // 基础感染概率（Clamp 防越界）
                    float p = Mathf.Clamp01(vs.virus.Infectivity / 100f);
                    if (p <= 0f)
                        continue;

                    if (Rand.Value < p)
                    {
                        // 仅当目标未感染此病毒时执行（重复判定交由 InfectionUtility）
                        if (InfectionUtility.IsInfectedWithVirus(victim, vs.virus))
                        {
                            InfectionUtility.ExecuteVirusTransmission(victim, vs.virus);
                        }
                    }
                }
            }
        }
    }
}
