


//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;

//public class WrappedCandy : Candy, ISpecialCandy
//{
//    public SpecialCandyType SpecialType => SpecialCandyType.WrappedCandy;

//    public IEnumerator Activate(IBoard board, IFXManager fxManager, string targetTag = null)
//    {
//        Debug.Log($"Activating Wrapped Candy ({targetTag}) at ({X},{Y}) - First Explosion.");

//        HashSet<GameObject> affectedCandies = new HashSet<GameObject>();
//        int boardWidth = board.Width;
//        int boardHeight = board.Height;

//        // Vùng nổ 3x3 quanh viên kẹo bọc
//        for (int xOffset = -1; xOffset <= 1; xOffset++)
//        {
//            for (int yOffset = -1; yOffset <= 1; yOffset++)
//            {
//                // Bỏ qua chính viên kẹo bọc trong lần nổ đầu tiên
//                if (xOffset == 0 && yOffset == 0) continue;

//                int targetX = X + xOffset;
//                int targetY = Y + yOffset;

//                if (targetX >= 0 && targetX < boardWidth && targetY >= 0 && targetY < boardHeight)
//                {
//                    GameObject candy = board.GetCandy(targetX, targetY);
//                    // Đảm bảo không thêm chính viên kẹo bọc vào đây nếu nó vẫn còn trên bảng
//                    if (candy != null) // Không cần kiểm tra candy != this.gameObject nữa vì đã bỏ qua (0,0)
//                    {
//                        affectedCandies.Add(candy);
//                    }
//                }
//            }
//        }

//        // Báo cáo lần nổ đầu tiên và các kẹo bị ảnh hưởng
//        GameEvents.ReportSpecialCandyActivation(new Vector2Int(X, Y), SpecialType, affectedCandies, targetTag);

//        // Báo hiệu rằng cần nổ lần thứ hai, truyền CHÍNH GAMEOBJECT của nó
//        GameEvents.RequestWrappedCandySecondExplosion(this.gameObject); // <-- THAY ĐỔI Ở ĐÂY!

//        yield return new WaitForSeconds(0.2f); // Đợi một chút cho hiệu ứng trực quan
//    }

//    // Hàm cho lần nổ thứ 2 (sẽ được GameManager gọi)
//    // Sửa đổi để chấp nhận GameObject của Wrapped Candy
//    public static HashSet<GameObject> GetSecondExplosionAffectedCandies(GameObject wrappedCandyObject, IBoard board)
//    {
//        HashSet<GameObject> affectedCandies = new HashSet<GameObject>();

//        if (wrappedCandyObject == null)
//        {
//            Debug.LogWarning("WrappedCandy: Attempted second explosion with null wrappedCandyObject.");
//            return affectedCandies;
//        }

//        Candy wcComponent = wrappedCandyObject.GetComponent<Candy>();
//        if (wcComponent == null)
//        {
//            Debug.LogWarning("WrappedCandy: WrappedCandyObject does not have a Candy component for second explosion.");
//            return affectedCandies;
//        }

//        // Lấy vị trí HIỆN TẠI của viên kẹo bọc
//        Vector2Int currentPosition = new Vector2Int(wcComponent.X, wcComponent.Y);

//        int boardWidth = board.Width;
//        int boardHeight = board.Height;

//        // Vùng nổ 3x3 lần 2
//        for (int xOffset = -1; xOffset <= 1; xOffset++)
//        {
//            for (int yOffset = -1; yOffset <= 1; yOffset++)
//            {
//                int targetX = currentPosition.x + xOffset;
//                int targetY = currentPosition.y + yOffset;

//                if (targetX >= 0 && targetX < boardWidth && targetY >= 0 && targetY < boardHeight)
//                {
//                    GameObject candy = board.GetCandy(targetX, targetY);
//                    if (candy != null)
//                    {
//                        affectedCandies.Add(candy);
//                    }
//                }
//            }
//        }

//        // Thêm chính viên kẹo bọc vào danh sách để phá hủy nó ở lần nổ thứ hai này
//        affectedCandies.Add(wrappedCandyObject); // <-- THÊM ĐÂY ĐỂ NÓ BỊ PHÁ HỦY LẦN NÀY

