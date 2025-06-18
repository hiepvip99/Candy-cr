using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Match3GameManager : MonoBehaviour
{
    // Tham chiếu đến các implementation cụ thể (sẽ được gán trong Inspector hoặc Bootstrap)
    public Board boardInstance; // Kéo thả GameObject có script Board vào đây
    public MatchFinder matchFinderInstance; // Kéo thả GameObject có script MatchFinder vào đây
    public FXManager fxManagerInstance; // Kéo thả GameObject có script FXManager vào đây
    public ScoreManager scoreManagerInstance; // Kéo thả GameObject có script ScoreManager vào đây

    private IBoard _board;
    private IMatchFinder _matchFinder;
    private IFXManager _fxManager;
    private IScoreManager _scoreManager;

    // Biến cờ trạng thái game, có thể truy cập từ bên ngoài (CandyInputHandler)
    public bool IsProcessingBoard { get; private set; } = false;

    // Các biến cài đặt game tổng quát
    public int boardWidth = 8;
    public int boardHeight = 8;
    public GameObject[] candyPrefabs; // Các prefab kẹo
    public GameObject tilePrefab;     // Prefab ô gạch
    public GameObject strippedCandyPrefab;
    public GameObject wrappedCandyPrefab;
    public GameObject colorBombPrefab;

    void Awake()
    {
        // Gán các tham chiếu từ Inspector. Nếu không gán, tìm trong scene.
        _board = boardInstance;
        if (_board == null) _board = FindFirstObjectByType<Board>();
        _matchFinder = matchFinderInstance;
        if (_matchFinder == null) _matchFinder = FindFirstObjectByType<MatchFinder>();
        _fxManager = fxManagerInstance;
        if (_fxManager == null) _fxManager = FindFirstObjectByType<FXManager>();
        _scoreManager = scoreManagerInstance;
        if (_scoreManager == null) _scoreManager = FindFirstObjectByType<ScoreManager>();


        if (_board == null || _matchFinder == null || _fxManager == null || _scoreManager == null)
        {
            Debug.LogError("Match3GameManager: Missing required manager references!");
        }
    }

    void Start()
    {
        // Khởi tạo Camera
        Camera mainCamera = Camera.main;
        mainCamera.transform.position = new Vector3((boardWidth - 1) / 2f, (boardHeight - 1) / 2f, -10);
        mainCamera.orthographicSize = Mathf.Max(boardWidth, boardHeight) / 2f + 1f; // Thêm 1f để có chút lề

        // Khởi tạo bảng game thông qua Interface
        _board.Initialize(boardWidth, boardHeight, candyPrefabs, tilePrefab);

        IsProcessingBoard = false; // Ban đầu game sẵn sàng nhận input
    }

    /// <summary>
    /// Được gọi từ CandyInputHandler khi người chơi yêu cầu hoán đổi kẹo.
    /// </summary>
    public void RequestSwapCandies(int x1, int y1, int x2, int y2)
    {
        if (IsProcessingBoard) return; // Nếu game đang bận, bỏ qua input
        IsProcessingBoard = true; // Chặn input

        StartCoroutine(HandleSwapAndMatches(x1, y1, x2, y2));
    }

    private IEnumerator HandleSwapAndMatches(int x1, int y1, int x2, int y2)
    {
        GameObject candy1GO = _board.GetCandy(x1, y1);
        GameObject candy2GO = _board.GetCandy(x2, y2);

        if (candy1GO == null || candy2GO == null)
        {
            Debug.LogWarning("One of the candies is null during swap request.");
            IsProcessingBoard = false;
            yield break;
        }

        // Hoán đổi dữ liệu logic trên bảng
        _board.SwapCandiesData(x1, y1, x2, y2);

        // Chờ animation hoán đổi hoàn tất
        yield return _fxManager.AnimateSwap(candy1GO, candy2GO,
            _board.GetWorldPosition(x1, y1), _board.GetWorldPosition(x2, y2));

        // Kiểm tra match sau khi hoán đổi
        HashSet<GameObject> initialMatches = _matchFinder.FindAllMatches(_board.Candies, _board.Width, _board.Height);

        if (initialMatches.Count > 0)
        {
            // Có match, bắt đầu chu trình phá hủy/đổ kẹo/lấp đầy
            yield return StartCoroutine(ProcessBoardRoutine(initialMatches));
        }
        else
        {
            // Không có match, hoàn tác hoán đổi và animation trở lại
            Debug.Log("No match found after swap, swapping back.");
            _board.SwapCandiesData(x1, y1, x2, y2); // Hoán tác dữ liệu logic
            yield return _fxManager.AnimateSwap(candy1GO, candy2GO,
                _board.GetWorldPosition(x2, y2), _board.GetWorldPosition(x1, y1)); // Animation trở lại
        }

        IsProcessingBoard = false; // Mở lại input sau khi mọi thứ ổn định
        Debug.Log("Board processing finished. Player input enabled.");
    }

    ///// <summary>
    ///// Coroutine xử lý chu trình phá hủy, đổ kẹo, lấp đầy và cascade (dây chuyền).
    ///// </summary>
    ///// <param name="currentMatches">Tập hợp các GameObject kẹo đã match.</param>
    //private IEnumerator ProcessBoardRoutine(HashSet<GameObject> currentMatches)
    //{
    //    while (currentMatches.Count > 0)
    //    {
    //        // 1. Kích hoạt hiệu ứng và chờ animation phá hủy hoàn tất
    //        yield return _fxManager.AnimateDestroyMatches(currentMatches);

    //        // 2. Xóa kẹo đã match khỏi mảng logic của Board
    //        _board.DestroyCandies(currentMatches);

    //        // 3. Cộng điểm cho người chơi (giả sử mỗi kẹo match được 10 điểm)
    //        _scoreManager.AddScore(currentMatches.Count * 10);
    //        _fxManager.PlayMatchSound(); // Phát âm thanh match

    //        // 4. Áp dụng trọng lực: kẹo rơi xuống để lấp đầy chỗ trống
    //        List<Vector2Int> droppedPositions = _board.ApplyGravity();
    //        yield return _fxManager.AnimateDropCandies(droppedPositions, _board);

    //        // 5. Lấp đầy chỗ trống bằng kẹo mới từ trên cao
    //        List<Vector2Int> newCandyPositions = _board.FillEmptySpots(candyPrefabs);
    //        yield return _fxManager.AnimateNewCandiesDrop(newCandyPositions, _board);

    //        // 6. Kiểm tra lại xem có match mới nào được tạo ra sau khi kẹo rơi và lấp đầy (cascade)
    //        currentMatches = _matchFinder.FindAllMatches(_board.Candies, _board.Width, _board.Height);
    //    }
    //}

    private IEnumerator ProcessBoardRoutine(HashSet<GameObject> currentMatches)
    {
        while (currentMatches.Count > 0)
        {
            // 1. Xác định kẹo đặc biệt sẽ được tạo ra (trước khi phá hủy kẹo cũ)
            // Lấy các vị trí mà tại đó match xảy ra
            Dictionary<Vector2Int, SpecialCandyType> specialCandiesToCreate =
                DetermineSpecialCandies(currentMatches, _board.Candies, _board.Width, _board.Height);

            // 2. Kích hoạt hiệu ứng và chờ animation phá hủy hoàn tất
            yield return _fxManager.AnimateDestroyMatches(currentMatches);

            // 3. Xóa kẹo đã match khỏi mảng logic của Board
            _board.DestroyCandies(currentMatches);

            // 4. Cộng điểm cho người chơi
            _scoreManager.AddScore(currentMatches.Count * 10);
            _fxManager.PlayMatchSound();

            // 5. Tạo và đặt các kẹo đặc biệt vào bảng
            yield return CreateSpecialCandies(specialCandiesToCreate);


            // 6. Áp dụng trọng lực: kẹo rơi xuống để lấp đầy chỗ trống
            List<Vector2Int> droppedPositions = _board.ApplyGravity();
            yield return _fxManager.AnimateDropCandies(droppedPositions, _board);

            // 7. Lấp đầy chỗ trống bằng kẹo mới từ trên cao
            List<Vector2Int> newCandyPositions = _board.FillEmptySpots(candyPrefabs);
            yield return _fxManager.AnimateNewCandiesDrop(newCandyPositions, _board);

            // 8. Kiểm tra lại xem có match mới nào được tạo ra sau khi kẹo rơi và lấp đầy (cascade)
            currentMatches = _matchFinder.FindAllMatches(_board.Candies, _board.Width, _board.Height);
        }
    }

    /// <summary>
    /// Xác định loại kẹo đặc biệt sẽ tạo và vị trí của chúng.
    /// </summary>
    /// <param name="matchedCandies">Tất cả các kẹo đã match trong lần này.</param>
    /// <param name="currentBoardCandies">Bảng kẹo hiện tại (để kiểm tra lại các line).</param>
    private Dictionary<Vector2Int, SpecialCandyType> DetermineSpecialCandies(
        HashSet<GameObject> matchedCandies, GameObject[,] currentBoardCandies, int width, int height)
    {
        Dictionary<Vector2Int, SpecialCandyType> specialCandiesToCreate = new Dictionary<Vector2Int, SpecialCandyType>();

        // Tìm giao điểm của các match (nơi kẹo đặc biệt có thể được tạo)
        // Đây là cách đơn giản để tìm vị trí kẹo đặc biệt.
        // Có thể cần logic phức tạp hơn để chọn vị trí ưu tiên nếu có nhiều giao điểm.
        foreach (GameObject matchedCandy in matchedCandies)
        {
            Candy candyScript = matchedCandy.GetComponent<Candy>();
            if (candyScript == null) continue;

            int x = candyScript.X;
            int y = candyScript.Y;
            string tag = matchedCandy.tag;

            // Hỏi MatchFinder xem vị trí này có tạo thành kẹo đặc biệt không
            SpecialCandyType type = _matchFinder.GetSpecialCandyType(currentBoardCandies, x, y, tag);

            // Nếu vị trí này tạo ra một kẹo đặc biệt, thêm vào danh sách
            if (type != SpecialCandyType.None && !specialCandiesToCreate.ContainsKey(new Vector2Int(x, y)))
            {
                // Nếu có nhiều loại match tại 1 vị trí (ví dụ: match 5 vừa là L/T),
                // cần ưu tiên loại kẹo đặc biệt cao cấp hơn (ColorBomb > Wrapped > Stripped)
                SpecialCandyType existingType;
                if (specialCandiesToCreate.TryGetValue(new Vector2Int(x, y), out existingType))
                {
                    if (type > existingType) // Ưu tiên loại cao cấp hơn
                    {
                        specialCandiesToCreate[new Vector2Int(x, y)] = type;
                    }
                }
                else
                {
                    specialCandiesToCreate.Add(new Vector2Int(x, y), type);
                }
            }
        }
        return specialCandiesToCreate;
    }

    /// <summary>
    /// Tạo các kẹo đặc biệt đã xác định và đặt chúng vào Board.
    /// </summary>
    private IEnumerator CreateSpecialCandies(Dictionary<Vector2Int, SpecialCandyType> specialCandiesToCreate)
    {
        foreach (var entry in specialCandiesToCreate)
        {
            Vector2Int pos = entry.Key;
            SpecialCandyType type = entry.Value;

            GameObject specialCandyPrefabToUse = null;
            string newTag = ""; // Tag của kẹo đặc biệt sẽ khác với kẹo thường

            switch (type)
            {
                case SpecialCandyType.StrippedCandy:
                    specialCandyPrefabToUse = strippedCandyPrefab;
                    newTag = "StrippedCandy"; // Ví dụ tag
                    break;
                case SpecialCandyType.WrappedCandy:
                    specialCandyPrefabToUse = wrappedCandyPrefab;
                    newTag = "WrappedCandy"; // Ví dụ tag
                    break;
                case SpecialCandyType.ColorBomb:
                    specialCandyPrefabToUse = colorBombPrefab;
                    newTag = "ColorBomb"; // Ví dụ tag
                    break;
                case SpecialCandyType.None:
                    continue; // Bỏ qua
            }

            if (specialCandyPrefabToUse != null)
            {
                // Đảm bảo vị trí này đang trống (do kẹo cũ đã bị phá hủy)
                if (_board.GetCandy(pos.x, pos.y) == null)
                {
                    Vector2 worldPos = _board.GetWorldPosition(pos.x, pos.y);
                    GameObject newSpecialCandy = Instantiate(specialCandyPrefabToUse, worldPos, Quaternion.identity);
                    newSpecialCandy.transform.parent = boardInstance.transform; // Đặt làm con của _board

                    // Cập nhật tag của GameObject cho kẹo đặc biệt
                    newSpecialCandy.tag = newTag;

                    _board.SetCandy(pos.x, pos.y, newSpecialCandy); // Đặt kẹo đặc biệt vào board logic
                    newSpecialCandy.GetComponent<Candy>()?.Init(pos.x, pos.y); // Khởi tạo script Candy

                    // Có thể thêm hiệu ứng/animation khi kẹo đặc biệt xuất hiện
                    // yield return _fxManager.AnimateSpecialCandySpawn(newSpecialCandy); // Ví dụ
                }
            }
            else
            {
                Debug.LogWarning($"Missing prefab for special candy type: {type}");
            }
        }
        yield return null; // Chờ 1 frame để đảm bảo mọi thứ được cập nhật
    }
}

