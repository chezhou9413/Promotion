using PromotionLib.PrLibDefOf;
using PromotionLib.PrLibHediffComp;
using PromotionLib.PrLibThingComp;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace PromotionLib.PrLibWorkGiver
{
    public class WorkGive_InfectionCure : WorkGiver_Scanner
    {
        private static readonly JobDef InfectionCureJob = DefDatabase<JobDef>.GetNamed("Job_InfectionCure");
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override bool HasJobOnThing(Pawn worker, Thing t, bool forced = false)
        {
            Pawn patient = t as Pawn;

            if (patient == null || patient == worker) return false;
            if (!patient.InBed())
            {
                return false;
            }
            // ============================================

            // 如果病人已经在使用抗生素，则不需要治疗
            if (patient.health.hediffSet.HasHediff(PrLibHediffDefOf.PRON_Antibiotic))
            {
                return false;
            }

            List<VirusStrain> viruses = FindPawnIsInfectionVirus(patient);
            if (viruses.Count < 1)
            {
                return false;
            }

            // 检查医生是否能预留病人
            if (!worker.CanReserve(patient, 1, -1, null, forced))
            {
                return false;
            }

            viruses = viruses.OrderByDescending(v => v.NeedHealLeve).ToList();

            // 检查是否有药物
            foreach (var virus in viruses)
            {
                Thing medicine = FindComponentMedicine(worker, virus);
                if (medicine != null)
                {
                    if (worker.CanReserve(medicine, 1, -1, null, forced))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override Job JobOnThing(Pawn worker, Thing t, bool forced = false)
        {
            Pawn patient = t as Pawn;
            if (patient == null) return null;

            // 双重保险：再次检查是否在床上
            if (!patient.InBed()) return null;

            List<VirusStrain> viruses = FindPawnIsInfectionVirus(patient);
            viruses = viruses.OrderByDescending(v => v.NeedHealLeve).ToList();

            foreach (var virus in viruses)
            {
                Thing medicine = FindComponentMedicine(worker, virus);
                if (medicine != null && !medicine.Destroyed)
                {
                    if (worker.CanReserve(medicine, 1, -1, null, forced))
                    {
                        Job job = JobMaker.MakeJob(InfectionCureJob, patient, medicine);
                        job.count = 1;
                        return job;
                    }
                }
            }
            return null;
        }

        // 下面的辅助函数保持不变
        private List<VirusStrain> FindPawnIsInfectionVirus(Pawn patient)
        {
            List<VirusStrain> Virus = new List<VirusStrain>();
            List<Hediff> hediffs = patient.health.hediffSet.hediffs;
            foreach (var hediff in hediffs)
            {
                HediffComp_VirusStrainContainer hediffComp = hediff.TryGetComp<HediffComp_VirusStrainContainer>();
                if (hediffComp != null && hediffComp.virus != null)
                {
                    if (hediffComp.virus.IsPositiveEffect == false && hediffComp.IncubationPeriod)
                    {
                        Virus.Add(hediffComp.virus);
                    }
                }
            }
            return Virus;
        }

        private Thing FindComponentMedicine(Pawn worker, VirusStrain virus)
        {
            System.Predicate<Thing> validator = (Thing x) =>
            {
                if (x.IsForbidden(worker) || !worker.CanReserve(x))
                    return false;

                ThingComp_InfectionCure thingComp = x.TryGetComp<ThingComp_InfectionCure>();
                if (thingComp == null) return false;

                return thingComp.CureLeve >= virus.NeedHealLeve;
            };

            return GenClosest.ClosestThingReachable(
                worker.Position,
                worker.Map,
                ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
                PathEndMode.ClosestTouch,
                TraverseParms.For(worker),
                9999f,
                validator
            );
        }
    }
}