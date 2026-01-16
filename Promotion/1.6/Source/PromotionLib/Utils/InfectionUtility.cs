using PromotionLib.PrLibHediffComp;
using PromotionLib.Utils;
using System.Collections.Generic;
using Verse;

namespace PromotionLib
{
    /// <summary>
    /// 感染工具类 - 提供病毒感染相关的实用方法
    /// 用于检测和管理生物体的病毒感染状态
    /// </summary>
    public static class InfectionUtility
    {
        public const bool isDebug = false;

        public static void RBDugInfo(string info)
        {
            if (isDebug)
            {
                Log.Message("[PromotionLib] " + info);
            }
        }

        private static List<string> TargetRace = new List<string>();


        /// <summary>
        /// 处理两个Pawn之间通过体液发生的双向病毒传染。
        /// </summary>
        /// <returns>如果发生了至少一次成功的传染，则返回true；否则返回false。</returns>
        public static bool PawnFluidTransmissionInfectedPawn(Pawn OnePawn, Pawn TowPawn, float contagionFactor = 1)
        {
            // 尝试 OnePawn -> TowPawn 的传染
            bool transmissionOccurred1 = TryTransmitBetweenPawns(OnePawn, TowPawn, contagionFactor);

            // 尝试 TowPawn -> OnePawn 的传染
            bool transmissionOccurred2 = TryTransmitBetweenPawns(TowPawn, OnePawn, contagionFactor);

            // 只要任意一次传染成功，就返回 true
            return transmissionOccurred1 || transmissionOccurred2;
        }

