using PromotionLib.PrLibThingComp;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PromotionLib.PrLibJob
{
    public class JobDiver_InfectionCure : JobDriver
    {
        // 设置 TargetIndex 别名，方便阅读
        private const TargetIndex PatientInd = TargetIndex.A;
        private const TargetIndex MedicineInd = TargetIndex.B;

        protected Pawn Patient => (Pawn)job.GetTarget(PatientInd).Thing;
        protected Thing MedicineInHand => pawn.carryTracker.CarriedThing;

        // 用于存储工作进度的变量
        private float totalWorkNeeded = 300f;
        private float workLeft = 300f;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // 预留病人和药物
            return pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(job.GetTarget(MedicineInd), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. 各种失败条件检查
            this.FailOnDestroyedOrNull(PatientInd);
            this.FailOnAggroMentalState(PatientInd); // 病人发狂时不治疗
            // 只有当病人必须在床上时才启用下面这行，如果允许站立治疗则注释掉
            this.FailOn(() => !Patient.InBed());

            // 2. 走到药物位置
            yield return Toils_Goto.GotoThing(MedicineInd, PathEndMode.ClosestTouch)
                .FailOnForbidden(MedicineInd);

            // 3. 拿起药物
            yield return Toils_Haul.StartCarryThing(MedicineInd, false, false, false);

            // 4. 检查是否成功拿起药物（防御性编程）
            Toil checkCarry = new Toil();
            checkCarry.initAction = () =>
            {
                if (pawn.carryTracker.CarriedThing == null)
                {
                    // 没拿起来（可能被别人抢了或者逻辑错误），结束任务
                    EndJobWith(JobCondition.Incompletable);
                }
                else
                {
                    // 更新 TargetB 为手里拿着的东西，防止引用丢失
                    job.SetTarget(MedicineInd, pawn.carryTracker.CarriedThing);
                }
            };
            yield return checkCarry;

            // 5. 走到病人身边
            yield return Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch);

            // 6. 【核心修改】执行治疗逻辑
            Toil treatToil = new Toil();
            // 使用 Never，由我们在 tickAction 中手动决定何时结束
            treatToil.defaultCompleteMode = ToilCompleteMode.Never;

            treatToil.initAction = () =>
            {
                // 获取组件定义的时长
                var comp = MedicineInHand?.TryGetComp<ThingComp_InfectionCure>();
                // 默认 300 工作量（约5秒基准），如果有组件则读取组件
                totalWorkNeeded = comp != null ? comp.CureTick : 300f;
                workLeft = totalWorkNeeded;

                // 让小人站住别动
                pawn.pather.StopDead();
            };

            treatToil.tickAction = () =>
            {
                // 让医生始终面向病人
                pawn.rotationTracker.FaceTarget(Patient);

                // --- 速度计算逻辑 ---
                // 获取医生的医疗速度属性 (受医疗等级、健康、意识影响)
                // 默认普通人是 1.0，神医可能是 1.5 ~ 2.0
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

            // 自定义进度条 UI
            // 这里的计算公式是： 1 - (剩余 / 总量) = 当前进度百分比
            treatToil.WithProgressBar(PatientInd, () => 1f - (workLeft / totalWorkNeeded));

            // 药物没了就失败
            treatToil.FailOnDestroyedOrNull(MedicineInd);

            yield return treatToil;

            // 7. 治疗结算（施加效果）
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
                        comp.CurePawn(patient); // 执行你的治疗逻辑

                        // 【重要】一般在这里消耗掉药物
                        // 如果你的 CurePawn 里面写了 medicine.Destroy()，这里就不用写
                        if (!medicine.Destroyed)
                        {
                            medicine.SplitOff(1).Destroy();
                        }
                    }
                }
            };
            applyEffectToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return applyEffectToil;
        }

        // 保存加载逻辑：防止读档时工作进度丢失
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workLeft, "workLeft", 300f);
            Scribe_Values.Look(ref totalWorkNeeded, "totalWorkNeeded", 300f);
        }
    }
}