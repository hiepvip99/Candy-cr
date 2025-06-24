// Scripts/Implementations/MatchFinder.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Để sử dụng LINQ

public class MatchFinder : MonoBehaviour, IMatchFinder
{
    // Cấu trúc dữ liệu để lưu trữ một "line" (hàng hoặc cột) của các viên kẹo match
    // Giúp dễ dàng xử lý các match dài hơn và tìm L/T
    private class MatchedLine
    {
        public List<GameObject> Candies { get; private set; }
        public bool IsHorizontal { get; private set; }

        public MatchedLine(List<GameObject> candies, bool isHorizontal)
        {
            Candies = candies;
            IsHorizontal = isHorizontal;
        }
    }

    //public HashSet<GameObject> FindAllMatches(GameObject[,] currentCandies, int width, int height)
    //{
    //    HashSet<GameObject> allMatchedCandies = new HashSet<GameObject>();
    //    List<MatchedLine> foundLines = new List<MatchedLine>();

    //    // 1. Tìm tất cả các Match ngang (dài 3 trở lên)
    //    for (int y = 0; y < height; y++)
    //    {
    //        for (int x = 0; x < width; x++)
    //        {
    //            // Bắt đầu một chuỗi match tiềm năng từ đây
    //            GameObject startCandy = currentCandies[x, y];
    //            if (startCandy == null) continue;

    //            List<GameObject> horizontalMatch = new List<GameObject>();
    //            horizontalMatch.Add(startCandy);

    //            // Mở rộng sang phải
    //            for (int i = x + 1; i < width; i++)
    //            {
    //                GameObject nextCandy = currentCandies[i, y];
    //                if (nextCandy != null && nextCandy.CompareTag(startCandy.tag))
    //                {
    //                    horizontalMatch.Add(nextCandy);
    //                }
    //                else
    //                {
    //                    break; // Chuỗi bị đứt
    //                }
    //            }

    //            if (horizontalMatch.Count >= 3)
    //            {
    //                foundLines.Add(new MatchedLine(horizontalMatch, true));
    //                foreach (var candy in horizontalMatch)
    //                {
    //                    allMatchedCandies.Add(candy);
    //                }
    //            }
    //            // Di chuyển x đến cuối chuỗi match để tránh kiểm tra lại
    //            x += horizontalMatch.Count - 1;
    //        }
    //    }

    //    // 2. Tìm tất cả các Match dọc (dài 3 trở lên)
    //    for (int x = 0; x < width; x++)
    //    {
    //        for (int y = 0; y < height; y++)
    //        {
    //            GameObject startCandy = currentCandies[x, y];
    //            if (startCandy == null) continue;

    //            List<GameObject> verticalMatch = new List<GameObject>();
    //            verticalMatch.Add(startCandy);

    //            // Mở rộng xuống dưới
    //            for (int i = y + 1; i < height; i++)
    //            {
    //                GameObject nextCandy = currentCandies[x, i];
    //                if (nextCandy != null && nextCandy.CompareTag(startCandy.tag))
    //                {
    //                    verticalMatch.Add(nextCandy);
    //                }
    //                else
    //                {
    //                    break; // Chuỗi bị đứt
    //                }
    //            }

    //            if (verticalMatch.Count >= 3)
    //            {
    //                foundLines.Add(new MatchedLine(verticalMatch, false));
    //                foreach (var candy in verticalMatch)
    //                {
    //                    allMatchedCandies.Add(candy);
    //                }
    //            }
    //            // Di chuyển y đến cuối chuỗi match để tránh kiểm tra lại
    //            y += verticalMatch.Count - 1;
    //        }
    //    }

    //    // Trả về tất cả các kẹo tham gia vào bất kỳ match nào.
    //    // Sau này, GameManager sẽ cần biết loại match nào đã xảy ra để tạo kẹo đặc biệt.
    //    return allMatchedCandies;
    //}

