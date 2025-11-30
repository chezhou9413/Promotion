using PromotionLib.RwBioUI.UIMap;
using System.Collections.Generic; // Added for List
using UnityEngine;

public class keyevents : MonoBehaviour
{

    void Start()
    {


    }

    /// <summary>
    /// 验证粒子预制体是否正确加载
    /// </summary>

    void Update()
    {


        if (Input.GetKeyDown(UnityEngine.KeyCode.V))
        {

            if (UiMapData.uiclose == false)
            {
                UiMapData.uiclose = true; // 切换状态
                UiMapData.mainUI.SetActive(true); // 显示主UI
                UiMapData.mainUI.transform.Find("runUI/starUI/starVide").gameObject.SetActive(true);
                UiMapData.mainUI.transform.Find("runUI/starUI/starVide").gameObject.SetActive(true);

            }
            else
            {
                UiMapData.uiclose = false; // 切换状态
                UiMapData.mainUI.SetActive(false); // 隐藏主UI

            }
        }
        //if (Input.GetMouseButtonDown(0)) // 0 = 左键
        //{
        //    if (UiMapData.uiclose)
        //    {
        //        Vector3 worldPos = UiMapData.UIcamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(UiMapData.UIcamera.transform.position.z)));
        //        worldPos.z = 0;

        //        SpawnParticleAtPosition(worldPos);
        //    }

        //}
    }

    /// <summary>
    /// 在指定的世界坐标播放粒子
    /// </summary>
    /// <param name="worldPos">目标世界坐标</param>
    public void SpawnParticleAtPosition(Vector3 worldPos)
    {


        // 检查粒子预制体是否存在
        if (UiMapData.buyParticle == null)
        {

            return;
        }


        GameObject instGo = null;
        List<ParticleSystem> particleSystems = new List<ParticleSystem>(); // 存储所有找到的粒子系统
        var goPrefab = UiMapData.buyParticle as GameObject;
        if (goPrefab != null)
        {
            instGo = Object.Instantiate(goPrefab, worldPos, Quaternion.identity);

        }

    }
}

