using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Mask : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private List<RawImage>[] stickers;
    private GameManager gameManager;
    [SerializeField] private Texture2D texture;
    private List<bool>[] pasteState;
    public Transform[] uiParents;
    void Start()
    {
        gameManager = GameObject.FindFirstObjectByType<GameManager>();
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
            Debug.Log(e);
        }
    }
    void PasteStickers()
    {
        if(gameManager != null)
        {
            for(int i = 0; i < gameManager._players.Length; i++)
            {
                for(int j = 0; j < gameManager.pointState[i].Count; j++)
                {
                    if(gameManager.pointState[i][j] == true && pasteState[i][j] == false)
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
                        Vector2 pos = new Vector2(gameManager._players[i].points[j].x, gameManager._players[i].points[j].y);
                        stickers[i][j].rectTransform.position = pos;
                    }
                }
            }
        }
    }
    public void Reload()
    {
        pasteState = new List<bool>[gameManager._players.Length];
        stickers = new List<RawImage>[gameManager._players.Length];
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
