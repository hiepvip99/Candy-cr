// Tạo một class mới để quản lý trạng thái của một lượt xử lý
using System.Collections.Generic;
using UnityEngine;

public class TurnProcessingState
{
    public HashSet<GameObject> CandiesToDestroy { get; } = new HashSet<GameObject>();
    public Dictionary<Vector2Int, SpecialCandyCreationInfo> SpecialCandiesToCreate { get; } = new Dictionary<Vector2Int, SpecialCandyCreationInfo>();
    public List<GameObject> WrappedCandiesPendingSecondExplosion { get; } = new List<GameObject>();
    public HashSet<GameObject> SpecialCandiesActivatedThisCascade { get; } = new HashSet<GameObject>();

    public bool HasPendingActions()
    {
        return CandiesToDestroy.Count > 0 || WrappedCandiesPendingSecondExplosion.Count > 0;
    }
}