    //public MatchResult FindAllMatches(GameObject[,] currentCandies, int width, int height,
    //                                  Vector2Int? swappedCandyPosition = null,
    //                                  HashSet<Vector2Int> newlyAffectedPositions = null) // Thêm tham số này
    //{
    //    MatchResult result = new MatchResult();

    //    FindHorizontalMatches(currentCandies, width, height, result);
    //    FindVerticalMatches(currentCandies, width, height, result);

    //    // Truyền thêm newlyAffectedPositions vào đây
    //    IdentifySpecialCandyCreations(result, currentCandies, swappedCandyPosition, newlyAffectedPositions);

    //    return result;
    //}


    public MatchResult FindAllMatches(GameObject[,] currentCandies, int width, int height,
                                      Vector2Int? swappedCandyPosition = null,
                                      HashSet<Vector2Int> newlyAffectedPositions = null) // Thêm tham số này
    {
        MatchResult result = new MatchResult();

        FindHorizontalMatches(currentCandies, width, height, result);
        FindVerticalMatches(currentCandies, width, height, result);

        // Truyền thêm newlyAffectedPositions vào đây
        IdentifySpecialCandyCreations(result, currentCandies, swappedCandyPosition, newlyAffectedPositions);

        //Debug.Log($" SpecialCandiesToCreate: {result.SpecialCandiesToCreate.Count}");

        return result;
    }

    /// <summary>
    ///     Tìm các match ngang trong bảng kẹo.
    ///     
    /// </summary>
    /// <param name="currentCandies"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="result"></param>
    private void FindHorizontalMatches(GameObject[,] currentCandies, int width, int height, MatchResult result)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                if (currentCandies[x, y] == null) continue; // Bỏ qua nếu ô trống

                List<Candy> currentLine = new List<Candy>();
                Candy startCandy = currentCandies[x, y].GetComponent<Candy>();
                if (startCandy == null) continue;

                currentLine.Add(startCandy);

                for (int i = x + 1; i < width; i++)
                {
                    GameObject nextGO = currentCandies[i, y];
                    if (nextGO != null && nextGO.CompareTag(startCandy.gameObject.tag))
                    {
                        currentLine.Add(nextGO.GetComponent<Candy>());
                    }
                    else
                    {
                        break;
                    }
                }

