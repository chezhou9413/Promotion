using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PromotionLib
{
    public class VirusInfectivityController : MapComponent
    {
        private Dictionary<Pawn, int> pawnTickCounters = new Dictionary<Pawn, int>();

        public VirusInfectivityController(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            foreach (Pawn pawn in map.mapPawns.AllPawns)
            {
                if (!pawn.Spawned || pawn.Dead) continue;

                if (!pawnTickCounters.ContainsKey(pawn))
                {
                    pawnTickCounters[pawn] = 0;
                }

                pawnTickCounters[pawn]++;

                if (pawnTickCounters[pawn] >= 2500)
                {
                    pawnTickCounters[pawn] = 0;
                    HandlePawnInfectivity(pawn);
                }
            }

            // 使用新的扩展方法
            pawnTickCounters.RemoveAll(pair => pair.Key == null || pair.Key.Discarded || pair.Key.Dead);
        }

        private void HandlePawnInfectivity(Pawn pawn)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            foreach (Hediff hediff in hediffs)
            {
                HediffComp_VirusStrainContainer comp = hediff.TryGetComp<HediffComp_VirusStrainContainer>();
                if (comp != null && comp.virus != null)
                {
                    float increment = (comp.virus.AntigenStrength / 2f) / 10f;
                    comp.strainProgress = Mathf.Min(comp.strainProgress + increment, 1f);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pawnTickCounters, "pawnTickCounters", LookMode.Reference, LookMode.Value);
        }
    }
}
