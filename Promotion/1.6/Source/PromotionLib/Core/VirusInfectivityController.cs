using PromotionLib.PrLibHediffComp;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PromotionLib
{
    public class VirusInfectivityController : MapComponent
    {
        public VirusInfectivityController(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            List<Pawn> pawns = map.mapPawns.AllPawns;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.DestroyedOrNull() || pawn.Dead) continue;
                if (pawn.IsHashIntervalTick(250))
                {
                    HandlePawnInfectivity(pawn);
                }
            }
        }

        private void HandlePawnInfectivity(Pawn pawn)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            foreach (Hediff hediff in hediffs)
            {
                HediffComp_VirusStrainContainer comp = hediff.TryGetComp<HediffComp_VirusStrainContainer>();
                if (comp != null && comp.virus != null)
                {
                    float increment = (comp.virus.AntigenStrength / 2f) / 100f;
                    comp.strainProgress = Mathf.Min(comp.strainProgress + increment, 1f);
                }
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}
