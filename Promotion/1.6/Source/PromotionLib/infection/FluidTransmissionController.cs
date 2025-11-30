using HarmonyLib;
using PromotionLib.Utils;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace PromotionLib.infection
{
    public class FluidTransmissionController
    {
        public const bool isDebug = true;
        private static List<HediffComp_VirusStrainContainer> virusStrains = new List<HediffComp_VirusStrainContainer>();
        public static void RBDugInfo(string info)
        {
            if (isDebug)
            {
                Log.Message("[PromotionLib] " + info);
            }
        }


        [HarmonyPatch(typeof(RecipeWorker), nameof(RecipeWorker.ApplyOnPawn))]
        public static class Surgery_Patch
        {
            public static void Postfix(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
            {
                // 正确的检查方式：判断配方的执行器(Worker)是否为 Recipe_Surgery 类型。
                // 使用 "is" 关键字可以同时兼容 Recipe_Surgery 的所有子类，非常可靠。
                if (bill?.recipe?.Worker is Recipe_Surgery)
                {
                    // 确保billDoer和pawn都存在
                    if (billDoer != null && pawn != null)
                    {
                        // 现在这里的逻辑只会对手术生效
                        InfectionUtility.PawnFluidTransmissionInfectedPawn(billDoer, pawn, 1f);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TendUtility), nameof(TendUtility.DoTend))]
        public static class Tend_Patch
        {
            // 注意参数名称 "doctor" 和 "patient" 与原始方法中的完全一致
            public static void Postfix(Pawn doctor, Pawn patient)
            {
                // 确保doctor和patient都存在
                if (doctor != null && patient != null)
                {
                    InfectionUtility.PawnFluidTransmissionInfectedPawn(doctor, patient, 1f);
                }
            }
        }


        [HarmonyPatch(typeof(JobDriver_Lovin), "MakeNewToils")]
        //拦截原版滚床单行为
        public static class MakeNewToils_Patch
        {
            public static void Postfix(JobDriver_Lovin __instance)
            {
                // 提取发起行为的Pawn
                Pawn initiator = __instance.pawn;
                PropertyInfo partnerProperty = typeof(JobDriver_Lovin).GetProperty("Partner", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                Pawn partner = partnerProperty?.GetValue(__instance) as Pawn;

                // 如果没有公共属性，尝试获取名为 "partner" 的私有字段
                if (partner == null)
                {
                    FieldInfo partnerField = typeof(JobDriver_Lovin).GetField("partner", BindingFlags.NonPublic | BindingFlags.Instance);
                    partner = partnerField?.GetValue(__instance) as Pawn;
                }

                // 确保双方都成功获取
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
                // 检查攻击是否成功命中，避免处理未命中的情况
                // 注意：TryCastShot的返回值在1.6版本中需要通过反编译确认。
                // 此处假设其返回bool值表示是否成功执行。
                // 简单起见，我们在此处不检查返回值，直接提取参与者。

                // 提取攻击者
                Pawn attacker = __instance.CasterPawn;

                // 提取受击者
                // CurrentTarget 是 LocalTargetInfo 类型，其.Pawn 属性可以获取Pawn对象
                Pawn victim = __instance.CurrentTarget.Pawn;

                // 确保攻击者和受击者都存在且是Pawn
                if (attacker != null && victim != null)
                {
                    virusStrains = VirusStrainUtils.GetAllHediffComp_VirusStrainContainerForPawn(attacker);
                    foreach (HediffComp_VirusStrainContainer vs in virusStrains)
                    {
                        if (vs.virus.FluidTransmission)
                        {
                            if(vs.isFluidTransmissionEnabled)
                            {
                                continue;
                            }
                            if (!InfectionUtility.CheckRaceInfectability(victim, vs.virus))
                            {
                                continue;
                            }
                            if (Rand.Value < (vs.virus.Infectivity / 100f))
                            {
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
    }
}
