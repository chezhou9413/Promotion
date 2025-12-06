using RimWorld;
using UnityEngine;
using Verse;

namespace PromotionLib.Projectile
{
    /// <summary>
    /// 子抛射体（仅贝塞尔曲线弹道；无任何拖尾/特效改动）
    /// XML: <thingClass>PromotionLib.Projectile.Bullet_ClusterBullet</thingClass>
    /// 伤害/半径/速度仍由 XML 控制
    /// </summary>
    public class Bullet_ClusterBullet : Projectile_Explosive
    {
        // —— 可调参数 —— 
        private const bool UseBezierCurve = true;  // 设为 false 可随时恢复直线弹道
        private const float CtrlDistCells = 7f;    // 控制点沿前进方向的距离（格）
        private const float CtrlSideNoise = 2f;    // 控制点横向随机（格，左右摆幅）
        private const float MaxCtrlDistFrac = 0.7f; // 控制点距离不超过水平距离的此比例，避免短程过度弯曲

        // —— 运行时 —— 
        private bool _inited;
        private int _totalTicksToImpact = -1;
        private Vector3 _ctrlPoint; // 贝塞尔控制点（世界坐标）

        // —— 拖尾参数（白色半透明） ——
        private const int TrailSpawnIntervalTicks = 1; // 生成频率：每 N tick 生成 1 个拖尾粒子
        private const float TrailMoteScale = 0.35f;    // 拖尾粒子缩放
        private static readonly Color TrailColor = new Color(1f, 1f, 1f, 0.35f); // 半透明白
        private int _lastTrailSpawnTick = -9999;       // 上次生成 tick

        /// <summary>
        /// 只改“精确位置”：XZ 走贝塞尔，Y（高度/图层）沿用基类，保证与 flyOverhead/碰撞一致
        /// </summary>
        public override Vector3 ExactPosition
        {
            get
            {
                Vector3 basePos = base.ExactPosition;
                if (!UseBezierCurve) return basePos;

                EnsureInitialized();

                float t = 1f - (float)this.ticksToImpact / Mathf.Max(1, _totalTicksToImpact);
                t = Mathf.Clamp01(t);

                // 二次贝塞尔（xz）
                Vector2 p0 = new Vector2(this.origin.x, this.origin.z);
                Vector2 p1 = new Vector2(_ctrlPoint.x, _ctrlPoint.z);
                Vector2 p2 = new Vector2(this.destination.x, this.destination.z);

                float s = 1f - t;
                Vector2 b = s * s * p0 + 2f * s * t * p1 + t * t * p2;

                return new Vector3(b.x, basePos.y, b.y);
            }
        }

        private void EnsureInitialized()
        {
            if (_inited) return;
            _inited = true;

            _totalTicksToImpact = Mathf.Max(1, this.ticksToImpact);

            // 计算控制点：起点→终点方向 CtrlDist，再加横向随机（仅 xz 平面）
            Vector3 o = this.origin;
            Vector3 d = this.destination;
            Vector3 dir = d - o; dir.y = 0f;
            float horizLen = new Vector2(dir.x, dir.z).magnitude;

            Vector3 dirN = horizLen > 1e-6f ? dir / horizLen : new Vector3(1f, 0f, 0f);
            Vector3 perp = new Vector3(-dirN.z, 0f, dirN.x);

            // 控制点距离限制，避免目标很近时弯得过头
            float ctrlForward = Mathf.Min(CtrlDistCells, horizLen * MaxCtrlDistFrac);
            float side = Rand.Range(-CtrlSideNoise, CtrlSideNoise);

            _ctrlPoint = o + dirN * ctrlForward + perp * side;
        }

        protected override void Tick()
        {
            // 先在当前位置生成拖尾，再推进逻辑（这样拖尾更贴合实际轨迹）
            try
            {
                TrySpawnWhiteTrailMote();
            }
            catch (System.Exception ex)
            {
                // 记录错误但不中断子弹逻辑
                Log.Warning($"Bullet_ClusterBullet拖尾生成失败: {ex.Message}");
            }
            base.Tick();
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = this.Map;
            IntVec3 center = this.Position;
            base.Impact(hitThing, blockedByShield);
        }
        /// <summary>
        /// 生成白色半透明拖尾粒子（使用 Mote_Smoke，0 速度，叠加白色半透明颜色）。
        /// 为避免性能压力，按 TrailSpawnIntervalTicks 控制频率。
        /// </summary>
        private void TrySpawnWhiteTrailMote()
        {
            // 地图/生成状态校验
            if (this.Map == null || !this.Spawned || this.Destroyed) return;
            
            // 检查子弹是否还有效
            if (this.ticksToImpact <= 0) return;

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick - _lastTrailSpawnTick < TrailSpawnIntervalTicks)
            {
                return;
            }
            _lastTrailSpawnTick = currentTick;

            // 取当前精确位置，并将 y 设置到 Mote 图层高度，避免与地形/物体深度冲突
            Vector3 motePos = this.ExactPosition;
            if (float.IsNaN(motePos.x) || float.IsNaN(motePos.y) || float.IsNaN(motePos.z)) return;
            
            motePos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            // 尝试使用 FleckMaker 生成白色拖尾（更轻量级的方法）
            try
            {
                // 使用 FleckMaker 生成白色烟雾效果
                FleckMaker.ThrowSmoke(motePos, this.Map, TrailMoteScale);
                
                // 如果需要更精确的颜色控制，可以尝试创建自定义的 Fleck
                // 但基础版本先使用 ThrowSmoke 确保兼容性
            }
            catch (System.Exception ex)
            {
                // 如果 FleckMaker 失败，记录但不中断
                Log.Warning($"Bullet_ClusterBullet拖尾生成失败: {ex.Message}");
            }
        }
    }
}
