using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public GameObject strippedCandyPrefab;// mặc định là kẹo sọc dọc
    public GameObject wrappedCandyPrefab;
    public GameObject colorBombPrefab;

    // Các biến nội bộ để quản lý trạng thái game
    // Thay đổi kiểu của list này từ Vector2Int sang GameObject
    private List<GameObject> _wrappedCandiesPendingSecondExplosion = new List<GameObject>();
    private HashSet<GameObject> _allCandiesToDestroyThisTurn = new HashSet<GameObject>();
    private Dictionary<Vector2Int, SpecialCandyCreationInfo> _specialCandiesToCreateThisTurn =
        new Dictionary<Vector2Int, SpecialCandyCreationInfo>();
    private HashSet<GameObject> _specialCandiesActivatedThisCascade = new HashSet<GameObject>();


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
        if (IsProcessingBoard) return;
        IsProcessingBoard = true;

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

        // Kiểm tra match sau khi hoán đổi, truyền vị trí kẹo đã hoán đổi
        MatchResult matchResult = _matchFinder.FindAllMatches(_board.Candies, _board.Width, _board.Height, new Vector2Int(x2, y2)); // Vị trí cuối cùng của kẹo được chọn
        //Debug.Log($"Match found SpecialCandiesToCreate: {matchResult.SpecialCandiesToCreate.Count} .");
        if (matchResult.MatchedCandies.Count > 0)
        {
            yield return StartCoroutine(ProcessBoardRoutine(matchResult));
        }
        else
        {
            // Không có match, hoàn tác
            Debug.Log("No match found after swap, swapping back.");
            _board.SwapCandiesData(x1, y1, x2, y2);
            yield return _fxManager.AnimateSwap(candy1GO, candy2GO,
                _board.GetWorldPosition(x2, y2), _board.GetWorldPosition(x1, y1));
        }

        IsProcessingBoard = false;
        Debug.Log("Board processing finished. Player input enabled.");
    }
    private IEnumerator ProcessBoardRoutine(MatchResult initialMatchResult)
    {
        IsProcessingBoard = true;

        MatchResult currentMatchResult = initialMatchResult;

        // Reset các danh sách cho mỗi chuỗi cascade mới
        _allCandiesToDestroyThisTurn.Clear();
        _specialCandiesToCreateThisTurn.Clear();
        _wrappedCandiesPendingSecondExplosion.Clear();
        _specialCandiesActivatedThisCascade.Clear(); // <-- RESET ĐÂY!

        do // Vòng lặp chính để xử lý toàn bộ chuỗi cascade
        {
            HashSet<GameObject> candiesToDestroyInCurrentStep = new HashSet<GameObject>();
            Dictionary<Vector2Int, SpecialCandyCreationInfo> specialsToCreateInCurrentStep = new Dictionary<Vector2Int, SpecialCandyCreationInfo>();

            if (currentMatchResult.MatchedCandies.Count > 0)
            {
                specialsToCreateInCurrentStep = new Dictionary<Vector2Int, SpecialCandyCreationInfo>(currentMatchResult.SpecialCandiesToCreate);

                // --- PHẦN SỬA ĐỔI CHÍNH Ở ĐÂY ---
                // HandleMatchedSpecialCandyActivations sẽ kích hoạt các kẹo đặc biệt và thêm chúng vào _specialCandiesActivatedThisCascade
                yield return HandleMatchedSpecialCandyActivations(currentMatchResult.MatchedCandies);

                // Lọc các kẹo đặc biệt đã được kích hoạt khỏi danh sách MatchResult.MatchedCandies
                // để chúng không bị phá hủy ngay lập tức bởi match đó.
                foreach (GameObject matchedCandy in currentMatchResult.MatchedCandies)
                {
                    if (!_specialCandiesActivatedThisCascade.Contains(matchedCandy))
                    {
                        candiesToDestroyInCurrentStep.Add(matchedCandy);
                    }
                    else
                    {
                        // Debug.Log($"Skipping destruction of activated special candy: {matchedCandy.name}");
                    }
                }

                // Các kẹo bị phá hủy bởi special candy (thu thập qua event) vẫn được thêm vào
                //candiesToDestroyInCurrentStep.UnionWith(_allCandiesToDestroyThisTurn);
                //_allCandiesToDestroyThisTurn.Clear();

                // --- THAY ĐỔI QUAN TRỌNG: LỌC _allCandiesToDestroyThisTurn ---
                // Chỉ thêm vào danh sách phá hủy những kẹo KHÔNG phải là Wrapped Candy đang chờ nổ lần 2.
                // Các Wrapped Candy đã được thêm vào _wrappedCandiesPendingSecondExplosion sẽ được xử lý riêng.
                foreach (GameObject candy in _allCandiesToDestroyThisTurn)
                {
                    if (candy != null && !_wrappedCandiesPendingSecondExplosion.Contains(candy))
                    {
                        candiesToDestroyInCurrentStep.Add(candy);
                    }
                    else
                    {
                        // Debug.Log($"Skipping destruction of a Wrapped Candy in _allCandiesToDestroyThisTurn because it's pending second explosion: {candy.name}");
                    }
                }
                _allCandiesToDestroyThisTurn.Clear();
            }
            //else if (_wrappedCandiesPendingSecondExplosion.Count > 0)
            //{
            //    List<Vector2Int> currentSecondExplosions = new List<Vector2Int>(_wrappedCandiesPendingSecondExplosion);
            //    _wrappedCandiesPendingSecondExplosion.Clear();

            //    foreach (Vector2Int pos in currentSecondExplosions)
            //    {
            //        Debug.Log($"Activating Wrapped Candy - Second Explosion at ({pos.x},{pos.y}).");
            //        // Ở đây, WrappedCandy.GetSecondExplosionAffectedCandies sẽ trả về cả viên kẹo bọc tại vị trí đó
            //        // nếu nó vẫn còn (mà giờ nó sẽ còn)
            //        HashSet<GameObject> secondExplosionCandies = WrappedCandy.GetSecondExplosionAffectedCandies(pos, _board);
            //        candiesToDestroyInCurrentStep.UnionWith(secondExplosionCandies);
            //    }
            //}
            // ELSE IF NÀY ĐƯỢC CHỈNH SỬA
            else if (_wrappedCandiesPendingSecondExplosion.Count > 0)
            {
                // Lấy danh sách các GameObject của Wrapped Candy chờ nổ lần 2
                List<GameObject> currentSecondExplosionCandies = new List<GameObject>(_wrappedCandiesPendingSecondExplosion);
                _wrappedCandiesPendingSecondExplosion.Clear();

                foreach (GameObject wrappedCandyGo in currentSecondExplosionCandies)
                {
                    // Đảm bảo viên kẹo vẫn còn tồn tại và là kẹo bọc
                    if (wrappedCandyGo != null && wrappedCandyGo.GetComponent<WrappedCandy>() != null)
                    {
                        // Gọi GetSecondExplosionAffectedCandies với chính GameObject của kẹo bọc
                        HashSet<GameObject> secondExplosionCandies = WrappedCandy.GetSecondExplosionAffectedCandies(wrappedCandyGo, _board);
                        candiesToDestroyInCurrentStep.UnionWith(secondExplosionCandies);
                        // KHÔNG CẦN THÊM wrappedCandyGo VÀO ĐÂY NỮA, VÌ GETSECONDEFFECTEDCANDIES ĐÃ TỰ THÊM NÓ.
                        // Debug.Log($"Activating Wrapped Candy - Second Explosion at ACTUAL position ({wrappedCandyGo.GetComponent<Candy>().X},{wrappedCandyGo.GetComponent<Candy>().Y}).");
                    }
                    else
                    {
                        Debug.LogWarning("Wrapped Candy for second explosion was null or not a Wrapped Candy.");
                    }
                }
            }
            else
            {
                Debug.Log("ProcessBoardRoutine finished: No more matches or pending actions.");
                break;
            }

            // --- Thực hiện Phá hủy, Tạo kẹo, Trọng lực, Lấp đầy ---
            // Phần này không đổi, nhưng giờ candiesToDestroyInCurrentStep đã được lọc đúng.
            if (candiesToDestroyInCurrentStep.Count > 0)
            {
                yield return _fxManager.AnimateDestroyMatches(candiesToDestroyInCurrentStep);
                _board.DestroyCandies(candiesToDestroyInCurrentStep);
            }

            if (specialsToCreateInCurrentStep.Count > 0)
            {
                yield return CreateSpecialCandies(specialsToCreateInCurrentStep);
            }

            List<Vector2Int> droppedPositions = _board.ApplyGravity();
            if (droppedPositions.Count > 0)
            {
                yield return _fxManager.AnimateDropCandies(droppedPositions, _board);
            }

            List<Vector2Int> newCandyPositions = _board.FillEmptySpots(candyPrefabs);
            if (newCandyPositions.Count > 0)
            {
                yield return _fxManager.AnimateNewCandiesDrop(newCandyPositions, _board);
            }

            HashSet<Vector2Int> newlyAffectedPositions = new HashSet<Vector2Int>();
            foreach (var pos in droppedPositions) newlyAffectedPositions.Add(pos);
            foreach (var pos in newCandyPositions) newlyAffectedPositions.Add(pos);

            currentMatchResult = _matchFinder.FindAllMatches(
                _board.Candies, _board.Width, _board.Height,
                swappedCandyPosition: null,
                newlyAffectedPositions: newlyAffectedPositions
            );

        } while (currentMatchResult.MatchedCandies.Count > 0 || _wrappedCandiesPendingSecondExplosion.Count > 0);

        IsProcessingBoard = false;
    }

    /// <summary>
    /// Tạo các kẹo đặc biệt đã xác định và đặt chúng vào Board.
    /// </summary>
    //private IEnumerator CreateSpecialCandies(Dictionary<Vector2Int, SpecialCandyCreationInfo> specialCandiesToCreate)
    //{
    //    float angle = 0;

    //    foreach (var entry in specialCandiesToCreate)
    //    {
    //        Vector2Int pos = entry.Key;
    //        SpecialCandyCreationInfo creationInfo = entry.Value; // Lấy ra struct info

    //        GameObject specialCandyPrefabToUse = null;
    //        //string newTag = "";

    //        switch (creationInfo.Type) // Dùng creationInfo.Type
    //        {
    //            case SpecialCandyType.StrippedCandy:
    //                // Chọn prefab kẹo sọc dựa trên hướng
    //                specialCandyPrefabToUse = strippedCandyPrefab;
    //                angle = creationInfo.IsHorizontalStripped ? 0f : 90f; // Giả sử bạn có biến creationInfo.IsHorizontalStripped để xác định hướng
    //                //specialCandyPrefabToUse = creationInfo.IsHorizontalStripped ? /*horizontalStrippedCandyPrefab*/  :/* verticalStrippedCandyPrefab;*/
    //                //newTag = "StrippedCandy"; // Tag vẫn là "StrippedCandy" cho cả hai loại hướng
    //                break;
    //            case SpecialCandyType.WrappedCandy:
    //                specialCandyPrefabToUse = wrappedCandyPrefab;
    //                //newTag = "WrappedCandy";
    //                break;
    //            case SpecialCandyType.ColorBomb:
    //                specialCandyPrefabToUse = colorBombPrefab;
    //                //newTag = "ColorBomb";
    //                break;
    //            case SpecialCandyType.None:
    //                continue;
    //        }

    //        if (specialCandyPrefabToUse != null)
    //        {
    //            if (_board.GetCandy(pos.x, pos.y) == null)
    //            {
    //                Vector2 worldPos = _board.GetWorldPosition(pos.x, pos.y);
    //                GameObject newSpecialCandy = Instantiate(specialCandyPrefabToUse, worldPos, angle == 0 ? Quaternion.identity : Quaternion.Euler(0f, angle, 0f));
    //                newSpecialCandy.GetComponent<Candy>()?.UpdatePosition(pos.x, pos.y);
    //                newSpecialCandy.transform.parent = boardInstance.transform;

    //                //newSpecialCandy.tag = newTag;

    //                _board.SetCandy(pos.x, pos.y, newSpecialCandy);

    //                // Gán thông tin hướng vào Candy script nếu cần (ví dụ: StrippedCandy.cs)
    //                StrippedCandy strippedCandyScript = newSpecialCandy.GetComponent<StrippedCandy>();
    //                if (strippedCandyScript != null)
    //                {
    //                    strippedCandyScript.SetDirection(creationInfo.IsHorizontalStripped);
    //                }
    //                StrippedCandy strippedCandyScript = newSpecialCandy.GetComponent<StrippedCandy>();
    //                if (strippedCandyScript != null)
    //                {
    //                    strippedCandyScript.SetDirection(creationInfo.IsHorizontalStripped);
    //                }
    //            }
    //        }
    //        else
    //        {
    //            Debug.LogWarning($"Missing prefab for special candy type: {creationInfo.Type} (Horizontal: {creationInfo.IsHorizontalStripped})");
    //        }
    //    }
    //    yield return null;
    //}

    private IEnumerator CreateSpecialCandies(Dictionary<Vector2Int, SpecialCandyCreationInfo> specialCandiesToCreate)
    {
        float angle = 0f; // Biến góc để xoay kẹo đặc biệt nếu cần
        foreach (var entry in specialCandiesToCreate)
        {
            Vector2Int pos = entry.Key;
            SpecialCandyCreationInfo creationInfo = entry.Value;

            GameObject specialCandyPrefabToUse = null;
            string newCandyGameObjectTag = ""; // Đây sẽ là tag được gán cho GameObject của kẹo đặc biệt mới

            switch (creationInfo.Type)
            {
                case SpecialCandyType.StrippedCandy:
                    specialCandyPrefabToUse = strippedCandyPrefab;
                    angle = creationInfo.IsHorizontalStripped ? 0f : 90f;
                    newCandyGameObjectTag = creationInfo.BaseCandyTag; // Sử dụng tag gốc đã lưu
                    break;
                case SpecialCandyType.WrappedCandy:
                    specialCandyPrefabToUse = wrappedCandyPrefab;
                    newCandyGameObjectTag = creationInfo.BaseCandyTag; // Sử dụng tag gốc đã lưu
                    break;
                case SpecialCandyType.ColorBomb:
                    specialCandyPrefabToUse = colorBombPrefab;
                    // Bom màu luôn có tag "ColorBomb" riêng, không phụ thuộc màu gốc
                    newCandyGameObjectTag = "ColorBomb";
                    break;
                case SpecialCandyType.None:
                    continue;
            }

            if (specialCandyPrefabToUse != null)
            {
                // Đảm bảo vị trí pos đã trống do kẹo cũ đã bị phá hủy
                // (Sau khi _board.DestroyCandies(currentMatchResult.MatchedCandies) được gọi)
                // Hoặc nếu HandleSpecialCandyActivations đã xử lý nó
                if (_board.GetCandy(pos.x, pos.y) != null) // Kẹo đặc biệt sinh ra KHÔNG ĐƯỢC bị đè lên kẹo cũ chưa phá hủy
                {
                    Debug.LogWarning($"Attempting to create special candy at {pos} but it's not empty! Current candy: {_board.GetCandy(pos.x, pos.y).name}");
                    // Cần xử lý lỗi hoặc bỏ qua, tùy thuộc vào game design của bạn.
                    continue; // Tạm thời bỏ qua để tránh lỗi runtime
                }

                Vector2 worldPos = _board.GetWorldPosition(pos.x, pos.y);
                //GameObject newSpecialCandy = Instantiate(specialCandyPrefabToUse, worldPos, Quaternion.identity);
                GameObject newSpecialCandy = Instantiate(specialCandyPrefabToUse, worldPos, angle == 0 ? Quaternion.identity : Quaternion.Euler(0f, 0f, angle));
                newSpecialCandy.transform.parent = boardInstance.transform;

                if (newSpecialCandy == null) Debug.LogWarning($"Lỗi chưa tạo ra kẹo mới tại vị trí: {worldPos}");

                // GÁN TAG CHO GAME OBJECT CỦA KẸO ĐẶC BIỆT MỚI TẠO
                newSpecialCandy.tag = newCandyGameObjectTag;

                _board.SetCandy(pos.x, pos.y, newSpecialCandy);
                newSpecialCandy.GetComponent<Candy>()?.Init(pos.x, pos.y);

                StrippedCandy strippedCandyScript = newSpecialCandy.GetComponent<StrippedCandy>();
                if (strippedCandyScript != null)
                {
                    strippedCandyScript.SetDirection(creationInfo.IsHorizontalStripped);
                }
            }
            else
            {
                Debug.LogWarning($"Missing prefab for special candy type: {creationInfo.Type}");
            }
        }
        yield return null;
    }




    private void OnEnable()
    {
        GameEvents.OnSpecialCandyActivated += HandleSpecialCandyActivationEvent;
        GameEvents.OnWrappedCandySecondExplosionNeeded += AddWrappedCandyForSecondExplosion;
    }

    private void OnDisable()
    {
        GameEvents.OnSpecialCandyActivated -= HandleSpecialCandyActivationEvent;
        GameEvents.OnWrappedCandySecondExplosionNeeded -= AddWrappedCandyForSecondExplosion;
    }

    // Hàm xử lý khi một kẹo đặc biệt thông báo đã kích hoạt
    private void HandleSpecialCandyActivationEvent(Vector2Int position, SpecialCandyType type, HashSet<GameObject> affectedCandies, string targetTag)
    {
        // Thêm tất cả kẹo bị ảnh hưởng vào danh sách chung để phá hủy trong lượt này
        _allCandiesToDestroyThisTurn.UnionWith(affectedCandies);

        // Logic để thêm vào specialCandiesToCreateThisTurn đã được MatchFinder xử lý rồi
        // Nên ở đây chúng ta không cần thêm lại.
        // MatchFinder đã điền _specialCandiesToCreateThisTurn vào initialMatchResult.SpecialCandiesToCreate
        // và chúng ta đã lấy nó ở đầu ProcessBoardRoutine.
    }

    // Hàm thêm Wrapped Candy vào danh sách chờ nổ lần 2
    // Cập nhật hàm lắng nghe event để chấp nhận GameObject
    private void AddWrappedCandyForSecondExplosion(GameObject wrappedCandyGo)
    {
        if (wrappedCandyGo != null && !_wrappedCandiesPendingSecondExplosion.Contains(wrappedCandyGo))
        {
            _wrappedCandiesPendingSecondExplosion.Add(wrappedCandyGo);
        }
    }

    /// <summary>
    /// Duyệt qua các kẹo đã match và kích hoạt các kẹo đặc biệt nếu có.
    /// Kẹo đặc biệt sẽ tự report các kẹo nó phá hủy qua GameEvents.OnSpecialCandyActivated.
    /// </summary>