        /// <summary>
        /// 辅助函数：尝试将病毒从 source Pawn 传染给 target Pawn（单向）。
        /// </summary>
        /// <returns>如果发生了至少一次成功的传染，则返回true。</returns>
        private static bool TryTransmitBetweenPawns(Pawn source, Pawn target, float contagionFactor)
        {
            bool hasBeenInfected = false;
            // 1. 只获取源（source）身上的病毒
            List<HediffComp_VirusStrainContainer> sourceVirusStrains = VirusStrainUtils.GetAllHediffComp_VirusStrainContainerForPawn(source);

            foreach (HediffComp_VirusStrainContainer vs in sourceVirusStrains)
            {
                // 2. 检查病毒是否通过体液传播
                if (!vs.virus.FluidTransmission)
                {
                    continue; // 如果不是，跳过这个病毒
                }
                if(vs.isFluidTransmissionEnabled)
                {
                    continue; // 如果体液传播被禁用，跳过这个病毒
                }
                // 3. 检查目标（target）是否能被感染，以及是否尚未被感染
                //    这是最关键的逻辑修正：!InfectionUtility.IsInfectedWithVirus
                if (!InfectionUtility.IsInfectedWithVirus(target, vs.virus) && InfectionUtility.CheckRaceInfectability(target, vs.virus))
                {
                    // 4. 计算感染概率
                    if (Rand.Value < (vs.virus.Infectivity / 100f) * contagionFactor)
                    {
                        // 5. 执行感染
                        InfectionUtility.ExecuteVirusTransmission(target, vs.virus);
                        hasBeenInfected = true; // 标记发生了传染
                    }
                }
            }

            return hasBeenInfected;
        }
        /// <summary>
        /// 检测指定生物体是否已感染特定病毒
        /// </summary>
        /// <param name="pawn">要检测的生物体</param>
        /// <param name="virus">要检测的病毒株</param>
        /// <returns>如果生物体未感染该病毒则返回true，否则返回false</returns>
        /// <remarks>
        /// 该方法会遍历生物体的所有健康状态差异，检查是否存在相同类型的病毒
        /// 如果找到相同病毒株，则认为已感染，返回false
        /// 如果数据异常（如生物体为空或病毒为空），默认返回true允许感染
        /// </remarks>
        public static bool IsInfectedWithVirus(Pawn pawn, VirusStrain virus)
        {
            // 添加调试输出：开始检查病毒感染状态
            RBDugInfo($"开始检查病毒感染状态 - 生物: {pawn.Name?.ToStringFull ?? pawn.def.defName}, 病毒: {virus.StrainName}");

            // 外部参数检查
            if (pawn?.health?.hediffSet?.hediffs == null || virus == null)
            {
                RBDugInfo($"参数检查失败 - 生物或病毒为空，默认允许感染");
                return true; // 数据异常时，默认允许感染（可根据需求改成 false）
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            RBDugInfo($"检查生物的健康状态差异数量: {hediffs.Count}");

            foreach (Hediff hediff in hediffs)
            {
                if (hediff == null) continue;

                HediffComp_VirusStrainContainer comp = hediff.TryGetComp<HediffComp_VirusStrainContainer>();
                if (comp?.virus != null)
                {
                    if (comp.virus.UniqueID == virus.UniqueID)
                    {
                        RBDugInfo($"感染检查失败 - {pawn.Name?.ToStringFull ?? pawn.def.defName} 已感染相同病毒 {virus.StrainName}");
                        return false; // 找到同类型病毒，不能再次感染
                    }
                }
            }

            RBDugInfo($"感染检查通过 - {pawn.Name?.ToStringFull ?? pawn.def.defName} 未感染病毒 {virus.StrainName}，可以感染");
            return true; // 没找到同类型病毒，可以感染
        }


        /// <summary>
        /// 检测指定种族的病毒感染性
        /// </summary>
        /// <param name="pawn">要检测的生物体</param>
        /// <param name="virus">要检测的病毒株</param>
        /// <returns>该种族对病毒的感染性评估结果</returns>
        /// <remarks>
        /// 该方法用于评估不同种族对病毒的易感性
        /// 检查异种基因名称或种族名称是否在目标种族列表中
        /// 如果在列表中则返回true表示可以感染，否则返回false
        /// </remarks>
        public static bool CheckRaceInfectability(Pawn pawn, VirusStrain virus)
        {
            // 添加调试输出：开始检查种族感染性
            RBDugInfo($"开始检查种族感染性 - 生物: {pawn.Name?.ToStringFull ?? pawn.def.defName}, 病毒: {virus.StrainName}");

            // 检查是否为动物且病毒不能感染动物
            if (pawn.IsAnimal && !virus.CanInfectAnimals)
            {
                RBDugInfo($"感染检查失败 - {pawn.Name?.ToStringFull ?? pawn.def.defName} 是动物，但病毒 {virus.StrainName} 不能感染动物");
                return false;
            }
            // 检查是否为机械体且病毒不是机械病毒
            if (pawn.RaceProps.IsMechanoid && !virus.IsMechVirus)
            {
                RBDugInfo($"感染检查失败 - {pawn.Name?.ToStringFull ?? pawn.def.defName} 是机械体，但病毒 {virus.StrainName} 不是机械病毒");
                return false;
            }

            TargetRace = virus.TargetRace;
            RBDugInfo($"病毒 {virus.StrainName} 的目标种族列表: {(TargetRace != null ? string.Join(", ", TargetRace) : "无")}");

            // 如果目标种族列表为空，则所有种族都可以感染
            if (TargetRace == null || TargetRace.Count == 0)
            {
                RBDugInfo($"感染检查通过 - 病毒 {virus.StrainName} 没有特定目标种族限制");
                return true;
            }

            // 检查种族名称
            string raceName = pawn.def.defName;
            bool raceMatch = TargetRace.Contains(raceName);
            RBDugInfo($"种族匹配检查 - 生物种族: {raceName}, 匹配结果: {raceMatch}");

            // 检查异种基因名称
            bool xenotypeMatch = false;
            if (pawn.genes?.Xenotype != null)
            {
                string xenotypeName = pawn.genes.Xenotype.defName;
                xenotypeMatch = TargetRace.Contains(xenotypeName);
                RBDugInfo($"异种基因匹配检查 - 异种基因: {xenotypeName}, 匹配结果: {xenotypeMatch}");
            }
            else
            {
                RBDugInfo("异种基因检查跳过 - 生物没有异种基因");
            }

            // 如果种族或异种基因都不匹配，直接返回false
            if (!raceMatch && !xenotypeMatch)
            {
                RBDugInfo($"感染检查失败 - {pawn.Name?.ToStringFull ?? pawn.def.defName} 的种族和异种基因都不匹配病毒 {virus.StrainName} 的目标");
                return false;
            }

            RBDugInfo($"感染检查通过 - {pawn.Name?.ToStringFull ?? pawn.def.defName} 符合病毒 {virus.StrainName} 的感染条件");
            return true; // 修正：如果匹配成功应该返回true而不是false
        }

        /// <summary>
        /// 执行病毒传播 - 处理病毒从一个生物体传播到另一个生物体的逻辑
        /// </summary>
        /// <param name="sourcePawn">病毒源生物体（携带病毒的生物）</param>
        /// <param name="targetPawn">目标生物体（可能被感染的生物）</param>
        /// <param name="virus">要传播的病毒株</param>
        /// <returns>传播是否成功</returns>
        public static bool ExecuteVirusTransmission(Pawn targetPawn, VirusStrain virus)
        {
            if (targetPawn == null || virus == null)
                return false;

            string hediffDefName = "RimBio_GenericVirusContainer";
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (hediffDef == null)
            {
                Log.Error("[InfectionUtility] 未找到 HediffDef RimBio_GenericVirusContainer，无法施加病毒。");
                return false;
            }

            // 深拷贝病毒，避免共享引用串状态
            VirusStrain cloned = virus.Clone();

            Hediff newHediff = HediffMaker.MakeHediff(hediffDef, targetPawn);
            HediffComp_VirusStrainContainer newHediffComp = newHediff.TryGetComp<HediffComp_VirusStrainContainer>();
            if (newHediffComp == null)
            {
                Log.Error("[InfectionUtility] 新 Hediff 缺少 HediffComp_VirusStrainContainer 组件。");
                return false;
            }

            newHediffComp.SetVirusDirectly(cloned);
            newHediff.Severity = cloned.InfectionSeverity; // 可选：与病毒初始严重度对齐
            targetPawn.health.AddHediff(newHediff);
            return true;
        }
    }
}
