using System.Collections.Generic;
using UnityEngine;

public interface IMatchFinder
{
    HashSet<GameObject> FindAllMatches(GameObject[,] currentCandies, int width, int height);
    bool CheckForMatchAtPosition(int x, int y, string checkingTag, GameObject[,] currentCandies);
    public SpecialCandyType GetSpecialCandyType(GameObject[,] currentCandies, int x, int y, string tag);

}