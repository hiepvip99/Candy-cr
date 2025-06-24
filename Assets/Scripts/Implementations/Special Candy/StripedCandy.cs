
//*****************************************************************************************************************************************************************************//

// Scripts/Implementations/StrippedCandy.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StripedCandy : Candy, ISpecialCandy
{
    public SpecialCandyType SpecialType => SpecialCandyType.StripedCandy;
    public bool IsHorizontalStrike { get; private set; }

    public void SetDirection(bool isHorizontal) { IsHorizontalStrike = isHorizontal; }

    public IEnumerator Activate(IBoard board, IFXManager fxManager, string targetTag = null)
    {
        Debug.Log($"Activating Stripped Candy ({targetTag}) at ({X},{Y}). Horizontal Strike: {IsHorizontalStrike}");

        HashSet<GameObject> affectedCandies = new HashSet<GameObject>();
        int boardWidth = board.Width;
        int boardHeight = board.Height;

        affectedCandies.Add(this.gameObject); // Kẹo sọc tự phá hủy nó

        if (IsHorizontalStrike)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                GameObject candy = board.GetCandy(x, Y);
                if (candy != null) affectedCandies.Add(candy);
            }
        }
        else // Vertical Strike
        {
            for (int y = 0; y < boardHeight; y++)
            {
                GameObject candy = board.GetCandy(X, y);
                if (candy != null) affectedCandies.Add(candy);
            }
        }

        // Báo cáo các kẹo bị ảnh hưởng thông qua Event
        GameEvents.ReportSpecialCandyActivation(new Vector2Int(X, Y), SpecialType, affectedCandies, targetTag);

        yield return null;
    }
}