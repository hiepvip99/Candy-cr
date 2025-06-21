//// Scripts/Debug/CandyDebugTester.cs
//using UnityEngine;
//using System.Collections;
//using System.Collections.Generic;

//public class CandyDebugTester : MonoBehaviour
//{
//    [Header("References")]
//    [SerializeField] private Match3GameManager gameManager; // Kéo GameManager vào đây trong Inspector
//    [SerializeField] private Board board; // Kéo Board vào đây trong Inspector
//    [SerializeField] private FXManager fxManager; // Kéo FXManager vào đây trong Inspector

//    [Header("Debug Settings")]
//    [Tooltip("Type of special candy to create on right-click.")]
//    [SerializeField] private SpecialCandyType debugSpecialCandyType = SpecialCandyType.StrippedCandy;
//    [SerializeField] private bool strippedCandyIsHorizontal = true; // Chỉ có tác dụng nếu debugSpecialCandyType là StrippedCandy

//    private Camera mainCamera;

//    void Awake()
//    {
//        if (gameManager == null)
//        {
//            gameManager = FindFirstObjectByType<Match3GameManager>();
//        }
//        if (board == null)
//        {
//            board = FindFirstObjectByType<Board>();
//        }
//        if (fxManager == null)
//        {
//            fxManager = FindFirstObjectByType<FXManager>();
//        }
//        mainCamera = Camera.main;

//        if (gameManager == null || board == null || fxManager == null || mainCamera == null)
//        {
//            Debug.LogError("CandyDebugTester: Missing required references. Please assign GameManager, Board, FXManager, and ensure a Main Camera is in the scene.");
//            this.enabled = false; // Tắt script nếu thiếu tham chiếu
//        }
//    }

//    void Update()
//    {
//        // Kiểm tra chuột phải được nhấn
//        if (Input.GetMouseButtonDown(1)) // 1 là chuột phải, 0 là chuột trái
//        {
//            // Đảm bảo game không đang trong quá trình xử lý cascade
//            if (gameManager != null && gameManager.IsProcessingBoard)
//            {
//                Debug.Log("CandyDebugTester: Game is currently processing board. Cannot create special candy.");
//                return;
//            }

//            Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
//            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

//            if (hit.collider != null)
//            {
//                GameObject hitCandy = hit.collider.gameObject;
//                Candy candyComponent = hitCandy.GetComponent<Candy>();

//                if (candyComponent != null)
//                {
//                    Debug.Log($"Right-clicked on candy at ({candyComponent.X},{candyComponent.Y}). Attempting to convert to {debugSpecialCandyType}.");
//                    StartCoroutine(ConvertAndActivateSpecialCandy(candyComponent));
//                }
//            }
//        }
//    }

//    private IEnumerator ConvertAndActivateSpecialCandy(Candy targetCandy)
//    {
//        float angle = 0f; // Góc xoay cho kẹo sọc, mặc định là 0 (ngang)
//        // 1. Lấy vị trí của viên kẹo bị nhấn
//        Vector2Int candyPos = new Vector2Int(targetCandy.X, targetCandy.Y);

//        // 2. Xóa viên kẹo cũ khỏi bảng (logic và gameObject)
//        // Vì chúng ta sẽ thay thế nó bằng kẹo đặc biệt, hãy giả lập việc phá hủy
//        if (board.GetCandy(candyPos.x, candyPos.y) == targetCandy.gameObject)
//        {
//            board.SetCandy(candyPos.x, candyPos.y, null); // Đặt vị trí này thành null trong board logic
//        }
//        Destroy(targetCandy.gameObject); // Xóa GameObject cũ

//        // 3. Tạo kẹo đặc biệt mới tại vị trí đó
//        GameObject newSpecialCandyGO = null;
//        ISpecialCandy specialCandyScript = null;
//        string newCandyTag = null; // Tag cho kẹo mới

