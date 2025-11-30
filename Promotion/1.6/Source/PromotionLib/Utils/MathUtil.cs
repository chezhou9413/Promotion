public static class MathUtil
{
    /// <summary>
    /// 限制整数值在 min 和 max 之间
    /// </summary>
    public static int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    /// <summary>
    /// 限制浮点值在 min 和 max 之间
    /// </summary>
    public static float Clamp(float value, float min, float max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
