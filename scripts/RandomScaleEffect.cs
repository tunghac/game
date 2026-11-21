using UnityEngine;

public class RandomScaleEffect : MonoBehaviour
{
    // private => Inspector không ghi đè được nữa
    private readonly float[] scaleMultipliers = { 0.1f, 0.5f, 1f, 2f, 4f };
    private readonly string[] stateNames = { "Siêu nhỏ", "Nhỏ", "Bình thường", "Lớn", "Siêu lớn" };

    public float changeIntervalMin = 3f;
    public float changeIntervalMax = 8f;
    public float scaleTransitionSpeed = 4f;

    [Tooltip("Cầm bất kỳ vật phẩm nào trên tay thì giữ kích thước bình thường")]
    public bool normalWhenHoldingItem = true;

    public bool isCured = false;
    public bool isPermanentlyCured = false;

    public float cureTimer = 0f;
    public int currentStateIndex = 2;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private float nextChangeTime;
    private player playerScript;
    private bool wasForcedNormal = false;

    bool IsHolding => normalWhenHoldingItem && playerScript != null && playerScript.IsHoldingItem;
    bool ForcedNormal => isCured || isPermanentlyCured || IsHolding;

    void Awake()
    {
        StopAllCoroutines();
        playerScript = GetComponent<player>();
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Start()
    {
        ScheduleNextChange();
    }

    void Update()
    {
        // Đếm ngược thuốc (30s)
        if (isCured && !isPermanentlyCured)
        {
            cureTimer -= Time.deltaTime;
            if (cureTimer <= 0)
            {
                RemoveCure();
                if (playerScript != null) playerScript.ForceDestroyHeldItem();
            }
        }

        if (ForcedNormal)
        {
            targetScale = originalScale;
            currentStateIndex = 2;
            ScheduleNextChange();

            // Vừa chuyển sang trạng thái "bình thường" -> về ngay lập tức
            if (!wasForcedNormal) transform.localScale = originalScale;
            wasForcedNormal = true;
        }
        else
        {
            wasForcedNormal = false;
            if (Time.time >= nextChangeTime)
            {
                currentStateIndex = Random.Range(0, scaleMultipliers.Length);
                targetScale = originalScale * scaleMultipliers[currentStateIndex];
                ScheduleNextChange();
            }
        }
    }

    void LateUpdate()
    {
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleTransitionSpeed);
            if (Vector3.Distance(transform.localScale, targetScale) < 0.005f)
                transform.localScale = targetScale;
        }
    }

    void ScheduleNextChange()
    {
        nextChangeTime = Time.time + Random.Range(changeIntervalMin, changeIntervalMax);
    }

    public void ApplyCure(float duration)
    {
        isCured = true;
        cureTimer = duration;
        currentStateIndex = 2;
        targetScale = originalScale;
        transform.localScale = originalScale;
    }

    public void RemoveCure()
    {
        isCured = false;
        cureTimer = 0f;
        ScheduleNextChange();
    }

    public void ApplyPermanentCure()
    {
        isPermanentlyCured = true;
        currentStateIndex = 2;
        targetScale = originalScale;
        transform.localScale = originalScale;
    }

    public void RemovePermanentCure()
    {
        isPermanentlyCured = false;
        ScheduleNextChange();
    }

    public bool IsNormalState()
    {
        return currentStateIndex == 2 || ForcedNormal;
    }

    public string GetStateString()
    {
        if (ForcedNormal) return "Bình thường";
        return stateNames[currentStateIndex];
    }
}