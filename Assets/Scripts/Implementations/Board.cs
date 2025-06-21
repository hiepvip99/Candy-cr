using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour, IBoard
{
    private int _width;
    private int _height;
    public GameObject[] candyPrefabs;
    public GameObject tilePrefab;
    public float candySize = 1.0f;
    private GameObject[,] _candies;
    private IMatchFinder _matchFinder;
    
    public int Width => _width;

    public int Height => _height;

    public GameObject[,] Candies => _candies;

    public void Initialize(int width, int height, GameObject[] candyPrefabs, GameObject tilePrefab)
    {
        this._width = width;
        this._height = height;
        this.candyPrefabs = candyPrefabs;
        this.tilePrefab = tilePrefab;
        this._candies = new GameObject[width, height];

        // Lấy MatchFinder instance (tạm thời, sau này dùng DI)
        _matchFinder = FindFirstObjectByType<MatchFinder>();
        if (_matchFinder == null) Debug.LogError("Board: MatchFinder not found!");

        GenerateBoard();
    }

    private void GenerateBoard()
    {
        if (candyPrefabs == null || candyPrefabs.Length == 0)
        {
            Debug.LogError("No candy prefabs assigned to Board.");
            return;
        }

        for (int c = 0; c < candyPrefabs.Length; c++)
        {
            if (candyPrefabs[c].CompareTag("Untagged"))
            {
                Debug.LogError($"Candy prefab {candyPrefabs[c].name} does not have a tag assigned.");
                return;
            }
        }

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Vector2 position = GetWorldPosition(x, y);
                // Tạo Tile (nếu có)
                if (tilePrefab != null)
                {
                    GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity);
                    tile.transform.parent = this.transform;
                    tile.name = $"Tile_{x}_{y}";
                }

                int randomCandyIndex;
                GameObject newCandyPrefab;
                string newCandyTag;

                do
                {
                    randomCandyIndex = UnityEngine.Random.Range(0, candyPrefabs.Length);
                    newCandyPrefab = candyPrefabs[randomCandyIndex];
                    newCandyTag = newCandyPrefab.tag;
                }
                while (_matchFinder.CheckForMatchAtPosition(x, y,newCandyTag, _candies)); // Sử dụng _matchFinder đã được gán

                GameObject candy = Instantiate(newCandyPrefab, position, Quaternion.identity);
                candy.transform.parent = this.transform; // Đặt làm con của Board GameObject
                _candies[x, y] = candy;

                // Gán Candy component và khởi tạo
                Candy candyScript = candy.GetComponent<Candy>();
                if (candyScript != null)
                {
                    candyScript.Init(x, y); // Candy chỉ cần biết tọa độ
                }
                else
                {
                    Debug.LogError($"Candy script not found on {candy.name}.");
                }
            }
        }
    }

    public List<Vector2Int> ApplyGravity()
    {
        List<Vector2Int> droppedPositions = new List<Vector2Int>();
        for (int x = 0; x < _width; x++)
        {
            int nullCount = 0;
            for (int y = 0; y < _height; y++)
            {
                if (_candies[x, y] == null)
                {
                    nullCount++;
                }
                else if (nullCount > 0)
                {
                    GameObject fallingCandy = _candies[x, y];
                    _candies[x, y - nullCount] = fallingCandy;
                    _candies[x, y] = null;

                    fallingCandy.GetComponent<Candy>().UpdatePosition(x, y - nullCount);
                    droppedPositions.Add(new Vector2Int(x, y - nullCount));
                }
            }
        }
        return droppedPositions;
    }

    public void DestroyCandies(HashSet<GameObject> candiesToDestroy)
    {
        foreach (GameObject candyGO in candiesToDestroy)
        {
            if (candyGO != null)
            {
                Candy candyScript = candyGO.GetComponent<Candy>();
                if (candyScript != null)
                {
                    int x = candyScript.X;
                    int y = candyScript.Y;
                    if (x >= 0 && x < _width && y >= 0 && y < _height && _candies[x, y] == candyGO)
                    {
                        // Hủy Kẹo Kera và đặt null trong mảng _candies
                        Destroy(candyGO);
                        _candies[x, y] = null;
                    }
                }
            }
        }
    }

    public List<Vector2Int> FillEmptySpots(GameObject[] candyPrefabs)
    {
        List<Vector2Int> newCandyPositions = new List<Vector2Int>();
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_candies[x, y] == null)
                {
                    int randomCandyIndex;
                    GameObject newCandyPrefab;
                    string newCandyTag;

                    do
                    {
                        randomCandyIndex = UnityEngine.Random.Range(0, candyPrefabs.Length);
                        newCandyPrefab = candyPrefabs[randomCandyIndex];
                        newCandyTag = newCandyPrefab.tag;
                    }
                    while (_matchFinder.CheckForMatchAtPosition(x, y,newCandyTag, _candies));

                    Vector2 spawnPos = GetWorldPosition(x, _height + UnityEngine.Random.Range(1, 3));
                    GameObject newCandy = Instantiate(newCandyPrefab, spawnPos, Quaternion.identity);
                    newCandy.transform.parent = this.transform;

                    _candies[x, y] = newCandy;
                    newCandy.GetComponent<Candy>().Init(x, y);
                    newCandyPositions.Add(new Vector2Int(x, y));
                }
            }
        }
        return newCandyPositions;
    }

    public GameObject GetCandy(int x, int y)
    {
        if (x >= 0 && x < _width && y >= 0 && y < _height)
            return _candies[x, y];
        return null;
    }

    public Vector2 GetWorldPosition(int x, int y)
    {
        return new Vector2(x * candySize, y * candySize);
    }

    public void SetCandy(int x, int y, GameObject candy)
    {
        if (x >= 0 && x < _width && y >= 0 && y < _height)
        {
            _candies[x, y] = candy;
            if (candy != null) candy.GetComponent<Candy>()?.UpdatePosition(x, y); // Sử dụng ?. để tránh lỗi null
        }
    }

    public void SwapCandiesData(int x1, int y1, int x2, int y2)
    {
        GameObject temp = _candies[x1, y1];
        _candies[x1, y1] = _candies[x2, y2];
        _candies[x2, y2] = temp;

        _candies[x1, y1]?.GetComponent<Candy>()?.UpdatePosition(x1, y1);
        _candies[x2, y2]?.GetComponent<Candy>()?.UpdatePosition(x2, y2);
    }
}
