using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
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
    public bool gameRunning;
    //---------------------------------------------------------
    private Controller controller;
    private UITrigger uiTrigger;
    private GameRecorder screenShots;
    public bool isSetup = false;
    //private DatasToJson json = new DatasToJson();
    void Awake()
    {
        _tracked = FindFirstObjectByType<YOLODatas>();
        mask = FindFirstObjectByType<Mask>();
        controller = FindFirstObjectByType<Controller>();
        uiTrigger = FindFirstObjectByType<UITrigger>();
        screenShots = FindFirstObjectByType<GameRecorder>();
    }
    void Start()
    {
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
                Debug.Log(e.Message);
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
                Debug.LogWarning("Waiting for Tracked Data...：" + e.Message);
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
                Debug.LogWarning($"Player {i} not found ：" + e.Message);
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

        StartCoroutine(screenShots.Timeline());

        gameRunning = true;
    }
    public void GameStop(out bool gameRunning)
    {
        for(int i = 0; i < _ready.Length; i++)
        {
            _ready[i] = false;
        }
        gameRunning = false;
        controller.enabled = false;

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
            GameStart(out gameRunning);
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
