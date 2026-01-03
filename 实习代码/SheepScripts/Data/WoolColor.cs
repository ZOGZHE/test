using UnityEngine;

namespace WoolyPath
{
    /// <summary>
    /// 羊毛颜色枚举，定义了游戏中所有可用的羊毛颜色类型
    /// </summary>
    public enum WoolColor
    {
        Pink,     // 粉色 
        Yellow,   // 黄色
        Blue,     // 蓝色
        Purple,   // 紫色
        Green,    // 绿色
        Orange,   // 橙色
        Black     // 特殊颜色（可能作为通用颜色或障碍物标识）
    }

    /// <summary>
    /// WoolColor枚举的扩展方法类，提供颜色转换、名称显示等功能
    /// </summary>
    public static class WoolColorExtensions
    {
        /// <summary>
        /// 将WoolColor枚举转换为Unity可用的Color类型（RGB颜色值）
        /// </summary>
        /// <param name="woolColor">当前羊毛颜色枚举值</param>
        /// <returns>对应的Unity Color对象</returns>
        public static Color ToUnityColor(this WoolColor woolColor)
        {
            switch (woolColor)
            {
                case WoolColor.Green: return new Color(0.4f, 0.8f, 0.4f);  // 绿色（偏浅）
                case WoolColor.Yellow: return new Color(1f, 0.9f, 0.3f);   // 黄色（偏暖）
                case WoolColor.Pink: return new Color(1f, 0.6f, 0.8f);   // 粉色（柔和）
                case WoolColor.Orange: return new Color(1f, 0.6f, 0.2f);   // 橙色（明亮）
                case WoolColor.Blue: return new Color(0.3f, 0.7f, 1f);   // 蓝色（清澈）
                case WoolColor.Purple: return new Color(0.7f, 0.4f, 0.9f); // 紫色（柔和）
                case WoolColor.Black: return new Color(0.2f, 0.2f, 0.2f); // 黑色（偏灰，避免纯黑）
                default: return UnityEngine.Color.white;    // 默认白色（未定义颜色时）
            }
        }

        /// <summary>
        /// 将WoolColor枚举转换为中文显示名称（用于UI展示）
        /// </summary>
        /// <param name="woolColor">当前羊毛颜色枚举值</param>
        /// <returns>对应的中文名称字符串</returns>
        public static string ToDisplayName(this WoolColor woolColor)
        {
            switch (woolColor)
            {
                case WoolColor.Green: return "绿色";
                case WoolColor.Yellow: return "黄色";
                case WoolColor.Pink: return "粉色";
                case WoolColor.Orange: return "橙色";
                case WoolColor.Blue: return "蓝色";
                case WoolColor.Purple: return "紫色";
                case WoolColor.Black: return "黑色";
                default: return "未知";  // 未定义颜色时显示"未知"
            }
        }

        /// <summary>
        /// 获取一个随机的标准羊毛颜色（排除黑色，因为黑色是特殊颜色）
        /// </summary>
        /// <returns>随机的标准WoolColor枚举值</returns>
        public static WoolColor GetRandomColor()
        {
            // 定义标准颜色数组（不含黑色）
            WoolColor[] colors = {
                WoolColor.Green,
                WoolColor.Yellow,
                WoolColor.Pink,
                WoolColor.Orange,
                WoolColor.Blue,
                WoolColor.Purple
            };

            // 从数组中随机选择一个颜色返回
            return colors[Random.Range(0, colors.Length)];
        }

        /// <summary>
        /// 获取所有标准羊毛颜色的数组（排除黑色）
        /// </summary>
        /// <returns>包含所有标准颜色的WoolColor数组</returns>
        public static WoolColor[] GetStandardColors()
        {
            return new WoolColor[]
            {
                WoolColor.Green,
                WoolColor.Yellow,
                WoolColor.Pink,
                WoolColor.Orange,
                WoolColor.Blue,
                WoolColor.Purple
            };
        }
    }
}