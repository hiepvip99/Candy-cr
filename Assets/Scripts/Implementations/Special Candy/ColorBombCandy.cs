//// Scripts/Implementations/ColorBomb.cs
//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq; // Cần dùng LINQ cho các hàm như ToList(), Where()

//public class ColorBomb : Candy, ISpecialCandy
//{
//    public SpecialCandyType SpecialType => SpecialCandyType.ColorBomb;

//    // Bom màu thường không có "màu gốc" như Stripped/Wrapped.
//    // Tuy nhiên, khi được kích hoạt, nó cần một màu mục tiêu để phá hủy.
//    // Màu mục tiêu này sẽ được truyền vào khi Activate.

//    public IEnumerator Activate(IBoard board, IFXManager fxManager, HashSet<GameObject> affectedCandies, string targetTag = null)
//    {
//        Debug.Log($"Activating Color Bomb at ({X},{Y}). Target Tag: {targetTag}");

//        // Bom màu luôn tự phá hủy nó khi kích hoạt
//        affectedCandies.Add(this.gameObject);

//        // --- Xác định màu mục tiêu ---
//        // Nếu targetTag rỗng hoặc null, bom màu có thể phá hủy một màu ngẫu nhiên,
//        // hoặc màu của viên kẹo được chọn đầu tiên khi swap (nếu bạn có lưu trữ thông tin đó).
//        // Trong ngữ cảnh này, chúng ta giả định targetTag đã được xác định và truyền vào.
//        // Ví dụ: Nếu ColorBomb được swap với một kẹo Đỏ, targetTag sẽ là "Red".
//        if (string.IsNullOrEmpty(targetTag))
//        {
//            Debug.LogWarning("Color Bomb activated without a target tag. Defaulting to destroying a random color.");
//            // Ví dụ: lấy một tag ngẫu nhiên từ các kẹo trên bảng
//            targetTag = GetRandomCandyTag(board);
//        }

//        if (string.IsNullOrEmpty(targetTag))
//        {
//            Debug.LogWarning("No candies on board to pick a random tag. Color Bomb will do nothing.");
//            yield break;
//        }

//        // --- Phá hủy tất cả kẹo có màu mục tiêu ---
//        for (int x = 0; x < board.Width; x++)
//        {
//            for (int y = 0; y < board.Height; y++)
//            {
//                GameObject candy = board.GetCandy(x, y);
//                if (candy != null && candy.CompareTag(targetTag))
//                {
//                    affectedCandies.Add(candy);
//                }
//            }
//        }

//        // Tùy chọn: Thêm hiệu ứng đặc biệt khi Color Bomb kích hoạt
//        // yield return fxManager.AnimateColorBombActivation(this.gameObject, targetTag); // Ví dụ
//        yield return null;
//    }

//    //public IEnumerator Activate(IBoard board, IFXManager fxManager, HashSet<GameObject> affectedCandies)
//    //{
//    //    throw new System.NotImplementedException();
//    //}

//    // Hàm phụ trợ để lấy một tag ngẫu nhiên (nếu không có targetTag cụ thể)
//    private string GetRandomCandyTag(IBoard board)
//    {
//        List<GameObject> allCandies = new List<GameObject>();
//        for (int x = 0; x < board.Width; x++)
//        {
//            for (int y = 0; y < board.Height; y++)
//            {
//                GameObject candy = board.GetCandy(x, y);
//                if (candy != null && candy.GetComponent<Candy>() != null && candy.tag != "ColorBomb") // Không chọn ColorBomb làm màu mục tiêu
//                {
//                    allCandies.Add(candy);
//                }
//            }
//        }
//        if (allCandies.Count > 0)
//        {
//            return allCandies[Random.Range(0, allCandies.Count)].tag;
//        }
//        return null;
//    }
//}



// Scripts/Implementations/ColorBomb.cs
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