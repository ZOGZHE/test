#region
//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;

//namespace WoolyPath
//{
//    /// <summary>
//    /// Extension methods for Unity components and common objects
//    /// </summary>
//    public static class Extensions
//    {
//        #region Transform Extensions

//        /// <summary>
//        /// Set position with optional x, y, z parameters
//        /// </summary>
//        public static void SetPosition(this Transform transform, float? x = null, float? y = null, float? z = null)
//        {
//            Vector3 pos = transform.position;
//            transform.position = new Vector3(x ?? pos.x, y ?? pos.y, z ?? pos.z);
//        }

//        /// <summary>
//        /// Set local position with optional x, y, z parameters
//        /// </summary>
//        public static void SetLocalPosition(this Transform transform, float? x = null, float? y = null, float? z = null)
//        {
//            Vector3 pos = transform.localPosition;
//            transform.localPosition = new Vector3(x ?? pos.x, y ?? pos.y, z ?? pos.z);
//        }

//        /// <summary>
//        /// Reset transform to origin
//        /// </summary>
//        public static void ResetTransform(this Transform transform)
//        {
//            transform.position = Vector3.zero;
//            transform.localRotation = Quaternion.identity;
//            transform.localScale = Vector3.one;
//        }

//        /// <summary>
//        /// Destroy all children of this transform
//        /// </summary>
//        public static void DestroyChildren(this Transform transform)
//        {
//            for (int i = transform.childCount - 1; i >= 0; i--)
//            {
//                if (Application.isPlaying)
//                {
//                    Object.Destroy(transform.GetChild(i).gameObject);
//                }
//                else
//                {
//                    Object.DestroyImmediate(transform.GetChild(i).gameObject);
//                }
//            }
//        }

//        #endregion

//        #region Vector3 Extensions

//        /// <summary>
//        /// Get Vector3 with only x and z components (y set to 0)
//        /// </summary>
//        public static Vector3 FlattenY(this Vector3 vector)
//        {
//            return new Vector3(vector.x, 0f, vector.z);
//        }

//        /// <summary>
//        /// Check if vector is approximately equal to another
//        /// </summary>
//        public static bool Approximately(this Vector3 vector, Vector3 other, float threshold = 0.01f)
//        {
//            return Vector3.Distance(vector, other) < threshold;
//        }

//        #endregion

//        #region Color Extensions

//        /// <summary>
//        /// Set alpha value of color
//        /// </summary>
//        public static UnityEngine.Color WithAlpha(this UnityEngine.Color color, float alpha)
//        {
//            return new UnityEngine.Color(color.r, color.g, color.b, alpha);
//        }

//        /// <summary>
//        /// Blend two colors
//        /// </summary>
//        public static UnityEngine.Color Blend(this UnityEngine.Color color, UnityEngine.Color other, float t)
//        {
//            return UnityEngine.Color.Lerp(color, other, t);
//        }

//        #endregion

//        #region MonoBehaviour Extensions

//        /// <summary>
//        /// Invoke action after delay
//        /// </summary>
//        public static Coroutine DelayedCall(this MonoBehaviour mono, System.Action action, float delay)
//        {
//            return mono.StartCoroutine(DelayedCallCoroutine(action, delay));
//        }

//        private static IEnumerator DelayedCallCoroutine(System.Action action, float delay)
//        {
//            yield return new WaitForSeconds(delay);
//            action?.Invoke();
//        }

//        /// <summary>
//        /// Invoke action on next frame
//        /// </summary>
//        public static Coroutine NextFrame(this MonoBehaviour mono, System.Action action)
//        {
//            return mono.StartCoroutine(NextFrameCoroutine(action));
//        }

//        private static IEnumerator NextFrameCoroutine(System.Action action)
//        {
//            yield return null;
//            action?.Invoke();
//        }

//        #endregion

//        #region List Extensions

//        /// <summary>
//        /// Get random element from list
//        /// </summary>
//        public static T GetRandom<T>(this List<T> list)
//        {
//            if (list == null || list.Count == 0) return default(T);
//            return list[Random.Range(0, list.Count)];
//        }

//        /// <summary>
//        /// Shuffle list in place
//        /// </summary>
//        public static void Shuffle<T>(this List<T> list)
//        {
//            for (int i = 0; i < list.Count; i++)
//            {
//                int randomIndex = Random.Range(i, list.Count);
//                T temp = list[i];
//                list[i] = list[randomIndex];
//                list[randomIndex] = temp;
//            }
//        }

//        /// <summary>
//        /// Remove and return random element
//        /// </summary>
//        public static T PopRandom<T>(this List<T> list)
//        {
//            if (list == null || list.Count == 0) return default(T);

//            int randomIndex = Random.Range(0, list.Count);
//            T item = list[randomIndex];
//            list.RemoveAt(randomIndex);
//            return item;
//        }

//        #endregion

//        #region Array Extensions

//        /// <summary>
//        /// Get random element from array
//        /// </summary>
//        public static T GetRandom<T>(this T[] array)
//        {
//            if (array == null || array.Length == 0) return default(T);
//            return array[Random.Range(0, array.Length)];
//        }

