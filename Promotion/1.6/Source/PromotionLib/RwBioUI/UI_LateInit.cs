using HarmonyLib;
using PromotionLib.RwBioUI.MonoComp;
using PromotionLib.RwBioUI.UIMap;
using RimWorld;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;
namespace newpro
{
    public class GameComponent_LateInit : GameComponent
    {
        private bool didInit = false;
        private bool needReinitUI = false; // 标记是否需要重新初始化UI
        private bool hasInitializedThisSession = false; // 标记本次会话是否已初始化



        public GameComponent_LateInit(Game game) { }

        public List<string> UIheads;
        public List<string> UIbodys;
        public string UIimgPath;
        // 保存和加载状态
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref didInit, "didInit", false);

            // 只在加载存档时标记需要重新初始化UI
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                needReinitUI = true;
                hasInitializedThisSession = false;
                CleanupUIInstance();
            }
        }

        // 当游戏组件被移除时清理资源
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            // 确保UI在新游戏时正确初始化
            if (Current.ProgramState == ProgramState.Playing && !hasInitializedThisSession)
            {
                needReinitUI = true;
            }
        }

        // 清理旧的UI实例
        private void CleanupUIInstance()
        {
            UiMapData.Reset(); // 重置UI数据
            didInit = false;
            hasInitializedThisSession = false;
        }

        public override void GameComponentTick()
        {
            // 只在需要重新初始化UI时才清理和重新初始化
            if (needReinitUI)
            {
                CleanupUIInstance();
                needReinitUI = false;
                didInit = false; // 重新允许初始化
                hasInitializedThisSession = false;
            }
            // 等待游戏真正开始（地图加载完成后的一些Tick）
            // 只初始化一次，除非明确需要重新初始化
            if (!didInit && !hasInitializedThisSession && Find.TickManager.TicksGame > 10 && Find.CurrentMap != null)
            {
                hasInitializedThisSession = true; // 防止重复进入
                LongEventHandler.QueueLongEvent(() =>
                {
                    InitUI();
                }, "加载UI中", false, null);
            }
        }

        public static bool InitUI()
        {
            // 确保EventSystem存在
            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                eventSystem.AddComponent<keyevents>();
                UnityEngine.Object.DontDestroyOnLoad(eventSystem);
            }
            if (UiMapData.assetBundle != null)
            {
                UiMapData.assetBundle.Unload(false);
            }// 卸载旧的AssetBundle
            UiMapData.modRootPath = LoadedModManager.GetMod<newpro>().Content.RootDir;
            string abPath = Path.Combine(UiMapData.modRootPath, "AssetBundles", "PromotionLib.ab");
            Log.Message($"[抽卡UI] 尝试加载 AssetBundle 路径: {abPath}");
            var bundle = AssetBundle.LoadFromFile(abPath);
            if (bundle == null)
            {
                Log.Error("[抽卡UI] 加载 AssetBundle 失败！");
                return false;
            }
            UiMapData.assetBundle = bundle; // 保存加载的AssetBundle
            UiMapData.buyParticle = bundle.LoadAsset<GameObject>("Assets/Scenes/res/kongbao/boom.prefab");
            UiMapData.mainUI = bundle.LoadAsset<GameObject>("Assets/Prefabs/MineTunneIUI/MineTunnelUI.prefab");
            UiMapData.mainUI = UnityEngine.Object.Instantiate(UiMapData.mainUI);
            UiMapData.mainUI.SetActive(false); // 初始时隐藏UI
            UiMapData.mainUI.transform.Find("runUI/starUI/starVide").gameObject.AddComponent<StarLord>();
            GameObject Camera = bundle.LoadAsset<GameObject>("Assets/Scenes/res/UI/UICamera.prefab");
            UiMapData.UIcamera = UnityEngine.Object.Instantiate(Camera).GetComponent<Camera>();
            UiMapData.mainUI.GetComponent<Canvas>().worldCamera = UiMapData.UIcamera;
            GameObject.DontDestroyOnLoad(UiMapData.UIcamera); // 确保UI在场景切换时不被销毁          
            GameObject.DontDestroyOnLoad(UiMapData.mainUI); // 确保UI在场景切换时不被销毁
            lordeff(UiMapData.assetBundle); // 加载特效
            return true;
        }

        static void lordeff(AssetBundle asset)
        {
            effMapData.eff = new Dictionary<string, GameObject>();
            GameObject eff = asset.LoadAsset<GameObject>("Assets/Scenes/res/UI/hpadd.prefab");
            effMapData.eff.Add("hpadd", eff);
            GameObject eff1 = asset.LoadAsset<GameObject>("Assets/Scenes/res/kongbao/boom.prefab");
            effMapData.eff.Add("boom", eff1);
        }

        public class newpro : Mod
        {
            public newpro(ModContentPack content) : base(content)
            {
            }
        }

        [HarmonyPatch(typeof(UIRoot), "UIRootOnGUI")]
        public static class PatchDisableUIRootOnGUI
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (UiMapData.uiclose)
                {
                    // 阻止原方法执行前，先清理 GUI 状态栈，防止 BeginScrollView/EndScrollView 不匹配
                    Widgets.EnsureMousePositionStackEmpty();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UIRoot), "UIRootUpdate")]
        public static class PatchDisableUIRootUpdate
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (UiMapData.uiclose)
                {
                    // 阻止原方法执行前，先清理 GUI 状态栈，防止 BeginScrollView/EndScrollView 不匹配
                    Widgets.EnsureMousePositionStackEmpty();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UIRoot_Entry), "UIRootOnGUI")]
        public static class PatchDisableUIRootOnGUIE
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (UiMapData.uiclose)
                {
                    // 阻止原方法执行前，先清理 GUI 状态栈，防止 BeginScrollView/EndScrollView 不匹配
                    Widgets.EnsureMousePositionStackEmpty();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UIRoot_Entry), "UIRootUpdate")]
        public static class PatchDisableUIRootUpdateE
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (UiMapData.uiclose)
                {
                    // 阻止原方法执行前，先清理 GUI 状态栈，防止 BeginScrollView/EndScrollView 不匹配
                    Widgets.EnsureMousePositionStackEmpty();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UIRoot_Play), "UIRootOnGUI")]
        public static class PatchDisableUIRootOnGUIP
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (UiMapData.uiclose)
                {
                    // 阻止原方法执行前，先清理 GUI 状态栈，防止 BeginScrollView/EndScrollView 不匹配
                    Widgets.EnsureMousePositionStackEmpty();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UIRoot_Play), "UIRootUpdate")]
        public static class PatchDisableUIRootUpdateP
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (UiMapData.uiclose)
                {
                    // 阻止原方法执行前，先清理 GUI 状态栈，防止 BeginScrollView/EndScrollView 不匹配
                    Widgets.EnsureMousePositionStackEmpty();
                    return false;
                }
                return true;
            }
        }
    }

    namespace PromotionLib
    {
        // 监听游戏加载事件
        [HarmonyPatch(typeof(Game), "LoadGame")]
        public static class Patch_GameLoadGame
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                // 标记需要重新初始化UI
                if (Current.Game != null)
                {
                    var gameComponent = Current.Game.GetComponent<GameComponent_LateInit>();
                    if (gameComponent != null)
                    {
                        // 通过反射设置needReinitUI字段
                        var field = typeof(GameComponent_LateInit).GetField("needReinitUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(gameComponent, true);
                            // 删除Log.Message("游戏加载事件：标记UI需要重新初始化");
                        }
                    }
                }
            }
        }
    }
}


