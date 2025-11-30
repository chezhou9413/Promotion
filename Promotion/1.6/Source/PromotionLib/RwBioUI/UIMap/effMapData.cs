using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;
using Verse;

namespace PromotionLib.RwBioUI.UIMap
{
    [StaticConstructorOnStartup]
    public static class effMapData
    {
        public static Dictionary<string, GameObject> eff = new Dictionary<string, GameObject>();

        public static void spwneff(string effname,float size,Vector3 pos,float life)
        {
            var prefab = effMapData.eff[effname] as GameObject;
            if (prefab != null)
            {
                pos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
                GameObject go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
                
                // 设置主物体的缩放
                go.transform.localScale = new Vector3(size, size, size);
                
                // 遍历并缩放所有子物体
                ScaleAllChildren(go.transform, size);
                
                go.SetActive(true);
                UnityEngine.Object.Destroy(go, life);
            }
        } 

        /// <summary>
        /// 递归遍历并缩放所有子物体
        /// </summary>
        /// <param name="parentTransform">父物体的Transform组件</param>
        /// <param name="scale">要应用的缩放值</param>
        private static void ScaleAllChildren(Transform parentTransform, float scale)
        {
            // 遍历所有直接子物体
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform childTransform = parentTransform.GetChild(i);
                
                // 设置子物体的缩放
                childTransform.localScale = new Vector3(scale, scale, scale);
                
                // 递归处理子物体的子物体
                if (childTransform.childCount > 0)
                {
                    ScaleAllChildren(childTransform, scale);
                }
            }
        }

        static effMapData()
        {
        }

        /// <summary>
        /// 重置所有UI数据，用于地图切换或重新加载存档时
        /// </summary>
        public static void Reset()
        {
            
        }
    }
}
