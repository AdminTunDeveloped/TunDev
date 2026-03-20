using UnityEngine;
using System.Collections.Generic;

public class AutoAimWhenPullUp : MonoBehaviour
{
    [Header("───── Cài đặt Auto Aim (Ghìm Chặt Đầu 100%) ─────")]
    [Tooltip("Joystick aim/fire (VariableJoystick từ asset)")]
    public VariableJoystick aimJoystick;

    [Tooltip("Layer Enemy")]
    public LayerMask enemyLayer;

    [Tooltip("Tầm tìm địch tối đa")]
    public float maxAimDistance = 40f;

    [Tooltip("Góc tối đa để lock (độ) - tăng để lock dễ hơn")]
    [Range(40f, 120f)]
    public float maxAimAngle = 90f;  // Tăng lên để lock rộng hơn

    [Tooltip("Tốc độ snap/lerp về đầu (cao = snap nhanh, gần instant)")]
    public float aimLerpSpeed = 25f;  // 25-40 để snap rất nhanh

    [Tooltip("Ngưỡng kéo lên bật lock đầu")]
    [Range(0.5f, 0.95f)]
    public float pullUpThreshold = 0.7f;  // Kéo mạnh lên mới lock

    [Tooltip("Offset Y lên đầu (trán/đỉnh đầu, mét) - giúp headshot dễ hơn")]
    public float headOffsetY = 0.18f;

    [Header("───── Debug ─────")]
    public Transform currentTargetHead;
    public bool isAutoAiming;

    private Transform playerTransform;
    private Transform gunTransform;
    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        playerTransform = transform.root;

        gunTransform = playerTransform.Find("Gun") ?? playerTransform.Find("Weapon/Gun") ?? playerTransform.Find("MainGun");
        if (gunTransform == null)
        {
            Debug.LogError("Không tìm thấy Gun! Gán tay trong Inspector.");
        }
    }

    void Update()
    {
        if (aimJoystick == null || gunTransform == null) return;

        Vector2 input = aimJoystick.Direction;

        // Chỉ lock khi kéo lên khá mạnh
        bool shouldLockHead = input.y > pullUpThreshold;

        if (shouldLockHead)
        {
            TryPerfectHeadLock();
        }
        else
        {
            // Reset khi thả hoặc kéo xuống
            isAutoAiming = false;
            currentTargetHead = null;
        }

        if (currentTargetHead != null)
        {
            Debug.DrawLine(gunTransform.position, currentTargetHead.position + Vector3.up * headOffsetY, Color.magenta, 0f, false);
        }
    }

    private void TryPerfectHeadLock()
    {
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, maxAimDistance, enemyLayer);

        Transform bestHead = null;
        float bestAngle = maxAimAngle + 1f;

        Vector3 gunForward = gunTransform.forward;

        foreach (var hit in hits)
        {
            // Tìm bone Head (chuẩn FF-like)
            Transform headBone = hit.transform.Find("Head");

            // Fallback linh hoạt
            if (headBone == null)
            {
                foreach (Transform child in hit.transform.GetComponentsInChildren<Transform>())
                {
                    string n = child.name.ToLower();
                    if (n.Contains("head") || n.Contains("neck/head") || n.Contains("skull"))
                    {
                        headBone = child;
                        break;
                    }
                }
            }

            if (headBone == null) continue;

            // Vị trí aim target = đầu + offset
            Vector3 targetPos = headBone.position + Vector3.up * headOffsetY;

            Vector3 toTarget = (targetPos - gunTransform.position).normalized;
            float angle = Vector3.Angle(gunForward, toTarget);

            // Ưu tiên góc nhỏ nhất (gần tâm ngắm nhất)
            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestHead = headBone;
            }
        }

        if (bestHead != null && bestAngle <= maxAimAngle)
        {
            currentTargetHead = bestHead;
            isAutoAiming = true;

            // Vị trí aim chính xác
            Vector3 aimPosition = bestHead.position + Vector3.up * headOffsetY;
            Vector3 direction = (aimPosition - gunTransform.position).normalized;

            // Snap/lerp nhanh về hướng đầu
            Quaternion targetRot = Quaternion.LookRotation(direction);
            gunTransform.rotation = Quaternion.Slerp(
                gunTransform.rotation,
                targetRot,
                aimLerpSpeed * Time.deltaTime
            );

            // Optional: Để snap gần instant hơn, có thể dùng Lerp với factor cao hoặc trực tiếp set nếu muốn quá "cheat"
            // gunTransform.rotation = targetRot; // Uncomment nếu muốn 100% instant snap (không mượt)
        }
        else
        {
            currentTargetHead = null;
            isAutoAiming = false;
        }
    }

    // Gọi khi bắn (giữ fire button hoặc auto-fire)
    public void Shoot()
    {
        if (gunTransform == null) return;

        Vector3 shootDir;

        if (isAutoAiming && currentTargetHead != null)
        {
            // Bắn CHẮC CHẮN vào đầu + offset → headshot 100% nếu không có obstacle
            Vector3 aimPos = currentTargetHead.position + Vector3.up * headOffsetY;
            shootDir = (aimPos - gunTransform.position).normalized;
        }
        else
        {
            shootDir = gunTransform.forward;
        }

        Debug.Log("Bắn hướng: " + shootDir);

        // Raycast demo (thay bằng đạn thật)
        if (Physics.Raycast(gunTransform.position, shootDir, out RaycastHit hit, 200f))
        {
            if (hit.collider != null && hit.collider.CompareTag("Enemy"))
            {
                Debug.Log($"Headshot! Trúng {hit.collider.name} tại {hit.point}");
                // Gọi damage headshot: hit.collider.GetComponent<EnemyHealth>()?.TakeDamage(999f, true);
            }
        }

        Debug.DrawRay(gunTransform.position, shootDir * 100f, Color.cyan, 0.5f);
    }
}
