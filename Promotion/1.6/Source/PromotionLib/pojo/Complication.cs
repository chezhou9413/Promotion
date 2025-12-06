using Verse;

namespace PromotionLib
{
    public class Complication : IExposable
    {
        public string ComplicationType;
        //{
        //    /// <summary>
        //    /// 通用并发症：大多数病毒都会产生的常见并发症
        //    /// </summary>
        //    GenericComplication,
        //    /// <summary>
        //    /// 典型并发症：特定病毒的标志性症状
        //    /// </summary>
        //    SignatureComplication,

        //    /// <summary>
        //    /// 神经类典型并发症：具有神经系统特征的典型症状
        //    /// </summary>
        //    NeuroSignatureComplication,

        //    /// <summary>
        //    /// 技能类：全身状态，给予一个技能或能力
        //    /// </summary>
        //    AbilityComplication,

        //    /// <summary>
        //    /// 升华类：具有正面影响的特殊并发症（类似进化）
        //    /// </summary>
        //    EvolutionComplication
        //}

        public string TargetScope;
        //{
        //    /// <summary>
        //    /// 全身
        //    /// </summary>
        //    WholeBody,
        //    /// <summary>
        //    /// 部位
        //    /// </summary>
        //    BodyPart
        //}
        public int severityLevel;

        public void ExposeData()
        {
            Scribe_Values.Look(ref ComplicationType, "ComplicationType", "");
            Scribe_Values.Look(ref TargetScope, "TargetScope", "");
            Scribe_Values.Look(ref severityLevel, "severityLevel", 0);
        }
    }
}
