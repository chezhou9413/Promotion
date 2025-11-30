using UnityEngine;
using Verse;

namespace PromotionLib.RwBioUI.MonoComp
{
    /// <summary>
    /// 3D模型三向旋转脚本，让模型在X、Y、Z三个轴上同时旋转以展示3D效果
    /// </summary>
    public class ModelRotator : MonoBehaviour
    {
        [Header("三向旋转设置")]
        [Tooltip("X轴旋转速度（度/秒）")]
        public float rotationSpeedX = 60f; // X轴旋转速度
        
        [Tooltip("Y轴旋转速度（度/秒）")]
        public float rotationSpeedY = 120f; // Y轴旋转速度
        
        [Tooltip("Z轴旋转速度（度/秒）")]
        public float rotationSpeedZ = 90f; // Z轴旋转速度
        
        [Tooltip("是否启用旋转")]
        public bool enableRotation = true;
        
        [Header("高级设置")]
        [Tooltip("是否使用正弦波变化旋转速度")]
        public bool useSineWave = false;
        
        [Tooltip("正弦波幅度")]
        public float sineAmplitude = 20f;
        
        [Tooltip("正弦波频率")]
        public float sineFrequency = 1f;
        
        [Tooltip("是否启用随机旋转偏移")]
        public bool useRandomOffset = true;
        
        [Header("旋转轴控制")]
        [Tooltip("是否启用X轴旋转")]
        public bool enableXAxis = true;
        
        [Tooltip("是否启用Y轴旋转")]
        public bool enableYAxis = true;
        
        [Tooltip("是否启用Z轴旋转")]
        public bool enableZAxis = true;
        
        private float baseRotationSpeedX;
        private float baseRotationSpeedY;
        private float baseRotationSpeedZ;
        private float timeOffsetX;
        private float timeOffsetY;
        private float timeOffsetZ;
        
        void Start()
        {
            // 保存基础旋转速度
            baseRotationSpeedX = rotationSpeedX;
            baseRotationSpeedY = rotationSpeedY;
            baseRotationSpeedZ = rotationSpeedZ;
            
            // 随机化时间偏移，让多个模型有不同的旋转相位
            if (useRandomOffset)
            {
                timeOffsetX = Random.Range(0f, 2f * Mathf.PI);
                timeOffsetY = Random.Range(0f, 2f * Mathf.PI);
                timeOffsetZ = Random.Range(0f, 2f * Mathf.PI);
            }
            
            Log.Message($"[抽卡UI] 3D模型三向旋转脚本已启动 - X轴: {rotationSpeedX}度/秒, Y轴: {rotationSpeedY}度/秒, Z轴: {rotationSpeedZ}度/秒");
        }
        
        void Update()
        {
            if (!enableRotation) return;
            
            // 计算各轴的当前旋转速度
            float currentSpeedX = GetCurrentRotationSpeed(rotationSpeedX, baseRotationSpeedX, timeOffsetX, sineFrequency);
            float currentSpeedY = GetCurrentRotationSpeed(rotationSpeedY, baseRotationSpeedY, timeOffsetY, sineFrequency);
            float currentSpeedZ = GetCurrentRotationSpeed(rotationSpeedZ, baseRotationSpeedZ, timeOffsetZ, sineFrequency);
            
            // 执行三向旋转
            if (enableXAxis)
            {
                transform.Rotate(Vector3.right, currentSpeedX * Time.deltaTime, Space.World);
            }
            
            if (enableYAxis)
            {
                transform.Rotate(Vector3.up, currentSpeedY * Time.deltaTime, Space.World);
            }
            
            if (enableZAxis)
            {
                transform.Rotate(Vector3.forward, currentSpeedZ * Time.deltaTime, Space.World);
            }
        }
        
        /// <summary>
        /// 获取当前旋转速度（包含正弦波变化）
        /// </summary>
        private float GetCurrentRotationSpeed(float baseSpeed, float originalBaseSpeed, float timeOffset, float frequency)
        {
            float currentSpeed = baseSpeed;
            
            if (useSineWave)
            {
                float sineValue = Mathf.Sin((Time.time + timeOffset) * frequency);
                currentSpeed = originalBaseSpeed + sineValue * sineAmplitude;
            }
            
            return currentSpeed;
        }
        
        /// <summary>
        /// 设置三向旋转速度
        /// </summary>
        /// <param name="speedX">X轴旋转速度</param>
        /// <param name="speedY">Y轴旋转速度</param>
        /// <param name="speedZ">Z轴旋转速度</param>
        public void SetRotationSpeeds(float speedX, float speedY, float speedZ)
        {
            rotationSpeedX = speedX;
            rotationSpeedY = speedY;
            rotationSpeedZ = speedZ;
            
            baseRotationSpeedX = speedX;
            baseRotationSpeedY = speedY;
            baseRotationSpeedZ = speedZ;
        }
        
        /// <summary>
        /// 设置X轴旋转速度
        /// </summary>
        public void SetRotationSpeedX(float speed)
        {
            rotationSpeedX = speed;
            baseRotationSpeedX = speed;
        }
        
        /// <summary>
        /// 设置Y轴旋转速度
        /// </summary>
        public void SetRotationSpeedY(float speed)
        {
            rotationSpeedY = speed;
            baseRotationSpeedY = speed;
        }
        
        /// <summary>
        /// 设置Z轴旋转速度
        /// </summary>
        public void SetRotationSpeedZ(float speed)
        {
            rotationSpeedZ = speed;
            baseRotationSpeedZ = speed;
        }
        
        /// <summary>
        /// 启用或禁用特定轴的旋转
        /// </summary>
        public void SetAxisEnabled(bool xAxis, bool yAxis, bool zAxis)
        {
            enableXAxis = xAxis;
            enableYAxis = yAxis;
            enableZAxis = zAxis;
        }
        
        /// <summary>
        /// 启用或禁用所有旋转
        /// </summary>
        public void SetRotationEnabled(bool enabled)
        {
            enableRotation = enabled;
        }
        
        /// <summary>
        /// 切换所有轴的旋转方向
        /// </summary>
        public void ToggleAllRotationDirections()
        {
            rotationSpeedX = -rotationSpeedX;
            rotationSpeedY = -rotationSpeedY;
            rotationSpeedZ = -rotationSpeedZ;
            
            baseRotationSpeedX = -baseRotationSpeedX;
            baseRotationSpeedY = -baseRotationSpeedY;
            baseRotationSpeedZ = -baseRotationSpeedZ;
        }
        
        /// <summary>
        /// 重置旋转
        /// </summary>
        public void ResetRotation()
        {
            transform.rotation = Quaternion.identity;
        }
        
        /// <summary>
        /// 设置正弦波参数
        /// </summary>
        public void SetSineWaveParams(bool enabled, float amplitude, float frequency)
        {
            useSineWave = enabled;
            sineAmplitude = amplitude;
            sineFrequency = frequency;
        }
        
        /// <summary>
        /// 获取当前旋转状态信息
        /// </summary>
        public string GetRotationInfo()
        {
            return $"X轴: {rotationSpeedX:F1}°/s, Y轴: {rotationSpeedY:F1}°/s, Z轴: {rotationSpeedZ:F1}°/s";
        }
    }
} 