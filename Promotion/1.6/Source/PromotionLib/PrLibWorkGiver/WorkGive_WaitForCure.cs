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
            Building_Bed bed = FindBestMedicalBed(pawn);
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

       
        private Building_Bed FindBestMedicalBed(Pawn pawn)
        {
            Thing foundThing = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
                PathEndMode.OnCell,
                TraverseParms.For(pawn),
                9999f, // 搜索半径
                (Thing t) => IsValidMedicalBed(t, pawn) 
            );

            return foundThing as Building_Bed;
        }
        private bool IsValidMedicalBed(Thing t, Pawn pawn)
        {
            if (t is Building_Bed bed)
            {
                if (!bed.Medical) return false;
                if (bed.ForPrisoners != pawn.IsPrisoner) return false;
                if (bed.IsForbidden(pawn)) return false;
                if (bed.IsBurning()) return false;
                bool isOwnedByMe = bed.OwnersForReading.Contains(pawn);
                bool isOccupiedBySomeoneElse = false;
                foreach (var occupant in bed.CurOccupants)
                {
                    if (occupant != pawn)
                    {
                        isOccupiedBySomeoneElse = true;
                        break;
                    }
                }
                if (!isOwnedByMe && isOccupiedBySomeoneElse) return false;

                if (!pawn.CanReserve(bed)) return false;

                return true;
            }
            return false;
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