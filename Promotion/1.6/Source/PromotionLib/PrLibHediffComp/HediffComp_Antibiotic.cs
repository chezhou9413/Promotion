using PromotionLib.PrLibThingComp;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PromotionLib.PrLibHediffComp
{
    public class HediffCompProperties_Antibiotic : HediffCompProperties
    {
        public HediffCompProperties_Antibiotic()
        {
            this.compClass = typeof(HediffComp_Antibiotic);
        }
    }
    public class HediffComp_Antibiotic : HediffComp
    {
        public int cureLevel;
        public float curePower;
        public int ticksRemaining;
        public void InitializeFromDrug(ThingComp_InfectionCure drugProps)
        {
            this.cureLevel = drugProps.CureLeve;
            this.curePower = drugProps.CurePower;
            this.ticksRemaining = drugProps.CureTick;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            Pawn pawn = this.parent.pawn;
            if (pawn.DestroyedOrNull() || pawn.Dead) return;
            ticksRemaining--;
            if (ticksRemaining <= 0)
            {
                pawn.health.RemoveHediff(this.parent);
                return; // 移除后直接返回，不再执行治疗逻辑
            }
            if (pawn.IsHashIntervalTick(250))
            {
                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                for (int i = hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff hediff = hediffs[i];
                    HediffComp_VirusStrainContainer hediffComp = hediff.TryGetComp<HediffComp_VirusStrainContainer>();

                    if (hediffComp != null && hediffComp.virus != null)
                    {
                        if (hediffComp.virus.IsPositiveEffect == false)
                        {
                            float reduceAmount = 0f;
                            // 使用本地存储的 cureLevel 和 curePower
                            if (this.cureLevel >= hediffComp.virus.NeedHealLeve)
                            {
                                reduceAmount = this.curePower * Rand.Range(1f, 1.5f);
                            }
                            else
                            {
                                reduceAmount = this.curePower * Rand.Range(0.2f, 0.5f);
                            }
                            hediffComp.strainProgress = Mathf.Max(0f, hediffComp.strainProgress - reduceAmount);
                        }
                    }
                }
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", 0);
            Scribe_Values.Look(ref cureLevel, "cureLevel", 0);
            Scribe_Values.Look(ref curePower, "curePower", 0f);
        }
        public override string CompLabelInBracketsExtra => base.CompLabelInBracketsExtra + (ticksRemaining > 0 ? ticksRemaining.ToStringTicksToPeriod() : "");
    }
}