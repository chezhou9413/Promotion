using PromotionLib.PrLibThingComp;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PromotionLib.PrLibJob
{
    public class JobDiver_InfectionCure : JobDriver
    {
        private const TargetIndex PatientInd = TargetIndex.A;
        private const TargetIndex MedicineInd = TargetIndex.B;

        protected Pawn Patient => (Pawn)job.GetTarget(PatientInd).Thing;
        protected Thing MedicineInHand => pawn.carryTracker.CarriedThing;

        //用于存储工作进度
        private float totalWorkNeeded = 300f;
        private float workLeft = 300f;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            //预留病人和药物
            return pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(job.GetTarget(MedicineInd), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //各种失败条件检查
            this.FailOnDestroyedOrNull(PatientInd);
            this.FailOnAggroMentalState(PatientInd); //病人发狂时不治疗
            this.FailOn(() => !Patient.InBed());
            //走到药物位置
            yield return Toils_Goto.GotoThing(MedicineInd, PathEndMode.ClosestTouch)
                .FailOnForbidden(MedicineInd);
            //拿起药物
            yield return Toils_Haul.StartCarryThing(MedicineInd, false, false, false);
            //检查是否成功拿起药物（防御性编程）
            Toil checkCarry = new Toil();
            checkCarry.initAction = () =>
            {
                if (pawn.carryTracker.CarriedThing == null)
                {
                    //没拿起来
                    EndJobWith(JobCondition.Incompletable);
                }
                else
                {
                    job.SetTarget(MedicineInd, pawn.carryTracker.CarriedThing);
                }
            };
            yield return checkCarry;

            //走到病人身边
            yield return Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch);
            Toil treatToil = new Toil();
            treatToil.defaultCompleteMode = ToilCompleteMode.Never;

            treatToil.initAction = () =>
            {
                //获取组件定义的时长
                var comp = MedicineInHand?.TryGetComp<ThingComp_InfectionCure>();
                //默认300工作量
                totalWorkNeeded = comp != null ? comp.CureTick : 300f;
                workLeft = totalWorkNeeded;

                //让小人站住别动
                pawn.pather.StopDead();
            };

            treatToil.tickAction = () =>
            {
                //让医生始终面向病人
                pawn.rotationTracker.FaceTarget(Patient);
                float speed = pawn.GetStatValue(StatDefOf.MedicalTendSpeed);
                // 扣除工作量
                workLeft -= speed;
                // 判断工作是否完成
                if (workLeft <= 0)
                {
                    // 只有这里调用 ReadyForNextToil，才会进入下一步
                    treatToil.actor.jobs.curDriver.ReadyForNextToil();
                }
            };

            //进度条UI
            treatToil.WithProgressBar(PatientInd, () => 1f - (workLeft / totalWorkNeeded));

            //药物没了就失败
            treatToil.FailOnDestroyedOrNull(MedicineInd);

            yield return treatToil;

            //治疗结算（施加效果）
            Toil applyEffectToil = new Toil();
            applyEffectToil.initAction = () =>
            {
                Pawn patient = Patient;
                Thing medicine = MedicineInHand;

                if (patient != null && !patient.Destroyed && medicine != null)
                {
                    ThingComp_InfectionCure comp = medicine.TryGetComp<ThingComp_InfectionCure>();
                    if (comp != null)
                    {
                        comp.CurePawn(patient);
                    }
                }
            };
            applyEffectToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return applyEffectToil;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workLeft, "workLeft", 300f);
            Scribe_Values.Look(ref totalWorkNeeded, "totalWorkNeeded", 300f);
        }
    }
}