using UnityEngine;
using UnityEngine.UI;
namespace WoolyPath
{
    public class ButtonEffectTest : MonoBehaviour
    {
        // 按钮组件引用
        public Button triggerButton;
        // 特效组件引用
        public ParticleSystem effectParticle;

        void Start()
        {
            // 确保引用已赋值
            if (triggerButton != null && effectParticle != null)
            {
                // 为按钮添加点击事件监听
                triggerButton.onClick.AddListener(PlayEffect);

                // 初始时确保特效是停止状态
                effectParticle.Stop();
            }
            else
            {
                Debug.LogError("请在Inspector面板中赋值按钮和特效组件");
            }
        }

        // 播放特效的方法
        void PlayEffect()
        {
            // 停止当前可能正在播放的特效
            effectParticle.Stop();
            // 清除残留粒子
            effectParticle.Clear();
            // 播放特效
            effectParticle.Play();
            //EffectsManager.Instance.PlayShearingEffect
            Debug.Log("特效已触发播放");
        }
    }
}
