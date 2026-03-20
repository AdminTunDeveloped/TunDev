using UnityEngine;

public class SuperFFHeadLock : MonoBehaviour
{
    [Header("══════ SIÊU AUTO HEAD LOCK - FF PERFECT ══════")]
    [Tooltip("Kéo VariableJoystick (aim/fire) vào đây")]
    public VariableJoystick aimJoystick;

    [Tooltip("Layer chỉ Enemy")]
    public LayerMask enemyLayer;

    public float maxLockDistance = 60f;          // Tầm xa như FF
    public float lockFOV = 160f;                 // Góc lock rộng
    public float pullThreshold = 0.55f;          // Kéo lên nhẹ là lock

    [Tooltip("Tốc độ snap cứng (cao = ghim chắc, không rung)")]
    public float snapPower = 140f;               // Siêu cao cho lock tức thì

    [Tooltip("Offset tự động: % chiều cao model địch (0.85-0.95 = trán/đỉnh đầu)")]
    [Range(0.7f, 1.0f)]
    public float headOffsetPercent = 0.92f;      // Tự tính offset theo scale địch

    [Header("Debug & Visual")]
    public Transform currentLockedHead;
    public bool isHeadLocked;
    public string status = "Ready";

    private Transform playerRoot;
    private Transform gunPivot;                  // Transform súng (xoay)

    void Awake()
    {
        playerRoot = transform.root;

        // Tìm gunPivot tự động (thêm fallback)
        gunPivot = playerRoot.Find("Gun") ??
                   playerRoot.Find("Weapon/Gun") ??
                   playerRoot.Find("MainGun") ??
                   playerRoot.Find("Weapon");     // Nếu tên khác

        if (gunPivot == null)
        {
            Debug.LogError("Không tìm thấy Gun! Kéo object Gun vào field 'gunPivot' trong Inspector.");
        }
    }

    void Update()
    {
        if (aimJoystick == null || gunPivot == null) return;

        float vertical = aimJoystick.Direction.y;

        if (vertical > pullThreshold)
        {
            ExecuteHeadLock();
        }
        else if (vertical < 0.1f)  // Chỉ reset khi thả hẳn
        {
            currentLockedHead = null;
            isHeadLocked = false;
            status = "Thả → Reset";
        }

        // Line cyan nối đến điểm headshot (trán)
        if (currentLockedHead != null)
        {
            Vector3 aimPoint = GetHeadAimPoint(currentLockedHead);
            Debug.DrawLine(gunPivot.position, aimPoint, Color.cyan, 0f, false);
        }
    }

    private void ExecuteHeadLock()
    {
        // Giữ lock cũ nếu còn hợp lệ (không reset giữa chừng)
        if (currentLockedHead != null && IsTargetValid(currentLockedHead))
        {
            SnapToHead(currentLockedHead);
            status = "Ghim chặt đầu cũ!";
            return;
        }

        // Tìm địch mới
        Collider[] enemies = Physics.OverlapSphere(playerRoot.position, maxLockDistance, enemyLayer);

        Transform bestHead = null;
        float minAngle = lockFOV + 1f;

        Vector3 gunForward = gunPivot.forward;

        foreach (var enemy in enemies)
        {
            Transform head = FindHeadBone(enemy.transform);
            if (head == null) continue;

            Vector3 aimPos = GetHeadAimPoint(head);
            float angle = Vector3.Angle(gunForward, (aimPos - gunPivot.position).normalized);

            if (angle < minAngle)
            {
                minAngle = angle;
                bestHead = head;
            }
        }

        if (bestHead != null && minAngle <= lockFOV)
        {
            currentLockedHead = bestHead;
            isHeadLocked = true;
            SnapToHead(bestHead);
            status = $"Lock mới - Góc {minAngle:F1}°";
        }
        else
        {
            currentLockedHead = null;
            isHeadLocked = false;
            status = "Không có địch trong tầm";
        }
    }

    private bool IsTargetValid(Transform head)
    {
        if (head == null) return false;
        float dist = Vector3.Distance(gunPivot.position, head.position);
        if (dist > maxLockDistance + 8f) return false;

        // Không lock qua tường (raycast check)
        Vector3 dir = (head.position - gunPivot.position).normalized;
        return !Physics.Raycast(gunPivot.position, dir, dist, ~enemyLayer);
    }

    private void SnapToHead(Transform head)
    {
        Vector3 aimPoint = GetHeadAimPoint(head);
        Vector3 targetDir = (aimPoint - gunPivot.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(targetDir);

        // Snap mượt + cứng nhất (RotateTowards + instant khi gần)
        gunPivot.rotation = Quaternion.RotateTowards(
            gunPivot.rotation,
            targetRot,
            snapPower * Time.deltaTime
        );

        // Instant lock khi góc < 8° (không trượt tí nào)
        if (Vector3.Angle(gunPivot.forward, targetDir) < 8f)
        {
            gunPivot.rotation = targetRot;
        }
    }

    // Tính điểm aim tự động theo % chiều cao model địch
    private Vector3 GetHeadAimPoint(Transform headBone)
    {
        // Offset tự động dựa trên scale địch (thay vì fixed 0.22f)
        float autoOffset = headBone.lossyScale.y * headOffsetPercent;
        return headBone.position + Vector3.up * autoOffset;
    }

    // Tìm bone Head (linh hoạt cho model FF rip)
    private Transform FindHeadBone(Transform root)
    {
        Transform head = root.Find("Head");
        if (head != null) return head;

        Transform[] children = root.GetComponentsInChildren<Transform>();
        foreach (var child in children)
        {
            string n = child.name.ToLower();
            if (n.Contains("head") || n.Contains("skull") || n.Contains("neck/head"))
                return child;
        }
        return null;
    }

    // ── Bắn ── (gọi từ button giữ fire hoặc input)
    public void Shoot()
    {
        if (gunPivot == null) return;

        Vector3 shootDir = gunPivot.forward;

        if (isHeadLocked && currentLockedHead != null)
        {
            Vector3 aimPos = GetHeadAimPoint(currentLockedHead);
            shootDir = (aimPos - gunPivot.position).normalized;
            Debug.Log("<color=lime>🔥 HEAD LOCK ACTIVE - BẮN VÀO ĐẦU!</color>");
        }

        // Raycast demo (thay bằng đạn thật)
        if (Physics.Raycast(gunPivot.position, shootDir, out RaycastHit hit, 300f))
        {
            if (hit.collider != null && hit.collider.CompareTag("Enemy"))
            {
                Debug.Log("<color=yellow>💀 Trúng địch! Headshot nếu trúng đầu.</color>");
            }
        }

        Debug.DrawRay(gunPivot.position, shootDir * 150f, Color.red, 0.5f);
    }
}
