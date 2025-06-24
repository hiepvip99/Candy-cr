
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ColorBomb : Candy, ISpecialCandy
{
    public SpecialCandyType SpecialType => SpecialCandyType.ColorBomb;

    public IEnumerator Activate(IBoard board, IFXManager fxManager, string targetTag = null)
    {
        Debug.Log($"Activating Color Bomb at ({X},{Y}). Target Tag: {targetTag}");

        HashSet<GameObject> affectedCandies = new HashSet<GameObject>();
        affectedCandies.Add(this.gameObject); // Bom màu tự phá hủy nó

        if (string.IsNullOrEmpty(targetTag))
        {
            targetTag = GetRandomCandyTag(board);
        }

        if (!string.IsNullOrEmpty(targetTag))
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    GameObject candy = board.GetCandy(x, y);
                    if (candy != null && candy.CompareTag(targetTag))
                    {
                        affectedCandies.Add(candy);
                    }
                }
            }
        }

        // Báo cáo các kẹo bị ảnh hưởng thông qua Event
        GameEvents.ReportSpecialCandyActivation(new Vector2Int(X, Y), SpecialType, affectedCandies, targetTag);

        yield return null;
    }

    private string GetRandomCandyTag(IBoard board)
    {
        // ... (Code này không thay đổi) ...
        List<GameObject> allCandies = new List<GameObject>();
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                GameObject candy = board.GetCandy(x, y);
                if (candy != null && candy.GetComponent<Candy>() != null && candy.tag != "ColorBomb")
                {
                    allCandies.Add(candy);
                }
            }
        }
        if (allCandies.Count > 0)
        {
            return allCandies[Random.Range(0, allCandies.Count)].tag;
        }
        return null;
    }
}