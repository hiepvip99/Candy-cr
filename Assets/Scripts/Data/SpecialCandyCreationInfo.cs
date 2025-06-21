// Scripts/Data/SpecialCandyCreationInfo.cs (Tạo file mới)
using UnityEngine;

public struct SpecialCandyCreationInfo
{
    public SpecialCandyType Type;
    public bool IsHorizontalStripped; // Chỉ có ý nghĩa nếu Type là StrippedCandy
    public string BaseCandyTag;

    public SpecialCandyCreationInfo(SpecialCandyType type, string baseTag, bool isHorizontal = false)
    {
        Type = type;
        BaseCandyTag = baseTag;
        IsHorizontalStripped = isHorizontal;
    }
}
//// Scripts/Data/SpecialCandyCreationInfo.cs
//using UnityEngine;

//public struct SpecialCandyCreationInfo
//{
//    public SpecialCandyType Type;
//    public bool IsHorizontalStripped; // Chỉ có ý nghĩa nếu Type là StrippedCandy
//    public string BaseCandyTag;       // Thêm trường này để lưu tag gốc

//    public SpecialCandyCreationInfo(SpecialCandyType type, bool isHorizontal, string baseTag)
//    {
//        Type = type;
//        IsHorizontalStripped = isHorizontal;
//        BaseCandyTag = baseTag;
//    }
//}