using UnityEngine;
using TMPro; // Nếu bạn dùng TextMeshPro

public class ScoreManager : MonoBehaviour, IScoreManager
{
    public TextMeshProUGUI scoreText; // Kéo thả UI Text vào đây

    private int _currentScore = 0;
    public int CurrentScore => _currentScore;

    public event System.Action<int> OnScoreChanged;

    void Start()
    {
        UpdateScoreDisplay();
    }

    public void AddScore(int amount)
    {
        _currentScore += amount;
        UpdateScoreDisplay();
        OnScoreChanged?.Invoke(_currentScore); // Kích hoạt sự kiện
    }

    public void ResetScore()
    {
        _currentScore = 0;
        UpdateScoreDisplay();
        OnScoreChanged?.Invoke(_currentScore);
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {_currentScore}";
        }
    }
}