//        // Chọn prefab và tag dựa trên loại kẹo đặc biệt muốn debug
//        switch (debugSpecialCandyType)
//        {
//            case SpecialCandyType.StrippedCandy:
//                angle = strippedCandyIsHorizontal ? 0f : 90f;
//                GameObject strippedPrefab = gameManager.strippedCandyPrefab;
//                newSpecialCandyGO = Instantiate(strippedPrefab, board.GetWorldPosition(candyPos.x, candyPos.y), Quaternion.Euler(0f,0f,angle));
//                StrippedCandy stripped = newSpecialCandyGO.GetComponent<StrippedCandy>();
//                if (stripped != null)
//                {
//                    stripped.SetDirection(strippedCandyIsHorizontal);
//                    stripped.tag = targetCandy.tag; // Giữ nguyên màu gốc
//                    specialCandyScript = stripped;
//                    newCandyTag = stripped.tag; // Có thể đặt tag riêng cho kẹo sọc
//                }
//                break;
//            case SpecialCandyType.WrappedCandy:
//                newSpecialCandyGO = Instantiate(gameManager.wrappedCandyPrefab, board.GetWorldPosition(candyPos.x, candyPos.y), Quaternion.identity);
//                WrappedCandy wrapped = newSpecialCandyGO.GetComponent<WrappedCandy>();
//                if (wrapped != null)
//                {
//                    wrapped.tag = targetCandy.tag; // Giữ nguyên màu gốc
//                    specialCandyScript = wrapped;
//                    newCandyTag = wrapped.tag; // Có thể đặt tag riêng cho kẹo bọc
//                }
//                break;
//            case SpecialCandyType.ColorBomb:
//                newSpecialCandyGO = Instantiate(gameManager.colorBombPrefab, board.GetWorldPosition(candyPos.x, candyPos.y), Quaternion.identity);
//                ColorBomb colorBomb = newSpecialCandyGO.GetComponent<ColorBomb>();
//                if (colorBomb != null)
//                {
//                    // Bom màu không có BaseColorTag theo cách truyền thống, nhưng có thể gán nếu cần
//                    // colorBomb.BaseColorTag = "ColorBomb"; // hoặc một tag riêng biệt
//                    specialCandyScript = colorBomb;
//                    newCandyTag = colorBomb.tag; // Thường là "ColorBomb"
//                }
//                break;
//            default:
//                Debug.LogWarning("Debug type not handled: " + debugSpecialCandyType);
//                yield break;
//        }

//        if (newSpecialCandyGO != null && specialCandyScript != null)
//        {
//            // Thiết lập vị trí logic của kẹo mới trên bảng
//            board.SetCandy(candyPos.x, candyPos.y, newSpecialCandyGO);
//            Candy newCandyComponent = newSpecialCandyGO.GetComponent<Candy>();
//            if (newCandyComponent != null)
//            {
//                newCandyComponent.Init(candyPos.x, candyPos.y); // Khởi tạo vị trí logic
//                //newCandyComponent.X = candyPos.x;
//                //newCandyComponent.Y = candyPos.y;
//                newCandyComponent.gameObject.tag = newCandyTag != null ? newCandyTag : newSpecialCandyGO.tag; // Cập nhật tag
//            }

//            Debug.Log($"Created new {newSpecialCandyGO.tag} special candy at ({candyPos.x},{candyPos.y}). Activating...");

//            // 4. Kích hoạt kẹo đặc biệt mới tạo
//            // ColorBomb cần targetTag, có thể dùng tag của viên kẹo cũ nếu muốn
//            string activationTargetTag = null;
//            if (debugSpecialCandyType == SpecialCandyType.ColorBomb)
//            {
//                // Đối với debug, bạn có thể cứng hóa một màu hoặc lấy từ kẹo gốc
//                activationTargetTag = targetCandy.tag; // Phá hủy màu của kẹo gốc
//                // Hoặc: activationTargetTag = "Blue";
//            }

//            // Gọi Activate của kẹo đặc biệt
//            yield return specialCandyScript.Activate(board, fxManager, activationTargetTag);

//            // Do các kẹo đặc biệt tự báo cáo qua GameEvents,
//            // GameManager sẽ tự động xử lý các cascade sau đó.
//            // Chúng ta chỉ cần đảm bảo rằng GameManager biết rằng có một "lượt" mới cần xử lý.
//            // Điều này có thể được thực hiện bằng cách gọi lại ProcessBoardRoutine
//            // với một MatchResult giả định hoặc trực tiếp trigger nó.

