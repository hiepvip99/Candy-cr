using UnityEngine;
using UnityEngine.InputSystem; // Import namespace cho Input System

public class CandyInputHandler : MonoBehaviour // Có thể implement IInputService nếu phức tạp hơn
{
    // --- Dependencies (tham chiếu) ---
    private IBoard _board;
    private Match3GameManager _gameManager;

    // --- Input System Variables ---
    private GameInputActions _gameInputActions; // Tham chiếu đến Input Actions Asset đã tạo
    private GameObject _selectedCandy = null;   // Viên kẹo đầu tiên được chọn
    private Vector2 _firstTouchWorldPosition; // Vị trí chạm ban đầu trong không gian game

    // --- Unity Lifecycle Methods ---
    void Awake()
    {
        // Khởi tạo Input Actions
        _gameInputActions = new GameInputActions();

        // Gán tham chiếu tới Board và GameManager.
        // Tốt hơn là nên inject các dependency này thay vì dùng FindFirstObjectByType.
        // Ví dụ: làm thuộc tính [SerializeField] để kéo thả từ Inspector.
        _board = FindFirstObjectByType<Board>();
        _gameManager = FindFirstObjectByType<Match3GameManager>();

        if (_board == null || _gameManager == null)
        {
            Debug.LogError("CandyInputHandler: Missing references to Board or GameManager!");
        }
    }

    void OnEnable()
    {
        // Bật Input Action Map "Gameplay" khi script được kích hoạt
        _gameInputActions.Gameplay.Enable();

        // Đăng ký callback cho sự kiện TouchPress.started (khi bắt đầu nhấn/chạm)
        _gameInputActions.Gameplay.TouchPress.started += OnTouchPressStarted;
        // Đăng ký callback cho sự kiện TouchPress.canceled (khi nhả nhấn/chạm)
        // Lưu ý: Canceled event xảy ra khi nút được nhả ra hoặc khi tương tác bị hủy.
        _gameInputActions.Gameplay.TouchPress.canceled += OnTouchPressCanceled;
    }

    void OnDisable()
    {
        // Hủy đăng ký callback để tránh lỗi và rò rỉ bộ nhớ
        _gameInputActions.Gameplay.TouchPress.started -= OnTouchPressStarted;
        _gameInputActions.Gameplay.TouchPress.canceled -= OnTouchPressCanceled;

        // Tắt Input Action Map "Gameplay" khi script bị vô hiệu hóa
        _gameInputActions.Gameplay.Disable();
    }

    // --- Input Callbacks ---

    private void OnTouchPressStarted(InputAction.CallbackContext context)
    {
        if (_gameManager.IsProcessingBoard) return; // Nếu game đang xử lý, không nhận input

        // Lấy vị trí chuột/chạm hiện tại từ Input Action "TouchPosition"
        Vector2 screenPosition = _gameInputActions.Gameplay.TouchPosition.ReadValue<Vector2>();
        _firstTouchWorldPosition = Camera.main.ScreenToWorldPoint(screenPosition);

        // Raycast để kiểm tra xem có chạm vào viên kẹo nào không
        RaycastHit2D hit = Physics2D.Raycast(_firstTouchWorldPosition, Vector2.zero);

        if (hit.collider != null && !hit.collider.CompareTag("Untagged")) // Giả sử kẹo có tag "Candy"
        {
            _selectedCandy = hit.collider.gameObject;
        }
    }

    private void OnTouchPressCanceled(InputAction.CallbackContext context)
    {
        if (_gameManager.IsProcessingBoard || _selectedCandy == null)
        {
            _selectedCandy = null; // Đảm bảo reset nếu không có gì được chọn hợp lệ
            return;
        }

        // Lấy vị trí kết thúc vuốt
        Vector2 finalTouchScreenPosition = _gameInputActions.Gameplay.TouchPosition.ReadValue<Vector2>();
        Vector2 finalTouchWorldPosition = Camera.main.ScreenToWorldPoint(finalTouchScreenPosition);

        // Tính toán hướng vuốt
        Vector2 swipeDirection = (finalTouchWorldPosition - _firstTouchWorldPosition).normalized;

        // Xác định kẹo lân cận dựa trên hướng vuốt
        Candy selectedCandyScript = _selectedCandy.GetComponent<Candy>();
        if (selectedCandyScript == null)
        {
            _selectedCandy = null;
            return;
        }

        int x = selectedCandyScript.X;
        int y = selectedCandyScript.Y;
        int targetX = x;
        int targetY = y;

        // Ngưỡng để xác định hướng vuốt (có thể điều chỉnh)
        const float swipeThreshold = 0.5f;

        if (Mathf.Abs(swipeDirection.x) > Mathf.Abs(swipeDirection.y))
        {
            // Vuốt ngang
            if (swipeDirection.x > swipeThreshold) targetX = x + 1; // Vuốt phải
            else if (swipeDirection.x < -swipeThreshold) targetX = x - 1; // Vuốt trái
        }
        else
        {
            // Vuốt dọc
            if (swipeDirection.y > swipeThreshold) targetY = y + 1; // Vuốt lên
            else if (swipeDirection.y < -swipeThreshold) targetY = y - 1; // Vuốt xuống
        }

        // Kiểm tra xem viên kẹo đích có hợp lệ không (đã có sự thay đổi vị trí)
        if (targetX != x || targetY != y)
        {
            // Gọi GameManager để yêu cầu hoán đổi
            _gameManager.RequestSwapCandies(x, y, targetX, targetY);
        }

        _selectedCandy = null; // Reset kẹo đã chọn sau khi xử lý
    }
}