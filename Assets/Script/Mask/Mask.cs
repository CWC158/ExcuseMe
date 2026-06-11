using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Mask : MonoBehaviour
{
    private List<RawImage>[] stickers;
    private GameManager gameManager;
    
    // 1. 改為陣列，讓你可以從 Inspector 拖入 3 張不同的貼紙圖片
   [SerializeField] private Texture2D[] textures;
    
    private List<bool>[] pasteState;
    public Transform[] UIParents;

    void Start()
    {
        gameManager = GameObject.FindFirstObjectByType<GameManager>();
    }

    void Update()
    {
        // 優化：拿掉 try-catch，改用 if 檢查。如果還沒初始化，就直接跳過不執行
        if (gameManager == null || pasteState == null || stickers == null) 
            return;

        PasteStickers();
    }

    void PasteStickers()
    {
        for(int i = 0; i < gameManager._players.Length; i++)
        {
            // 安全檢查：確保陣列索引不會超出範圍
            if (i >= pasteState.Length || i >= gameManager.pointState.Length) continue;

            for(int j = 0; j < gameManager.pointState[i].Count; j++)
            {
                if (j >= pasteState[i].Count) continue;

                // 當需要貼貼紙，且該位置還沒貼過時
                if(gameManager.pointState[i][j] == true && pasteState[i][j] == false)
                {
                    GameObject instance = new GameObject("Sticker");
                    instance.transform.SetParent(UIParents[i] != null ? UIParents[i] : transform, false);
                    
                    RawImage img = instance.AddComponent<RawImage>();

                    // 2. 核心隨機邏輯：隨機挑選三種貼紙的其中一種
                    if (textures != null && textures.Length > 0)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, textures.Length);
                        img.texture = textures[randomIndex];
                    }

                    img.rectTransform.sizeDelta = new Vector2(200f, 200f);
                    img.rectTransform.anchoredPosition = new Vector2(img.rectTransform.position.x - img.rectTransform.sizeDelta.x / 2f, img.rectTransform.position.y - img.rectTransform.sizeDelta.y / 2f);
                    
                    stickers[i][j] = img;
                    pasteState[i][j] = true;
                }

                // 更新貼紙位置
                if(stickers[i][j] != null && pasteState[i][j] == true)
                {
                    Vector2 pos = new Vector2(gameManager._players[i].points[j].x, gameManager._players[i].points[j].y);
                    stickers[i][j].rectTransform.position = pos;
                }
            }
        }
    }

    public void Reload()
    {
        if (gameManager == null || gameManager._players == null) return;

        pasteState = new List<bool>[gameManager._players.Length];
        stickers = new List<RawImage>[gameManager._players.Length];

        for(int k = 0; k < UIParents.Length; k++)
        {
            if (UIParents[k] == null) continue; // 避免清空 Mask 自己底下的重要 UI
            
            foreach(Transform child in UIParents[k])
            {
                Destroy(child.gameObject);
            }
        }

        for(int i = 0; i < stickers.Length; i++)
        {
            stickers[i] = new List<RawImage>();
            for(int j = 0; j < gameManager._players[i].points.Count; j++)
            {
                stickers[i].Add(null);
            }
        }

        for(int i = 0; i < pasteState.Length; i++)
        {
            pasteState[i] = new List<bool>();
            for(int j = 0; j < gameManager._players[i].points.Count; j++)
            {
                pasteState[i].Add(false);
            }
        }
    }
}