// Cập nhật HandleMatchedSpecialCandyActivations để theo dõi các kẹo đặc biệt đã được kích hoạt
    private IEnumerator HandleMatchedSpecialCandyActivations(HashSet<GameObject> matchedCandies)
    {
        List<Coroutine> activationCoroutines = new List<Coroutine>();
        HashSet<GameObject> processedSpecialCandies = new HashSet<GameObject>(); // Để tránh xử lý trùng lặp trong hàm này

        // Xóa bất kỳ kẹo đặc biệt cũ nào khỏi danh sách theo dõi của cascade này (nếu đã dùng lại)
        // _specialCandiesActivatedThisCascade.Clear(); // Đã clear ở đầu ProcessBoardRoutine

        foreach (GameObject candyGO in matchedCandies)
        {
            if (candyGO == null || processedSpecialCandies.Contains(candyGO)) continue;

            ISpecialCandy specialCandyScript = candyGO.GetComponent<ISpecialCandy>();
            Candy regularCandyScript = candyGO.GetComponent<Candy>(); // Để lấy BaseColorTag cho ColorBomb

            if (specialCandyScript != null)
            {
                string targetTagForColorBomb = null;
                if (specialCandyScript.SpecialType == SpecialCandyType.ColorBomb)
                {
                    // Tìm một kẹo thường trong danh sách match để làm targetTag cho Color Bomb
                    foreach (GameObject otherCandyGO in matchedCandies)
                    {
                        if (otherCandyGO != candyGO && otherCandyGO != null)
                        {
                            Candy otherCandy = otherCandyGO.GetComponent<Candy>();
                            if (otherCandy != null && otherCandy.GetComponent<ISpecialCandy>() == null) // Là kẹo thường
                            {
                                targetTagForColorBomb = otherCandy.tag;
                                break;
                            }
                        }
                    }
                }

                activationCoroutines.Add(StartCoroutine(specialCandyScript.Activate(
                    _board, _fxManager, targetTagForColorBomb
                )));
                processedSpecialCandies.Add(candyGO); // Đánh dấu đã xử lý trong hàm này
                _specialCandiesActivatedThisCascade.Add(candyGO); // <-- THÊM VÀO ĐÂY!
            }
        }

        foreach (Coroutine co in activationCoroutines)
        {
            yield return co;
        }
    }
    /// <summary>
    /// Dành cho debug kẹo đặc biệt, kiểm tra các logic đã hoạt động đúng chưa.
    /// Hàm này cho phép bên ngoài yêu cầu GameManager bắt đầu/tiếp tục quá trình xử lý bảng
    /// </summary> 
    public void ForceProcessBoard()
    {
        if (!IsProcessingBoard)
        {
            Debug.Log("ForceProcessBoard: Starting new ProcessBoardRoutine.");
            // Bắt đầu với một MatchResult rỗng. ProcessBoardRoutine sẽ kiểm tra
            // _allCandiesToDestroyThisTurn và _wrappedCandiesPendingSecondExplosion
            // để biết có gì cần xử lý không.
            StartCoroutine(ProcessBoardRoutine(new MatchResult()));
        }
        else
        {
            Debug.Log("ForceProcessBoard: Board is already processing.");
        }
    }
}

