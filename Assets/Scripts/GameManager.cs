using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    [Header("Rules")]
    public int maxMisses = 10;

    [Header("UI (TMP)")]
    public TMP_Text scoreText;
    public TMP_Text ballsLeftText;
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;

    private int score = 0;
    private int misses = 0;
    private bool gameOver = false;

    private void Awake()
    {
        I = this;
    }

    private void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        RefreshUI();
    }

    public bool IsGameOver() => gameOver;

    public void AddScore(int amount)
    {
        if (gameOver) return;
        score += amount;
        RefreshUI();
    }

    public void RegisterMiss()
    {
        if (gameOver) return;

        misses++;
        RefreshUI();

        if (misses >= maxMisses)
            GameOver();
    }

    private void GameOver()
    {
        gameOver = true;

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (finalScoreText) finalScoreText.text = $"Final Score: {score}";
    }

    private void RefreshUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
        if (ballsLeftText) ballsLeftText.text = $"Balls Left: {Mathf.Max(0, maxMisses - misses)}";
    }

    public void RestartScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}
