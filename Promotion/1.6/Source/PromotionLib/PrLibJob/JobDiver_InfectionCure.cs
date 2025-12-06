using PromotionLib.PrLibDefOf;
using PromotionLib.PrLibThingComp;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PromotionLib.PrLibJob
{
    public class JobDiver_InfectionCure:JobDriver
    {
        protected Pawn Patient => (Pawn)job.targetA.Thing;
        protected Thing Medicine => job.targetB.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 同时预定病人和药物
            return pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(Medicine, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Log.Message($"[InfectionCure] 开始执行治疗任务:\n" +
            //             $"  - 医生 (Doctor): {pawn.LabelShort} (ID: {pawn.thingIDNumber})\n" +
            //             $"  - 病人 (Patient): {(Patient != null ? Patient.LabelShort : "null")}\n" +
            //             $"  - 药物 (Medicine): {(Medicine != null ? Medicine.LabelShort : "null")}");
            //去拿药
            // 设定失败条件：如果药没了或病人死了/好了
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalState(TargetIndex.A);
            this.FailOn(() => !Patient.InBed());
            //走到药物位置
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnForbidden(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil treatToil = new Toil();
            treatToil.defaultCompleteMode = ToilCompleteMode.Delay;
            ThingComp_InfectionCure comp_InfectionCure = Medicine?.TryGetComp<ThingComp_InfectionCure>();
            treatToil.defaultDuration = comp_InfectionCure != null ? comp_InfectionCure.CureTick : 500;
            //让病人不动
            treatToil.initAction = () =>
            {
                pawn.pather.StopDead();
            };

            treatToil.tickAction = () =>
            {
                pawn.rotationTracker.FaceTarget(Patient);
            };
            treatToil.WithProgressBarToilDelay(TargetIndex.A);
            treatToil.AddFinishAction(() =>
            {
                // ✅ 完善的防御性检查
                if (Patient != null && !Patient.Destroyed && 
                    Medicine != null && !Medicine.Destroyed)
                {
                    if (comp_InfectionCure != null)
                    {
                        comp_InfectionCure.CurePawn(Patient);
                    }
                    else
                    {
                        Log.Warning($"[InfectionCure] 药物 {Medicine.Label} 没有 ThingComp_InfectionCure 组件");
                    }
                }
                else
                {
                    Log.Warning($"[InfectionCure] 治疗失败 - 病人或药物已销毁或为null");
                }
            });
            yield return treatToil;
        }
    }
}
