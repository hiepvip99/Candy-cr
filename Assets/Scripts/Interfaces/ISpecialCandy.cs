//// Ví dụ: Scripts/Interfaces/ISpecialCandy.cs
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public interface ISpecialCandy
//{
//    SpecialCandyType SpecialType { get; }
//    IEnumerator Activate(IBoard board, IFXManager fxManager); // Hàm kích hoạt hiệu ứng
//}

//*****************************************************************************************************************************************************************************//

//// Scripts/Interfaces/ISpecialCandy.cs
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public interface ISpecialCandy
//{
//    SpecialCandyType SpecialType { get; }
//    // Thêm tham số tùy chọn targetTag cho Color Bomb
//    IEnumerator Activate(IBoard board, IFXManager fxManager, HashSet<GameObject> affectedCandies, string targetTag = null);
//}


//*****************************************************************************************************************************************************************************//

// Scripts/Interfaces/ISpecialCandy.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISpecialCandy
{
    SpecialCandyType SpecialType { get; }
    // Activate giờ không truyền affectedCandies trực tiếp, mà sẽ report qua event
    // targetTag vẫn cần cho ColorBomb
    IEnumerator Activate(IBoard board, IFXManager fxManager, string targetTag = null);
}