using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace PromotionLib
{
    public class ComplicationSpawner : MapComponent
    {
        // 为每个 Pawn 维护独立 tick 计数器
        private Dictionary<Pawn, int> pawnTickCounters = new Dictionary<Pawn, int>();

        public ComplicationSpawner(Map map) : base(map)
        {
        }
        //通用并发症
        private List<Hediff> GenericComplicationList = new List<Hediff>();
        //特征并发症
        private List<Hediff> SignatureComplication = new List<Hediff>();
        private List<string> symptoms = new List<string>();
        private HediffDef hediffDef = new HediffDef();
        private ComplicationComp complicationComp;
        private List<Hediff> toAdd = new List<Hediff>();

        public override void MapComponentTick()
        {
            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                if (!pawn.Spawned || pawn.Dead) continue;

                // 初始化 Pawn 的计数器
                if (!pawnTickCounters.ContainsKey(pawn))
                {
                    pawnTickCounters[pawn] = 0;
                }

                pawnTickCounters[pawn]++;
                //15000
                if (pawnTickCounters[pawn] >= 1000) // 每 6 小时
                {
                    pawnTickCounters[pawn] = 0;
                    HandleComplication(pawn);
                }
            }

            // 清理已死亡或被移除的 Pawn
            pawnTickCounters.RemoveAll(pair => pair.Key == null || pair.Key.Dead || pair.Key.Discarded);
        }

        private void HandleComplication(Pawn pawn)
        {
            toAdd.Clear();
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            foreach (Hediff h in hediffs)
            {
                HediffComp_VirusStrainContainer hediffComp = h.TryGetComp<HediffComp_VirusStrainContainer>();
                if (hediffComp != null && hediffComp.virus != null)
                {
                    GenericComplicationList.Clear();
                    SignatureComplication.Clear();
                    symptoms = hediffComp.virus.Symptoms;
                    classifyComplications(symptoms, pawn);

                    float baseProbability = GetBaseProbability(hediffComp.virus.Pathogenicity);
                    float ageFactor = GetAgeFactor(pawn);
                    float temperatureFactor = CalculateTemperatureImpact(
                        pawn.Position.GetTemperature(pawn.Map),
                        hediffComp.virus.MinAdaptedTemperature,
                        hediffComp.virus.MaxAdaptedTemperature);
                    float infectionFactor = GetInfectionImpact(hediffComp.strainProgress);

                    bool isInfected = ShouldInfect(baseProbability, ageFactor, temperatureFactor, infectionFactor, pawn.LabelShort);

                    if (isInfected)
                    {
                        Hediff hediff = null;
                        if (!hediffComp.IncubationPeriod)
                        {
                            hediff = GenericComplicationList.RandomElementWithFallback(null);
                        }
                        else
                        {
                            hediff = SignatureComplication.RandomElementWithFallback(null);
                        }

                        if (hediff != null)
                        {
                            toAdd.Add(hediff); // ✅ 不在循环中添加，先存到临时表
                        }
                    }
                }
            }
            foreach (var h in toAdd)
            {
                pawn.health.AddHediff(h);
            }

        }


        /// <summary>
        /// 用于分类并发症的函数。
        /// </summary>
        /// <param name="strings">需要分类的并发症字符串列表。</param>
        public void classifyComplications(List<string> strings, Pawn pawn)
        {
            foreach (string s in strings)
            {
                hediffDef = DefDatabase<HediffDef>.GetNamed(s);
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                complicationComp = hediff.TryGetComp<ComplicationComp>();
                if (complicationComp != null)
                {
                    if (complicationComp.Complication.ComplicationType == "GenericComplication")
                    {
                        GenericComplicationList.Add(hediff);
                    }
                    else if (complicationComp.Complication.ComplicationType == "SignatureComplication" || complicationComp.Complication.ComplicationType == "NeuroSignatureComplication" || complicationComp.Complication.ComplicationType == "AbilityComplication" || complicationComp.Complication.ComplicationType == "EvolutionComplication")
                    {
                        SignatureComplication.Add(hediff);
                    }
                }
            }
        }

        /// <summary>
        /// 根据当前温度与小人的适应温度范围，计算温度影响因子。
        /// 温度影响因子 = max(0.1, 1.0 - 温度偏差 * 0.05)
        /// </summary>
        /// <param name="currentTemp">当前小人所处环境温度</param>
        /// <param name="minComfortTemp">毒株能适应的最小温度</param>
        /// <param name="maxComfortTemp">毒株能适应的最大温度</param>
        /// <returns>返回 0.1 到 1.0 之间的温度影响因子，越低表示越不适应</returns>
        public static float CalculateTemperatureImpact(float currentTemp, float minComfortTemp, float maxComfortTemp)
        {
            float deviation = 0f;

            // 如果当前温度低于适应范围，则计算与最小适温的偏差
            if (currentTemp < minComfortTemp)
            {
                deviation = minComfortTemp - currentTemp;
            }
            // 如果当前温度高于适应范围，则计算与最大适温的偏差
            else if (currentTemp > maxComfortTemp)
            {
                deviation = currentTemp - maxComfortTemp;
            }

            // 计算影响因子，并限定最小为 0.1（最差情况）
            float impact = 1.0f - deviation * 0.05f;
            return Math.Max(0.1f, impact);
        }

        /// <summary>
        /// 根据小人的年龄阶段，返回一个对应的数值因子。
        /// 幼年返回 1.1，成年返回 1.0，老年则根据年龄增加计算因子。
        /// </summary>
        /// <param name="pawn">需要计算的小人对象</param>
        /// <returns>返回根据年龄阶段计算的因子</returns>
        /// <summary>
        /// 根据 Pawn（小人）的年龄阶段，返回影响因子：
        /// - 幼年：返回 1.1
        /// - 老年：返回 1.0 + 0.024 × (当前年龄 - 老年起始年龄)
        /// - 其余阶段：返回 1.0
        /// </summary>
        /// <param name="pawn">小人（Pawn）对象</param>
        /// <returns>年龄影响因子</returns>
        public static float GetAgeFactor(Pawn pawn)
        {
            if (pawn == null || pawn.ageTracker == null || pawn.RaceProps == null)
                return 1.0f;

            float age = pawn.ageTracker.AgeBiologicalYearsFloat;

            // 当前生命周期阶段
            LifeStageDef curStage = pawn.ageTracker.CurLifeStage;

            // 幼年判断：如果当前阶段名称中包含 "child"
            if (curStage != null && curStage.defName.ToLower().Contains("child"))
            {
                return 1.1f;
            }

            // 查找“Elder”阶段的最小年龄
            float elderAge = 45f; // 默认老年起始年龄
            var lifeStages = pawn.RaceProps.lifeStageAges;

            for (int i = lifeStages.Count - 1; i >= 0; i--)
            {
                var stage = lifeStages[i];
                if (stage.def.defName.ToLower().Contains("elder"))
                {
                    elderAge = stage.minAge;
                    break;
                }
            }

            // 如果小人年龄大于等于老年起始年龄，计算公式
            if (age >= elderAge)
            {
                float delta = age - elderAge;
                return 1.0f + 0.024f * delta;
            }

            // 默认中青年阶段返回 1.0
            return 1.0f;
        }

        /// <summary>
        /// 根据传入的感染程度（0 ~ 1）计算感染度影响因子。
        /// </summary>
        /// <param name="severity">感染程度（0 到 1 之间）</param>
        /// <returns>返回感染度影响因子</returns>
        public static float GetInfectionImpact(float severity)
        {
            if (severity < 0f)
                severity = 0f;
            else if (severity > 1f)
                severity = 1f;

            if (severity < 0.35f)
            {
                return 1.0f;
            }
            else if (severity < 0.85f)
            {
                return 1.0f + 5.0f * (severity - 0.35f);
            }
            else // severity in [0.85, 1.0]
            {
                return 3.5f + (2.0f / 3.0f) * (severity - 0.85f);
            }
        }

        /// <summary>
        /// 根据传入的致病率（百分比 0~100），计算基础概率（为致病率的 1/5）。
        /// 输入致病率应为 0~100 之间的整数或浮点数。
        /// </summary>
        /// <param name="pathogenicityPercent">致病率百分比（如 10 表示 10%）</param>
        /// <returns>返回基础概率（小数形式，如 0.04 表示 4%）</returns>
        public static float GetBaseProbability(float pathogenicityPercent)
        {
            // 限定输入范围为 0 ~ 100
            if (pathogenicityPercent < 0f)
                pathogenicityPercent = 0f;
            else if (pathogenicityPercent > 100f)
                pathogenicityPercent = 100f;

            // 百分比转为 0~1 小数，并计算基础概率
            float pathogenicity = pathogenicityPercent / 100f;
            return pathogenicity / 1.5f;
        }
        /// <summary>
        /// 根据各因子计算最终感染概率，并判断是否感染。
        /// </summary>
        /// <param name="baseProbability">基础概率（如 0.05 = 5%）</param>
        /// <param name="ageFactor">年龄影响因子</param>
        /// <param name="temperatureFactor">温度影响因子（将被 /2）</param>
        /// <param name="infectionFactor">感染度影响因子</param>
        /// <param name="debugLabel">调试标签（小人名等）</param>
        /// <returns>是否感染（true=感染，false=未感染）</returns>
        public static bool ShouldInfect(
     float baseProbability,
     float ageFactor,
     float temperatureFactor,
     float infectionFactor,
     string debugLabel = "未知小人")
        {
            // 计算最终概率
            float adjustedTempFactor = (temperatureFactor+2) / 2.0f;
            float finalChance = baseProbability * ageFactor * adjustedTempFactor * infectionFactor;

            // 生成 0~1 随机数
            float roll = Rand.Value;

            return roll < finalChance;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pawnTickCounters, "pawnTickCounters", LookMode.Reference, LookMode.Value);
        }
    }
}
