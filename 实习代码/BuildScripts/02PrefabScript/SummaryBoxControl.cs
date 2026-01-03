using ConnectMaster;
using UnityEngine;

public class SummaryBoxControl : MonoBehaviour
{
    [HideInInspector]
    public int targetRowIndex;
    public string EffectString;

    [Header("特效设置")]
    public Vector2 effectOffset = Vector2.zero; // 特效偏移


    private void Start()
    {
        PlayEffect();
    }

    public void PlayEffect()
    {
        if (EffectManager.Instance == null || string.IsNullOrEmpty(EffectString))
        {
            return;
        }

        RectTransform uiRect = GetComponent<RectTransform>();
        EffectManager.Instance.CreateUIEffectForCameraSimple(EffectString, uiRect, effectOffset);

    }

    public void SetTargetRow(int rowIndex)
    {
        targetRowIndex = rowIndex;
    }
}