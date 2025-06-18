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

    public HashSet<GameObject> FindAllMatches(GameObject[,] currentCandies, int width, int height)
    {
        HashSet<GameObject> allMatchedCandies = new HashSet<GameObject>();
        List<MatchedLine> foundLines = new List<MatchedLine>();

        // 1. Tìm tất cả các Match ngang (dài 3 trở lên)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Bắt đầu một chuỗi match tiềm năng từ đây
                GameObject startCandy = currentCandies[x, y];
                if (startCandy == null) continue;

                List<GameObject> horizontalMatch = new List<GameObject>();
                horizontalMatch.Add(startCandy);

                // Mở rộng sang phải
                for (int i = x + 1; i < width; i++)
                {
                    GameObject nextCandy = currentCandies[i, y];
                    if (nextCandy != null && nextCandy.CompareTag(startCandy.tag))
                    {
                        horizontalMatch.Add(nextCandy);
                    }
                    else
                    {
                        break; // Chuỗi bị đứt
                    }
                }

                if (horizontalMatch.Count >= 3)
                {
                    foundLines.Add(new MatchedLine(horizontalMatch, true));
                    foreach (var candy in horizontalMatch)
                    {
                        allMatchedCandies.Add(candy);
                    }
                }
                // Di chuyển x đến cuối chuỗi match để tránh kiểm tra lại
                x += horizontalMatch.Count - 1;
            }
        }

        // 2. Tìm tất cả các Match dọc (dài 3 trở lên)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject startCandy = currentCandies[x, y];
                if (startCandy == null) continue;

                List<GameObject> verticalMatch = new List<GameObject>();
                verticalMatch.Add(startCandy);

                // Mở rộng xuống dưới
                for (int i = y + 1; i < height; i++)
                {
                    GameObject nextCandy = currentCandies[x, i];
                    if (nextCandy != null && nextCandy.CompareTag(startCandy.tag))
                    {
                        verticalMatch.Add(nextCandy);
                    }
                    else
                    {
                        break; // Chuỗi bị đứt
                    }
                }

                if (verticalMatch.Count >= 3)
                {
                    foundLines.Add(new MatchedLine(verticalMatch, false));
                    foreach (var candy in verticalMatch)
                    {
                        allMatchedCandies.Add(candy);
                    }
                }
                // Di chuyển y đến cuối chuỗi match để tránh kiểm tra lại
                y += verticalMatch.Count - 1;
            }
        }

        // Trả về tất cả các kẹo tham gia vào bất kỳ match nào.
        // Sau này, GameManager sẽ cần biết loại match nào đã xảy ra để tạo kẹo đặc biệt.
        return allMatchedCandies;
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
            return SpecialCandyType.StrippedCandy; // Tạo kẹo sọc
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
    StrippedCandy, // Kẹo sọc (match 4)
    WrappedCandy,  // Kẹo bọc (L/T shape)
    ColorBomb      // Bom màu (match 5)
}