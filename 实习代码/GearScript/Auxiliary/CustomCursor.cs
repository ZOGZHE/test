using UnityEngine;

public class HandCursorController : MonoBehaviour
{
    #region 数据配置
    [Header("游标资源设置")]
    [Tooltip("平时状态：手指直指的游标")]
    public Texture2D pointingCursor;  // 直指状态的游标图片
    [Tooltip("点击/交互状态：手指下弯曲的游标")]
    public Texture2D clickingCursor;  // 弯曲状态的游标图片

    [Header("热点设置（点击生效点）")]
    [Tooltip("直指状态游标的点击点（通常在指尖位置）")]
    public Vector2 pointingHotSpot = new Vector2(24, 10);  // 示例值，需根据实际图片调整
    [Tooltip("弯曲状态游标的点击点（通常在指尖位置）")]
    public Vector2 clickingHotSpot = new Vector2(20, 15);  // 示例值，需根据实际图片调整

    [Header("交互配置")]
    [Tooltip("鼠标悬停在可交互对象上时是否自动切换为弯曲状态")]
    public bool switchOnHover = true;
    [Tooltip("启用空格键快捷切换自定义/默认鼠标")]
    public bool enableSpaceToggle = true;

    // 当前是否为点击状态
    private bool isClicking = false;
    // 当前是否悬停在可交互对象上
    private bool isHovering = false;
    // 是否使用自定义鼠标（false则使用系统默认鼠标）
    private bool isUsingCustomCursor = true;
    #endregion

    #region 生命周期函数
    void Start()
    {
        // 初始化时设置为直指状态
        SetPointingCursor();
    }

    void Update()
    {
        // 空格键开关鼠标显示
        if (enableSpaceToggle && Input.GetKeyDown(KeyCode.Space))
        {
            ToggleCursorVisibility();
        }

        // 鼠标按下时切换为弯曲状态
        if (Input.GetMouseButtonDown(0) && !isClicking)
        {
            isClicking = true;
            SetClickingCursor();
        }

        // 鼠标松开时恢复为直指状态（或根据悬停状态判断）
        if (Input.GetMouseButtonUp(0) && isClicking)
        {
            isClicking = false;
            if (isHovering && switchOnHover)
                SetClickingCursor();  // 若悬停在可交互对象上，保持弯曲
            else
                SetPointingCursor();  // 否则恢复直指
        }
    }
    #endregion

    #region  鼠标游标
    // 当鼠标进入可交互对象时（需对象有碰撞体）
    private void OnMouseEnter()
    {
        if (switchOnHover && !isClicking)
        {
            isHovering = true;
            SetClickingCursor();  // 悬停时切换为弯曲状态
        }
    }

    // 当鼠标离开可交互对象时
    private void OnMouseExit()
    {
        if (switchOnHover && !isClicking)
        {
            isHovering = false;
            SetPointingCursor();  // 离开时恢复直指状态
        }
    }

    // 设置为直指状态游标
    public void SetPointingCursor()
    {
        // 只有在使用自定义鼠标模式时才切换
        if (!isUsingCustomCursor) return;

        if (pointingCursor != null)
        {
            Cursor.SetCursor(pointingCursor, pointingHotSpot, CursorMode.ForceSoftware);
        }
        else
        {
            Debug.LogWarning("未设置直指状态的游标图片！");
        }
    }

    // 设置为弯曲状态游标
    public void SetClickingCursor()
    {
        // 只有在使用自定义鼠标模式时才切换
        if (!isUsingCustomCursor) return;

        if (clickingCursor != null)
        {
            Cursor.SetCursor(clickingCursor, clickingHotSpot, CursorMode.ForceSoftware);
        }
        else
        {
            Debug.LogWarning("未设置弯曲状态的游标图片！");
        }
    }

    // 恢复系统默认游标（可选）
    public void ResetToDefaultCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
    }

    // 切换自定义鼠标/系统默认鼠标
    public void ToggleCursorVisibility()
    {
        isUsingCustomCursor = !isUsingCustomCursor;

        if (isUsingCustomCursor)
        {
            // 切换到自定义鼠标时，根据当前状态设置正确的游标
            if (isClicking || (isHovering && switchOnHover))
                SetClickingCursor();
            else
                SetPointingCursor();
        }
        else
        {
            // 切换到系统默认鼠标
            ResetToDefaultCursor();
        }
    }

    // 使用自定义鼠标
    public void UseCustomCursor()
    {
        isUsingCustomCursor = true;
        SetPointingCursor();
    }

    // 使用系统默认鼠标
    public void UseSystemCursor()
    {
        isUsingCustomCursor = false;
        ResetToDefaultCursor();
    }
    #endregion

}
