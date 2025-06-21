// Scripts/Data/MatchResult.cs
using System.Collections.Generic;
using UnityEngine;

public class MatchResult
{
    public HashSet<GameObject> MatchedCandies { get; private set; }
    public List<MatchLineInfo> FoundMatchLines { get; private set; }

    // Thay đổi kiểu dữ liệu của Dictionary
    public Dictionary<Vector2Int, SpecialCandyCreationInfo> SpecialCandiesToCreate { get; private set; }

    public MatchResult()
    {
        MatchedCandies = new HashSet<GameObject>();
        FoundMatchLines = new List<MatchLineInfo>();
        SpecialCandiesToCreate = new Dictionary<Vector2Int, SpecialCandyCreationInfo>();
    }
}