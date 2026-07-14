using UnityEngine;

/// <summary>
/// 挂载到角色身上：WASD 前后左右移动 + 空格键跳跃 + E 键拖拽标签为 "mp" 的物体
/// </summary>
public class dd : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 720f;

    [Header("跳跃设置")]
    public float jumpForce = 8f;             // 跳跃力度
    public float groundCheckDistance = 0.2f; // 地面检测距离
    public LayerMask groundLayer;            // 地面层级（在 Inspector 中设置）

    [Header("拖拽设置")]
    public float detectRadius = 3f;          // 检测半径
    public float dragDistance = 2f;          // 拖拽时物体与角色的目标距离
    public float smoothDrag = 10f;           // 拖拽跟随平滑度

    private Rigidbody _rb;
    private GameObject _dragTarget;           // 当前拖拽的物体
    private bool _isDragging;
    private Vector3 _moveInput;               // 缓存输入方向
    private bool _isGrounded;                 // 是否在地面上

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true; // 防止碰撞导致角色旋转
    }

    void Update()
    {
        // 在 Update 中读取输入
        float h = Input.GetAxisRaw("Horizontal"); // A / D
        float v = Input.GetAxisRaw("Vertical");   // W / S
        _moveInput = new Vector3(h, 0f, v).normalized;

        // 地面检测
        CheckGrounded();

        // 跳跃输入（在 Update 中检测按键）
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            Jump();
        }

        // E 键：拖拽 / 释放物体
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_isDragging)
            {
                ReleaseDrag();
            }
            else
            {
                TryPickUp();
            }
        }
    }

    void FixedUpdate()
    {
        // 物理移动必须放在 FixedUpdate
        HandleMovement();

        if (_isDragging && _dragTarget != null)
        {
            SmoothDragToTarget();
        }
    }

    // ───────── 地面检测 ─────────
    private void CheckGrounded()
    {
        // 从角色脚底向下发射射线检测地面
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.05f, groundLayer);
    }

    // ───────── 跳跃逻辑 ─────────
    private void Jump()
    {
        // 重置竖直方向速度后施加跳跃力
        Vector3 velocity = _rb.velocity;
        velocity.y = jumpForce;
        _rb.velocity = velocity;
        Debug.Log("[跳跃] 跳跃！");
    }

    // ───────── 移动逻辑（基于摄像机方向） ─────────
    private void HandleMovement()
    {
        if (_moveInput.magnitude > 0.01f)
        {
            // 获取摄像机的前方和右方方向（忽略 Y 轴）
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 camForward = cam.transform.forward;
                Vector3 camRight = cam.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                // 基于摄像机方向计算世界移动方向
                Vector3 worldDir = (camForward * _moveInput.z + camRight * _moveInput.x).normalized;

                // 移动角色
                _rb.MovePosition(transform.position + worldDir * moveSpeed * Time.fixedDeltaTime);

                // 让角色朝向移动方向
                Quaternion targetRotation = Quaternion.LookRotation(worldDir);
                _rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime));
            }
            else
            {
                // 如果没有主摄像机，回退到自身坐标系
                Vector3 worldDir = transform.TransformDirection(_moveInput);
                _rb.MovePosition(transform.position + worldDir * moveSpeed * Time.fixedDeltaTime);
            }
        }
    }

    // ───────── 检测并拾取最近的 "mp" 物体 ─────────
    private void TryPickUp()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);

        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider c in hits)
        {
            if (c.CompareTag("mp"))
            {
                float d = Vector3.Distance(transform.position, c.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = c.gameObject;
                }
            }
        }

        if (closest != null)
        {
            _dragTarget = closest;
            _isDragging = true;

            Rigidbody targetRb = closest.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.isKinematic = true;
            }

            Debug.Log("[拖拽] 拾取了物体: " + closest.name);
        }
        else
        {
            Debug.Log("[拖拽] 附近未找到标签为 mp 的物体");
        }
    }

    // ───────── 释放拖拽物体 ─────────
    private void ReleaseDrag()
    {
        if (_dragTarget != null)
        {
            Rigidbody targetRb = _dragTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                targetRb.isKinematic = false;
            }

            Debug.Log("[拖拽] 释放了物体: " + _dragTarget.name);
            _dragTarget = null;
        }
        _isDragging = false;
    }

    // ───────── 平滑拖拽跟随 ─────────
    private void SmoothDragToTarget()
    {
        Vector3 targetPos = transform.position + transform.forward * dragDistance;

        _dragTarget.transform.position = Vector3.Lerp(
            _dragTarget.transform.position,
            targetPos,
            Time.fixedDeltaTime * smoothDrag
        );
    }

    // ───────── 编辑器可视化检测范围 ─────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _isDragging ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        // 绘制地面检测线
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (groundCheckDistance + 0.05f));

        if (_isDragging && _dragTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _dragTarget.transform.position);
        }
    }
}
