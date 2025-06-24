using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;

public class FXManager : MonoBehaviour, IFXManager
{
    public float swapDuration = 0.2f;
    public float dropDuration = 0.1f;
    public float destroyAnimationDuration = 0.15f;

    public GameObject explosionFXPrefab; // Kéo thả Particle System prefab vào đây
    public GameObject lazeHozirontalPerfab; // Kéo thả Particle System prefab vào đây

    private IBoard _board;

    private void Awake()
    {
        _board = FindFirstObjectByType<Board>();
    }

    public IEnumerator AnimateSwap(GameObject candy1, GameObject candy2, Vector2 startPos1, Vector2 endPos1)
    {
        Vector2 startPos2 = endPos1;
        Vector2 endPos2 = startPos1;

        float timer = 0f;
        while (timer < swapDuration)
        {
            if (candy1 == null || candy2 == null) yield break;
            timer += Time.deltaTime;
            float t = timer / swapDuration;
            candy1.transform.position = Vector2.Lerp(startPos1, endPos1, t);
            candy2.transform.position = Vector2.Lerp(startPos2, endPos2, t);
            yield return null;
        }

        if (candy1 != null) candy1.transform.position = endPos1;
        if (candy2 != null) candy2.transform.position = endPos2;
    }

    public IEnumerator AnimateDestroyMatches(HashSet<GameObject> matches)
    {
        List<Coroutine> destroyAnims = new List<Coroutine>();
        foreach (GameObject candyToDestroy in matches)
        {
            if (candyToDestroy != null)
            {
                if (explosionFXPrefab != null)
                {
                    GameObject explosion = Instantiate(explosionFXPrefab, candyToDestroy.transform.position, Quaternion.identity);
                    Destroy(explosion, 1f); // Giả sử hiệu ứng nổ sẽ tự hủy sau 1 giây
                }
                destroyAnims.Add(StartCoroutine(AnimateDestroySingleCandy(candyToDestroy, destroyAnimationDuration)));
            }
        }
        foreach (Coroutine anim in destroyAnims)
        {
            yield return anim;
        }
    }

    private IEnumerator AnimateDestroySingleCandy(GameObject candy, float duration)
    {
        if (candy == null) yield break;
        Vector2 originalScale = candy.transform.localScale;
        float timer = 0f;
        while (timer < duration)
        {
            if (candy == null) yield break;
            timer += Time.deltaTime;
            float t = 1 - (timer / duration);
            candy.transform.localScale = originalScale * t;
            yield return null;
        }
        if (candy != null)
        {
            candy.transform.localScale = Vector3.zero;
            // Không Destroy GameObject ở đây, Board.DestroyCandies sẽ làm việc đó
            // để đảm bảo logic và animation được tách bạch.
        }
    }

    public IEnumerator AnimateDropCandies(List<Vector2Int> droppedPositions, IBoard board)
    {
        List<Coroutine> dropAnims = new List<Coroutine>();
        foreach (Vector2Int pos in droppedPositions)
        {
            // Lấy kẹo tại vị trí logic mới (vì Board đã cập nhật data)
            GameObject candy = board.GetCandy(pos.x, pos.y);
            if (candy != null)
            {
                // Vị trí bắt đầu của animation là vị trí cũ của kẹo trên board trước khi gravity
                // Vị trí này cần được truyền từ Board khi nó tính toán gravity.
                // Để đơn giản, hiện tại ta giả định kẹo đã được đặt đúng vị trí vật lý trước khi gọi animation
                // hoặc lấy vị trí hiện tại của GO và vị trí đích theo board.GetWorldPosition
                Vector2 startPos = candy.transform.position; // Vị trí hiện tại của GO
                Vector2 endPos = board.GetWorldPosition(pos.x, pos.y); // Vị trí đích logic
                dropAnims.Add(StartCoroutine(AnimateSingleCandyDrop(candy, startPos, endPos, dropDuration)));
            }
        }
        foreach (Coroutine anim in dropAnims)
        {
            yield return anim;
        }
    }

