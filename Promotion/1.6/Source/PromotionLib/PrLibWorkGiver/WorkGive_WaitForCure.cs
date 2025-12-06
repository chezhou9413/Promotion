using PromotionLib.PrLibHediffComp;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PromotionLib.PrLibWorkGiver
{
    public class WorkGive_WaitForCure : WorkGiver_Scanner
    {
        private JobDef WaitForCure = DefDatabase<JobDef>.GetNamed("Job_WaitForCure");

        public override Job NonScanJob(Pawn pawn)
        {
            //如果小人被征召，不要分配这个任务，否则会冲突报错
            if (pawn.Drafted)
            {
                return null;
            }

            //检查是否有特定病毒
            if (!FindPawnIsInfectionVirus(pawn))
            {
                return null;
            }

            //已经在床上就不需要再发Job了
            if (pawn.InBed())
            {
                return null;
            }
            Building_Bed bed = RestUtility.FindPatientBedFor(pawn);
            if (bed == null)
            {
                bed = RestUtility.FindBedFor(pawn);
            }

            if (bed == null)
            {
                return null;
            }
            if (!pawn.CanReserve(bed))
            {
                return null;
            }
            Job job = JobMaker.MakeJob(WaitForCure, bed);
            return job;
        }

        private bool FindPawnIsInfectionVirus(Pawn patient)
        {
            if (patient.health?.hediffSet?.hediffs == null) return false;

            foreach (var hediff in patient.health.hediffSet.hediffs)
            {
                HediffComp_VirusStrainContainer hediffComp = hediff.TryGetComp<HediffComp_VirusStrainContainer>();
                if (hediffComp?.virus != null)
                {
                    if (hediffComp.virus.IsPositiveEffect == false && !hediffComp.IncubationPeriod)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}