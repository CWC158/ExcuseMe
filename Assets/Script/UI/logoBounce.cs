using UnityEngine;

public class logoBounce : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    [Header("移動設定")]
    [Tooltip("移動速度")]
    public float speed = 300f; 

    [Header("限定移動範圍尺寸")]
    public float targetWidth = 2048f;
    public float targetHeight = 1152f;

    private Vector2 moveDirection;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // 尋找最上層的 Canvas，用來計算縮放比例
        parentCanvas = GetComponentInParent<Canvas>();

        // 初始隨機方向
        float randomAngle = Random.Range(25f, 65f);
        float xDir = Random.value > 0.5f ? 1f : -1f;
        float yDir = Random.value > 0.5f ? 1f : -1f;
        moveDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad) * xDir, Mathf.Sin(randomAngle * Mathf.Deg2Rad) * yDir).normalized;
    }

    void Update()
    {
        // 1. 先照常移動 (利用 Translate 直接在世界空間移動，無視 UI 錨點干擾)
        Vector3 moveStep = (Vector3)(moveDirection * speed * Time.deltaTime);
        rectTransform.Translate(moveStep, Space.World);

        // 2. 抓取 Logo 目前在世界空間（World Space）中的四個角座標
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        // corners[0] = 左下, corners[1] = 左上, corners[2] = 右上, corners[3] = 右下

        // 3. 計算 $2048 \times 1152$ 區域在世界空間中的實際大小
        // 必須乘上 Canvas 的 lossyScale，這樣在任何解析度下才會等比例縮放
        float canvasScaleX = parentCanvas != null ? parentCanvas.transform.lossyScale.x : 1f;
        float canvasScaleY = parentCanvas != null ? parentCanvas.transform.lossyScale.y : 1f;
        
        float worldWidth = targetWidth * canvasScaleX;
        float worldHeight = targetHeight * canvasScaleY;

        // 以螢幕/Canvas 中心點為基準，算出 2048x1152 的四個世界邊界
        float screenCenterX = Screen.width / 2f;
        float screenCenterY = Screen.height / 2f;

        float minWorldX = screenCenterX - (worldWidth / 2f);
        float maxX = screenCenterX + (worldWidth / 2f);
        float minWorldY = screenCenterY - (worldHeight / 2f);
        float maxWorldY = screenCenterY + (worldHeight / 2f);

        // 4. 進行邊界碰撞檢查與反彈
        Vector3 currentWorldPos = rectTransform.position;

        // 檢查左右邊界
        if (corners[0].x < minWorldX) // 撞到左邊
        {
            moveDirection.x = Mathf.Abs(moveDirection.x); // 強制往右
            float overlap = minWorldX - corners[0].x;
            currentWorldPos.x += overlap + 1f; // 推回邊界內
        }
        else if (corners[2].x > maxX) // 撞到右邊
        {
            moveDirection.x = -Mathf.Abs(moveDirection.x); // 強制往左
            float overlap = corners[2].x - maxX;
            currentWorldPos.x -= (overlap + 1f);
        }

        // 檢查上下邊界
        if (corners[0].y < minWorldY) // 撞到下邊
        {
            moveDirection.y = Mathf.Abs(moveDirection.y); // 強制往上
            float overlap = minWorldY - corners[0].y;
            currentWorldPos.y += overlap + 1f;
        }
        else if (corners[1].y > maxWorldY) // 撞到上邊
        {
            moveDirection.y = -Mathf.Abs(moveDirection.y); // 強制往下
            float overlap = corners[1].y - maxWorldY;
            currentWorldPos.y -= (overlap + 1f);
        }

        // 套用修正後的世界座標
        rectTransform.position = currentWorldPos;
    }
}