// 文件：Projectile_ClusterRocket.cs
using PromotionLib.RwBioUI.UIMap;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace PromotionLib.Projectile
{
    /// <summary>
    /// 顶飞集束火箭：命中后主爆 + 同心环（每圈等角均分）发射子抛射体
    /// 主抛射体 XML 需启用 <flyOverhead>true</flyOverhead>（按你原设）
    /// 子抛射体的速度/伤害/半径在其 XML（Bullet_ClusterBullet）里配置
    /// </summary>
    /// 
    public class Bullet_ClusterRocketExt : DefModExtension
    {
        public int RingCount = 5;
        public string clusterBulletDefName = "Bullet_ClusterBullet";
        public List<int> PerRingCounts = new List<int> { 6, 12, 18, 24, 30 };
        // 半径配置：首圈半径 + 每圈递增
        public float FirstRingRadius = 3f; // 第1圈半径（格）
        public float RingSpacing = 4f;  // 相邻两圈半径差（格）
    }
    public class Bullet_ClusterRocket : Projectile_Explosive
    {
        // ===== 可调参数（按需改） =====

        // 圈数
        public static int RingCount = 5;
        //子单体定义名
        public static string clusterBulletDefName = "Bullet_ClusterBullet";
        // 每圈数量：两种方式 ②优先生效 → ①兜底
        // ② 若提供数组，则按圈使用（不足的圈用 ① 的默认值）
        public static List<int> PerRingCounts = new List<int> { 6, 12, 18, 24, 30 }; // 示例：第1圈6发，第2圈10发，第3圈14发
        // ① 若没配或超出数组，就用这个统一数量
        private const int PerRingDefaultCount = 8;

        // 半径配置：首圈半径 + 每圈递增
        public static float FirstRingRadius = 3f; // 第1圈半径（格）
        public static float RingSpacing = 4f;  // 相邻两圈半径差（格）

        // 环上角度布局：是否让相邻圈相对错半步（更像“花瓣”）
        private const bool AlternateRingOffset = true;

        // 发射位置的小抖动，避免多发完全重合导致弹道重影
        private const float LaunchJitter = 0.35f;

        // 命中标志：烟花式更像直达落点，建议 IntendedTarget；要途中可被拦截则用 All
        private const ProjectileHitFlags HitFlagsForRings = ProjectileHitFlags.IntendedTarget;

        // ====== 逻辑 ======

        // RimWorld 1.4+ 正确签名（包含 blockedByShield）
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            var ext = def.GetModExtension<Bullet_ClusterRocketExt>();
            if (ext != null)
            {
                RingCount = ext.RingCount;
                clusterBulletDefName = ext.clusterBulletDefName;
                PerRingCounts = ext.PerRingCounts;
                FirstRingRadius = ext.FirstRingRadius;
                RingSpacing = ext.RingSpacing;
            }
            Map map = this.Map;
            IntVec3 center = this.Position;
            effMapData.spwneff("boom", 1f, center.ToVector3Shifted(), 3f);
            if (map == null || !center.InBounds(map))
            {
                base.Impact(hitThing, blockedByShield);
                return;
            }

            // 主爆：沿用本弹 def 的半径/伤害
            GenExplosion.DoExplosion(
                center: center,
                map: map,
                radius: this.def.projectile.explosionRadius,
                damType: this.def.projectile.damageDef ?? DamageDefOf.Bomb,
                instigator: this.launcher,
                damAmount: this.DamageAmount,
                armorPenetration: this.ArmorPenetration,
                weapon: this.equipmentDef,
                projectile: this.def
            );

            // 发射同心环的子抛射体
            SpawnClusterProjectiles_InRings(map, center);

            // 主弹消失
            this.Destroy(DestroyMode.Vanish);
        }

        /// <summary>
        /// 按同心环发射子抛射体（每圈等角均分；一次性全部生成）
        /// </summary>
        private void SpawnClusterProjectiles_InRings(Map map, IntVec3 center)
        {
            // 子弹头 def
            ThingDef clusterBulletDef = DefDatabase<ThingDef>.GetNamed(clusterBulletDefName, errorOnFail: false);
            if (clusterBulletDef == null)
            {
                Log.Warning("[PromotionLib] 未找到子抛射体定义 Bullet_ClusterBullet，跳过集束生成。");
                return;
            }

            // 给整朵“花”一个随机总旋转，让每次方向不完全一致
            float globalAngleOffset = Rand.Range(0f, Mathf.PI * 2f);

            for (int ring = 0; ring < Mathf.Max(0, RingCount); ring++)
            {
                // 本圈半径
                float radius = Mathf.Max(1f, FirstRingRadius + RingSpacing * ring);

                // 本圈数量（优先取数组）
                int count =
                    (PerRingCounts != null && ring < PerRingCounts.Count && PerRingCounts[ring] > 0)
                    ? PerRingCounts[ring]
                    : Mathf.Max(1, PerRingDefaultCount);

                // 等角步进
                float angleStep = Mathf.PI * 2f / count;

                // 偏移设置：相邻圈可错开半步角，更像花瓣
                float ringOffset = AlternateRingOffset && (ring % 2 == 1) ? angleStep * 0.5f : 0f;

                for (int i = 0; i < count; i++)
                {
                    float angle = globalAngleOffset + ringOffset + i * angleStep;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    // 目标点：沿射线取该圈半径，并把越界点沿射线夹回地图内
                    Vector3 targetV3 = center.ToVector3Shifted() + new Vector3(dir.x * radius, 0f, dir.y * radius);
                    IntVec3 targetCell = ClampToMapBoundsAlongRay(center, targetV3.ToIntVec3(), map);

                    // 发射位置（轻微抖动）
                    Vector3 launchPos = center.ToVector3Shifted() +
                        new Vector3(Rand.Range(-LaunchJitter, LaunchJitter), 0f, Rand.Range(-LaunchJitter, LaunchJitter));
                    IntVec3 launchCell = launchPos.ToIntVec3();
                    if (!launchCell.InBounds(map))
                    {
                        launchCell = center;
                        launchPos = center.ToVector3Shifted();
                    }

                    // 先 Spawn 到地图，再 Launch
                    var projThing = GenSpawn.Spawn(clusterBulletDef, launchCell, map, WipeMode.Vanish);
                    var proj = projThing as Verse.Projectile;
                    if (proj == null)
                    {
                        Log.Warning($"[PromotionLib] 生成的子抛射体不是 Verse.Projectile（def={clusterBulletDef.defName}，type={projThing?.GetType().FullName ?? "null"}）。检查 XML 的 <thingClass>。");
                        continue;
                    }

                    proj.Launch(
                        launcher: this.launcher,
                        origin: launchPos,
                        usedTarget: new LocalTargetInfo(targetCell),
                        intendedTarget: new LocalTargetInfo(targetCell),
                        hitFlags: HitFlagsForRings,
                        preventFriendlyFire: false,
                        equipment: null,
                        targetCoverDef: null
                    );
                }
            }
        }

        /// <summary>
        /// 沿中心->目标的射线把目标夹回地图内（越界时逐步回退）
        /// </summary>
        private static IntVec3 ClampToMapBoundsAlongRay(IntVec3 origin, IntVec3 target, Map map)
        {
            if (target.InBounds(map)) return target;

            Vector3 o = origin.ToVector3Shifted();
            Vector3 t = target.ToVector3Shifted();
            Vector3 d = t - o;
            float len = d.magnitude;
            if (len < 0.001f) return origin;

            d /= len; // 单位向量

            for (float s = len; s >= 1f; s -= 1f)
            {
                IntVec3 probe = (o + d * s).ToIntVec3();
                if (probe.InBounds(map)) return probe;
            }
            return origin;
        }
    }
}
