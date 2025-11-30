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
    public static class UiMapData
    {
        /// <summary>
        /// 全局可访问的字典 Map，用于保存任意键值对。
        /// </summary>
        public static bool uiclose = false;

        public static string modRootPath;

        public static AssetBundle assetBundle;

        public static GameObject mainUI;

        public static Camera UIcamera;

        public static GameObject buyParticle;
        
        /// <summary>
        /// 3D模型对象，用于在玩家pawn位置显示
        /// </summary>
        public static GameObject model3D;
        
        static UiMapData()
        {
        }

        /// <summary>
        /// 重置所有UI数据，用于地图切换或重新加载存档时
        /// </summary>
        public static void Reset()
        {
            UIcamera = null; // 重置UI相机
            mainUI = null; // 重置主UI对象
            assetBundle = null; // 重置AssetBundle
            modRootPath = null; // 重置mod根目录路径
            uiclose = false;
            buyParticle = null; // 重置购买粒子预制体
            model3D = null; // 重置3D模型对象
            // UIraceimg 路径不需要重置，因为它是基于mod根目录的
        }
    }
}
