using UnityEngine;

/// <summary>
/// 挂载到角色身上：WASD 移动 + 空格键检测并拖拽标签为 "mp" 的物体
/// </summary>
public class CharacterMoveAndDrag : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 720f;

    [Header("拖拽设置")]
    public float detectRadius = 3f;          // 检测半径
    public float dragDistance = 2f;          // 拖拽时物体与角色的目标距离
    public float smoothDrag = 10f;           // 拖拽跟随平滑度

    private Rigidbody _rb;
    private GameObject _dragTarget;           // 当前拖拽的物体
    private bool _isDragging;
    private Vector3 _moveInput;               // 缓存输入方向

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 在 Update 中读取输入
        float h = Input.GetAxisRaw("Horizontal"); // A / D
        float v = Input.GetAxisRaw("Vertical");   // W / S
        _moveInput = new Vector3(h, 0f, v).normalized;

        // 空格键检测（输入类逻辑可以留在 Update）
        HandleDragInput();
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

    // ───────── 移动逻辑 ─────────
    private void HandleMovement()
    {
        if (_moveInput.magnitude > 0.01f)
        {
            Vector3 worldDir = transform.TransformDirection(_moveInput);
            _rb.MovePosition(transform.position + worldDir * moveSpeed * Time.fixedDeltaTime);
        }
    }

    // ───────── 空格键：检测 / 释放 ─────────
    private void HandleDragInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
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

            Debug.Log($"[拖拽] 拾取了物体: {closest.name}");
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

            Debug.Log($"[拖拽] 释放了物体: {_dragTarget.name}");
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

        if (_isDragging && _dragTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _dragTarget.transform.position);
        }
    }
}
