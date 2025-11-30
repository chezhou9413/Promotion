using PromotionLib.comp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PromotionLib.Place
{
    public class PlaceWorker_ShowEffectRange : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            // 从建筑的Def中获取你的自定义组件属性
            BuildingStrainClearCompProperties compProps = def.GetCompProperties<BuildingStrainClearCompProperties>();

            // 如果找到了这个组件，并且它的范围大于0
            if (compProps != null && compProps.effectRange > 0f)
            {
                // 就使用它的 effectRange 属性来绘制范围圈
                GenDraw.DrawRadiusRing(center, compProps.effectRange);
            }
        }
    }
}
