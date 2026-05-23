using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Mask : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private List<RawImage>[] stickers = new List<RawImage>[4];
    private GameSystem gameSystem;
    [SerializeField] private Texture2D texture;
    private List<bool>[] pasteState = new List<bool>[4];
    public Transform[] uiParents;
    void Start()
    {
        gameSystem = GameObject.FindFirstObjectByType<GameSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            PasteStickers();
        }
        catch (Exception e)
        {
            Debug.Log("No Datas");
        }
    }
    void PasteStickers()
    {
        if(gameSystem != null)
        {
            for(int i = 0; i < gameSystem._players.Length; i++)
            {
                for(int j = 0; j < gameSystem._players[i].pointState.Count; j++)
                {
                    if(gameSystem.pointState[i][j] == true && pasteState[i][j] == false)
                    {
                        GameObject instance = new GameObject("Sticker");
                        instance.transform.SetParent(uiParents[i] != null ? uiParents[i] : transform, false);
                        RawImage img = instance.AddComponent<RawImage>();
                        img.texture = texture;
                        img.rectTransform.sizeDelta = new Vector2(200f, 200f);
                        img.rectTransform.anchoredPosition = new Vector2(img.rectTransform.position.x - img.rectTransform.sizeDelta.x / 2f, img.rectTransform.position.y - img.rectTransform.sizeDelta.y / 2f);
                        stickers[i][j] = img;
                        pasteState[i][j] = true;
                    }
                    if(stickers[i][j] != null && pasteState[i][j] == true)
                    {
                        Vector2 pos = new Vector2(gameSystem._players[i].points[j].x, gameSystem._players[i].points[j].y);
                        stickers[i][j].rectTransform.position = pos;
                    }
                }
            }
        }
    }
    public void LoadDatas()
    {
        for(int k = 0; k < uiParents.Length; k++)
        {
            foreach(Transform child in uiParents[k] != null ? uiParents[k] : transform)
            {
                Destroy(child.gameObject);
            }
        }
        for(int i = 0; i < stickers.Length; i++)
        {
            stickers[i] = new List<RawImage>();
            for(int j = 0; j < gameSystem.pointState[i].Count; j++)
            {
                stickers[i].Add(null);
            }
        }
        for(int i = 0; i < pasteState.Length; i++)
        {
            pasteState[i] = new List<bool>();
            for(int j = 0; j < gameSystem.pointState[i].Count; j++)
            {
                pasteState[i].Add(false);
            }
        }
    }
}