//        #endregion

//        #region GameObject Extensions

//        /// <summary>
//        /// Set active state if different from current
//        /// </summary>
//        public static void SetActiveOptimized(this GameObject gameObject, bool active)
//        {
//            if (gameObject.activeSelf != active)
//            {
//                gameObject.SetActive(active);
//            }
//        }

//        /// <summary>
//        /// Get or add component
//        /// </summary>
//        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
//        {
//            T component = gameObject.GetComponent<T>();
//            if (component == null)
//            {
//                component = gameObject.AddComponent<T>();
//            }
//            return component;
//        }

//        /// <summary>
//        /// Check if GameObject has component
//        /// </summary>
//        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
//        {
//            return gameObject.GetComponent<T>() != null;
//        }

//        #endregion

//        #region String Extensions

//        /// <summary>
//        /// Check if string is null or empty
//        /// </summary>
//        public static bool IsNullOrEmpty(this string str)
//        {
//            return string.IsNullOrEmpty(str);
//        }

//        /// <summary>
//        /// Capitalize first letter
//        /// </summary>
//        public static string Capitalize(this string str)
//        {
//            if (str.IsNullOrEmpty()) return str;
//            if (str.Length == 1) return str.ToUpper();
//            return char.ToUpper(str[0]) + str.Substring(1);
//        }

//        #endregion

//        #region Math Extensions

//        /// <summary>
//        /// Remap value from one range to another
//        /// </summary>
//        public static float Remap(this float value, float from1, float to1, float from2, float to2)
//        {
//            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
//        }

//        /// <summary>
//        /// Check if value is between min and max (inclusive)
//        /// </summary>
//        public static bool IsBetween(this float value, float min, float max)
//        {
//            return value >= min && value <= max;
//        }

//        /// <summary>
//        /// Wrap angle to -180 to 180 range
//        /// </summary>
//        public static float WrapAngle(this float angle)
//        {
//            angle %= 360f;
//            if (angle > 180f) angle -= 360f;
//            if (angle < -180f) angle += 360f;
//            return angle;
//        }

//        #endregion

//        #region Animation Extensions

//        /// <summary>
//        /// Animate float value over time
//        /// </summary>
//        public static IEnumerator AnimateFloat(float from, float to, float duration, System.Action<float> onUpdate, System.Action onComplete = null)
//        {
//            float elapsed = 0f;

//            while (elapsed < duration)
//            {
//                elapsed += Time.deltaTime;
//                float t = elapsed / duration;
//                float value = Mathf.Lerp(from, to, t);
//                onUpdate?.Invoke(value);
//                yield return null;
//            }

//            onUpdate?.Invoke(to);
//            onComplete?.Invoke();
//        }

//        /// <summary>
//        /// Animate Vector3 value over time
//        /// </summary>
//        public static IEnumerator AnimateVector3(Vector3 from, Vector3 to, float duration, System.Action<Vector3> onUpdate, System.Action onComplete = null)
//        {
//            float elapsed = 0f;

//            while (elapsed < duration)
//            {
//                elapsed += Time.deltaTime;
//                float t = elapsed / duration;
//                Vector3 value = Vector3.Lerp(from, to, t);
//                onUpdate?.Invoke(value);
//                yield return null;
//            }

//            onUpdate?.Invoke(to);
//            onComplete?.Invoke();
//        }

//        /// <summary>
//        /// Animate with custom curve
//        /// </summary>
//        public static IEnumerator AnimateWithCurve(float duration, AnimationCurve curve, System.Action<float> onUpdate, System.Action onComplete = null)
//        {
//            float elapsed = 0f;

//            while (elapsed < duration)
//            {
//                elapsed += Time.deltaTime;
//                float t = elapsed / duration;
//                float value = curve.Evaluate(t);
//                onUpdate?.Invoke(value);
//                yield return null;
//            }

//            onUpdate?.Invoke(curve.Evaluate(1f));
//            onComplete?.Invoke();
//        }

