using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PromotionLib.RwBioUI.MonoComp
{
    public class forpawn : MonoBehaviour
    {
        public Pawn pawn; // 关联的Pawn对象
        private Vector3 worldPos;
        void Start()
        {


        }

        /// <summary>
        /// 验证粒子预制体是否正确加载
        /// </summary>

        void Update()
        {
            worldPos = pawn.DrawPos;
            worldPos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            worldPos.z = pawn.DrawPos.z - 0.5f;
            this.transform.transform.position = worldPos;
        }
    }
}
