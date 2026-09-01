using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class qt : MonoBehaviour
{
    [Header("──── Q弹效果 ────")]
    [Tooltip("Q弹强度（越大越Q）")]
    [Range(0.1f, 1f)]
    public float bounciness = 0.5f;

    [Tooltip("回弹速度")]
    [Range(3f, 25f)]
    public float bounceSpeed = 10f;

    [Tooltip("呼吸效果强度（0=关闭）")]
    [Range(0f, 0.15f)]
    public float breathAmount = 0.04f;

    [Tooltip("呼吸速度")]
    [Range(0.5f, 3f)]
    public float breathSpeed = 1.5f;

    [Header("──── 角色移动 ────")]
    [Tooltip("移动速度")]
    [Range(1f, 20f)]
    public float moveSpeed = 8f;

    [Tooltip("跳跃力度")]
    [Range(3f, 20f)]
    public float jumpForce = 10f;

    [Tooltip("移动时惯性晃动强度")]
    [Range(0f, 0.3f)]
    public float moveWobble = 0.1f;

    [Header("──── 触发设置 ────")]
    [Tooltip("最小落地速度才触发压扁")]
    [Range(0f, 10f)]
    public float landingThreshold = 2f;

    private Rigidbody rb;
    private Vector3 baseScale;
    private Vector3 squashVelocity = Vector3.zero;
    private Vector3 squashOffset = Vector3.zero;
    private bool wasGrounded;
    private float wobbleTimer;
    private Vector3 lastMoveDirection;

    private float stiffness;
    private float damping;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        baseScale = transform.localScale;

        stiffness = bounceSpeed * bounceSpeed;
        damping = 2f * bounceSpeed * 0.7f;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = new Vector3(h, 0, v).normalized;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Vector3 targetVel = moveDir * moveSpeed;
            targetVel.y = rb.velocity.y;
            rb.velocity = Vector3.Lerp(rb.velocity, targetVel, 15f * Time.deltaTime);

            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);

            lastMoveDirection = moveDir;
            wobbleTimer += Time.deltaTime * 10f;
        }

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        UpdateSpringPhysics();
        ApplyBreathing();
        ApplyScale();
    }

    void FixedUpdate()
    {
        bool isGrounded = IsGrounded();

        if (!wasGrounded && isGrounded)
        {
            float verticalSpeed = Mathf.Abs(rb.velocity.y);

            if (verticalSpeed > landingThreshold)
            {
                float intensity = Mathf.Clamp01(verticalSpeed * 0.08f) * bounciness;
                TriggerSquash(Vector3.up, intensity);
            }
        }

        wasGrounded = isGrounded;

        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        if (moveDir.sqrMagnitude > 0.01f && IsGrounded())
        {
            float wobbleX = Mathf.Sin(wobbleTimer * 1.5f) * moveWobble * bounciness;
            float wobbleZ = Mathf.Cos(wobbleTimer * 1.5f) * moveWobble * bounciness;
            squashVelocity += new Vector3(wobbleX * 0.3f, 0, wobbleZ * 0.3f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.impulse.magnitude;

        if (impactForce > 1f && collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 impactDir = contact.normal;

            float intensity = Mathf.Clamp01(impactForce * 0.03f) * bounciness;
            TriggerSquash(impactDir, intensity);
        }
    }

    void UpdateSpringPhysics()
    {
        Vector3 springForce = -stiffness * squashOffset;
        Vector3 dampingForce = -damping * squashVelocity;

        squashVelocity += (springForce + dampingForce) * Time.deltaTime;
        squashOffset += squashVelocity * Time.deltaTime;

        if (squashOffset.sqrMagnitude < 0.00001f && squashVelocity.sqrMagnitude < 0.00001f)
        {
            squashOffset = Vector3.zero;
            squashVelocity = Vector3.zero;
        }
    }

    void ApplyBreathing()
    {
        if (breathAmount <= 0f) return;

        float breath = Mathf.Sin(Time.time * breathSpeed * Mathf.PI * 2f) * breathAmount;
        float breathInv = -breath * 0.5f;

        squashOffset.y += breath * Time.deltaTime * 3f;
        squashOffset.x += breathInv * Time.deltaTime * 3f;
        squashOffset.z += breathInv * Time.deltaTime * 3f;
    }

    void ApplyScale()
    {
        float scaleX = 1f + squashOffset.x;
        float scaleY = 1f + squashOffset.y;
        float scaleZ = 1f + squashOffset.z;

        float volume = scaleX * scaleY * scaleZ;
        if (volume > 0.01f)
        {
            float correction = 1f / Mathf.Pow(volume, 1f / 3f);
            scaleX *= correction;
            scaleY *= correction;
            scaleZ *= correction;
        }

        transform.localScale = new Vector3(
            baseScale.x * scaleX,
            baseScale.y * scaleY,
            baseScale.z * scaleZ
        );
    }

    public void TriggerSquash(Vector3 worldDirection, float intensity)
    {
        Vector3 localDir = transform.InverseTransformDirection(worldDirection).normalized;

        Vector3 squashImpulse = new Vector3(
            localDir.x != 0 ? -localDir.x * intensity : intensity * 0.5f,
            localDir.y != 0 ? -localDir.y * intensity : intensity * 0.5f,
            localDir.z != 0 ? -localDir.z * intensity : intensity * 0.5f
        );

        squashVelocity += squashImpulse * bounceSpeed * 2f;
    }

    public void TriggerStretch(Vector3 worldDirection, float intensity)
    {
        TriggerSquash(-worldDirection, intensity);
    }

    bool IsGrounded()
    {
        float sphereRadius = GetComponent<Collider>().bounds.extents.y * 0.9f;
        return Physics.SphereCast(transform.position + Vector3.up * 0.1f, sphereRadius, Vector3.down, out RaycastHit hit, 0.3f);
    }

    public void ResetJelly()
    {
        squashOffset = Vector3.zero;
        squashVelocity = Vector3.zero;
        transform.localScale = baseScale;
    }

    void OnDisable()
    {
        ResetJelly();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (TryGetComponent<Collider>(out Collider col))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, col.bounds.extents.y * 0.9f);
        }
    }
#endif
}