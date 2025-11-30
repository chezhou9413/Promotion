using PromotionLib.RwBioUI.MonoComp;
using PromotionLib.RwBioUI.UIMap;
using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace PromotionLib.Projectile
{
    public class Bullet_healing_pistoExt : DefModExtension
    {
        // 追踪强度（越大越“吸”向目标）
        public float TrackingStrength = 20f;
        // 治疗品质范围 [0..1]
        public float TendQualityMin = 0.20f;
        public float TendQualityMax = 0.60f;
    }
    public class Bullet_healing_pistol : Bullet
    {
        // ==== 配置缓存（来自 DefModExtension）====
        private float cfgTrackingStrength = 20f;
        private float cfgTendQualityMin = 0.20f;
        private float cfgTendQualityMax = 0.60f;

        // 其余字段保持不变……
        private Vector3 lastPosition;
        private bool initialized = false;
        private int creationTick = 0;
        private LocalTargetInfo originalTarget;
        private Thing launcher;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            var ext = def.GetModExtension<Bullet_healing_pistoExt>();
            if (ext != null)
            {
                // 读取并规整参数
                cfgTrackingStrength = Mathf.Max(0f, ext.TrackingStrength);

                // 允许写反：自动取 min/max，并夹到 [0,1]
                float a = Mathf.Clamp01(ext.TendQualityMin);
                float b = Mathf.Clamp01(ext.TendQualityMax);
                cfgTendQualityMin = Mathf.Min(a, b);
                cfgTendQualityMax = Mathf.Max(a, b);
            }
        }

        protected override void Tick()
        {
            if (!initialized) Initialize();

            if (Find.TickManager.TicksGame - creationTick > 200)
            {
                ForceHitTarget();
                return;
            }
            base.Tick();
        }

        private void Initialize()
        {
            lastPosition = base.ExactPosition; // 用基类位置初始化
            initialized = true;
            creationTick = Find.TickManager.TicksGame;
            originalTarget = usedTarget;
            FindRealTarget();
        }

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget,
            ProjectileHitFlags projectileHitFlags, bool preventFriendlyFire = false, Thing equipment = null, ThingDef targetCoverDef = null)
        {
            this.launcher = launcher;
            this.originalTarget = usedTarget;
            base.Launch(launcher, origin, usedTarget, intendedTarget, projectileHitFlags, preventFriendlyFire, equipment, targetCoverDef);
        }

        // ⚠ 你的原实现里这里用到了未定义的变量 origin；改为用当前弹体位置。
        private void FindRealTarget()
        {
            try
            {
                if (launcher is Pawn shooterPawn)
                {
                    if (shooterPawn.CurJob?.targetA.HasThing == true)
                        originalTarget = shooterPawn.CurJob.targetA;

                    if (shooterPawn.stances?.curStance is Stance_Warmup warmupStance && warmupStance.focusTarg.IsValid)
                        originalTarget = warmupStance.focusTarg;
                }

                if (!originalTarget.HasThing && originalTarget.Cell.IsValid)
                {
                    var list = originalTarget.Cell.GetThingList(Find.CurrentMap)
                        .OfType<Pawn>().Where(p => p.Spawned && !p.Dead).ToList();
                    if (list.Any())
                    {
                        var here = base.ExactPosition; // ← 用当前弹体位置做距离比较
                        var nearest = list.OrderBy(p => Vector3.Distance(here, p.TrueCenter())).First();
                        originalTarget = new LocalTargetInfo(nearest);
                    }
                }
            }
            catch { /* 保持原目标 */ }
        }

        public override Vector3 ExactPosition
        {
            get
            {
                if (!originalTarget.IsValid) return base.ExactPosition;

                Vector3 targetPos = GetCurrentTargetPosition();
                Vector3 basePos = base.ExactPosition;

                float distance = Vector3.Distance(basePos, targetPos);
                if (distance < 0.5f) return targetPos;

                Vector3 dir = (targetPos - basePos).normalized;
                // 使用配置化追踪强度
                Vector3 trackingPos = basePos + dir * (cfgTrackingStrength * 0.1f);

                lastPosition = trackingPos;
                return trackingPos;
            }
        }

        private Vector3 GetCurrentTargetPosition()
        {
            try
            {
                if (originalTarget.HasThing && originalTarget.Thing.Spawned)
                    return originalTarget.Thing.TrueCenter();
                return originalTarget.Cell.ToVector3Shifted();
            }
            catch { return originalTarget.Cell.ToVector3Shifted(); }
        }

        private void ForceHitTarget()
        {
            if (!originalTarget.IsValid) return;
            try
            {
                Thing t = originalTarget.Thing;
                Impact(t != null && t.Spawned ? t : null, false);
                Destroy();
            }
            catch { Destroy(); }
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            if (hitThing == null && originalTarget.IsValid && originalTarget.Thing != null)
                hitThing = originalTarget.Thing;

            if (hitThing is Pawn pawn && pawn.health != null)
                ApplyHealing(pawn);

            base.Impact(hitThing, blockedByShield);
        }

        private void ApplyHealing(Pawn targetPawn)
        {
            if (targetPawn?.health == null) return;

            var stimulantDef = DefDatabase<HediffDef>.GetNamed("HealingStimulant", errorOnFail: false);
            if (stimulantDef != null)
            {
                var ex = targetPawn.health.hediffSet.GetFirstHediffOfDef(stimulantDef);
                if (ex != null) targetPawn.health.RemoveHediff(ex);
                var stim = HediffMaker.MakeHediff(stimulantDef, targetPawn);
                stim.Severity = 1.0f;
                targetPawn.health.AddHediff(stim);
            }

            // 用配置的品质范围进行包扎
            var tendables = targetPawn.health.hediffSet.hediffs
                .OfType<HediffWithComps>()
                .Where(h => h.TendableNow(ignoreTimer: false) && !h.IsTended())
                .ToList();

            foreach (var h in tendables)
            {
                float q = Rand.Range(cfgTendQualityMin, cfgTendQualityMax); // ← 配置化
                q = Mathf.Clamp01(q);
                h.Tended(q, 1f);
            }

            var prefab = effMapData.eff["hpadd"] as GameObject;
            if (prefab != null)
            {
                GameObject go = UnityEngine.Object.Instantiate(prefab, new Vector2(-9999, -9999), Quaternion.identity);
                var fp = go.AddComponent<forpawn>();
                fp.pawn = targetPawn;
                go.SetActive(true);
                UnityEngine.Object.Destroy(go, 2f);
            }
        }

        // 可选：把配置序列化（读档不丢）
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cfgTrackingStrength, "cfgTrackingStrength", 20f);
            Scribe_Values.Look(ref cfgTendQualityMin, "cfgTendQualityMin", 0.20f);
            Scribe_Values.Look(ref cfgTendQualityMax, "cfgTendQualityMax", 0.60f);
            Scribe_Values.Look(ref creationTick, "creationTick", 0);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_TargetInfo.Look(ref originalTarget, "originalTarget");
        }
    }

}