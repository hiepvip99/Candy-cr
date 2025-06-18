using UnityEngine;

public interface IScoreManager
{
    int CurrentScore { get; }
    void AddScore(int amount);
    void ResetScore();
    // Có thể thêm sự kiện (event) để UI lắng nghe
    event System.Action<int> OnScoreChanged;
}
