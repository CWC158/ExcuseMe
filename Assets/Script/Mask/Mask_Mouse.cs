using System.Collections.Generic;
using System.Net.Http.Headers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MaskMou : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private List<RawImage> stickers = new List<RawImage>();
    private ControllerSystemMouse controllerSystem;
    private Texture2D texture;
    private List<bool> pasteState = new List<bool>();
    public Transform uiParent;
    public void Awake()
    {
        controllerSystem = FindFirstObjectByType<ControllerSystemMouse>();
    }
    void Start()
    {
        texture = Resources.Load<Texture2D>("Texture/flower");

        for(int i = 0; i < 17; i++)
        {
            stickers.Add(null);
        }

        for(int i = 0; i < 17; i++)
        {
            pasteState.Add(false);
        }

    }

    // Update is called once per frame
    void Update()
    {
        PasteStickers();
    }
    void PasteStickers()
    {
        if(controllerSystem != null)
        {
            for(int i = 0; i < controllerSystem.pointState.Count; i++)
            {
                if(controllerSystem.pointState[i] == true && pasteState[i] == false)
                {
                    GameObject instance = new GameObject("Sticker");
                    instance.transform.SetParent(uiParent != null ? uiParent : transform, false);
                    RawImage img = instance.AddComponent<RawImage>();
                    img.texture = texture;
                    img.rectTransform.sizeDelta = new Vector2(200f, 200f);
                    img.rectTransform.anchoredPosition = new Vector2(img.rectTransform.position.x - img.rectTransform.sizeDelta.x / 2f, img.rectTransform.position.y - img.rectTransform.sizeDelta.y / 2f);
                    stickers[i] = img;
                    pasteState[i] = true;
                }
                if(stickers[i] != null && pasteState[i] == true)
                {
                    Vector2 pos = new Vector2(controllerSystem.playerData.points[i].x, controllerSystem.playerData.points[i].y);
                    stickers[i].rectTransform.position = pos;
                }
            }
        }
    }
    
}
