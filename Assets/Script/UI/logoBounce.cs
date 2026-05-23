using UnityEngine;

public class logoBounce : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    [Header("移動設定")]
    [Tooltip("移動速度")]
    public float speed = 300f; 

    private Vector2 moveDirection;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        // 尋找最上層的 Canvas
        parentCanvas = GetComponentInParent<Canvas>();

        // 初始隨機方向
        float randomAngle = Random.Range(25f, 65f);
        float xDir = Random.value > 0.5f ? 1f : -1f;
        float yDir = Random.value > 0.5f ? 1f : -1f;
        moveDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad) * xDir, Mathf.Sin(randomAngle * Mathf.Deg2Rad) * yDir).normalized;
    }

    void Update()
    {
        // 1. 先照常移動
        rectTransform.anchoredPosition += moveDirection * speed * Time.deltaTime;

        // 2. 核心大絕招：直接把 Logo 的四個角換算成「真正的螢幕坐標 (Screen Space)」
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        // corners[0] = 左下, corners[1] = 左上, corners[2] = 右上, corners[3] = 右下

        // 如果有 Canvas，用 Canvas 的相機或螢幕大小來抓標準邊界
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // 3. 檢查左/右邊界 (用世界/螢幕坐標判斷)
        if (corners[0].x < 0) // 左邊超出螢幕
        {
            moveDirection.x = Mathf.Abs(moveDirection.x); // 強制往右
            rectTransform.anchoredPosition += new Vector2(5f, 0f); // 稍微往內推，防止卡死
        }
        else if (corners[2].x > screenWidth) // 右邊超出螢幕
        {
            moveDirection.x = -Mathf.Abs(moveDirection.x); // 強制往左
            rectTransform.anchoredPosition += new Vector2(-5f, 0f);
        }

        // 4. 檢查上/下邊界
        if (corners[0].y < 0) // 下邊超出螢幕
        {
            moveDirection.y = Mathf.Abs(moveDirection.y); // 強制往上
            rectTransform.anchoredPosition += new Vector2(0f, 5f);
        }
        else if (corners[1].y > screenHeight) // 上邊超出螢幕
        {
            moveDirection.y = -Mathf.Abs(moveDirection.y); // 強制往下
            rectTransform.anchoredPosition += new Vector2(0f, -5f);
        }
    }
}