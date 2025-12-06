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

        // 动态获取小人携带系统里当前拿着的东西。
        protected Thing MedicineInHand => pawn.carryTracker.CarriedThing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 预留病人和地面上的药物
            return pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 病人必须存在，必须在床上
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnAggroMentalState(TargetIndex.A);
            this.FailOn(() => !Patient.InBed());

            //走到药物位置
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch)
                .FailOnForbidden(TargetIndex.B);

            //拿起药物
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false);

            //检查是否真的拿到了药物
            Toil checkCarry = new Toil();
            checkCarry.initAction = () =>
            {
                //如果手里是空的，说明拿取失败
                if (pawn.carryTracker.CarriedThing == null)
                {
                    EndJobWith(JobCondition.Incompletable);
                }
                else
                {
                    job.SetTarget(TargetIndex.B, pawn.carryTracker.CarriedThing);
                }
            };
            yield return checkCarry;

            //走到病人位置
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            //治疗读条
            Toil treatToil = new Toil();
            treatToil.defaultCompleteMode = ToilCompleteMode.Delay;
            treatToil.WithProgressBarToilDelay(TargetIndex.A);

            treatToil.initAction = () =>
            {
                //获取手中药物的组件来决定治疗时间
                var comp = MedicineInHand?.TryGetComp<ThingComp_InfectionCure>();
                treatToil.defaultDuration = comp != null ? comp.CureTick : 500;
                pawn.pather.StopDead();
            };

            treatToil.tickAction = () =>
            {
                pawn.rotationTracker.FaceTarget(Patient);
            };

            treatToil.FailOnDestroyedOrNull(TargetIndex.B);

            yield return treatToil;

            //治疗结算（施加 Hediff 并消耗药物）
            Toil applyEffectToil = new Toil();
            applyEffectToil.initAction = () =>
            {
                Pawn patient = Patient;
                Thing medicine = MedicineInHand; //使用我们定义的属性获取手中的药

                if (patient != null && !patient.Destroyed && medicine != null)
                {
                    ThingComp_InfectionCure comp = medicine.TryGetComp<ThingComp_InfectionCure>();
                    if (comp != null)
                    {
                        // 执行治疗：施加 Hediff
                        comp.CurePawn(patient);

                        // CurePawn 里你写了 thing.SplitOff(1).Destroy()
                        // 因为现在 medicine 就是手里的那个（数量通常为1），SplitOff(1) 会返回它自己，然后被销毁
                        // 逻辑是通的。
                    }
                    else
                    {
                        Log.Error($"[InfectionCure] 错误：手中的物品 {medicine.Label} 没有 ThingComp_InfectionCure 组件。");
                    }
                }
            };
            applyEffectToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return applyEffectToil;
        }
    }
}