                if (currentLine.Count >= 3)
                {
                    result.FoundMatchLines.Add(new MatchLineInfo(currentLine, true));
                    foreach (var candy in currentLine)
                    {
                        result.MatchedCandies.Add(candy.gameObject);
                    }
                }
                x += currentLine.Count - 1; // Nhảy qua các kẹo đã kiểm tra trong chuỗi
            }
        }
    }

    private void FindVerticalMatches(GameObject[,] currentCandies, int width, int height, MatchResult result)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                if (currentCandies[x, y] == null) continue;

                List<Candy> currentLine = new List<Candy>();
                Candy startCandy = currentCandies[x, y].GetComponent<Candy>();
                if (startCandy == null) continue;

                currentLine.Add(startCandy);

                for (int i = y + 1; i < height; i++)
                {
                    GameObject nextGO = currentCandies[x, i];
                    if (nextGO != null && nextGO.CompareTag(startCandy.gameObject.tag))
                    {
                        currentLine.Add(nextGO.GetComponent<Candy>());
                    }
                    else
                    {
                        break;
                    }
                }

                if (currentLine.Count >= 3)
                {
                    result.FoundMatchLines.Add(new MatchLineInfo(currentLine, false));
                    foreach (var candy in currentLine)
                    {
                        result.MatchedCandies.Add(candy.gameObject);
                    }
                }
                y += currentLine.Count - 1; // Nhảy qua các kẹo đã kiểm tra trong chuỗi
            }
        }
    }

    // Hàm này vẫn giữ nguyên, dùng để sinh bảng ban đầu.
    public bool CheckForMatchAtPosition(int x, int y, string checkingTag, GameObject[,] currentCandies)
    {
        // Kiểm tra match ngang (2 kẹo cùng loại bên trái)
        if (x >= 2 &&
            currentCandies[x - 1, y] != null && currentCandies[x - 1, y].CompareTag(checkingTag) &&
            currentCandies[x - 2, y] != null && currentCandies[x - 2, y].CompareTag(checkingTag))
        {
            return true;
        }

        // Kiểm tra match dọc (2 kẹo cùng loại bên dưới)
        if (y >= 2 &&
            currentCandies[x, y - 1] != null && currentCandies[x, y - 1].CompareTag(checkingTag) &&
            currentCandies[x, y - 2] != null && currentCandies[x, y - 2].CompareTag(checkingTag))
        {
            return true;
        }

        return false;
    }

    private void IdentifySpecialCandyCreations(MatchResult result, GameObject[,] currentCandies,
                                                   Vector2Int? swappedCandyPosition,
                                                   HashSet<Vector2Int> newlyAffectedPositions)
    {
        Dictionary<Vector2Int, SpecialCandyCreationInfo> proposedSpecialCandies = new Dictionary<Vector2Int, SpecialCandyCreationInfo>();

        // Lặp qua các line matches đã tìm được (để lấy tag gốc)
        // Lưu ý: Logic lấy baseCandyTag ở đây sẽ cần cẩn thận nếu một kẹo là phần của nhiều match.
        // Thông thường, nó là tag của viên kẹo tại vị trí spawnPos.

        // Bước 1: Tìm Match 5 (Color Bomb)
        foreach (var line in result.FoundMatchLines)
        {
            if (line.Candies.Count >= 5)
            {
                Vector2Int spawnPos = GetSpecialCandySpawnPosition(line.Candies, swappedCandyPosition, newlyAffectedPositions);
                // Lấy tag của viên kẹo tại vị trí sinh
                string baseTag = currentCandies[spawnPos.x, spawnPos.y]?.tag ?? "DefaultCandyTag"; // Fallback nếu null

                proposedSpecialCandies[spawnPos] = new SpecialCandyCreationInfo(SpecialCandyType.ColorBomb,baseTag , false);
            }
        }

        // Bước 2: Tìm L/T Shapes (Wrapped Candy)
        foreach (var horizontalLine in result.FoundMatchLines.Where(l => l.IsHorizontal))
        {
            foreach (var verticalLine in result.FoundMatchLines.Where(l => !l.IsHorizontal))
            {
                var intersection = horizontalLine.Candies.Intersect(verticalLine.Candies).ToList();

                if (intersection.Count == 1)
                {
                    if (horizontalLine.Candies.Count >= 3 && verticalLine.Candies.Count >= 3 &&
                        horizontalLine.Candies.Count + verticalLine.Candies.Count - 1 >= 5)
                    {
                        Candy intersectionCandy = intersection[0];
                        Vector2Int spawnPos = new Vector2Int(intersectionCandy.X, intersectionCandy.Y);

                        SpecialCandyCreationInfo existingInfo;
                        if (proposedSpecialCandies.TryGetValue(spawnPos, out existingInfo) && existingInfo.Type == SpecialCandyType.ColorBomb)
                        {
                            continue;
                        }

                        // Lấy tag của viên kẹo tại giao điểm
                        string baseTag = currentCandies[spawnPos.x, spawnPos.y]?.tag ?? "DefaultCandyTag"; // Fallback
                        proposedSpecialCandies[spawnPos] = new SpecialCandyCreationInfo(SpecialCandyType.WrappedCandy, baseTag, false);
                    }
                }
            }
        }

        // Bước 3: Tìm Match 4 (Stripped Candy)
        foreach (var line in result.FoundMatchLines)
        {
            if (line.Candies.Count == 4)
            {
                Vector2Int spawnPos = GetSpecialCandySpawnPosition(line.Candies, swappedCandyPosition, newlyAffectedPositions);

                SpecialCandyCreationInfo existingInfo;
                if (proposedSpecialCandies.TryGetValue(spawnPos, out existingInfo) &&
                    (existingInfo.Type == SpecialCandyType.ColorBomb || existingInfo.Type == SpecialCandyType.WrappedCandy))
                {
                    continue;
                }

                bool isHorizontalStripped = line.IsHorizontal;
                // Lấy tag của viên kẹo tại vị trí sinh
                string baseTag = currentCandies[spawnPos.x, spawnPos.y]?.tag ?? "DefaultCandyTag"; // Fallback
                proposedSpecialCandies[spawnPos] = new SpecialCandyCreationInfo(SpecialCandyType.StripedCandy, baseTag, isHorizontalStripped);
            }
        }

        foreach (var entry in proposedSpecialCandies)
        {
            result.SpecialCandiesToCreate.Add(entry.Key, entry.Value);
        }
    }

    /// <summary>
    /// Xác định vị trí sinh kẹo đặc biệt dựa trên các viên kẹo match, vị trí kẹo được hoán đổi,
    /// và các vị trí kẹo mới rơi xuống/sinh ra.
    /// Ưu tiên: Kẹo được hoán đổi -> Kẹo mới rơi/sinh ra -> Kẹo trung tâm (cho L/T), hoặc kẹo ở giữa line (cho match 4/5).
    /// </summary>
    private Vector2Int GetSpecialCandySpawnPosition(List<Candy> matchedCandiesInLine,
                                                    Vector2Int? swappedPos,
                                                    HashSet<Vector2Int> newlyAffectedPositions)
    {
        // Danh sách các kẹo match theo thứ tự X hoặc Y (để tìm kẹo trung tâm)
        List<Candy> sortedCandies = matchedCandiesInLine.OrderBy(c => c.X).ThenBy(c => c.Y).ToList();

        // 1. Ưu tiên vị trí của viên kẹo được hoán đổi
        if (swappedPos.HasValue)
        {
            Candy swappedCandyInMatch = matchedCandiesInLine.FirstOrDefault(c => c.X == swappedPos.Value.x && c.Y == swappedPos.Value.y);
            if (swappedCandyInMatch != null)
            {
                return new Vector2Int(swappedCandyInMatch.X, swappedCandyInMatch.Y);
            }
        }

        // 2. Ưu tiên vị trí của viên kẹo mới rơi/sinh ra nằm trong match
        if (newlyAffectedPositions != null)
        {
            foreach (var newlyPos in newlyAffectedPositions)
            {
                Candy newlyAffectedCandyInMatch = matchedCandiesInLine.FirstOrDefault(c => c.X == newlyPos.x && c.Y == newlyPos.y);
                if (newlyAffectedCandyInMatch != null)
                {
                    return newlyPos; // Ưu tiên vị trí này
                }
            }
        }

        // 3. Với match 4/5, chọn viên kẹo ở giữa
        // Với L/T, viên kẹo giao điểm (nếu hàm này được gọi cho L/T)
        // Trong trường hợp này, hàm này thường được gọi cho một line match (match 4/5)
        // Đối với L/T, vị trí giao điểm đã được xử lý riêng trong IdentifySpecialCandyCreations.
        if (sortedCandies.Count > 0)
        {
            // Trả về viên kẹo "gần giữa" nhất
            return new Vector2Int(sortedCandies[sortedCandies.Count / 2].X, sortedCandies[sortedCandies.Count / 2].Y);
        }

        // Trường hợp không mong muốn, trả về 0,0
        return Vector2Int.zero;
    }
    // Phương thức mới để xác định loại kẹo đặc biệt sẽ tạo
    // Sẽ được gọi bởi GameManager sau khi đã tìm thấy các matchedCandies
    public SpecialCandyType GetSpecialCandyType(GameObject[,] currentCandies, int x, int y, string tag)
    {
        // Tạm thời, chúng ta sẽ cần logic phức tạp hơn ở đây.
        // Cách tiếp cận đơn giản là kiểm tra xem vị trí (x,y) này có tạo thành match 4, 5, L, T hay không.

        List<GameObject> horizontalMatch = GetLineOfCandies(currentCandies, x, y, tag, true);
        List<GameObject> verticalMatch = GetLineOfCandies(currentCandies, x, y, tag, false);

        // Kiểm tra Match 5
        if (horizontalMatch.Count >= 5 || verticalMatch.Count >= 5)
        {
            return SpecialCandyType.ColorBomb; // Tạo bom màu
        }

        // Kiểm tra Match 4
        if (horizontalMatch.Count >= 4 || verticalMatch.Count >= 4)
        {
            return SpecialCandyType.StripedCandy; // Tạo kẹo sọc
        }

        // Kiểm tra L/T shape
        // Tìm giao điểm của match ngang và dọc (chỉ hoạt động nếu kẹo (x,y) là một phần của cả 2 match)
        if (horizontalMatch.Count >= 3 && verticalMatch.Count >= 3)
        {
            // Để xác định L/T chính xác, cần kiểm tra liệu có chung một viên kẹo tại giao điểm không
            // và tổng số kẹo tham gia là 5
            HashSet<GameObject> combinedLTCandies = new HashSet<GameObject>();
            foreach (var c in horizontalMatch) combinedLTCandies.Add(c);
            foreach (var c in verticalMatch) combinedLTCandies.Add(c);

            // Nếu tổng số kẹo là 5 (3 ngang + 3 dọc - 1 kẹo chung = 5)
            if (combinedLTCandies.Count == 5)
            {
                return SpecialCandyType.WrappedCandy; // Tạo kẹo bọc
            }
        }

        // Mặc định không tạo kẹo đặc biệt (chỉ là match 3 bình thường)
        return SpecialCandyType.None;
    }

    // Hàm hỗ trợ để lấy một chuỗi kẹo cùng loại từ một vị trí
    private List<GameObject> GetLineOfCandies(GameObject[,] currentCandies, int startX, int startY, string tag, bool isHorizontal)
    {
        List<GameObject> line = new List<GameObject>();
        if (currentCandies[startX, startY] == null) return line;

        // Tìm điểm bắt đầu thực sự của chuỗi (ví dụ: điểm tận cùng bên trái hoặc trên cùng)
        int currentX = startX;
        int currentY = startY;

        if (isHorizontal)
        {
            while (currentX >= 0 && currentCandies[currentX, currentY] != null && currentCandies[currentX, currentY].CompareTag(tag))
            {
                currentX--;
            }
            currentX++; // Quay lại điểm bắt đầu của chuỗi
            while (currentX < currentCandies.GetLength(0) && currentCandies[currentX, currentY] != null && currentCandies[currentX, currentY].CompareTag(tag))
            {
                line.Add(currentCandies[currentX, currentY]);
                currentX++;
            }
        }
        else // Vertical
        {
            while (currentY >= 0 && currentCandies[currentX, currentY] != null && currentCandies[currentX, currentY].CompareTag(tag))
            {
                currentY--;
            }
            currentY++; // Quay lại điểm bắt đầu của chuỗi
            while (currentY < currentCandies.GetLength(1) && currentCandies[currentX, currentY] != null && currentCandies[currentX, currentY].CompareTag(tag))
            {
                line.Add(currentCandies[currentX, currentY]);
                currentY++;
            }
        }
        return line;
    }

    
}

// Enum cho các loại kẹo đặc biệt
public enum SpecialCandyType
{
    None,
    StripedCandy, // Kẹo sọc (match 4)
    WrappedCandy,  // Kẹo bọc (L/T shape)
    ColorBomb      // Bom màu (match 5)
}