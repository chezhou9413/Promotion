using PromotionLib.PrLibThingComp;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PromotionLib.PrLibJob
{
    public class JobDiver_InfectionCure : JobDriver
    {
        protected Pawn Patient => (Pawn)job.targetA.Thing;
        protected Thing Medicine => job.targetB.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(Medicine, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 基础失败条件
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalState(TargetIndex.A);
            this.FailOn(() => !Patient.InBed());
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnForbidden(TargetIndex.B);

            //拿起药物
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false);
            Toil checkCarry = new Toil();
            checkCarry.initAction = () =>
            {
                if (pawn.carryTracker.CarriedThing == null || pawn.carryTracker.CarriedThing != Medicine)
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            };
            yield return checkCarry;

            //走到病人位置
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            //治疗过程（读条）
            Toil treatToil = new Toil();
            treatToil.defaultCompleteMode = ToilCompleteMode.Delay;
            var tempComp = Medicine?.TryGetComp<ThingComp_InfectionCure>();
            treatToil.defaultDuration = tempComp != null ? tempComp.CureTick : 500;

            treatToil.WithProgressBarToilDelay(TargetIndex.A);
            treatToil.initAction = () =>
            {
                pawn.pather.StopDead();
            };
            treatToil.tickAction = () =>
            {
                pawn.rotationTracker.FaceTarget(Patient);
            };
            //只有当Toil正常完成时才进入下一步，如果被打断将不会进入下一个
            yield return treatToil;

            // 确保只有上面的读条完全结束后才会执行
            Toil applyEffectToil = new Toil();
            applyEffectToil.initAction = () =>
            {
                Pawn actor = applyEffectToil.actor;
                Pawn patient = Patient;
                Thing medicine = Medicine;

                if (patient != null && !patient.Destroyed && medicine != null && !medicine.Destroyed)
                {
                    // 在执行的这一刻，重新获取组件，确保获取的是正确的实例（因为堆叠分裂可能改变了实例）
                    ThingComp_InfectionCure finalComp = medicine.TryGetComp<ThingComp_InfectionCure>();

                    if (finalComp != null)
                    {
                        finalComp.CurePawn(patient);
                    }
                }
            };
            applyEffectToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return applyEffectToil;
        }
    }
}