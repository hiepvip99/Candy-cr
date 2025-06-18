using System.Collections.Generic;
using UnityEngine;

public interface IBoard 
{
     int Width { get; }
     int Height { get;}
     GameObject[,] Candies { get; }
    void Initialize(int width, int height, GameObject[] candyPrefabs, GameObject tilePerfab);
    Vector2 GetWorldPosition(int x, int y);
    GameObject GetCandy(int x, int y);
    void SetCandy(int x, int y, GameObject candy);
    void SwapCandiesData(int x1, int y1, int x2, int y2);
    void DestroyCandies(HashSet<GameObject> candiesToDestroy);
    List<Vector2Int> ApplyGravity();
    List<Vector2Int> FillEmptySpots(GameObject[] candyPrefabs);
}
