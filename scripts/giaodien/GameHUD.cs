using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameHUD : MonoBehaviour
{
    // Biến static (toàn cục) lưu trữ xuyên suốt không bị mất khi load map
    public static int GlobalDoorCount = 0;

    [Header("Liên kết UI")]
    public TMP_Text txtTrangThai;
    public TMP_Text txtSoCua;
    public TMP_Text txtThoiGian;
    public GameObject panelChienThang; // Bảng thông báo thắng game

    private player playerScript;
    private RandomScaleEffect scaleEffect;

    void Start()
    {
        playerScript = FindFirstObjectByType<player>();
        if (playerScript != null)
        {
            scaleEffect = playerScript.GetComponent<RandomScaleEffect>();
        }

        if (panelChienThang != null) panelChienThang.SetActive(false);
    }

    void Update()
    {
        if (scaleEffect == null) return;

        // 1. Hiển thị Trạng thái
        if (txtTrangThai != null)
            txtTrangThai.text ="" + scaleEffect.GetStateString();

        // 2. Hiển thị Số cửa & Kiểm tra thắng
        if (txtSoCua != null)
            txtSoCua.text = $"{GlobalDoorCount} / 100";

        if (GlobalDoorCount >= 100 && panelChienThang != null && !panelChienThang.activeSelf)
        {
            WinGame();
        }

        // 3. Hiển thị Thời gian
        if (txtThoiGian != null)
        {
            if (scaleEffect.isPermanentlyCured)
            {
                txtThoiGian.text = " ∞";
            }
            else if (scaleEffect.isCured)
            {
                int seconds = Mathf.CeilToInt(scaleEffect.cureTimer);
                txtThoiGian.text = $" 00:{seconds:D2}";
            }
            else
            {
                txtThoiGian.text = " 00:00";
            }
        }
    }

    void WinGame()
    {
        panelChienThang.SetActive(true);
        Time.timeScale = 0f; // Dừng thời gian
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true; // Hiện chuột để bấm nút Thoát/Chơi lại

        if (playerScript != null) playerScript.enabled = false;
    }

    // Hàm gắn vào nút ở bảng chiến thắng để về sảnh
    public void QuayVeSanh()
    {
        Time.timeScale = 1f;
        GlobalDoorCount = 0; // Reset lại điểm cho ván mới
        SceneManager.LoadScene(0); // Load lại Scene Sảnh (số 0)
    }
}