//        #endregion
//    }
//}
#endregion
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WoolyPath
{
    /// <summary>Unity组件和常用对象的扩展方法</summary>
    public static class Extensions
    {
        //改变位置
        #region Transform扩展

        /// <summary>设置位置，可选择性指定x、y、z参数</summary>
        public static void SetPosition(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            Vector3 pos = transform.position;
            transform.position = new Vector3(x ?? pos.x, y ?? pos.y, z ?? pos.z);
        }

        /// <summary>设置本地位置，可选择性指定x、y、z参数</summary>
        public static void SetLocalPosition(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            Vector3 pos = transform.localPosition;
            transform.localPosition = new Vector3(x ?? pos.x, y ?? pos.y, z ?? pos.z);
        }

        /// <summary>重置变换到初始状态（位置零、旋转identity、缩放1）</summary>
        public static void ResetTransform(this Transform transform)
        {
            transform.position = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>销毁该变换的所有子物体（自动适配运行时/编辑器）</summary>
        public static void DestroyChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(transform.GetChild(i).gameObject);
                }
                else
                {
                    Object.DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }
        }

        #endregion

        #region Vector3扩展

        /// <summary>获取仅保留x和z分量的Vector3（y设为0）</summary>
        public static Vector3 FlattenY(this Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }

        /// <summary>检查向量是否与另一个向量近似相等（可自定义阈值）</summary>
        public static bool Approximately(this Vector3 vector, Vector3 other, float threshold = 0.01f)
        {
            return Vector3.Distance(vector, other) < threshold;
        }

        #endregion

        #region Color扩展

        /// <summary>设置颜色的透明度</summary>
        public static UnityEngine.Color WithAlpha(this UnityEngine.Color color, float alpha)
        {
            return new UnityEngine.Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>混合两种颜色（线性插值）</summary>
        public static UnityEngine.Color Blend(this UnityEngine.Color color, UnityEngine.Color other, float t)
        {
            return UnityEngine.Color.Lerp(color, other, t);
        }

        #endregion

        #region MonoBehaviour扩展

        /// <summary>延迟指定时间后执行方法</summary>
        public static Coroutine DelayedCall(this MonoBehaviour mono, System.Action action, float delay)
        {
            return mono.StartCoroutine(DelayedCallCoroutine(action, delay));
        }

        private static IEnumerator DelayedCallCoroutine(System.Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        /// <summary>在下一帧执行方法</summary>
        public static Coroutine NextFrame(this MonoBehaviour mono, System.Action action)
        {
            return mono.StartCoroutine(NextFrameCoroutine(action));
        }

        private static IEnumerator NextFrameCoroutine(System.Action action)
        {
            yield return null;
            action?.Invoke();
        }

        #endregion
        //变换羊群颜色 
        #region List扩展
        

        /// <summary>从列表中随机获取一个元素</summary>
        public static T GetRandom<T>(this List<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>原地打乱列表元素顺序</summary>
        public static void Shuffle<T>(this List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        /// <summary>随机移除并返回列表中的一个元素</summary>
        public static T PopRandom<T>(this List<T> list)
        {
            if (list == null || list.Count == 0) return default(T);

            int randomIndex = Random.Range(0, list.Count);
            T item = list[randomIndex];
            list.RemoveAt(randomIndex);
            return item;
        }

        #endregion

        #region Array扩展

        /// <summary>从数组中随机获取一个元素</summary>
        public static T GetRandom<T>(this T[] array)
        {
            if (array == null || array.Length == 0) return default(T);
            return array[Random.Range(0, array.Length)];
        }

        #endregion

        #region GameObject扩展

        /// <summary>仅在状态不同时设置激活状态（优化性能）</summary>
        public static void SetActiveOptimized(this GameObject gameObject, bool active)
        {
            if (gameObject.activeSelf != active)
            {
                gameObject.SetActive(active);
            }
        }

        /// <summary>获取组件，若不存在则自动添加</summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>检查游戏对象是否有指定组件</summary>
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() != null;
        }

        #endregion

        #region String扩展

        /// <summary>检查字符串是否为null或空</summary>
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>将字符串首字母大写</summary>
        public static string Capitalize(this string str)
        {
            if (str.IsNullOrEmpty()) return str;
            if (str.Length == 1) return str.ToUpper();
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        #endregion

        #region 数学扩展

        /// <summary>将值从一个范围映射到另一个范围</summary>
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        /// <summary>检查值是否在min和max之间（包含边界）</summary>
        public static bool IsBetween(this float value, float min, float max)
        {
            return value >= min && value <= max;
        }

        /// <summary>将角度归一化到-180到180范围</summary>
        public static float WrapAngle(this float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        #endregion

        #region 动画扩展

        /// <summary>随时间平滑过渡float值</summary>
        public static IEnumerator AnimateFloat(float from, float to, float duration, System.Action<float> onUpdate, System.Action onComplete = null)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float value = Mathf.Lerp(from, to, t);
                onUpdate?.Invoke(value);
                yield return null;
            }

            onUpdate?.Invoke(to);
            onComplete?.Invoke();
        }

        /// <summary>随时间平滑过渡Vector3值</summary>
        public static IEnumerator AnimateVector3(Vector3 from, Vector3 to, float duration, System.Action<Vector3> onUpdate, System.Action onComplete = null)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Vector3 value = Vector3.Lerp(from, to, t);
                onUpdate?.Invoke(value);
                yield return null;
            }

            onUpdate?.Invoke(to);
            onComplete?.Invoke();
        }

        /// <summary>使用自定义动画曲线进行过渡动画</summary>
        public static IEnumerator AnimateWithCurve(float duration, AnimationCurve curve, System.Action<float> onUpdate, System.Action onComplete = null)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float value = curve.Evaluate(t);
                onUpdate?.Invoke(value);
                yield return null;
            }

            onUpdate?.Invoke(curve.Evaluate(1f));
            onComplete?.Invoke();
        }

        #endregion
    }
}