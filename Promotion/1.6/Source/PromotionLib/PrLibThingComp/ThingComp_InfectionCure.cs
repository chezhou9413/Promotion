using PromotionLib.PrLibDefOf;
using RimWorld;
using UnityEngine;
using Verse;

namespace PromotionLib.PrLibThingComp
{
    public class ThingCompProperties_InfectionCure : CompProperties
    {
        public int CureLeve;
        public float CurePower;
        public int CureTick; // 如果没有设置随机范围，可以用这个做固定值
        public Vector2 CureMaintainTime; // x = 最小值, y = 最大值

        public ThingCompProperties_InfectionCure()
        {
            compClass = typeof(ThingComp_InfectionCure);
        }
    }

    public class ThingComp_InfectionCure : ThingComp
    {
        public int CureLeve;
        public float CurePower;
        public int CureTick;
        public int CureMaintainTick; // 药物效果持续时间
        public ThingCompProperties_InfectionCure Props => (ThingCompProperties_InfectionCure)this.props;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CureLeve = Props.CureLeve;
            CurePower = Props.CurePower;
            // CureTick 是治疗读条时间，直接使用配置值
            CureTick = Props.CureTick;
            
            // CureMaintainTime 是药物效果持续时间，可以是随机范围
            if (Props.CureMaintainTime != Vector2.zero)
            {
                CureMaintainTick = (int)Rand.Range(Props.CureMaintainTime.x, Props.CureMaintainTime.y);
            }
            else
            {
                CureMaintainTick = Props.CureTick; // 如果没设置，默认等于治疗时间
            }
        }

        public void CurePawn(Pawn pawn)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(PrLibHediffDefOf.PRON_Antibiotic);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
            hediff  = pawn.health.AddHediff(PrLibHediffDefOf.PRON_Antibiotic);
            var antibioticComp = hediff.TryGetComp<PrLibHediffComp.HediffComp_Antibiotic>();
            if (antibioticComp != null)
            {
                antibioticComp.InitializeFromDrug(this);
            }
            Thing thing = this.parent as Thing;
            thing.SplitOff(1).Destroy();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref CureLeve, "CureLeve", 0);
            Scribe_Values.Look(ref CurePower, "CurePower", 0f);
            Scribe_Values.Look(ref CureTick, "CureTick", 0);
            Scribe_Values.Look(ref CureMaintainTick, "CureMaintainTick", 0);
        }
    }
}