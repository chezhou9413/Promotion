using PromotionLib.PrLibDefOf;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace PromotionLib.PrLibJob
{
    public class JobDiver_WaitForCure: JobDriver
    {
        private const TargetIndex BedIndex = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(BedIndex), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Bed.GotoBed(BedIndex);
            Toil layDown = Toils_LayDown.LayDown(BedIndex, true, false, false, false);
            layDown.AddPreTickAction(() =>
            {
                if (pawn.health.hediffSet.HasHediff(PrLibHediffDefOf.PRON_Antibiotic))
                {
                    EndJobWith(JobCondition.Succeeded);
                }
            });
            yield return layDown;
        }
    }
}