//        return affectedCandies;
//    }
//}


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WrappedCandy : Candy, ISpecialCandy
{
    public SpecialCandyType SpecialType => SpecialCandyType.WrappedCandy;

    public IEnumerator Activate(IBoard board, IFXManager fxManager, string targetTag = null)
    {
        Debug.Log($"Activating Wrapped Candy ({targetTag}) at ({X},{Y}) - First Explosion.");

        HashSet<GameObject> affectedCandies = new HashSet<GameObject>();
        int boardWidth = board.Width;
        int boardHeight = board.Height;

        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                // Bỏ qua chính viên kẹo bọc trong lần nổ đầu tiên
                if (xOffset == 0 && yOffset == 0) continue;

                int targetX = X + xOffset;
                int targetY = Y + yOffset;

                if (targetX >= 0 && targetX < boardWidth && targetY >= 0 && targetY < boardHeight)
                {
                    GameObject candy = board.GetCandy(targetX, targetY);
                    if (candy != null)
                    {
                        // THAY ĐỔI Ở ĐÂY: Nếu kẹo bị ảnh hưởng là một Wrapped Candy khác,
                        // KHÔNG THÊM NÓ VÀO affectedCandies để phá hủy ngay.
                        // Thay vào đó, nó sẽ tự xử lý hoặc được đánh dấu để nổ lần 2.
                        WrappedCandy otherWrappedCandy = candy.GetComponent<WrappedCandy>();
                        if (otherWrappedCandy != null)
                        {
                            // Nếu kẹo này là một Wrapped Candy khác, chúng ta KHÔNG phá hủy nó ngay lập tức.
                            // Thay vào đó, nó sẽ được thêm vào _wrappedCandiesPendingSecondExplosion
                            // thông qua event từ Match3GameManager khi nó tự activate.
                            // Hoặc, nếu nó chưa activate, chúng ta sẽ request nó activate lần 1.
                            // Nhưng để đơn giản, trong match 3, tất cả kẹo bọc trong match đều activate.
                            // Vậy nên, chúng ta không cần làm gì ở đây, nó sẽ tự thêm mình vào pending list.
                            // Vấn đề là nếu nó không match, nhưng bị ảnh hưởng bởi vụ nổ 3x3 khác.
                            // Trong trường hợp này, nếu nó bị ảnh hưởng bởi vụ nổ 3x3, nó CŨNG SẼ NỔ.
                            // Đây là logic phức tạp hơn.
                            // Đối với trường hợp [Kẹo bọc A] [Kẹo bọc B] [Kẹo thường]:
                            // Khi A nổ, nó báo B bị ảnh hưởng (nếu B trong vùng 3x3 của A).
                            // Nếu B bị ảnh hưởng, nó sẽ được thêm vào _allCandiesToDestroyThisTurn.
                            // Đây là vấn đề!

                            // Giải pháp: Không thêm Wrapped Candy khác vào danh sách affectedCandies
                            // của một Wrapped Candy đang nổ.
                            // Thay vào đó, chúng ta sẽ để GameManager kiểm soát việc kích hoạt Wrapped Candy.
                            // Hoặc đơn giản là không thêm nó vào danh sách bị phá hủy ở đây.

                            // Quyết định: Nếu đó là một Wrapped Candy khác, KHÔNG THÊM vào affectedCandies.
                            // Nó sẽ được kích hoạt riêng hoặc được HandleMatchedSpecialCandyActivations xử lý.
                            // Hoặc nếu nó là một kẹo bọc bình thường bị dính vụ nổ, nó cũng sẽ kích hoạt lần 1.
                            continue; // Bỏ qua Wrapped Candy khác trong vụ nổ 3x3 đầu tiên.
                        }

                        // Chỉ thêm các kẹo không phải Wrapped Candy vào danh sách bị ảnh hưởng
                        affectedCandies.Add(candy);
                    }
                }
            }
        }

        GameEvents.ReportSpecialCandyActivation(new Vector2Int(X, Y), SpecialType, affectedCandies, targetTag);

        // Báo hiệu rằng cần nổ lần thứ hai, truyền CHÍNH GAMEOBJECT của nó
        GameEvents.RequestWrappedCandySecondExplosion(this.gameObject);

        yield return new WaitForSeconds(0.2f);
    }

    // Hàm cho lần nổ thứ 2 (sẽ được GameManager gọi)
    public static HashSet<GameObject> GetSecondExplosionAffectedCandies(GameObject wrappedCandyObject, IBoard board)
    {
        HashSet<GameObject> affectedCandies = new HashSet<GameObject>();

        if (wrappedCandyObject == null)
        {
            Debug.LogWarning("WrappedCandy: Attempted second explosion with null wrappedCandyObject.");
            return affectedCandies;
        }

        Candy wcComponent = wrappedCandyObject.GetComponent<Candy>();
        if (wcComponent == null)
        {
            Debug.LogWarning("WrappedCandy: WrappedCandyObject does not have a Candy component for second explosion.");
            return affectedCandies;
        }

        Vector2Int currentPosition = new Vector2Int(wcComponent.X, wcComponent.Y);

        int boardWidth = board.Width;
        int boardHeight = board.Height;

        for (int xOffset = -1; xOffset <= 1; xOffset++)
        {
            for (int yOffset = -1; yOffset <= 1; yOffset++)
            {
                int targetX = currentPosition.x + xOffset;
                int targetY = currentPosition.y + yOffset;

                if (targetX >= 0 && targetX < boardWidth && targetY >= 0 && targetY < boardHeight)
                {
                    GameObject candy = board.GetCandy(targetX, targetY);
                    if (candy != null)
                    {
                        affectedCandies.Add(candy);
                    }
                }
            }
        }

        // Thêm chính viên kẹo bọc vào danh sách để phá hủy nó ở lần nổ thứ hai này
        affectedCandies.Add(wrappedCandyObject);

        return affectedCandies;
    }
}