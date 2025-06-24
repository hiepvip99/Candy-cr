

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
            // Fix for CS1061: Replace 'setParent' with 'SetParent', which is the correct method name in the Transform class.
            newSpecialCandyGO.transform.SetParent(board.transform); // Đặt parent là Board để quản lý tốt hơn
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