    public IEnumerator AnimateNewCandiesDrop(List<Vector2Int> newCandyPositions, IBoard board)
    {
        List<Coroutine> newCandyDropAnims = new List<Coroutine>();
        foreach (Vector2Int pos in newCandyPositions)
        {
            GameObject candy = board.GetCandy(pos.x, pos.y);
            if (candy != null)
            {
                Vector2 startPos = candy.transform.position; // Kẹo mới được Instantiate ở vị trí cao
                Vector2 endPos = board.GetWorldPosition(pos.x, pos.y);
                newCandyDropAnims.Add(StartCoroutine(AnimateSingleCandyDrop(candy, startPos, endPos, dropDuration)));
            }
        }
        foreach (Coroutine anim in newCandyDropAnims)
        {
            yield return anim;
        }
    }

    private IEnumerator AnimateSingleCandyDrop(GameObject candy, Vector2 startPosition, Vector2 endPosition, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            if (candy == null) yield break;
            timer += Time.deltaTime;
            float t = timer / duration;
            candy.transform.position = Vector2.Lerp(startPosition, endPosition, t);
            yield return null;
        }
        if (candy != null)
        {
            candy.transform.position = endPosition;
        }
    }

    public void PlayMatchSound()
    {
        // TODO: Phát âm thanh match
        // Ví dụ: AudioManager.Instance.PlaySFX("MatchSound");
    }

    public void SpawnExplosionEffect(Vector2Int position, float scale = 1)
    {
        if (explosionFXPrefab != null)
        {
            GameObject explosion = Instantiate(explosionFXPrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
            explosion.transform.localScale = Vector3.one * scale; // Thay đổi kích thước nếu cần
            Destroy(explosion, 1f);
        }
    }

    public IEnumerator PlayDoubleStripedComboFX(Vector2Int center)
    {
        bool isHorizontal = true;
        // Hiệu ứng nổ hàng ngang
        for (int x = 0; x < _board.Width; x++)
        {
            Vector2 worldPos = _board.GetWorldPosition(x, center.y);
            SpawnLaserEffect(worldPos, isHorizontal);
        }

        // Hiệu ứng nổ cột dọc
        for (int y = 0; y < _board.Height; y++)
        {
            Vector2 worldPos = _board.GetWorldPosition(center.x, y);
            SpawnLaserEffect(worldPos, !isHorizontal);
        }

        yield return new WaitForSeconds(0.3f); // Thời gian hiệu ứng
    }

    public IEnumerator PlayWrappedCandyFX(Vector2Int center, bool isBigExplosion)
    {
        Vector2Int worldPos = _board.GetWorldPosition(center.x, center.y).ConvertTo<Vector2Int>();

        if (isBigExplosion)
        {
            // Hiệu ứng vụ nổ lớn
            SpawnExplosionEffect(worldPos, scale: 2f);
            //CameraShake.Shake(duration: 0.2f, strength: 0.5f);
        }
        else
        {
            // Vụ nổ nhỏ thường (khi là kẹo bọc đơn lẻ)
            SpawnExplosionEffect(worldPos, scale: 1f);
        }

        yield return new WaitForSeconds(0.3f);
    }

    public IEnumerator PlayStrippedWrappedComboFX(Vector2Int center)
    {
        Vector2 worldCenter = _board.GetWorldPosition(center.x, center.y);
        bool isHorizontal = true;
        // Hiệu ứng nổ chữ thập lớn – 3 hàng và 3 cột
        for (int dx = -1; dx <= 1; dx++)
        {
            int x = center.x + dx;
            if (x > 0 && x <= _board.Width )
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    Vector2 pos = _board.GetWorldPosition(x, y);
                    SpawnLaserEffect(pos, !isHorizontal);
                }
            }
        }

        for (int dy = -1; dy <= 1; dy++)
        {
            int y = center.y + dy;
            if (y > 0 && y <= _board.Height)
            {
                for (int x = 0; x < _board.Width; x++)
                {
                    Vector2 pos = _board.GetWorldPosition(x, y);
                    SpawnLaserEffect(pos, isHorizontal);
                }
            }
        }

        //CameraShake.Shake(duration: 0.25f, strength: 0.6f);
        yield return new WaitForSeconds(0.4f);
    }

    void SpawnLaserEffect(Vector2 position, bool isHozirontal)
    {
        GameObject lazeEffect = Instantiate(lazeHozirontalPerfab, position, Quaternion.Euler(0, 0, isHozirontal ? 0 : 90));
        Destroy(lazeEffect, 1f); // Giả sử hiệu ứng sẽ tự hủy sau 1 giây
    }
}