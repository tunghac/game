using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("Kéo Panel PauseMenuUI vào đây")]
    public GameObject pauseMenuUI;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume(); // Nếu đang pause thì tiếp tục
            }
            else
            {
                Pause(); // Nếu đang chơi thì pause
            }
        }

    }
    public void Resume()
    {
        pauseMenuUI.SetActive(false); // Ẩn UI
        Time.timeScale = 1f;          // Cho thời gian chạy lại bình thường
        GameIsPaused = false;         // Đánh dấu là hết pause

        // Khóa và ẩn chuột để chơi tiếp
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);  // Hiện UI
        Time.timeScale = 0f;          // Đóng băng thời gian (game dừng lại)
        GameIsPaused = true;          // Đánh dấu là đang pause

        // Mở khóa và hiện chuột để bấm nút
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Restart()
    {
        // QUAN TRỌNG: Phải đưa thời gian về 1 trước khi load lại map, nếu không map mới sẽ bị đóng băng
        Time.timeScale = 1f;
        GameIsPaused = false;

        // Load lại Scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Đang thoát game...");
        Application.Quit(); // Thoát game (Chỉ hoạt động khi đã Build ra file .exe)
    }
}
