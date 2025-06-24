using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.InputSystem.InputControlScheme;

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

        // Lấy thông tin về kẹo trước khi hoán đổi dữ liệu logic
        ISpecialCandy specialCandy1 = candy1GO.GetComponent<ISpecialCandy>();
        ISpecialCandy specialCandy2 = candy2GO.GetComponent<ISpecialCandy>();

        bool isColorBomb1 = (specialCandy1 != null && specialCandy1.SpecialType == SpecialCandyType.ColorBomb);
        bool isColorBomb2 = (specialCandy2 != null && specialCandy2.SpecialType == SpecialCandyType.ColorBomb);

        // Hoán đổi dữ liệu logic trên bảng ngay lập tức
        _board.SwapCandiesData(x1, y1, x2, y2);

        // Chờ animation hoán đổi hoàn tất
        yield return _fxManager.AnimateSwap(candy1GO, candy2GO,
            _board.GetWorldPosition(x1, y1), _board.GetWorldPosition(x2, y2));

        MatchResult matchResult = new MatchResult(); // Khởi tạo null

        // --- LOGIC XỬ LÝ KẾT HỢP COLOR BOMB ---
        if (isColorBomb1 || isColorBomb2)
        {
            GameObject colorBombGO = isColorBomb1 ? candy1GO : candy2GO;
            GameObject otherCandyGO = isColorBomb1 ? candy2GO : candy1GO;

            ISpecialCandy otherSpecialCandy = otherCandyGO.GetComponent<ISpecialCandy>();
            Candy otherRegularCandy = otherCandyGO.GetComponent<Candy>();
            if (otherSpecialCandy != null && otherSpecialCandy.SpecialType == SpecialCandyType.ColorBomb)
            {
                Debug.Log("Color Bomb + Color Bomb: Clearing entire board!");

                HashSet<GameObject> allCandiesOnBoard = new HashSet<GameObject>();
                for (int x = 0; x < _board.Width; x++)
                {
                    for (int y = 0; y < _board.Height; y++)
                    {
                        GameObject candy = _board.GetCandy(x, y);
                        if (candy != null)
                        {
                            allCandiesOnBoard.Add(candy);
                        }
                    }
                }
                Debug.Log($"Total candies to destroy: {allCandiesOnBoard.Count}");

                // TRUYỀN DANH SÁCH NÀY TRỰC TIẾP VÀO HÀM
                // Giả sử bạn có một hàm StartProcessBoardRoutine
                // yield return StartCoroutine(ProcessBoardRoutine(new MatchResult(), allCandiesOnBoard));
                // Hoặc nếu bạn gọi trực tiếp trong một Coroutine khác:
                yield return ProcessBoardRoutine(new MatchResult(), allCandiesOnBoard);

                // Lưu ý: Có thể bạn muốn bỏ qua việc tìm kiếm match thông thường
                // trong lượt này và chỉ tập trung vào việc xóa bảng.
                // Việc truyền MatchResult rỗng (new MatchResult()) là hợp lý.
            }
            //// Color Bomb + Color Bomb
            //if (otherSpecialCandy != null && otherSpecialCandy.SpecialType == SpecialCandyType.ColorBomb)
            //{
            //    Debug.Log("Color Bomb + Color Bomb: Clearing entire board!");

            //    // --- THAY ĐỔI Ở ĐÂY: XÓA SẠCH _allCandiesToDestroyThisTurn và thêm tất cả kẹo vào ---
            //    _allCandiesToDestroyThisTurn.Clear(); // Đảm bảo danh sách trống rỗng
            //    for (int x = 0; x < _board.Width; x++)
            //    {
            //        for (int y = 0; y < _board.Height; y++)
            //        {
            //            GameObject candy = _board.GetCandy(x, y);
            //            if (candy != null)
            //            {
            //                _allCandiesToDestroyThisTurn.Add(candy);
            //            }
            //        }
            //    }

            //    Debug.Log($"Total candies to destroy: {_allCandiesToDestroyThisTurn.Count}");
            //    // Không cần thêm colorBombGO và otherCandyGO riêng nữa vì chúng đã nằm trong _board.Candies

            //    //matchResult = new MatchResult(); // Vẫn cần MatchResult rỗng để ProcessBoardRoutine chạy
            //    //                                 // và có thể chứa các kẹo đặc biệt đã kích hoạt nếu cần.
            //}
            // Color Bomb + Stripped Candy hoặc Wrapped Candy
            else if (otherSpecialCandy != null &&
                     (otherSpecialCandy.SpecialType == SpecialCandyType.StrippedCandy ||
                      otherSpecialCandy.SpecialType == SpecialCandyType.WrappedCandy))
            {
                string targetColorTag = otherRegularCandy.tag;
                SpecialCandyType typeToTransformInto = otherSpecialCandy.SpecialType;

                Debug.Log($"Color Bomb + {typeToTransformInto}: Transforming all {targetColorTag} candies!");

                // Thêm ColorBomb và kẹo đặc biệt kia vào matchResult.MatchedCandies
                matchResult.MatchedCandies.Add(colorBombGO);
                matchResult.MatchedCandies.Add(otherCandyGO);

                List<GameObject> candiesToTransform = new List<GameObject>(); // Các kẹo thường sẽ bị thay thế
                for (int x = 0; x < _board.Width; x++)
                {
                    for (int y = 0; y < _board.Height; y++)
                    {
                        GameObject candy = _board.GetCandy(x, y);
                        if (candy != null && candy.CompareTag(targetColorTag))
                        {
                            if (candy.GetComponent<ISpecialCandy>() == null) // Chỉ biến đổi kẹo thường
                            {
                                candiesToTransform.Add(candy);
                            }
                        }
                    }
                }

                // GỌI HÀM TRANSFORM MỚI (KHÔNG PHẢI COROUTINE)
                List<GameObject> transformedSpecialCandies = TransformCandies(candiesToTransform, typeToTransformInto);

                // THÊM TẤT CẢ CÁC KẸO ĐẶC BIỆT MỚI TẠO VÀO matchResult.MatchedCandies
                // Để HandleMatchedSpecialCandyActivations xử lý kích hoạt chúng
                foreach (GameObject newSpecialCandy in transformedSpecialCandies)
                {
                    matchResult.MatchedCandies.Add(newSpecialCandy);
                }
            }// Color Bomb + Kẹo thường
            else if (otherRegularCandy != null)
            {
                Debug.Log($"Color Bomb + Regular Candy ({otherRegularCandy.tag}): Clearing all {otherRegularCandy.tag} candies.");
                matchResult = new MatchResult();
                matchResult.MatchedCandies.Add(colorBombGO);
                matchResult.MatchedCandies.Add(otherCandyGO); // Kẹo thường cũng được coi là match để kích hoạt ColorBomb

                // Color Bomb sẽ tự xử lý việc tìm kẹo cùng màu thông qua targetTag trong Activate() của nó
                // Chúng ta chỉ cần đảm bảo nó được kích hoạt.
                // Thêm ColorBomb vào danh sách kích hoạt
                _specialCandiesActivatedThisCascade.Add(colorBombGO);

                // Báo cáo để ColorBomb tự Activate với targetTag là màu của kẹo thường
                // Chúng ta sẽ cần một cách để truyền targetTag này tới ProcessBoardRoutine.
                // Tạm thời, để nó chạy qua HandleMatchedSpecialCandyActivations.
                // HandleMatchedSpecialCandyActivations sẽ tìm targetTag.
            }
            else
            {
                Debug.LogWarning("Color Bomb swapped with an unexpected object or null.");
                IsProcessingBoard = false;
                yield break;
            }

            // Sau khi Color Bomb được hoán đổi, luôn xử lý bảng
            // currentMatchResult.MatchedCandies sẽ bao gồm Color Bomb và kẹo kia
            yield return StartCoroutine(ProcessBoardRoutine(matchResult));
        }
        // --- KẾT THÚC LOGIC XỬ LÝ KẾT HỢP COLOR BOMB ---
        else // Không có Color Bomb được hoán đổi, xử lý match thông thường
        {
            matchResult = _matchFinder.FindAllMatches(_board.Candies, _board.Width, _board.Height, new Vector2Int(x2, y2));
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
        }

        IsProcessingBoard = false;
        Debug.Log("Board processing finished. Player input enabled.");
    }

    // PHƯƠNG THỨC MỚI: Chỉ làm nhiệm vụ biến đổi kẹo
    // The TransformCandies method (previously ProcessTransformAndActivateCandies)
    private List<GameObject> TransformCandies(List<GameObject> candiesToTransform, SpecialCandyType typeToTransformInto)
    {
        List<GameObject> newlyCreatedSpecialCandies = new List<GameObject>();

        foreach (GameObject originalCandyGO in candiesToTransform)
        {
            if (originalCandyGO == null) continue;

            Candy originalCandy = originalCandyGO.GetComponent<Candy>();
            if (originalCandy == null) continue;

            Vector2Int pos = new Vector2Int(originalCandy.X, originalCandy.Y);
            string originalColorTag = originalCandy.tag;

            _board.SetCandy(pos.x, pos.y, null); // Xóa kẹo cũ khỏi bảng logic

            GameObject newSpecialCandyGO = null;
            ISpecialCandy newSpecialCandyScript = null;
            float angle = 0f; // Khởi tạo góc

            if (typeToTransformInto == SpecialCandyType.StrippedCandy)
            {
                // --- ĐOẠN CODE KHÔI PHỤC RANDOM HƯỚNG ---
                bool strippedCandyIsHorizontal = Random.Range(0, 2) == 0; // 0 = ngang, 1 = dọc
                angle = strippedCandyIsHorizontal ? 0f : 90f; // 0 độ cho ngang, 90 độ cho dọc
                                                              // ----------------------------------------

                newSpecialCandyGO = Instantiate(strippedCandyPrefab, _board.GetWorldPosition(pos.x, pos.y), Quaternion.Euler(0f, 0f, angle));
                StrippedCandy stripped = newSpecialCandyGO?.GetComponent<StrippedCandy>(); // Sử dụng ?. để an toàn
                if (stripped != null)
                {
                    stripped.SetDirection(strippedCandyIsHorizontal);
                    // Debug.Log($"Stripped Candy: IsHorizontal: {strippedCandyIsHorizontal} at ({pos.x},{pos.y})"); // Có thể thêm debug log để kiểm tra
                    stripped.tag = originalColorTag;
                    newSpecialCandyScript = stripped;
                }
            }
            else if (typeToTransformInto == SpecialCandyType.WrappedCandy)
            {
                newSpecialCandyGO = Instantiate(wrappedCandyPrefab, _board.GetWorldPosition(pos.x, pos.y), Quaternion.identity);
                WrappedCandy wrapped = newSpecialCandyGO?.GetComponent<WrappedCandy>(); // Sử dụng ?. để an toàn
                if (wrapped != null)
                {
                    wrapped.tag = originalColorTag;
                    newSpecialCandyScript = wrapped;
                }
            }

            if (newSpecialCandyGO != null)
            {
                newSpecialCandyGO.transform.parent = boardInstance.transform;
                _board.SetCandy(pos.x, pos.y, newSpecialCandyGO);
                newSpecialCandyGO.GetComponent<Candy>()?.Init(pos.x, pos.y);

                newlyCreatedSpecialCandies.Add(newSpecialCandyGO);
            }
            Destroy(originalCandyGO);
        }
        return newlyCreatedSpecialCandies;
    }

    private IEnumerator ProcessBoardRoutine(MatchResult initialMatchResult, HashSet<GameObject> forceDestroyCandies = null)
    {
        IsProcessingBoard = true;

        // Reset tất cả các danh sách liên quan đến cascade mới
        _allCandiesToDestroyThisTurn.Clear();
        _specialCandiesToCreateThisTurn.Clear();
        _wrappedCandiesPendingSecondExplosion.Clear();
        _specialCandiesActivatedThisCascade.Clear();

        MatchResult currentMatchResult = initialMatchResult;

        // Vòng lặp chính để xử lý toàn bộ chuỗi cascade (bao gồm cả các phản ứng dây chuyền)
        do
        {
            HashSet<GameObject> candiesToDestroyInCurrentStep = new HashSet<GameObject>();
            Dictionary<Vector2Int, SpecialCandyCreationInfo> specialsToCreateInCurrentStep = new Dictionary<Vector2Int, SpecialCandyCreationInfo>();

            // --- LOGIC MỚI: Xử lý các kẹo cần phá hủy bắt buộc (ví dụ: Color Bomb + Color Bomb) ---
            if (forceDestroyCandies != null && forceDestroyCandies.Count > 0)
            {
                Debug.Log($"[ProcessBoardRoutine] Forcing destruction of {forceDestroyCandies.Count} candies.");
                candiesToDestroyInCurrentStep.UnionWith(forceDestroyCandies); // Thêm tất cả kẹo bắt buộc vào danh sách phá hủy hiện tại
                forceDestroyCandies.Clear(); // Xóa danh sách này để nó chỉ được xử lý một lần

                // Sau khi xử lý phá hủy bắt buộc, chúng ta có thể coi như không còn match ban đầu
                // để tránh logic match thông thường gây xung đột hoặc xử lý lặp lại.
                currentMatchResult = new MatchResult();
            }
            // --- KẾT THÚC LOGIC MỚI ---

            if (currentMatchResult.MatchedCandies.Count > 0)
            {
                // Thêm các kẹo đặc biệt cần tạo từ match hiện tại
                specialsToCreateInCurrentStep = new Dictionary<Vector2Int, SpecialCandyCreationInfo>(currentMatchResult.SpecialCandiesToCreate);

                // Kích hoạt các kẹo đặc biệt đã match và đợi hiệu ứng của chúng hoàn tất
                // Hàm này sẽ thêm các kẹo bị phá hủy bởi hiệu ứng vào _allCandiesToDestroyThisTurn
                // và các kẹo đặc biệt đã kích hoạt vào _specialCandiesActivatedThisCascade.
                yield return HandleMatchedSpecialCandyActivations(currentMatchResult.MatchedCandies);

                // Lọc các kẹo đã match: chỉ thêm vào danh sách phá hủy nếu chúng KHÔNG phải là kẹo đặc biệt đã được kích hoạt
                foreach (GameObject matchedCandy in currentMatchResult.MatchedCandies)
                {
                    if (matchedCandy != null && !_specialCandiesActivatedThisCascade.Contains(matchedCandy))
                    {
                        candiesToDestroyInCurrentStep.Add(matchedCandy);
                    }
                    else
                    {
                        // Debug.Log($"[ProcessBoardRoutine] Skipping destruction of activated special candy: {matchedCandy?.name}");
                    }
                }

                // Thêm các kẹo bị phá hủy do hiệu ứng từ kẹo đặc biệt (được thu thập vào _allCandiesToDestroyThisTurn)
                // nhưng loại trừ các Wrapped Candy đang chờ nổ lần 2.
                foreach (GameObject candy in _allCandiesToDestroyThisTurn)
                {
                    if (candy != null && !_wrappedCandiesPendingSecondExplosion.Contains(candy))
                    {
                        candiesToDestroyInCurrentStep.Add(candy);
                    }
                    else
                    {
                        // Debug.Log($"[ProcessBoardRoutine] Skipping destruction of a Wrapped Candy in _allCandiesToDestroyThisTurn because it's pending second explosion: {candy?.name}");
                    }
                }
                _allCandiesToDestroyThisTurn.Clear(); // Xóa danh sách này sau khi đã chuyển sang candiesToDestroyInCurrentStep
            }
            else if (_wrappedCandiesPendingSecondExplosion.Count > 0) // Xử lý vụ nổ lần hai của kẹo bọc nếu không có match mới
            {
                List<GameObject> currentSecondExplosionCandies = new List<GameObject>(_wrappedCandiesPendingSecondExplosion);
                _wrappedCandiesPendingSecondExplosion.Clear(); // Xóa danh sách chờ để chuẩn bị cho các vụ nổ tiếp theo

                foreach (GameObject wrappedCandyGo in currentSecondExplosionCandies)
                {
                    if (wrappedCandyGo != null && wrappedCandyGo.GetComponent<WrappedCandy>() != null)
                    {
                        // Lấy các kẹo bị ảnh hưởng bởi vụ nổ lần hai của kẹo bọc
                        HashSet<GameObject> secondExplosionCandies = WrappedCandy.GetSecondExplosionAffectedCandies(wrappedCandyGo, _board);
                        candiesToDestroyInCurrentStep.UnionWith(secondExplosionCandies);
                        // Debug.Log($"[ProcessBoardRoutine] Activating Wrapped Candy - Second Explosion at ({wrappedCandyGo.GetComponent<Candy>().X},{wrappedCandyGo.GetComponent<Candy>().Y}).");
                    }
                    else
                    {
                        Debug.LogWarning("[ProcessBoardRoutine] Wrapped Candy for second explosion was null or not a Wrapped Candy.");
                    }
                }
            }
            else if (candiesToDestroyInCurrentStep.Count == 0) // Nếu không có match, không có kẹo bọc nổ lần 2, và không có kẹo bắt buộc để phá hủy
            {
                Debug.Log("[ProcessBoardRoutine] ProcessBoardRoutine finished: No more matches or pending actions.");
                break; // Thoát khỏi vòng lặp do không còn gì để xử lý
            }

            // --- Thực hiện Phá hủy, Tạo kẹo, Trọng lực, Lấp đầy (phần này không thay đổi nhiều) ---
            if (candiesToDestroyInCurrentStep.Count > 0)
            {
                yield return _fxManager.AnimateDestroyMatches(candiesToDestroyInCurrentStep); // Chờ animation phá hủy
                _board.DestroyCandies(candiesToDestroyInCurrentStep); // Xóa kẹo khỏi bảng logic
            }

            if (specialsToCreateInCurrentStep.Count > 0)
            {
                yield return CreateSpecialCandies(specialsToCreateInCurrentStep); // Chờ tạo kẹo đặc biệt
            }

            List<Vector2Int> droppedPositions = _board.ApplyGravity(); // Áp dụng trọng lực, kẹo rơi xuống
            if (droppedPositions.Count > 0)
            {
                yield return _fxManager.AnimateDropCandies(droppedPositions, _board); // Chờ animation rơi kẹo
            }

            List<Vector2Int> newCandyPositions = _board.FillEmptySpots(candyPrefabs); // Lấp đầy các ô trống bằng kẹo mới
            if (newCandyPositions.Count > 0)
            {
                yield return _fxManager.AnimateNewCandiesDrop(newCandyPositions, _board); // Chờ animation kẹo mới rơi
            }

            // Xác định các vị trí bị ảnh hưởng để tối ưu hóa việc tìm kiếm match tiếp theo
            HashSet<Vector2Int> newlyAffectedPositions = new HashSet<Vector2Int>();
            foreach (var pos in droppedPositions) newlyAffectedPositions.Add(pos);
            foreach (var pos in newCandyPositions) newlyAffectedPositions.Add(pos);

            // Tìm kiếm các trận đấu mới sau khi bảng đã thay đổi
            currentMatchResult = _matchFinder.FindAllMatches(
                _board.Candies, _board.Width, _board.Height,
                swappedCandyPosition: null, // Giả định đây không phải là lượt đổi kẹo ban đầu
                newlyAffectedPositions: newlyAffectedPositions
            );

        } while (currentMatchResult.MatchedCandies.Count > 0 || // Còn match mới
                 _wrappedCandiesPendingSecondExplosion.Count > 0 || // Còn kẹo bọc chờ nổ lần 2
                 (forceDestroyCandies != null && forceDestroyCandies.Count > 0)); // HOẶC còn kẹo cần phá hủy bắt buộc (cho trường hợp 1 lần duy nhất)

        IsProcessingBoard = false; // Đặt lại cờ hiệu khi quá trình xử lý hoàn tất
    }

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

