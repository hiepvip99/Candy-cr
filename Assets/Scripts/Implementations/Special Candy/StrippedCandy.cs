

//// Scripts/Implementations/StrippedCandy.cs
//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;

//public class StrippedCandy : Candy, ISpecialCandy
//{
//    public SpecialCandyType SpecialType => SpecialCandyType.StrippedCandy;
//    public bool IsHorizontalStrike { get; private set; }

//    // Hàm này sẽ được GameManager gọi để thiết lập hướng sau khi Instantiate
//    public void SetDirection(bool isHorizontal)
//    {
//        IsHorizontalStrike = isHorizontal;
//        // Tùy chỉnh hiển thị sprite hoặc animation tại đây nếu cần thiết
//        // Ví dụ: GetComponent<SpriteRenderer>().sprite = IsHorizontalStrike ? horizontalSprite : verticalSprite;
//    }

//    // Thêm tham số affectedCandies để truyền vào và điền các kẹo bị phá hủy
//    public IEnumerator Activate(IBoard board, IFXManager fxManager, HashSet<GameObject> affectedCandies, string targetTag = null)
//    {
//        Debug.Log($"Activating Stripped Candy ({targetTag}) at ({X},{Y}). Horizontal Strike: {IsHorizontalStrike}");

//        // Không gọi fxManager.AnimateDestroyMatches hay board.DestroyCandies ở đây nữa
//        // Chỉ xác định các kẹo bị ảnh hưởng và thêm vào 'affectedCandies'

//        int boardWidth = board.Width;
//        int boardHeight = board.Height;

//        if (IsHorizontalStrike)
//        {
//            for (int x = 0; x < boardWidth; x++)
//            {
//                GameObject candy = board.GetCandy(x, Y);
//                if (candy != null) affectedCandies.Add(candy);
//            }
//        }
//        else // Vertical Strike
//        {
//            for (int y = 0; y < boardHeight; y++)
//            {
//                GameObject candy = board.GetCandy(X, y);
//                if (candy != null) affectedCandies.Add(candy);
//            }
//        }

//        // Có thể thêm hiệu ứng riêng cho kẹo sọc kích hoạt ở đây (không phải phá hủy kẹo)
//        // yield return fxManager.AnimateStrippedCandyActivation(this.gameObject); // Ví dụ
//        yield return null; // Quan trọng để yield nếu là IEnumerator
//    }
//}



//*****************************************************************************************************************************************************************************//

// Scripts/Implementations/StrippedCandy.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StrippedCandy : Candy, ISpecialCandy
{
    public SpecialCandyType SpecialType => SpecialCandyType.StrippedCandy;
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