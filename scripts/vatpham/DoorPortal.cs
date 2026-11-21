using System.Collections;
using UnityEngine;

public class DoorPortal : MonoBehaviour
{
    [Header("Thiết lập cánh cửa")]
    public Transform door1;
    public Transform door2;

    [Header("Thông số mở cửa")]
    public float openAngle = 90f;
    public float openSpeed = 3f;

    [Header("Hiệu ứng chuyển Map")]
    public bool isTeleportDoor = true;
    public float suckDuration = 2f;
    public float suckForce = 15f;

    private bool isOpen = false;
    private bool isAnimating = false;
    private bool isTeleporting = false;
    private bool initialized = false;

    private Quaternion door1ClosedRot, door1OpenRot;
    private Quaternion door2ClosedRot, door2OpenRot;

    void Start()
    {
        if (door1 != null)
        {
            door1ClosedRot = door1.localRotation;
            door1OpenRot = door1ClosedRot * Quaternion.Euler(0, openAngle, 0);
        }
        if (door2 != null)
        {
            door2ClosedRot = door2.localRotation;
            door2OpenRot = door2ClosedRot * Quaternion.Euler(0, -openAngle, 0);
        }
        initialized = true;
    }

    // Map bị tắt -> coroutine của cửa bị giết, nên phải tự reset trạng thái ở đây
    void OnDisable()
    {
        ResetDoor();
    }

    void ResetDoor()
    {
        isOpen = false;
        isAnimating = false;
        isTeleporting = false;
        if (!initialized) return;
        if (door1 != null) door1.localRotation = door1ClosedRot;
        if (door2 != null) door2.localRotation = door2ClosedRot;
    }

    // Trả về true nếu tương tác được chấp nhận
    public bool Interact()
    {
        if (isAnimating || isTeleporting) return false;

        if (isTeleportDoor && !isOpen)
        {
            player p = FindFirstObjectByType<player>();
            Map m = FindFirstObjectByType<Map>();
            if (p == null || m == null) return false;

            isOpen = true;
            isTeleporting = true;
            p.enabled = false; // khóa điều khiển ngay lập tức

            StartCoroutine(AnimateDoor(true));
            // QUAN TRỌNG: chạy coroutine trên PLAYER (luôn active), không chạy trên cửa
            // vì cửa nằm trong map cũ sẽ bị tắt giữa chừng.
            p.StartCoroutine(TeleportSequence(p, m));
            return true;
        }

        isOpen = !isOpen;
        StartCoroutine(AnimateDoor(isOpen));
        return true;
    }

    IEnumerator TeleportSequence(player p, Map m)
    {
        yield return new WaitForSeconds(1f);

        CharacterController controller = p.GetComponent<CharacterController>();

        float elapsed = 0f;
        while (elapsed < suckDuration)
        {
            elapsed += Time.deltaTime;

            Vector3 toDoor = transform.position - p.transform.position;
            toDoor.y = 0f;
            if (toDoor.magnitude < 1.5f) break;

            if (controller != null && controller.enabled)
                controller.Move(toDoor.normalized * suckForce * Time.deltaTime);

            yield return null;
        }

        m.ChangeRandomMap();   // cửa này sẽ bị tắt tại đây, nhưng coroutine vẫn chạy tiếp vì nằm trên player
        yield return null;     // chờ 1 khung hình cho vật lý cập nhật vị trí mới

        p.enabled = true;      // MỞ KHÓA LẠI NHÂN VẬT
        p.RefreshHeldItem();   // đảm bảo vật phẩm trên tay (Hack) còn nguyên
        ResetDoor();
    }

    IEnumerator AnimateDoor(bool targetState)
    {
        isAnimating = true;
        float time = 0;
        Quaternion targetRot1 = targetState ? door1OpenRot : door1ClosedRot;
        Quaternion targetRot2 = targetState ? door2OpenRot : door2ClosedRot;

        while (time < 1)
        {
            time += Time.deltaTime * openSpeed;
            if (door1 != null) door1.localRotation = Quaternion.Slerp(door1.localRotation, targetRot1, time);
            if (door2 != null) door2.localRotation = Quaternion.Slerp(door2.localRotation, targetRot2, time);
            yield return null;
        }
        isAnimating = false;
    }
}