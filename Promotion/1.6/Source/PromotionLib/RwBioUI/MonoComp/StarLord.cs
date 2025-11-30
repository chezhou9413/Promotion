using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

namespace PromotionLib.RwBioUI.MonoComp
{
    public class StarLord:MonoBehaviour
    {

        [SerializeField] 
        private VideoPlayer videoPlayer;
        private bool hasExecuted = false; // 防止重复执行

        void Start()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
        }

        void OnEnable()
        {
            // 每次激活时重置状态并开始播放
            hasExecuted = false;
            if (videoPlayer != null)
            {
                videoPlayer.frame = 0; // 重置到第一帧
                videoPlayer.Play(); // 开始播放视频
            }
        }

        void Update()
        {
            // 检查视频是否播放完成且未执行过回调
            if (!hasExecuted && videoPlayer.frame >= (long)videoPlayer.frameCount - 1)
            {
                hasExecuted = true;
                OnVideoFinished();
            }
        }

        private void OnVideoFinished()
        {
            ExecuteAfterVideoFinished();
        }

        private void ExecuteAfterVideoFinished()
        {
            this.gameObject.SetActive(false);
        }
    }
}
