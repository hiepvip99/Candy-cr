// Scripts/Data/MatchLineInfo.cs (Tạo file mới)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MatchLineInfo
{
    public List<Candy> Candies { get; private set; } // Danh sách các script Candy
    public bool IsHorizontal { get; private set; }
    public Vector2Int StartPos { get; private set; }
    public Vector2Int EndPos { get; private set; }

    public MatchLineInfo(List<Candy> candies, bool isHorizontal)
    {
        Candies = candies;
        IsHorizontal = isHorizontal;
        if (candies.Any())
        {
            StartPos = new Vector2Int(candies.Min(c => c.X), candies.Min(c => c.Y));
            EndPos = new Vector2Int(candies.Max(c => c.X), candies.Max(c => c.Y));
        }
    }
}