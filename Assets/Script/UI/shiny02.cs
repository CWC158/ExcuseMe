using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class shiny02 : MonoBehaviour
{
    private Image uiImage;
    
    [Header("閃爍設定")]
    [Tooltip("最暗時的透明度 (0 ~ 1)")]
    public float minAlpha = 0.2f;
    
    [Tooltip("最亮時的透明度 (0 ~ 1)")]
    public float maxAlpha = 1.0f;
    
    [Tooltip("閃爍的速度，數值越高閃越快")]
    public float flickerSpeed = 2.0f;

    void Start()
    {
        // 取得身上的 Image 元件
        uiImage = GetComponent<Image>();

        if (uiImage != null)
        {
            // 開啟協程讓它無限循環閃爍
            StartCoroutine(DoFlicker());
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] 上找不到 Image 元件！", gameObject);
        }
    }

    IEnumerator DoFlicker()
    {
        float timer = 0f;

        while (true)
        {
            timer += Time.deltaTime * flickerSpeed;
            
            // 使用 Mathf.PingPong 讓數值在 0 ~ 1 之間來回平滑變動
            float lerpValue = Mathf.PingPong(timer, 1f);
            
            // 根據 lerpValue 計算出當前的 Alpha 值
            float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, lerpValue);

            // 更新 Image 的顏色透明度
            Color newColor = uiImage.color;
            newColor.a = currentAlpha;
            uiImage.color = newColor;

            // 等待下一幀繼續
            yield return null;
        }
    }
}