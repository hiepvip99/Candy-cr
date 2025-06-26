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
    private HashSet<GameObject> _wrappedCandiesPendingSecondExplosion = new HashSet<GameObject>();
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
        Debug.Log("[Debug] RequestSwapCandies called."); //
        if (IsProcessingBoard) return;
        IsProcessingBoard = true;

        StartCoroutine(HandleSwapAndMatches(x1, y1, x2, y2));
    }

    private IEnumerator HandleSwapAndMatches(int x1, int y1, int x2, int y2)
    {
        Debug.Log("[Debug] HandleSwapAndMatches started."); //
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

        // Xác định loại kẹo đặc biệt
        bool isStriped1 = (specialCandy1 != null && specialCandy1.SpecialType == SpecialCandyType.StripedCandy);
        bool isStriped2 = (specialCandy2 != null && specialCandy2.SpecialType == SpecialCandyType.StripedCandy);

        bool isWrapped1 = (specialCandy1 != null && specialCandy1.SpecialType == SpecialCandyType.WrappedCandy);
        bool isWrapped2 = (specialCandy2 != null && specialCandy2.SpecialType == SpecialCandyType.WrappedCandy);

        MatchResult matchResult = new MatchResult(); // Khởi tạo null

        if (specialCandy1 != null || specialCandy2 != null)
        {
            // --- LOGIC XỬ LÝ KẾT HỢP COLOR BOMB ---
            if (isColorBomb1 || isColorBomb2)
            {
                GameObject colorBombGO = isColorBomb1 ? candy1GO : candy2GO;
                GameObject otherCandyGO = isColorBomb1 ? candy2GO : candy1GO;

                ISpecialCandy otherSpecialCandy = otherCandyGO.GetComponent<ISpecialCandy>();
                Candy otherRegularCandy = otherCandyGO.GetComponent<Candy>();
                //Colorbomb + Colorbomb
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
                    matchResult.MatchedCandies.AddRange(allCandiesOnBoard);
                    yield return StartCoroutine(ProcessBoardRoutine(matchResult));
                    yield return ProcessBoardRoutine(new MatchResult(), allCandiesOnBoard);

                    // Lưu ý: Có thể bạn muốn bỏ qua việc tìm kiếm match thông thường
                    // trong lượt này và chỉ tập trung vào việc xóa bảng.
                    // Việc truyền MatchResult rỗng (new MatchResult()) là hợp lý.
                }
                // Color Bomb + Stripped Candy hoặc Wrapped Candy
                else if (otherSpecialCandy != null &&
                         (otherSpecialCandy.SpecialType == SpecialCandyType.StripedCandy ||
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
                }
                // Color Bomb + Kẹo thường
                else if (otherRegularCandy != null)
                {
                    Debug.Log($"Color Bomb + Regular Candy ({otherRegularCandy.tag}): Clearing all {otherRegularCandy.tag} candies.");
                    //matchResult = new MatchResult();
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

            // 1. Sọc + Sọc
            // HashSet để lưu trữ tất cả các kẹo bị ảnh hưởng bởi combo
            HashSet<GameObject> candiesToDestroyByCombo = new HashSet<GameObject>();
            if (isStriped1 && isStriped2)
            {
                yield return StartCoroutine(ActivateDoubleStrippedCombo(candy1GO, candy2GO));
            }
            // 2. Bọc + Bọc
            else if (isWrapped1 && isWrapped2)
            {
                yield return StartCoroutine(ActivateDoubleWrappedCombo(candy1GO, candy2GO));
            }
            // 3. Sọc + Bọc
            else if ((isStriped1 && isWrapped2) || (isWrapped1 && isStriped2))
            {
                yield return StartCoroutine(ActivateStripedWrappedCombo(candy1GO, candy2GO));
            }

            matchResult.MatchedCandies.AddRange(_allCandiesToDestroyThisTurn);
            yield return StartCoroutine(ProcessBoardRoutine(matchResult));
            //Debug.Log($"[Debug] _allCandiesToDestroyThisTurn: {_allCandiesToDestroyThisTurn.Count}.");
            //yield return ProcessBoardRoutine(new MatchResult(), _allCandiesToDestroyThisTurn);
        }
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

            if (typeToTransformInto == SpecialCandyType.StripedCandy)
            {
                // --- ĐOẠN CODE KHÔI PHỤC RANDOM HƯỚNG ---
                bool strippedCandyIsHorizontal = Random.Range(0, 2) == 0; // 0 = ngang, 1 = dọc
                angle = strippedCandyIsHorizontal ? 0f : 90f; // 0 độ cho ngang, 90 độ cho dọc
                                                              // ----------------------------------------

                newSpecialCandyGO = Instantiate(strippedCandyPrefab, _board.GetWorldPosition(pos.x, pos.y), Quaternion.Euler(0f, 0f, angle));
                StripedCandy stripped = newSpecialCandyGO?.GetComponent<StripedCandy>(); // Sử dụng ?. để an toàn
                if (stripped != null)
                {
                    stripped.SetDirection(strippedCandyIsHorizontal);
                    // Debug.Log($"Stripped Candy: IsHorizontal: {strippedCandyIsHorizontal} at ({pos.x},{pos.y})"); // Có thể thêm debug log để kiểm tra
                    stripped.tag = originalColorTag;
                    newSpecialCandyScript = stripped;
                }
                else { Debug.LogWarning("StripedCandy component not found on the new special candy prefab!"); }
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
                else { Debug.LogWarning("WrappedCandy component not found on the new special candy prefab!"); }
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

    //private IEnumerator ProcessBoardRoutine(MatchResult initialMatchResult, HashSet<GameObject> forceDestroyCandies = null)
    //{
    //    Debug.Log("[ProcessBoardRoutine] Starting board processing routine.");
    //    IsProcessingBoard = true;

    //    // Reset tất cả các danh sách liên quan đến cascade mới
    //    _allCandiesToDestroyThisTurn.Clear();
    //    _specialCandiesToCreateThisTurn.Clear();
    //    _wrappedCandiesPendingSecondExplosion.Clear();
    //    _specialCandiesActivatedThisCascade.Clear();

    //    MatchResult currentMatchResult = initialMatchResult;

    //    // Vòng lặp chính để xử lý toàn bộ chuỗi cascade (bao gồm cả các phản ứng dây chuyền)
    //    do
    //    {
    //        HashSet<GameObject> candiesToDestroyInCurrentStep = new HashSet<GameObject>();
    //        Dictionary<Vector2Int, SpecialCandyCreationInfo> specialsToCreateInCurrentStep = new Dictionary<Vector2Int, SpecialCandyCreationInfo>();

    //        // --- LOGIC MỚI: Xử lý các kẹo cần phá hủy bắt buộc (ví dụ: Color Bomb + Color Bomb) ---
    //        if (forceDestroyCandies != null && forceDestroyCandies.Count > 0)
    //        {
    //            Debug.Log($"[ProcessBoardRoutine] Forcing destruction of {forceDestroyCandies.Count} candies.");
    //            candiesToDestroyInCurrentStep.UnionWith(forceDestroyCandies); // Thêm tất cả kẹo bắt buộc vào danh sách phá hủy hiện tại
    //            forceDestroyCandies.Clear(); // Xóa danh sách này để nó chỉ được xử lý một lần

    //            // Sau khi xử lý phá hủy bắt buộc, chúng ta có thể coi như không còn match ban đầu
    //            // để tránh logic match thông thường gây xung đột hoặc xử lý lặp lại.
    //            currentMatchResult = new MatchResult();
    //        }
    //        // --- KẾT THÚC LOGIC MỚI ---

    //        if (currentMatchResult.MatchedCandies.Count > 0)
    //        {
    //            // Thêm các kẹo đặc biệt cần tạo từ match hiện tại
    //            specialsToCreateInCurrentStep = new Dictionary<Vector2Int, SpecialCandyCreationInfo>(currentMatchResult.SpecialCandiesToCreate);

    //            // Kích hoạt các kẹo đặc biệt đã match và đợi hiệu ứng của chúng hoàn tất
    //            // Hàm này sẽ thêm các kẹo bị phá hủy bởi hiệu ứng vào _allCandiesToDestroyThisTurn
    //            // và các kẹo đặc biệt đã kích hoạt vào _specialCandiesActivatedThisCascade.
    //            yield return HandleMatchedSpecialCandyActivations(currentMatchResult.MatchedCandies);

    //            // Lọc các kẹo đã match: chỉ thêm vào danh sách phá hủy nếu chúng KHÔNG phải là kẹo đặc biệt đã được kích hoạt
    //            foreach (GameObject matchedCandy in currentMatchResult.MatchedCandies)
    //            {
    //                if (matchedCandy != null && !_specialCandiesActivatedThisCascade.Contains(matchedCandy))
    //                {
    //                    candiesToDestroyInCurrentStep.Add(matchedCandy);
    //                }
    //                else
    //                {
    //                    // Debug.Log($"[ProcessBoardRoutine] Skipping destruction of activated special candy: {matchedCandy?.name}");
    //                }
    //            }

    //            // Thêm các kẹo bị phá hủy do hiệu ứng từ kẹo đặc biệt (được thu thập vào _allCandiesToDestroyThisTurn)
    //            // nhưng loại trừ các Wrapped Candy đang chờ nổ lần 2.
    //            //Debug.Log($"[ProcessBoardRoutine] Before moving: _allCandiesToDestroyThisTurn has {_allCandiesToDestroyThisTurn.Count} candies.");
    //            //foreach (GameObject candy in _allCandiesToDestroyThisTurn)
    //            //{
    //            //    if (candy != null && !_wrappedCandiesPendingSecondExplosion.Contains(candy))
    //            //    {
    //            //        candiesToDestroyInCurrentStep.Add(candy);
    //            //    }
    //            //    else
    //            //    {
    //            //        // Debug.Log($"[ProcessBoardRoutine] Skipping destruction of a Wrapped Candy in _allCandiesToDestroyThisTurn because it's pending second explosion: {candy?.name}");
    //            //    }
    //            //}

    //            Debug.Log($"[Debug] _allCandiesToDestroyThisTurn count before transfer: {_allCandiesToDestroyThisTurn.Count}"); //
    //            foreach (GameObject candy in _allCandiesToDestroyThisTurn) //
    //            {
    //                if (candy != null && !_wrappedCandiesPendingSecondExplosion.Contains(candy)) //
    //                {
    //                    candiesToDestroyInCurrentStep.Add(candy); //
    //                }
    //                else if (candy != null) // Thêm log này để xem kẹo nào bị bỏ qua
    //                {
    //                    Debug.Log($"[Debug] Skipping candy {candy.name} at {candy.GetComponent<Candy>()?.Position} because it's pending second explosion.");
    //                }
    //                else // Thêm log này nếu có kẹo null
    //                {
    //                    Debug.Log("[Debug] Skipping null candy in _allCandiesToDestroyThisTurn.");
    //                }
    //            }
    //            _allCandiesToDestroyThisTurn.Clear(); //
    //            Debug.Log($"[Debug] candiesToDestroyInCurrentStep count after transfer (before destruction): {candiesToDestroyInCurrentStep.Count}"); //

    //            _allCandiesToDestroyThisTurn.Clear(); // Xóa danh sách này sau khi đã chuyển sang candiesToDestroyInCurrentStep
    //        }
    //        // Xử lý vụ nổ lần hai của kẹo bọc nếu không có match mới
    //        else if (_wrappedCandiesPendingSecondExplosion.Count > 0)
    //        {
    //            List<GameObject> currentSecondExplosionCandies = new List<GameObject>(_wrappedCandiesPendingSecondExplosion);
    //            _wrappedCandiesPendingSecondExplosion.Clear();

    //            foreach (GameObject wrappedCandyGo in currentSecondExplosionCandies)
    //            {
    //                // --- LOGIC ĐƯỢC SỬA ĐỔI ---
    //                if (wrappedCandyGo != null)
    //                {
    //                    var wrappedCandyScript = wrappedCandyGo.GetComponent<WrappedCandy>();
    //                    var candyScript = wrappedCandyGo.GetComponent<Candy>();

    //                    if (wrappedCandyScript != null && candyScript != null)
    //                    {
    //                        HashSet<GameObject> secondExplosionCandies;

    //                        // Kiểm tra xem đây có phải là vụ nổ lớn từ combo không
    //                        if (wrappedCandyScript.IsPendingBigExplosion)
    //                        {
    //                            Debug.Log($"[ProcessBoardRoutine] Activating BIG second explosion at {candyScript.Position}.");
    //                            secondExplosionCandies = GetBigExplosionAffectedCandies(candyScript.Position, _board);

    //                            // Chơi hiệu ứng nổ lớn
    //                            StartCoroutine(_fxManager.PlayWrappedCandyFX(candyScript.Position, isBigExplosion: true));

    //                            // Reset cờ sau khi dùng
    //                            wrappedCandyScript.IsPendingBigExplosion = false;
    //                        }
    //                        else
    //                        {
    //                            Debug.Log($"[ProcessBoardRoutine] Activating NORMAL second explosion at {candyScript.Position}.");
    //                            // Dùng logic cũ cho vụ nổ thường (3x3)
    //                            secondExplosionCandies = WrappedCandy.GetSecondExplosionAffectedCandies(wrappedCandyGo, _board);

    //                            // Chơi hiệu ứng nổ thường (giả sử fxManager có hàm này)
    //                            StartCoroutine(_fxManager.PlayWrappedCandyFX(candyScript.Position, isBigExplosion: false));
    //                        }

    //                        // Thêm các kẹo bị ảnh hưởng và chính kẹo bọc vào danh sách phá hủy
    //                        candiesToDestroyInCurrentStep.UnionWith(secondExplosionCandies);
    //                        candiesToDestroyInCurrentStep.Add(wrappedCandyGo);
    //                    }
    //                }
    //                // --- KẾT THÚC LOGIC SỬA ĐỔI ---
    //                else
    //                {
    //                    Debug.LogWarning("[ProcessBoardRoutine] Wrapped Candy for second explosion was null.");
    //                }
    //            }
    //        }
    //        else if (candiesToDestroyInCurrentStep.Count == 0)
    //        {
    //            Debug.Log("[ProcessBoardRoutine] ProcessBoardRoutine finished: No more matches or pending actions.");
    //            break;
    //        }
    //        //else if (_wrappedCandiesPendingSecondExplosion.Count > 0) // Xử lý vụ nổ lần hai của kẹo bọc nếu không có match mới
    //        //{
    //        //    List<GameObject> currentSecondExplosionCandies = new List<GameObject>(_wrappedCandiesPendingSecondExplosion);
    //        //    _wrappedCandiesPendingSecondExplosion.Clear(); // Xóa danh sách chờ để chuẩn bị cho các vụ nổ tiếp theo

    //        //    foreach (GameObject wrappedCandyGo in currentSecondExplosionCandies)
    //        //    {
    //        //        if (wrappedCandyGo != null && wrappedCandyGo.GetComponent<WrappedCandy>() != null)
    //        //        {
    //        //            // Lấy các kẹo bị ảnh hưởng bởi vụ nổ lần hai của kẹo bọc
    //        //            HashSet<GameObject> secondExplosionCandies = WrappedCandy.GetSecondExplosionAffectedCandies(wrappedCandyGo, _board);
    //        //            candiesToDestroyInCurrentStep.UnionWith(secondExplosionCandies);

    //        //            // Debug.Log($"[ProcessBoardRoutine] Activating Wrapped Candy - Second Explosion at ({wrappedCandyGo.GetComponent<Candy>().X},{wrappedCandyGo.GetComponent<Candy>().Y}).");
    //        //        }
    //        //        else
    //        //        {
    //        //            Debug.LogWarning("[ProcessBoardRoutine] Wrapped Candy for second explosion was null or not a Wrapped Candy.");
    //        //        }
    //        //    }
    //        //}
    //        else if (candiesToDestroyInCurrentStep.Count == 0) // Nếu không có match, không có kẹo bọc nổ lần 2, và không có kẹo bắt buộc để phá hủy
    //        {
    //            Debug.Log("[ProcessBoardRoutine] ProcessBoardRoutine finished: No more matches or pending actions.");
    //            break; // Thoát khỏi vòng lặp do không còn gì để xử lý
    //        }

    //        // --- Thực hiện Phá hủy, Tạo kẹo, Trọng lực, Lấp đầy (phần này không thay đổi nhiều) ---
    //        Debug.Log($"[ProcessBoardRoutine] candiesToDestroyInCurrentStep count: {candiesToDestroyInCurrentStep.Count}");
    //        if (candiesToDestroyInCurrentStep.Count > 0)
    //        {
    //            yield return _fxManager.AnimateDestroyMatches(candiesToDestroyInCurrentStep); // Chờ animation phá hủy
    //            _board.DestroyCandies(candiesToDestroyInCurrentStep); // Xóa kẹo khỏi bảng logic
    //        }

    //        if (specialsToCreateInCurrentStep.Count > 0)
    //        {
    //            yield return CreateSpecialCandies(specialsToCreateInCurrentStep); // Chờ tạo kẹo đặc biệt
    //        }

    //        List<Vector2Int> droppedPositions = _board.ApplyGravity(); // Áp dụng trọng lực, kẹo rơi xuống
    //        if (droppedPositions.Count > 0)
    //        {
    //            yield return _fxManager.AnimateDropCandies(droppedPositions, _board); // Chờ animation rơi kẹo
    //        }

    //        List<Vector2Int> newCandyPositions = _board.FillEmptySpots(candyPrefabs); // Lấp đầy các ô trống bằng kẹo mới
    //        if (newCandyPositions.Count > 0)
    //        {
    //            yield return _fxManager.AnimateNewCandiesDrop(newCandyPositions, _board); // Chờ animation kẹo mới rơi
    //        }

    //        // Xác định các vị trí bị ảnh hưởng để tối ưu hóa việc tìm kiếm match tiếp theo
    //        HashSet<Vector2Int> newlyAffectedPositions = new HashSet<Vector2Int>();
    //        foreach (var pos in droppedPositions) newlyAffectedPositions.Add(pos);
    //        foreach (var pos in newCandyPositions) newlyAffectedPositions.Add(pos);

    //        // Tìm kiếm các trận đấu mới sau khi bảng đã thay đổi
    //        currentMatchResult = _matchFinder.FindAllMatches(
    //            _board.Candies, _board.Width, _board.Height,
    //            swappedCandyPosition: null, // Giả định đây không phải là lượt đổi kẹo ban đầu
    //            newlyAffectedPositions: newlyAffectedPositions
    //        );
    //        Debug.Log($"[Debug] Loop conditions: MatchedCandies.Count={currentMatchResult.MatchedCandies.Count}, WrappedPendingSecondExplosion.Count={_wrappedCandiesPendingSecondExplosion.Count}, ForceDestroyCandies.Count={(forceDestroyCandies != null ? forceDestroyCandies.Count : 0)}");
    //    } while (currentMatchResult.MatchedCandies.Count > 0 || // Còn match mới
    //             _wrappedCandiesPendingSecondExplosion.Count > 0 || // Còn kẹo bọc chờ nổ lần 2
    //             (forceDestroyCandies != null && forceDestroyCandies.Count > 0)); // HOẶC còn kẹo cần phá hủy bắt buộc (cho trường hợp 1 lần duy nhất)

    //    IsProcessingBoard = false; // Đặt lại cờ hiệu khi quá trình xử lý hoàn tất
    //}

    private IEnumerator ProcessBoardRoutine(MatchResult initialMatchResult, HashSet<GameObject> forceDestroyCandies = null)
    {
        Debug.Log("[ProcessBoardRoutine] Starting board processing routine.");
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
            bool somethingProcessedInThisIteration = false; // Cờ hiệu để kiểm tra xem có hành động nào xảy ra trong vòng lặp này không

            // --- BƯỚC 1: Xử lý các kẹo cần phá hủy bắt buộc (ví dụ: Color Bomb + Color Bomb) ---
            if (forceDestroyCandies != null && forceDestroyCandies.Count > 0)
            {
                Debug.Log($"[ProcessBoardRoutine] Forcing destruction of {forceDestroyCandies.Count} candies.");
                candiesToDestroyInCurrentStep.UnionWith(forceDestroyCandies);
                forceDestroyCandies.Clear(); // Xóa danh sách này để nó chỉ được xử lý một lần
                currentMatchResult = new MatchResult(); // Coi như không có match ban đầu để tránh xung đột
                somethingProcessedInThisIteration = true;
            }

            // --- BƯỚC 2: Xử lý Match mới hoặc Kích hoạt Special Candies từ Match ban đầu ---
            // Chỉ xử lý nếu có match mới VÀ KHÔNG có kẹo bắt buộc phải phá hủy từ bước trước (đã được forceDestroyCandies.Clear() xử lý)
            if (currentMatchResult.MatchedCandies.Count > 0)
            {
                Debug.Log($"[ProcessBoardRoutine] Found {currentMatchResult.MatchedCandies.Count} initial matched candies.");
                specialsToCreateInCurrentStep = new Dictionary<Vector2Int, SpecialCandyCreationInfo>(currentMatchResult.SpecialCandiesToCreate);

                // Kích hoạt các kẹo đặc biệt đã match và đợi hiệu ứng của chúng hoàn tất
                yield return HandleMatchedSpecialCandyActivations(currentMatchResult.MatchedCandies);

                // Thêm các kẹo đã match (trừ kẹo đặc biệt đã kích hoạt) vào danh sách phá hủy
                foreach (GameObject matchedCandy in currentMatchResult.MatchedCandies)
                {
                    if (matchedCandy != null && !_specialCandiesActivatedThisCascade.Contains(matchedCandy))
                    {
                        candiesToDestroyInCurrentStep.Add(matchedCandy);
                    }
                }

                // Thêm các kẹo bị phá hủy do hiệu ứng từ kẹo đặc biệt (trừ Wrapped Candy chờ nổ lần 2)
                Debug.Log($"[Debug] _allCandiesToDestroyThisTurn count before transfer: {_allCandiesToDestroyThisTurn.Count}");
                foreach (GameObject candy in _allCandiesToDestroyThisTurn)
                {
                    if (candy != null && !_wrappedCandiesPendingSecondExplosion.Contains(candy))
                    {
                        candiesToDestroyInCurrentStep.Add(candy);
                    }
                    else if (candy != null)
                    {
                        Debug.Log($"[Debug] Skipping destruction of candy {candy.name} at {candy.GetComponent<Candy>()?.Position} because it's pending second explosion.");
                    }
                    else
                    {
                        Debug.Log("[Debug] Skipping null candy in _allCandiesToDestroyThisTurn.");
                    }
                }
                _allCandiesToDestroyThisTurn.Clear();
                somethingProcessedInThisIteration = true;
            }
            // --- KẾT THÚC BƯỚC 2 ---

            // --- BƯỚC 3: Xử lý vụ nổ lần hai của kẹo bọc (nếu không có match mới HOẶC force destroy) ---
            // Nếu BƯỚC 1 hoặc BƯỚC 2 không xử lý gì, và có kẹo bọc chờ nổ lần 2, thì xử lý chúng.
            if (!somethingProcessedInThisIteration && _wrappedCandiesPendingSecondExplosion.Count > 0)
            {
                Debug.Log("[ProcessBoardRoutine] Activating Wrapped Candies for second explosion.");
                List<GameObject> currentSecondExplosionCandies = new List<GameObject>(_wrappedCandiesPendingSecondExplosion);
                _wrappedCandiesPendingSecondExplosion.Clear(); // Xóa danh sách chờ để chuẩn bị cho các vụ nổ tiếp theo

                foreach (GameObject wrappedCandyGo in currentSecondExplosionCandies)
                {
                    if (wrappedCandyGo != null)
                    {
                        var wrappedCandyScript = wrappedCandyGo.GetComponent<WrappedCandy>();
                        var candyScript = wrappedCandyGo.GetComponent<Candy>();

                        if (wrappedCandyScript != null && candyScript != null)
                        {
                            HashSet<GameObject> affectedCandies;
                            if (wrappedCandyScript.IsPendingBigExplosion)
                            {
                                Debug.Log($"[ProcessBoardRoutine] Activating BIG second explosion at {candyScript.Position}.");
                                affectedCandies = GetBigExplosionAffectedCandies(candyScript.Position, _board);
                                StartCoroutine(_fxManager.PlayWrappedCandyFX(candyScript.Position, isBigExplosion: true));
                                wrappedCandyScript.IsPendingBigExplosion = false; // Reset cờ sau khi dùng
                            }
                            else
                            {
                                Debug.Log($"[ProcessBoardRoutine] Activating NORMAL second explosion at {candyScript.Position}.");
                                affectedCandies = WrappedCandy.GetSecondExplosionAffectedCandies(wrappedCandyGo, _board);
                                StartCoroutine(_fxManager.PlayWrappedCandyFX(candyScript.Position, isBigExplosion: false));
                            }

                            // Thêm các kẹo bị ảnh hưởng VÀ CHÍNH KẸO BỌC vào danh sách phá hủy.
                            // Việc này là CẦN THIẾT để đảm bảo kẹo bọc bị xóa khỏi bảng sau khi nổ lần 2.
                            candiesToDestroyInCurrentStep.UnionWith(affectedCandies);
                            candiesToDestroyInCurrentStep.Add(wrappedCandyGo);
                            somethingProcessedInThisIteration = true; // Đã xử lý vụ nổ bọc
                        }
                        else
                        {
                            Debug.LogWarning("[ProcessBoardRoutine] Wrapped Candy for second explosion was null or missing WrappedCandy/Candy script.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[ProcessBoardRoutine] Wrapped Candy for second explosion was null in list.");
                    }
                }
            }
            // --- KẾT THÚC BƯỚC 3 ---

            // --- ĐIỀU KIỆN THOÁT CHÍNH CỦA VÒNG LẶP ---
            // Nếu không có bất kỳ hành động nào diễn ra trong vòng lặp hiện tại, thoát.
            // Điều này ngăn chặn vòng lặp vô hạn khi không còn match, không còn kẹo bọc chờ nổ,
            // và không có kẹo bắt buộc nào cần phá hủy.
            if (!somethingProcessedInThisIteration && candiesToDestroyInCurrentStep.Count == 0 && specialsToCreateInCurrentStep.Count == 0)
            {
                Debug.Log("[ProcessBoardRoutine] ProcessBoardRoutine finished: No more matches or pending actions in this iteration.");
                break; // Thoát khỏi vòng lặp do không còn gì để xử lý
            }
            // --- KẾT THÚC ĐIỀU KIỆN THOÁT ---

            // --- BƯỚC 4: Thực hiện Phá hủy, Tạo kẹo, Trọng lực, Lấp đầy ---
            Debug.Log($"[ProcessBoardRoutine] candiesToDestroyInCurrentStep count: {candiesToDestroyInCurrentStep.Count}");
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

            // --- BƯỚC 5: Tìm kiếm các match mới cho vòng lặp tiếp theo ---
            HashSet<Vector2Int> newlyAffectedPositions = new HashSet<Vector2Int>();
            foreach (var pos in droppedPositions) newlyAffectedPositions.Add(pos);
            foreach (var pos in newCandyPositions) newlyAffectedPositions.Add(pos);

            currentMatchResult = _matchFinder.FindAllMatches(
                _board.Candies, _board.Width, _board.Height,
                swappedCandyPosition: null, // Giả định đây không phải là lượt đổi kẹo ban đầu
                newlyAffectedPositions: newlyAffectedPositions
            );

            Debug.Log($"[Debug] End of iteration. Loop conditions: MatchedCandies.Count={currentMatchResult.MatchedCandies.Count}, WrappedPendingSecondExplosion.Count={_wrappedCandiesPendingSecondExplosion.Count}, ForceDestroyCandies.Count={(forceDestroyCandies != null ? forceDestroyCandies.Count : 0)}");

        } while (currentMatchResult.MatchedCandies.Count > 0 || // Còn match mới
                 _wrappedCandiesPendingSecondExplosion.Count > 0 || // Còn kẹo bọc chờ nổ lần 2
                 (forceDestroyCandies != null && forceDestroyCandies.Count > 0)); // HOẶC còn kẹo cần phá hủy bắt buộc (cho trường hợp 1 lần duy nhất)

        IsProcessingBoard = false; // Đặt lại cờ hiệu khi quá trình xử lý hoàn tất
        Debug.Log("[ProcessBoardRoutine] Board processing routine finished.");
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
                case SpecialCandyType.StripedCandy:
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

                StripedCandy strippedCandyScript = newSpecialCandy.GetComponent<StripedCandy>();
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
        Debug.Log($"Kẹo bị phá hủy có trong affectedCandies: {affectedCandies.Count}");
        // Logic để thêm vào specialCandiesToCreateThisTurn đã được MatchFinder xử lý rồi
        // Nên ở đây chúng ta không cần thêm lại.
        // MatchFinder đã điền _specialCandiesToCreateThisTurn vào initialMatchResult.SpecialCandiesToCreate
        // và chúng ta đã lấy nó ở đầu ProcessBoardRoutine.
    }

    // Hàm thêm Wrapped Candy vào danh sách chờ nổ lần 2
    // Cập nhật hàm lắng nghe event để chấp nhận GameObject
    private void AddWrappedCandyForSecondExplosion(GameObject wrappedCandyGo)
    {
        if (wrappedCandyGo != null)
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

    private IEnumerator ActivateDoubleStrippedCombo(GameObject striped1, GameObject striped2)
    {
        Vector2Int pos1 = striped1.GetComponent<Candy>().Position;
        Vector2Int pos2 = striped2.GetComponent<Candy>().Position;
        Vector2Int center = pos1;

        HashSet<GameObject> affected = new HashSet<GameObject>();

        // Nổ hàng
        for (int x = 0; x < _board.Width; x++)
        {
            GameObject c = _board.GetCandy(x, center.y);
            if (c != null) affected.Add(c);
        }

        // Nổ cột
        for (int y = 0; y < _board.Height; y++)
        {
            GameObject c = _board.GetCandy(center.x, y);
            if (c != null) affected.Add(c);
        }

        GameEvents.ReportSpecialCandyActivation(center, SpecialCandyType.StripedCandy, affected, "DoubleStriped");
        Debug.Log($"Kẹo bị phá hủy có trong affected: {affected.Count}");
        yield return _fxManager.PlayDoubleStripedComboFX(center);
        yield return new WaitForSeconds(0.2f);
    }

    /// <summary>
    /// Kích hoạt combo Kẹo Bọc + Kẹo Bọc.
    /// Hàm này sẽ thực hiện vụ nổ 5x5 đầu tiên ngay lập tức,
    /// sau đó đăng ký kẹo bọc thứ hai để thực hiện một vụ nổ 5x5 khác trong lượt cascade tiếp theo.
    /// </summary>
    private IEnumerator ActivateDoubleWrappedCombo(GameObject wrapped1, GameObject wrapped2)
    {
        Vector2Int center1 = wrapped1.GetComponent<Candy>().Position;
        Vector2Int center2 = wrapped2.GetComponent<Candy>().Position;

        // --- VỤ NỔ 1 (Thực hiện ngay) ---
        Debug.Log($"[ActivateDoubleWrappedCombo] Activating first big explosion at {center1}");
        HashSet<GameObject> affected1 = GetBigExplosionAffectedCandies(center1, _board);

        // Thêm chính kẹo bọc đã kích hoạt vào danh sách phá hủy
        affected1.Add(wrapped1);

        // Thêm tất cả kẹo bị ảnh hưởng vào danh sách phá hủy chung của lượt này
        _allCandiesToDestroyThisTurn.UnionWith(affected1);

        // Gửi sự kiện và chơi hiệu ứng
        GameEvents.ReportSpecialCandyActivation(center1, SpecialCandyType.WrappedCandy, affected1, "DoubleWrapped_Part1");
        yield return _fxManager.PlayWrappedCandyFX(center1, isBigExplosion: true);

        // --- ĐĂNG KÝ VỤ NỔ 2 (Cho cascade tiếp theo) ---
        // Lấy script của kẹo thứ hai
        var wrappedCandy2Script = wrapped2.GetComponent<WrappedCandy>();
        if (wrappedCandy2Script != null)
        {
            Debug.Log($"[ActivateDoubleWrappedCombo] Scheduling second big explosion for candy at {center2}");
            // Đánh dấu nó sẽ tạo ra một vụ nổ lớn
            wrappedCandy2Script.IsPendingBigExplosion = true;

            // Thêm nó vào danh sách chờ nổ lần hai.
            // ProcessBoardRoutine sẽ xử lý nó ở vòng lặp tiếp theo.
            _wrappedCandiesPendingSecondExplosion.Add(wrapped2);
        }

        // Không cần chờ đợi thêm ở đây, hàm này đã hoàn thành nhiệm vụ của nó cho lượt cascade hiện tại.
        yield return new WaitForSeconds(0.1f); // Một khoảng chờ nhỏ để hiệu ứng mượt hơn
    }

    /// <summary>
    /// Helper function để lấy các kẹo trong vùng nổ lớn 5x5.
    /// </summary>
    private HashSet<GameObject> GetBigExplosionAffectedCandies(Vector2Int center, IBoard board)
    {
        HashSet<GameObject> affected = new HashSet<GameObject>();
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                int x = center.x + dx;
                int y = center.y + dy;
                if (board.IsValidPosition(x, y))
                {
                    GameObject c = board.GetCandy(x, y);
                    if (c != null) affected.Add(c);
                }
            }
        }
        return affected;
    }

    private IEnumerator ActivateStripedWrappedCombo(GameObject wrapped, GameObject striped)
    {
        Vector2Int centerWrapped = wrapped.GetComponent<Candy>().Position;
        Vector2Int centerStriped = striped.GetComponent<Candy>().Position;

        // Dùng tâm chính là viên Wrapped
        Vector2Int center = centerWrapped;

        HashSet<GameObject> affected = new HashSet<GameObject>();

        // Nổ 3 cột: x-1, x, x+1
        for (int dx = -1; dx <= 1; dx++)
        {
            int x = center.x + dx;
            if (x < 0 || x >= _board.Width) continue;

            for (int y = 0; y < _board.Height; y++)
            {
                GameObject c = _board.GetCandy(x, y);
                if (c != null) affected.Add(c);
            }
        }

        // Nổ 3 hàng: y-1, y, y+1
        for (int dy = -1; dy <= 1; dy++)
        {
            int y = center.y + dy;
            if (y < 0 || y >= _board.Height) continue;

            for (int x = 0; x < _board.Width; x++)
            {
                GameObject c = _board.GetCandy(x, y);
                if (c != null) affected.Add(c);
            }
        }

        GameEvents.ReportSpecialCandyActivation(center, SpecialCandyType.StripedCandy, affected, "Wrapped+Striped");
        yield return _fxManager.PlayStrippedWrappedComboFX(center);
        yield return new WaitForSeconds(0.2f);
    }


}

