using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Đồng bộ cửa 3D")]
    public LobbyDoor[] brownDoors; // Kéo TẤT CẢ các cánh cửa màu Nâu vào danh sách này
    public LobbyDoor whiteDoor;    // Kéo 1 cánh cửa Trắng vào đây

    [Header("Hiệu ứng Vào Game")]
    public Image fadeOverlay; // Kéo UI màn hình đen vào đây
    public float fadeDuration = 1f;

    [Header("Hiệu ứng Thoát Game")]
    public player playerScript; // Kéo nhân vật vào đây
    public float suckDuration = 2f;
    public float suckForce = 15f;

    private bool isActionTriggered = false; // Chống spam click

    void Start()
    {
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color; c.a = 0f; fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(false);
        }

        // CỐ ĐỊNH NGAY TỪ ĐẦU: Hiện chuột lên để di chuyển hover
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Khóa không cho nhân vật di chuyển hay xoay camera khi đang ở sảnh
        if (playerScript != null)
        {
            playerScript.enabled = false;
        }
    }

    // ---------------- NÚT VÀO GAME ----------------
    public void OnHoverEnter_VaoGame()
    {
        if (isActionTriggered) return;
        foreach (var door in brownDoors) if (door != null) door.OpenDoor(); // Mở tất cả cửa nâu
    }

    public void OnHoverExit_VaoGame()
    {
        if (isActionTriggered) return;
        foreach (var door in brownDoors) if (door != null) door.CloseDoor(); // Đóng tất cả cửa nâu
    }

    public void OnClick_VaoGame()
    {
        if (isActionTriggered) return;
        isActionTriggered = true;
        StartCoroutine(EnterGameSequence());
    }

    // ---------------- NÚT THOÁT GAME ----------------
    public void OnHoverEnter_ThoatGame()
    {
        if (isActionTriggered) return;
        if (whiteDoor != null) whiteDoor.OpenDoor(); // Mở cửa trắng
    }

    public void OnHoverExit_ThoatGame()
    {
        if (isActionTriggered) return;
        if (whiteDoor != null) whiteDoor.CloseDoor(); // Đóng cửa trắng
    }

    public void OnClick_ThoatGame()
    {
        if (isActionTriggered) return;
        isActionTriggered = true;
        StartCoroutine(QuitGameSequence());
    }

    // ================= CHUỖI HÀNH ĐỘNG =================

    IEnumerator EnterGameSequence()
    {
        // 1. Làm tối màn hình
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float time = 0;
            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                Color c = fadeOverlay.color;
                c.a = Mathf.Lerp(0f, 1f, time / fadeDuration);
                fadeOverlay.color = c;
                yield return null;
            }
        }

        // 2. Dịch chuyển AN TOÀN TUYỆT ĐỐI
        Map mapScript = FindFirstObjectByType<Map>();
        if (playerScript != null && mapScript != null)
        {
            CharacterController cc = playerScript.GetComponent<CharacterController>();

            // Tắt hệ thống vật lý và script điều khiển
            if (cc != null) cc.enabled = false;
            playerScript.enabled = false;

            // Đổi map
            mapScript.ChangeRandomMap();

            // CHỐNG KẸT: Bắt Unity đợi 1 khung hình để xử lý xong tọa độ mới
            yield return new WaitForEndOfFrame();

            // Bật lại hệ thống di chuyển
            if (cc != null) cc.enabled = true;
            playerScript.enabled = true;
        }

        // 3. Ẩn chuột, trả quyền ngắm bắn cho người chơi
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 4. Dọn dẹp giao diện sảnh
        isActionTriggered = false;
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color; c.a = 0f; fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(false);
        }

        gameObject.SetActive(false); // Tắt hoàn toàn Canvas sảnh
    }

    IEnumerator QuitGameSequence()
    {
        if (playerScript == null || whiteDoor == null) yield break;

        CharacterController controller = playerScript.GetComponent<CharacterController>();
        playerScript.enabled = false;

        // Lôi tuột nhân vật vào cánh cửa trắng
        float elapsed = 0f;
        while (elapsed < suckDuration)
        {
            elapsed += Time.deltaTime;
            Vector3 pullDirection = (whiteDoor.transform.position - playerScript.transform.position).normalized;
            pullDirection.y = 0;

            if (controller != null) controller.Move(pullDirection * suckForce * Time.deltaTime);
            if (Vector3.Distance(playerScript.transform.position, whiteDoor.transform.position) < 1.5f) break;
            yield return null;
        }

        Debug.Log("Thoát Game!");
        Application.Quit();
    }
}