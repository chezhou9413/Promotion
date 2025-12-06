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
            if (patient.Faction != Faction.OfPlayer && !patient.IsPrisonerOfColony && !patient.InBed())
            {
                return false;
            }
            if (patient.health.hediffSet.HasHediff(PrLibHediffDefOf.PRON_Antibiotic))
            {
                return false;
            }
            List<VirusStrain> viruses = FindPawnIsInfectionVirus(patient);
            viruses = viruses.OrderByDescending(v => v.NeedHealLeve).ToList();
            if (viruses.Count < 1)
            {
                return false;
            }
            if (!worker.CanReserve(patient, 1, -1, null, forced))
            {
                return false;
            }
            // 检查是否有可用的药物来治疗任何一种病毒
            foreach (var virus in viruses)
            {
                Thing medicine = FindComponentMedicine(worker, virus);
                if (medicine != null)
                {
                    // ✅ 立即预留药物，防止其他 Pawn 拿走
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
            if (patient == null)
            {
                return null;
            }

            List<VirusStrain> viruses = FindPawnIsInfectionVirus(patient);
            viruses = viruses.OrderByDescending(v => v.NeedHealLeve).ToList();
            
            foreach (var virus in viruses)
            {
                Thing medicine = FindComponentMedicine(worker, virus);
                if (medicine != null && !medicine.Destroyed)
                {
                    // ✅ 再次确认可以预留
                    if (worker.CanReserve(medicine, 1, -1, null, forced))
                    {
                        Job job = JobMaker.MakeJob(InfectionCureJob, patient, medicine);
                        job.count = 1;
                        return job;
                    }
                }
            }
            
            // ❌ 如果找不到药物，返回 null（WorkGiver 会重新评估）
            return null;
        }

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
        // 查找药物的辅助函数
        private Thing FindComponentMedicine(Pawn worker, VirusStrain virus)
        {
            // 定义搜索条件
            System.Predicate<Thing> validator = (Thing x) =>
            {
                if (x.IsForbidden(worker) || !worker.CanReserve(x)) 
                    return false;
                
                ThingComp_InfectionCure thingComp = x.TryGetComp<ThingComp_InfectionCure>();
                if (thingComp == null)
                {
                    return false;
                }
                
                if (thingComp.CureLeve >= virus.NeedHealLeve)
                {
                    return true;
                }
                
                return false;
            };

            // 在地图上搜索最近的符合条件的物品
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