//            // CÁCH AN TOÀN NHẤT: Bắt đầu một cascade giả định.
//            // (Bạn có thể cần sửa đổi GameManager để có một hàm public StartCascade(MatchResult) )
//            // Hiện tại, chúng ta sẽ giả định GameManager đang chờ một match result để xử lý.

//            // Tạo một MatchResult giả định chứa viên kẹo đặc biệt này là "match"
//            MatchResult debugMatchResult = new MatchResult();
//            debugMatchResult.MatchedCandies.Add(newSpecialCandyGO);
//            // debugMatchResult.SpecialCandiesToCreate sẽ trống vì chúng ta đã tạo nó rồi

//            // Kích hoạt lại ProcessBoardRoutine của GameManager
//            if (gameManager != null)
//            {
//                // Kiểm tra xem ProcessBoardRoutine có phải là public không
//                // Nếu không, bạn cần tạo một wrapper public cho nó.
//                // Hoặc đơn giản là GameManager luôn lắng nghe các event và tự kích hoạt.

//                // Nếu ProcessBoardRoutine được gọi từ bên ngoài, nó thường cần một MatchResult
//                // Hàm này sẽ giả lập việc một kẹo đặc biệt được "match" và kích hoạt.
//                // gameManager.StartCoroutine(gameManager.ProcessBoardRoutine(debugMatchResult)); 
//                // ^ Dòng này chỉ hoạt động nếu ProcessBoardRoutine là public.

//                // CÁCH TỐT HƠN VỚI HỆ THỐNG EVENT:
//                // Vì SpecialCandy.Activate đã gọi GameEvents.ReportSpecialCandyActivation,
//                // GameManager sẽ tự động thu thập thông tin vào _allCandiesToDestroyThisTurn.
//                // Bây giờ chúng ta cần một cách để bảo GameManager "hãy bắt đầu xử lý cascade ngay bây giờ".

//                // Phương thức tốt nhất là GameManager có một hàm StartProcessing()
//                // hoặc nó tự động kích hoạt ProcessBoardRoutine khi có sự thay đổi.

//                // Giả định GameManager của bạn sẽ tự kích hoạt ProcessBoardRoutine khi có một match mới
//                // hoặc khi _allCandiesToDestroyThisTurn có dữ liệu.
//                // Nếu không, bạn cần một hàm trong GameManager như:
//                // public void TriggerCascadeProcessing() {
//                //     if (!IsProcessingBoard) {
//                //         StartCoroutine(ProcessBoardRoutine(new MatchResult())); // Bắt đầu với match rỗng để nó kiểm tra _allCandiesToDestroyThisTurn
//                //     }
//                // }
//                // Hoặc bạn có thể gọi ProcessBoardRoutine trực tiếp nếu nó là public, 
//                // nhưng đó không phải là cách tốt nhất cho thiết kế.

//                // Để đơn giản cho mục đích debug:
//                // Nếu GameManager không tự bắt đầu ProcessBoardRoutine,
//                // bạn có thể buộc nó bắt đầu một coroutine mới:
//                // Nếu ProcessBoardRoutine là private, bạn không thể gọi trực tiếp.
//                // Bạn có thể cần một hàm public để StartCoroutine nó.

//                // Tùy chọn 1: Nếu `ProcessBoardRoutine` được gọi khi có MatchResult (như hiện tại)
//                // Thì bạn cần tạo một MatchResult có chứa viên kẹo đặc biệt này để kích hoạt lại game loop.
//                // Tuy nhiên, vì kẹo đã kích hoạt trong ConvertAndActivateSpecialCandy,
//                // và nó đã báo cáo qua event, GameManager đã có thông tin.
//                // Vấn đề là GameManager cần được báo rằng có một "lượt" mới.

