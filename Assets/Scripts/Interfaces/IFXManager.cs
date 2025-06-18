using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFXManager
{
    IEnumerator AnimateSwap(GameObject candy1, GameObject candy2, Vector2 startPos1, Vector2 endPos1);
    IEnumerator AnimateDestroyMatches(HashSet<GameObject> matches);
    IEnumerator AnimateDropCandies(List<Vector2Int> droppedPositions, IBoard board);
    IEnumerator AnimateNewCandiesDrop(List<Vector2Int> newCandyPositions, IBoard board);
    void PlayMatchSound();
    void SpawnExplosionEffect(Vector3 position);
}
