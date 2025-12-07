using PromotionLib.PrLibHediffComp;
using RimWorld;
using Verse;
using Verse.AI;

namespace PromotionLib.PrLibWorkGiver
{
    public class WorkGive_WaitForCure : WorkGiver_Scanner
    {
        private JobDef WaitForCure = DefDatabase<JobDef>.GetNamed("Job_WaitForCure");


        public override Job NonScanJob(Pawn pawn)
        {
            if (pawn.Drafted)
            {
                return null;
            }
            if (pawn.CurJobDef == WaitForCure) return null;
            if (pawn.health.hediffSet.HasHediff(PrLibDefOf.PrLibHediffDefOf.PRON_Antibiotic))
            {
                return null;
            }
            if (!FindPawnIsInfectionVirus(pawn))
            {
                return null;
            }
            if (pawn.InBed())
            {
                return null;
            }
            if (!FindPawnIsInfectionVirus(pawn)) return null;
            if (pawn.InBed() && pawn.CurrentBed().Medical)
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