//                // CÁCH AN TOÀN NHẤT CHO DEBUG:
//                // Gọi một hàm trong GameManager để "ép" nó bắt đầu xử lý lại.
//                // Bạn cần thêm hàm này vào Match3GameManager:
//                // public void ForceProcessBoard() {
//                //     if (!IsProcessingBoard) {
//                //         StartCoroutine(ProcessBoardRoutine(new MatchResult())); // Bắt đầu với MatchResult rỗng để kích hoạt kiểm tra _allCandiesToDestroyThisTurn
//                //     }
//                // }
//                // Sau đó, trong CandyDebugTester:
//                gameManager.ForceProcessBoard();
//            }
//        }
//    }
//}


// Scripts/Debug/CandyDebugTester.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem; // Import namespace cho Input System

public class CandyDebugTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Match3GameManager gameManager; // Kéo GameManager vào đây trong Inspector
    [SerializeField] private Board board; // Kéo Board vào đây trong Inspector
    [SerializeField] private FXManager fxManager; // Kéo FXManager vào đây trong Inspector

    [Header("Debug Settings")]
    [Tooltip("Type of special candy to create on right-click.")]
    [SerializeField] private SpecialCandyType debugSpecialCandyType = SpecialCandyType.StrippedCandy;
    [SerializeField] private bool strippedCandyIsHorizontal = true; // Chỉ có tác dụng nếu debugSpecialCandyType là StrippedCandy

    private Camera mainCamera;
    private GameInputActions _gameInputActions; // Tham chiếu đến Input Actions Asset

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<Match3GameManager>();
        if (board == null) board = FindFirstObjectByType<Board>();
        if (fxManager == null) fxManager = FindFirstObjectByType<FXManager>();
        mainCamera = Camera.main;

        if (gameManager == null || board == null || fxManager == null || mainCamera == null)
        {
            Debug.LogError("CandyDebugTester: Missing required references. Please assign GameManager, Board, FXManager, and ensure a Main Camera is in the scene.");
            this.enabled = false;
            return; // Thoát sớm nếu thiếu reference
        }

        // Khởi tạo Input Actions
        _gameInputActions = new GameInputActions();
    }

    void OnEnable()
    {
        _gameInputActions.Gameplay.Enable();
        // Đăng ký callback cho sự kiện DebugSpecialCandy.started (khi bắt đầu nhấn chuột phải/phím debug)
        _gameInputActions.Gameplay.DebugSpecialCandy.started += OnDebugSpecialCandyStarted;
    }

    void OnDisable()
    {
        // Hủy đăng ký callback để tránh lỗi và rò rỉ bộ nhớ
        _gameInputActions.Gameplay.DebugSpecialCandy.started -= OnDebugSpecialCandyStarted;
        _gameInputActions.Gameplay.Disable();
    }

    // Callback cho Input Action "DebugSpecialCandy"
    private void OnDebugSpecialCandyStarted(InputAction.CallbackContext context)
    {
        // Đảm bảo game không đang trong quá trình xử lý cascade
        if (gameManager != null && gameManager.IsProcessingBoard)
        {
            Debug.Log("CandyDebugTester: Game is currently processing board. Cannot create special candy.");
            return;
        }

        // Lấy vị trí chuột từ Input Action "TouchPosition" (nơi chuột đang ở)
        // Đây là cách đúng để lấy vị trí con trỏ hiện tại với Input System.
        Vector2 screenPosition = _gameInputActions.Gameplay.TouchPosition.ReadValue<Vector2>();
        Vector2 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);

        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);

        if (hit.collider != null)
        {
            GameObject hitCandy = hit.collider.gameObject;
            Candy candyComponent = hitCandy.GetComponent<Candy>();

            // Giả sử các kẹo có một tag cụ thể, ví dụ "Candy", nếu không thì kiểm tra null
            // if (candyComponent != null && hit.collider.CompareTag("Candy")) // Ví dụ kiểm tra tag
            if (candyComponent != null) // Hoặc chỉ cần kiểm tra component Candy
            {
                Debug.Log($"DebugTester: Right-clicked/Debug key pressed on candy at ({candyComponent.X},{candyComponent.Y}). Converting to {debugSpecialCandyType}.");
                StartCoroutine(ConvertAndActivateSpecialCandy(candyComponent));
            }
        }
    }

    private IEnumerator ConvertAndActivateSpecialCandy(Candy targetCandy)
    {
        float angle = 0f; // Góc xoay cho kẹo sọc, mặc định là 0 (ngang)
        Vector2Int candyPos = new Vector2Int(targetCandy.X, targetCandy.Y);

        // Đảm bảo kẹo vẫn còn ở vị trí đó trước khi phá hủy
        if (board.GetCandy(candyPos.x, candyPos.y) == targetCandy.gameObject)
        {
            board.SetCandy(candyPos.x, candyPos.y, null);
        }
        Destroy(targetCandy.gameObject);

        GameObject newSpecialCandyGO = null;
        ISpecialCandy specialCandyScript = null;
        string newCandyTag = null;

        switch (debugSpecialCandyType)
        {
            case SpecialCandyType.StrippedCandy:
                angle = strippedCandyIsHorizontal ? 0f : 90f;
                GameObject strippedPrefab = gameManager.strippedCandyPrefab;
                newSpecialCandyGO = Instantiate(strippedPrefab, board.GetWorldPosition(candyPos.x, candyPos.y), angle == 0 ? Quaternion.identity : Quaternion.Euler(0f, 0f, angle));
                StrippedCandy stripped = newSpecialCandyGO.GetComponent<StrippedCandy>();
                if (stripped != null)
                {
                    stripped.SetDirection(strippedCandyIsHorizontal);
                    Debug.Log($"strippedCandyIsHorizontal : {strippedCandyIsHorizontal} ||||| stripped is :{stripped.IsHorizontalStrike}");
                    stripped.tag = targetCandy.tag;
                    specialCandyScript = stripped;
                    newCandyTag = stripped.tag;
                }
                break;
            case SpecialCandyType.WrappedCandy:
                newSpecialCandyGO = Instantiate(gameManager.wrappedCandyPrefab, board.GetWorldPosition(candyPos.x, candyPos.y), Quaternion.identity);
                WrappedCandy wrapped = newSpecialCandyGO.GetComponent<WrappedCandy>();
                if (wrapped != null)
                {
                    wrapped.tag = targetCandy.tag;
                    specialCandyScript = wrapped;
                    newCandyTag = wrapped.tag;
                }
                break;
            case SpecialCandyType.ColorBomb:
                newSpecialCandyGO = Instantiate(gameManager.colorBombPrefab, board.GetWorldPosition(candyPos.x, candyPos.y), Quaternion.identity);
                ColorBomb colorBomb = newSpecialCandyGO.GetComponent<ColorBomb>();
                if (colorBomb != null)
                {
                    specialCandyScript = colorBomb;
                    newCandyTag = colorBomb.tag;
                }
                break;
            default:
                Debug.LogWarning("CandyDebugTester: Debug type not handled: " + debugSpecialCandyType);
                yield break;
        }

        if (newSpecialCandyGO != null && specialCandyScript != null)
        {
            board.SetCandy(candyPos.x, candyPos.y, newSpecialCandyGO);
            Candy newCandyComponent = newSpecialCandyGO.GetComponent<Candy>();
            if (newCandyComponent != null)
            {
                newCandyComponent.Init(candyPos.x, candyPos.y); // Khởi tạo vị trí logic
                //newCandyComponent.X = candyPos.x;
                //newCandyComponent.Y = candyPos.y;
                newCandyComponent.gameObject.tag = newCandyTag != null ? newCandyTag : newSpecialCandyGO.tag;
            }

            Debug.Log($"DebugTester: Created new {newSpecialCandyGO.tag} special candy at ({candyPos.x},{candyPos.y}). Activating...");

            string activationTargetTag = null;
            if (debugSpecialCandyType == SpecialCandyType.ColorBomb)
            {
                activationTargetTag = targetCandy.tag;
            }

            yield return specialCandyScript.Activate(board, fxManager, activationTargetTag);

            // Yêu cầu GameManager bắt đầu xử lý cascade sau khi kẹo debug được kích hoạt
            if (gameManager != null)
            {
                gameManager.ForceProcessBoard();
            }
        }
    }
}