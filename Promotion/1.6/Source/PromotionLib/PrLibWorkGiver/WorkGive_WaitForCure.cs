using PromotionLib.PrLibHediffComp;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace PromotionLib.PrLibWorkGiver
{
    public class WorkGive_WaitForCure : WorkGiver_Scanner
    {
        private JobDef WaitForCure = DefDatabase<JobDef>.GetNamed("Job_WaitForCure");
        public override Job NonScanJob(Pawn pawn)
        {
            if (!FindPawnIsInfectionVirus(pawn))
            {
                return null; 
            }
            if (pawn.InBed())
            {
                return null;
            }
            Building_Bed bed = RestUtility.FindPatientBedFor(pawn);
            if (bed == null)
            {
                bed = RestUtility.FindBedFor(pawn);
                if (bed == null)
                {
                    return null;
                }
            }
            // 4. 生成 Job：去那个床，并执行等待逻辑
            Job job = JobMaker.MakeJob(WaitForCure, bed);
            return job;
        }

        private bool FindPawnIsInfectionVirus(Pawn patient)
        {
            List<Hediff> hediffs = patient.health.hediffSet.hediffs;
            foreach (var hediff in hediffs)
            {
                HediffComp_VirusStrainContainer hediffComp = hediff.TryGetComp<HediffComp_VirusStrainContainer>();
                if (hediffComp != null && hediffComp.virus != null)
                {
                    if (hediffComp.virus.IsPositiveEffect == false && hediffComp.IncubationPeriod)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
