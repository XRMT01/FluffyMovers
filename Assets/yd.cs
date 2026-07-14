using UnityEngine;

/// <summary>
/// 操控说明：
///   鼠标移动 - 旋转视角（水平/垂直）
///   W/S      - 前进/后退
///   A/D      - 左移/右移
///   Space    - 上升（可选飞行模式）
///   Left Ctrl- 下降（可选飞行模式）
///   鼠标右键  - 锁定/解锁光标
/// </summary>
public class yd : MonoBehaviour
{
    [Header("=== 移动设置 ===")]
    [Tooltip("移动速度（单位/秒）")]
    public float moveSpeed = 8f;

    [Tooltip("冲刺速度倍率")]
    public float sprintMultiplier = 2f;

    [Tooltip("按住 Shift 冲刺")]
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("=== 视角设置 ===")]
    [Tooltip("水平旋转灵敏度")]
    public float mouseSensitivityX = 2f;

    [Tooltip("垂直旋转灵敏度")]
    public float mouseSensitivityY = 2f;

    [Tooltip("垂直视角最大上仰角度")]
    public float maxPitchAngle = 80f;

    [Tooltip("垂直视角最大下俯角度")]
    public float minPitchAngle = -80f;

    [Header("=== 飞行模式（可选） ===")]
    [Tooltip("是否启用上下飞行")]
    public bool enableFlyMode = true;

    [Tooltip("上升按键")]
    public KeyCode flyUpKey = KeyCode.Space;

    [Tooltip("下降按键")]
    public KeyCode flyDownKey = KeyCode.LeftControl;

    [Header("=== 其他设置 ===")]
    [Tooltip("右键锁定光标（设为 false 则始终锁定）")]
    public bool rightClickToLock = true;

    [Tooltip("重力影响（关闭则无重力，适合飞行模式）")]
    public bool useGravity = false;

    [Tooltip("重力加速度")]
    public float gravity = -9.81f;

    // 内部状态
    private float yaw;      // 水平旋转角（绕 Y 轴）
    private float pitch;    // 垂直旋转角（绕 X 轴）
    private float verticalVelocity; // 重力/飞行 垂直速度
    private CharacterController characterController;

    void Awake()
    {
        // 尝试获取或自动添加 CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            Debug.Log("[CameraController] 已自动添加 CharacterController 组件。");
        }

        // 初始化鼠标锁定
        if (!rightClickToLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Start()
    {
        // 用当前摄像头朝向初始化旋转角
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;

        // 确保主角子物体位置归零（相对于摄像头）
        ResetChildPosition();
    }

    void Update()
    {
        HandleCursorLock();
        HandleMouseLook();
        HandleMovement();
    }

    /// <summary>
    /// 处理鼠标光标锁定/解锁
    /// </summary>
    private void HandleCursorLock()
    {
        if (rightClickToLock)
        {
            if (Input.GetMouseButtonDown(1)) // 右键
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }

    /// <summary>
    /// 处理鼠标视角旋转
    /// </summary>
    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;

        // 水平旋转（绕世界 Y 轴）
        yaw += mouseX;

        // 垂直旋转（俯仰），限制角度范围
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitchAngle, maxPitchAngle);

        // 应用旋转
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// 处理 WASD 移动 + 飞行 + 重力
    /// </summary>
    private void HandleMovement()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        // 读取输入
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S
        bool isSprinting = Input.GetKey(sprintKey);

        // 基于摄像头水平方向计算移动向量
        Vector3 forward = Vector3.Scale(transform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 right = Vector3.Scale(transform.right, new Vector3(1, 0, 1)).normalized;

        Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;

        // 计算速度
        float speed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        // 垂直方向处理
        if (enableFlyMode)
        {
            // 飞行模式：按键直接控制上下
            verticalVelocity = 0f;
            if (Input.GetKey(flyUpKey))
                verticalVelocity = speed;
            if (Input.GetKey(flyDownKey))
                verticalVelocity = -speed;
        }
        else if (useGravity && characterController.isGrounded)
        {
            verticalVelocity = -1f; // 小的负值保持贴地
        }
        else if (useGravity)
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 合成最终移动向量
        Vector3 velocity = moveDirection * speed + Vector3.up * verticalVelocity;

        // 使用 CharacterController 移动
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// 重置所有子物体（主角）的本地位置到原点
    /// </summary>
    private void ResetChildPosition()
    {
        foreach (Transform child in transform)
        {
            child.localPosition = Vector3.zero;
            Debug.Log($"[CameraController] 已重置子物体 \"{child.name}\" 的位置。");
        }
    }

    // ========== 公开方法（供外部脚本调用） ==========

    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
    }

    /// <summary>
    /// 设置鼠标灵敏度
    /// </summary>
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivityX = sensitivity;
        mouseSensitivityY = sensitivity;
    }

    /// <summary>
    /// 强制锁定/解锁光标
    /// </summary>
    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>
    /// 获取当前是否在冲刺
    /// </summary>
    public bool IsSprinting => Input.GetKey(sprintKey);
}