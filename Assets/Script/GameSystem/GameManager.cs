using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Threading;
using System;
using System.IO;
using UnityEditor;

public class GameManager : MonoBehaviour
{
    public PlayerData[] _players;
    public int[] _playerId;
    private int[] _padId;
    //---------------------------------------------------------
    public YOLODatas _tracked;
    public List<bool>[] pointState;
    //---------------------------------------------------------
    private Mask mask;
    //---------------------------------------------------------
    public bool[] _ready;
    [SerializeField]private string folder;
    public static event Action _start;
    public static event Action _stop;
    private bool gameRunning;
    //---------------------------------------------------------
    private string record;
    private int phase;
    //---------------------------------------------------------
    private Controller controller;
    [SerializeField] private RawImage[] pictures;
    private UITrigger uiTrigger;
    public bool isSetup = false;
    //private DatasToJson json = new DatasToJson();
    void Awake()
    {
        _tracked = FindFirstObjectByType<YOLODatas>();
        mask = FindFirstObjectByType<Mask>();
        controller = FindFirstObjectByType<Controller>();
        uiTrigger = FindFirstObjectByType<UITrigger>();
    }
    void Start()
    {
        _start += () =>GameStart(out gameRunning);
        _stop += () =>GameStop(out gameRunning);

        folder = "Assets/Screenshots";

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        _tracked.StartLoad();
        StartCoroutine(UpdatePlayerData());
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GameReload(out isSetup, out gameRunning);
            PlayerHidden();
        }
        if(!gameRunning && uiTrigger.triggerNum == 1)
        {
            try
            {
                Ready();
            }
            catch(Exception e)
            {
                Debug.Log(e);
            }
        }
    }
    // Update the playersData array with the current cursor, selection box, selecting state, selection points, and landmark be selected for each player
    IEnumerator UpdatePlayerData()
    {
        while (true)
        {
            try
            {
                for(int i = 0; i < _players.Length; i++)
                {
                    if(_tracked.tracked.people == null) continue;
                    for(int j = 0; j < _tracked.tracked.people.Length; j++)
                    {
                        int index = Array.IndexOf(_playerId, _tracked.tracked.people[j].person_id);
                        if (index == -1) continue;

                        _players[index].points = _tracked.points[j];
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("Waiting for Tracked Data...：" + e);
            }
            yield return null;
        }
    }
    private void GameReload(out bool isSetup, out bool gameRunning)
    {   
        controller.Reload();
        controller.enabled = true;
        
        _playerId = new int[controller.gamePads.Length];
        _players = new PlayerData[controller.gamePads.Length];
        _ready = new bool[controller.gamePads.Length];
        _padId = new int[controller.gamePads.Length];
        pointState = new List<bool>[controller.gamePads.Length];

        for (int i = 0; i < _playerId.Length; i++)
        {
            try
            {
                _playerId[i] = _tracked.tracked.people[i].person_id;
            }
            catch(Exception e)
            {
                _playerId[i] = -1;
                Debug.LogWarning($"Player {i} not found ：" + e);
            }
        }

        for (int i = 0; i < _padId.Length; i++)
        {
            _padId[i] = controller.gamePads[i].deviceId;
        }

        for(int i = 0; i < pointState.Length; i++)
        {
            pointState[i] = new List<bool>();
            for(int j = 0; j < _tracked.points[i].Count; j++)
            {
                pointState[i].Add(false);
            }
        }
        for (int i = 0; i < _players.Length; i++)
        {
            _players[i] = new PlayerData(_playerId[i], _padId[i], controller._cursors[i], controller._selectionbox[i], controller._state[i], _tracked.points[i], pointState[i]);
        }

        for (int i = 0; i < _ready.Length; i++)
        {
            _ready[i] = false;
        }

        mask.Reload();
        uiTrigger.triggerNum = 0;
        uiTrigger.director.time = 0f;
        isSetup = true;
        gameRunning = false;
    }
    private void GameStart(out bool gameRunning)
    {
        for(int i = 0; i < pointState.Length; i++)
        {
            pointState[i].Clear();
            for(int j = 0; j < _tracked.points[i].Count; j++)
            {
                pointState[i].Add(false);
            }
            _players[i].pointState = pointState[i];
        }

        mask.Reload();
        PlayerHidden();
        uiTrigger.triggerNum = 2;

        record = DateTime.Now.ToString("MMdd_HHmmss");
        StartCoroutine(Timeline());

        gameRunning = true;
    }
    private void GameStop(out bool gameRunning)
    {
        for(int i = 0; i < _ready.Length; i++)
        {
            _ready[i] = false;
        }
        gameRunning = false;
        controller.enabled = false;

         for(int i = 0; i < pictures.Length; i++)
        {
            Texture2D loadedTexture = new Texture2D(1, 1);
            string path = folder + "/" + record + $"__{i}" + ".png";
            byte[] pic = File.ReadAllBytes(path);

            loadedTexture.LoadImage(pic);
            pictures[i].texture = loadedTexture;
        }
        uiTrigger.triggerNum = 3;
        #if UNITY_EDITOR
        AssetDatabase.Refresh();
        #endif
    }
    private void Ready()
    {
        if(_ready.All(x => x == true))
        {
            Debug.Log("All Players are Ready. Starting the Game...");
            _start?.Invoke();
        }
    }
    IEnumerator Timeline()
    {
        string file = ""; 
        phase = 0;

        while(true)
        {
            switch (phase)
            {
                case 0:
                    if (uiTrigger.director.time >= 6f)
                    {
                        Debug.Log("Take Screenshot 1");
                        file = $"{folder}/{record}__0.png";
                        ScreenCapture.CaptureScreenshot(file);
                        phase = 1;
                    }
                    break;

                case 1:
                    if (uiTrigger.director.time >= 17f)
                    {
                        Debug.Log("Start!!!");
                        PlayerShow();
                        phase = 2;
                    }
                    break;

                case 2:
                    if (uiTrigger.director.time >= 28.25f)
                    {
                        Debug.Log("Take Screenshot 2");
                        file = $"{folder}/{record}__1.png";
                        ScreenCapture.CaptureScreenshot(file);
                        phase = 3;
                    }
                    break;

                case 3:
                    if (uiTrigger.director.time >= 38.5f)
                    {
                        Debug.Log("Take Screenshot 3");
                        file = $"{folder}/{record}__2.png";
                        ScreenCapture.CaptureScreenshot(file);
                        phase = 4;
                    }
                    break;

                case 4:
                    if (uiTrigger.director.time >= uiTrigger.director.duration)
                    {
                        Debug.Log("Take Screenshot 4");
                        file = $"{folder}/{record}__3.png";
                        ScreenCapture.CaptureScreenshot(file);
                        phase = 5;
                        yield return null;

                        Debug.Log("Game Stop by Timeline");
                        _stop?.Invoke();
                        yield break;
                    }
                    break;
            }
            yield return new WaitForSeconds(0.05f); 
        }
    }
    public void PlayerHidden()
    {
        for(int i = 0; i < 4; i++)
        {
            controller._cursors[i].SetActive(false);
            controller._cursors[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(960, 540);
            
            controller._selectionbox[i].SetActive(false);
            controller._selectionbox[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(960, 540);
            controller._selectionbox[i].GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
        }
        controller.enabled = false;
    }
    public void PlayerShow()
    {
        for(int i = 0; i < 4; i++)
        {
            controller._cursors[i].SetActive(true);
            
            controller._selectionbox[i].SetActive(true);
        }
        controller.enabled = true;
    }
    public class PlayerData
    {
        public int playerId;
        public int padId;
        public GameObject cursor;
        public GameObject selectionBox;
        public bool isSelected;
        public List<Vector2> points;
        public List<bool> pointState;
        public PlayerData(int playerId = 0, int padId = 0, GameObject cursor = null, GameObject selectionBox = null, bool isSelecting = false, List<Vector2> points = null, List<bool> pointState = null)
        {
            this.playerId = playerId;
            this.padId = padId;
            this.cursor = cursor;
            this.selectionBox = selectionBox;
            this.isSelected = isSelecting;
            this.points = points ?? new List<Vector2>();
            this.pointState = pointState ?? new List<bool>();
        }
    }
}
