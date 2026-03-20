using UnityEngine;

public class FFStyleHeadLockPerfect : MonoBehaviour
{
    [Header("════ FF STYLE HEAD LOCK - CLEAR & BALANCED ════")]
    [Tooltip("Joystick aim/fire - kéo VariableJoystick vào đây")]
    public VariableJoystick aimJoystick;

    public LayerMask enemyLayer;

    public float maxLockDistance = 50f;
    public float lockFOVAngle = 140f;           // Góc rộng để lock dễ
    public float pullUpThreshold = 0.5f;        // Kéo lên 50% là bật lock

    [Tooltip("Tốc độ lia mượt vào đầu (giảm rung/lố)")]
    public float aimSnapSpeed = 90f;           // 80-120 tùy test

    [Tooltip("Offset đầu (trán) tự động + fixed")]
    [Range(0.8f, 1.0f)] public float headPercent = 0.92f;
    public float fixedHeadY = 0.30f;

    [Header("Crosshair & Debug - Để thấy rõ lock")]
    public GameObject crosshairPrefab;          // Kéo prefab crosshair (vòng đỏ) vào
    private Transform crosshairInstance;
    public Color lockLineColor = Color.cyan;

    private Transform playerRoot;
    private Transform gunTransform;
    private Transform lockedHead;

    void Awake()
    {
        playerRoot = transform.root;

        gunTransform = playerRoot.Find("Gun") ?? playerRoot.Find("Weapon/Gun") ?? playerRoot.Find("MainGun");
        if (gunTransform == null)
        {
            Debug.LogError("Gán gunTransform trong Inspector (kéo object Gun vào public field nếu thêm)");
        }

        // Tạo crosshair world-space (nếu có prefab)
        if (crosshairPrefab != null)
        {
            crosshairInstance = Instantiate(crosshairPrefab).transform;
            crosshairInstance.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (aimJoystick == null || gunTransform == null) return;

        float v = aimJoystick.Direction.y;

        if (v > pullUpThreshold)
        {
            TryLockAndAimHead();
        }
        else if (v < 0.1f)
        {
            lockedHead = null;
            if (crosshairInstance != null) crosshairInstance.gameObject.SetActive(false);
        }

        // Visual rõ rệt: line nối + crosshair di chuyển theo điểm đầu
        if (lockedHead != null)
        {
            Vector3 aimPoint = GetHeadAimPoint(lockedHead);
            Debug.DrawLine(gunTransform.position, aimPoint, lockLineColor);

            if (crosshairInstance != null)
            {
                crosshairInstance.position = aimPoint;
                crosshairInstance.gameObject.SetActive(true);
                crosshairInstance.LookAt(Camera.main.transform); // Crosshair luôn hướng camera
            }
        }
    }

    private void TryLockAndAimHead()
    {
        if (lockedHead != null && Vector3.Distance(gunTransform.position, lockedHead.position) <= maxLockDistance + 5f)
        {
            AimToHead(lockedHead);
            return;
        }

        Collider[] hits = Physics.OverlapSphere(playerRoot.position, maxLockDistance, enemyLayer);

        Transform bestHead = null;
        float bestAngle = lockFOVAngle + 1f;

        Vector3 gunFwd = gunTransform.forward;

        foreach (var hit in hits)
        {
            Transform head = FindHeadBone(hit.transform);
            if (head == null) continue;

            Vector3 aimPt = GetHeadAimPoint(head);
            float angle = Vector3.Angle(gunFwd, (aimPt - gunTransform.position).normalized);

            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestHead = head;
            }
        }

        if (bestHead != null && bestAngle <= lockFOVAngle)
        {
            lockedHead = bestHead;
            AimToHead(bestHead);
        }
        else
        {
            lockedHead = null;
        }
    }

    private void AimToHead(Transform head)
    {
        Vector3 aimPoint = GetHeadAimPoint(head);
        Vector3 dir = (aimPoint - gunTransform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        float currentAngle = Vector3.Angle(gunTransform.forward, dir);

        if (currentAngle > 8f) // Chỉ snap nếu chưa gần
        {
            gunTransform.rotation = Quaternion.RotateTowards(gunTransform.rotation, targetRot, aimSnapSpeed * Time.deltaTime);
        }
        // Không snap khi đã gần → tránh rung/lố
    }

    private Vector3 GetHeadAimPoint(Transform headBone)
    {
        float autoOffset = headBone.lossyScale.y * headPercent;
        return headBone.position + Vector3.up * Mathf.Max(autoOffset, fixedHeadY);
    }

    private Transform FindHeadBone(Transform enemy)
    {
        Transform head = enemy.Find("Head") ?? enemy.Find("head") ?? enemy.Find("Neck/Head");
        if (head != null) return head;

        foreach (Transform t in enemy.GetComponentsInChildren<Transform>())
        {
            string n = t.name.ToLower();
            if (n.Contains("head") || n.Contains("skull") || n.Contains("neck/head") || n.Contains("face"))
                return t;
        }
        return null;
    }

    public void Shoot()
    {
        if (gunTransform == null) return;

        Vector3 shootDir = gunTransform.forward;

        if (lockedHead != null)
        {
            Vector3 aimPt = GetHeadAimPoint(lockedHead);
            shootDir = (aimPt - gunTransform.position).normalized;
            Debug.Log("BẮN VỚI HEAD LOCK - Headshot cao!");
        }

        // Raycast bắn (thay bằng Instantiate đạn thật nếu cần)
        if (Physics.Raycast(gunTransform.position, shootDir, out RaycastHit hit, 200f))
        {
            if (hit.collider != null && hit.collider.CompareTag("Enemy"))
            {
                Debug.Log("Trúng địch! Headshot nếu lock đúng đầu.");
                // Gọi damage head: hit.collider.SendMessage("TakeDamage", 100f, SendMessageOptions.DontRequireReceiver);
            }
        }

        Debug.DrawRay(gunTransform.position, shootDir * 100f, Color.red, 0.4f);
    }
}