//GameObject sdtest = bundle.LoadAsset<GameObject>("Assets/Scenes/res/UI/0x18f11a4e18192cfe.prefab");

//// 获取随机玩家pawn并在其位置显示3D模型
//if (Find.CurrentMap != null)
//{
//    // 获取所有玩家控制的pawn
//    var playerPawns = Find.CurrentMap.mapPawns.FreeColonists;
//    if (playerPawns.Count > 0)
//    {
//        // 随机选择一个玩家pawn
//        var randomPawn = playerPawns.RandomElement();
//        var pawnPosition = randomPawn.Position;

//        // 将地图坐标转换为世界坐标
//        var worldPos = randomPawn.Position.ToVector3();
//        worldPos.y = 5.0f; // 设置Y轴高度为3.0，让模型完全悬浮在地面上方，避免与地面重合

//        // 实例化3D模型并设置位置
//        UiMapData.model3D = UnityEngine.Object.Instantiate(sdtest);
//        UiMapData.model3D.transform.position = worldPos;

//        // 为3D模型添加旋转脚本，让玩家能看出是3D的
//        var rotator = UiMapData.model3D.AddComponent<ModelRotator>();

//        // 配置三向旋转参数
//        rotator.SetRotationSpeeds(80f, 120f, 60f); // X轴80度/秒, Y轴120度/秒, Z轴60度/秒
//        rotator.enableRotation = true; // 启用旋转
//        rotator.useSineWave = true; // 启用正弦波变化，让旋转更有趣
//        rotator.sineAmplitude = 15f; // 正弦波幅度
//        rotator.sineFrequency = 0.6f; // 正弦波频率
//        rotator.useRandomOffset = true; // 启用随机偏移

//        // 确保所有轴都启用旋转
//        rotator.SetAxisEnabled(true, true, true);

//        // 确保3D模型在场景切换时不被销毁
//        GameObject.DontDestroyOnLoad(UiMapData.model3D);

//        Log.Message($"[抽卡UI] 3D模型已在玩家pawn位置生成，坐标: {pawnPosition}，已添加旋转脚本");
//    }
//    else
//    {
//        Log.Warning("[抽卡UI] 未找到玩家pawn，无法生成3D模型");
//    }
//}