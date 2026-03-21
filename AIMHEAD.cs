using UnityEngine;

public class FFStyleHeadLockSUPER : MonoBehaviour
{
    [Header("═══════ 🔥 SIÊU HEADLOCK FF - BAND ACC MODE 🔥 ═══════")]
    [Tooltip("Kéo VariableJoystick aim vào đây")]
    public VariableJoystick aimJoystick;

    public LayerMask enemyLayer;

    public float maxLockDistance = 120f;        // Siêu xa
    public float lockFOVAngle = 180f;           // Toàn màn hình
    public float pullUpThreshold = 0.25f;       // Chỉ cần kéo nhẹ là lock

    [Tooltip("Tốc độ snap SIÊU NHANH - 300-500 là band acc chắc")]
    public float aimSnapSpeed = 420f;           // 🔥 Siêu mạnh

    [Header("Head Offset - Chính xác trán 100%")]
    [Range(0.85f, 1.05f)] public float headPercent = 0.98f;
    public float fixedHeadY = 0.35f;

    [Header("Visual Cheat - Để bạn thấy rõ đang hack")]
    public GameObject crosshairPrefab;          // Prefab vòng đỏ to
    private Transform crosshairInstance;
    public Color lockLineColor = Color.magenta; // Siêu nổi

    private Transform playerRoot;
    private Transform gunTransform;
    private Transform lockedHead;

    void Awake()
    {
        playerRoot = transform.root;

        gunTransform = playerRoot.Find("Gun") ?? 
                       playerRoot.Find("Weapon/Gun") ?? 
                       playerRoot.Find("MainGun") ?? 
                       playerRoot.Find("Weapon");

        if (gunTransform == null)
            Debug.LogError("🔥 Gán GunTransform trong Inspector đi bro!");

        if (crosshairPrefab != null)
        {
            crosshairInstance = Instantiate(crosshairPrefab).transform;
            crosshairInstance.localScale = Vector3.one * 1.8f; // To hơn
            crosshairInstance.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (aimJoystick == null || gunTransform == null) return;

        float v = aimJoystick.Direction.y;

        if (v > pullUpThreshold)           // Chỉ cần kéo nhẹ
        {
            SuperLockAndAim();             // 🔥 Hàm siêu lock mới
        }
        else if (v < 0.15f)
        {
            lockedHead = null;
            if (crosshairInstance) crosshairInstance.gameObject.SetActive(false);
        }

        // Visual siêu rõ
        if (lockedHead != null)
        {
            Vector3 aimPoint = GetHeadAimPoint(lockedHead);
            Debug.DrawLine(gunTransform.position, aimPoint, lockLineColor, 0.1f);

            if (crosshairInstance)
            {
                crosshairInstance.position = aimPoint + Vector3.up * 0.3f;
                crosshairInstance.gameObject.SetActive(true);
                crosshairInstance.LookAt(Camera.main.transform);
                crosshairInstance.Rotate(0, 0, Time.time * 180f); // Xoay vòng đỏ cho ngầu
            }
        }
    }

    private void SuperLockAndAim()
    {
        // Luôn tìm lại mục tiêu mới mỗi frame (đổi siêu nhanh)
        Collider[] hits = Physics.OverlapSphere(playerRoot.position, maxLockDistance, enemyLayer);

        Transform bestHead = null;
        float bestScore = float.MaxValue;

        Vector3 gunPos = gunTransform.position;
        Vector3 gunFwd = gunTransform.forward;

        foreach (var hit in hits)
        {
            Transform head = FindHeadBoneSUPER(hit.transform);
            if (head == null) continue;

            Vector3 aimPt = GetHeadAimPoint(head);
            Vector3 dirToHead = (aimPt - gunPos).normalized;
            float dist = Vector3.Distance(gunPos, aimPt);
            float angle = Vector3.Angle(gunFwd, dirToHead);

            if (angle > lockFOVAngle) continue;

            // Score siêu thiên vị: góc nhỏ + gần = thắng tuyệt đối
            float score = angle * 1.0f + dist * 0.3f;
            if (score < bestScore)
            {
                bestScore = score;
                bestHead = head;
            }
        }

        if (bestHead != null)
        {
            lockedHead = bestHead;
            AimToHeadSUPER(bestHead);   // Snap cực mạnh
        }
        else
        {
            lockedHead = null;
        }
    }

    private void AimToHeadSUPER(Transform head)
    {
        Vector3 aimPoint = GetHeadAimPoint(head);
        Vector3 dir = (aimPoint - gunTransform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        // 🔥 Snap gần như tức thì + không giới hạn
        gunTransform.rotation = Quaternion.Slerp(gunTransform.rotation, targetRot, aimSnapSpeed * Time.deltaTime * 2f);
        // Hoặc dùng RotateTowards full tốc độ:
        // gunTransform.rotation = Quaternion.RotateTowards(gunTransform.rotation, targetRot, 999f);
    }

    private Vector3 GetHeadAimPoint(Transform headBone)
    {
        float offset = headBone.lossyScale.y * headPercent + fixedHeadY;
        return headBone.position + Vector3.up * offset + Random.insideUnitSphere * 0.05f; // rung nhẹ giả tự nhiên
    }

    private Transform FindHeadBoneSUPER(Transform enemy)
    {
        // Tìm siêu mạnh - bao quát hầu hết model Free Fire / Unity
        string[] names = { "Head", "head", "HEAD", "mixamorig:Head", "Bip001 Head", "Head.001", "head_01", "skull", "face", "Neck/Head", "Head_end" };

        foreach (string n in names)
        {
            Transform t = enemy.Find(n);
            if (t != null) return t;
        }

        // Tìm tất cả child
        foreach (Transform t in enemy.GetComponentsInChildren<Transform>())
        {
            string lower = t.name.ToLower();
            if (lower.Contains("head") || lower.Contains("skull") || lower.Contains("face") || lower.Contains("neck"))
                return t;
        }
        return null;
    }

    public void Shoot()   // 🔥 Gọi hàm này khi bắn
    {
        if (gunTransform == null) return;

        Vector3 shootDir = gunTransform.forward;

        if (lockedHead != null)
        {
            Vector3 perfectHead = GetHeadAimPoint(lockedHead);
            shootDir = (perfectHead - gunTransform.position).normalized;
            Debug.Log("💀 SIÊU HEADSHOT ACTIVATED - 100% Head!");
        }

        // Raycast siêu xa + chắc chắn trúng
        if (Physics.Raycast(gunTransform.position, shootDir, out RaycastHit hit, 300f))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                Debug.Log("🎯 BANG! Headshot confirmed - Enemy chết chắc!");
                // hit.collider.SendMessage("TakeDamage", 999f, SendMessageOptions.DontRequireReceiver);
            }
        }

        Debug.DrawRay(gunTransform.position, shootDir * 150f, Color.red, 1f);
    }

    // Bonus: Hàm bật/tắt siêu mode
    public void ToggleGodMode(bool on)
    {
        aimSnapSpeed = on ? 999f : 80f;
        maxLockDistance = on ? 200f : 50f;
        lockFOVAngle = on ? 180f : 90f;
        Debug.Log(on ? "🔥 GOD HEADLOCK ĐÃ BẬT - Band acc incoming!" : "Đã tắt cheat");
    }
}
