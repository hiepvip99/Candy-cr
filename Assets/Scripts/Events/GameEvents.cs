//// Scripts/Events/GameEvents.cs (Tạo file mới)
//using UnityEngine;
//using System;
//using System.Collections.Generic;

//public static class GameEvents
//{
//    // Event này sẽ được kẹo đặc biệt gọi để thông báo các viên kẹo bị ảnh hưởng bởi nó
//    // Param 1: Vị trí của kẹo đặc biệt đã kích hoạt
//    // Param 2: Loại kẹo đặc biệt
//    // Param 3: HashSet của tất cả các GameObject kẹo bị ảnh hưởng/phá hủy bởi event này
//    // Param 4: (Tùy chọn) Tag mục tiêu cho Color Bomb
//    public static event Action<Vector2Int, SpecialCandyType, HashSet<GameObject>, string> OnSpecialCandyActivated;

//    // Event này sẽ được gọi khi một Wrapped Candy cần được kích hoạt lần 2
//    // Param 1: Vị trí của Wrapped Candy
//    public static event Action<Vector2Int> OnWrappedCandySecondExplosionNeeded;

//    // Hàm tiện ích để gọi event OnSpecialCandyActivated
//    public static void ReportSpecialCandyActivation(Vector2Int position, SpecialCandyType type, HashSet<GameObject> affectedCandies, string targetTag = null)
//    {
//        OnSpecialCandyActivated?.Invoke(position, type, affectedCandies, targetTag);
//    }

//    // Hàm tiện ích để gọi event OnWrappedCandySecondExplosionNeeded
//    public static void RequestWrappedCandySecondExplosion(Vector2Int position)
//    {
//        OnWrappedCandySecondExplosionNeeded?.Invoke(position);
//    }


//}

// Scripts/Events/GameEvents.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public static class GameEvents
{
    // Event này sẽ được kẹo đặc biệt gọi để thông báo các viên kẹo bị ảnh hưởng bởi nó
    // Param 1: Vị trí của kẹo đặc biệt đã kích hoạt
    // Param 2: Loại kẹo đặc biệt
    // Param 3: HashSet của tất cả các GameObject kẹo bị ảnh hưởng/phá hủy bởi event này
    // Param 4: (Tùy chọn) Tag mục tiêu cho Color Bomb
    public static event Action<Vector2Int, SpecialCandyType, HashSet<GameObject>, string> OnSpecialCandyActivated;

    // Event này sẽ được gọi khi một Wrapped Candy cần được kích hoạt lần 2
    // Param 1: Vị trí của Wrapped Candy
    // --> THAY ĐỔI DUY NHẤT Ở ĐÂY: Từ Vector2Int thành GameObject <--
    public static event Action<GameObject> OnWrappedCandySecondExplosionNeeded;

    // Hàm tiện ích để gọi event OnSpecialCandyActivated
    public static void ReportSpecialCandyActivation(Vector2Int position, SpecialCandyType type, HashSet<GameObject> affectedCandies, string targetTag = null)
    {
        OnSpecialCandyActivated?.Invoke(position, type, affectedCandies, targetTag);
    }

    // Hàm tiện ích để gọi event OnWrappedCandySecondExplosionNeeded
    // --> THAY ĐỔI DUY NHẤT Ở ĐÂY: Từ Vector2Int position thành GameObject wrappedCandyGo <--
    public static void RequestWrappedCandySecondExplosion(GameObject wrappedCandyGo)
    {
        OnWrappedCandySecondExplosionNeeded?.Invoke(wrappedCandyGo);
    }
}