//using System.Collections.Generic;
//using UnityEngine;

//public interface IMatchFinder
//{
//    HashSet<GameObject> FindAllMatches(GameObject[,] currentCandies, int width, int height);
//    bool CheckForMatchAtPosition(int x, int y, string checkingTag, GameObject[,] currentCandies);
//    public SpecialCandyType GetSpecialCandyType(GameObject[,] currentCandies, int x, int y, string tag);

//}


// Scripts/Interfaces/IMatchFinder.cs
using UnityEngine;
using System.Collections.Generic;

public interface IMatchFinder
{
    // Thêm tham số newlyAffectedPositions
    MatchResult FindAllMatches(GameObject[,] currentCandies, int width, int height,
                               Vector2Int? swappedCandyPosition = null,
                               HashSet<Vector2Int> newlyAffectedPositions = null);

    bool CheckForMatchAtPosition(int x, int y, string checkingTag, GameObject[,] currentCandies);
}