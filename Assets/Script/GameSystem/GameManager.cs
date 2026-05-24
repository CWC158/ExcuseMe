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
    private int[] _playerId;
    private int[] _padId;
    //---------------------------------------------------------
    private YOLODatas _tracked;
    private Thread thread;
    public List<bool>[] pointState;
    //---------------------------------------------------------
    private Mask mask;
    //---------------------------------------------------------
    public bool[] _ready;
    public float time;
    [SerializeField] private float interval;
    private float captureTime;
    [SerializeField]private string folder;
    public static event Action _start;
    public static event Action _stop;
    private bool gameRunning;
    //---------------------------------------------------------
    private string record;
    private int num;
    private Coroutine coroutine;
    //---------------------------------------------------------
    private Controller controller;
    //private DatasToJson json = new DatasToJson();
    void Awake()
    {
        _tracked = FindFirstObjectByType<YOLODatas>();
        mask = FindFirstObjectByType<Mask>();
        controller = FindFirstObjectByType<Controller>();
    }
    void Start()
    {
        _start += GameStart;
        _stop += GameStop;

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
            ReloadPlayerData();
        }
        if(!gameRunning)
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
    // Define a Player class to store player information such as player ID, gamepad ID, and cursor reference
    public class PlayerData
    {
        public int playerId;
        public int padId;
        public RectTransform cursor;
        public RectTransform selectionBox;
        public bool isSelected;
        public List<Vector2> points;
        public List<bool> pointState;
        public PlayerData(int playerId = 0, int padId = 0, RectTransform cursor = null, RectTransform selectionBox = null, bool isSelecting = false, List<Vector2> points = null, List<bool> pointState = null)
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
    void Ready()
    {
        if(_ready.All(x => x == true))
        {
            Debug.Log("All Players are Ready. Starting the Game...");
            _start?.Invoke();
        }
    }
    // Check if the landmark points of other players are within the selection area of the current player and update the landmarkSelected list accordingly
    public void CalculatePointState(int gamepadIndex)
    {
        Vector3[] corners = new Vector3[4];
        _players[gamepadIndex].selectionBox.GetComponent<RectTransform>().GetWorldCorners(corners);

        for (int n = 0; n < _tracked.tracked.people.Length; n++)
        {
            int index = Array.IndexOf(_playerId, _tracked.tracked.people[n].person_id);
            if (index == -1) continue;

            // if (index == gamepadIndex) continue;
            for(int m = 0; m < _players[index].pointState.Count; m++)
            {
                Vector2 position = _players[index].points[m];

                float xRange = Math.Abs(corners[2].x - corners[1].x);
                float yRange = Math.Abs(corners[1].y - corners[0].y);
                Vector2 selectionCenter = _players[gamepadIndex].selectionBox.anchoredPosition;

                if(Math.Abs(position.x - selectionCenter.x) <= xRange / 2f && Math.Abs(position.y - selectionCenter.y) <= yRange / 2f)
                {
                    _players[index].pointState[m] = true;
                }
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
                Debug.Log("Waiting for Data...");
            }
            yield return null;
        }
    }
    public void ReloadPlayerData()
    {   
        if(coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        controller.Reload();

        time = 0f;
        captureTime = interval;
        num = 0;
        gameRunning = false;
        
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
                Debug.Log($"Player {i} not found");
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
    }
    public void GameStart()
    {
        for(int i = 0; i < pointState.Length; i++)
        {
            pointState[i] = new List<bool>();
            for(int j = 0; j < _tracked.points[i].Count; j++)
            {
                pointState[i].Add(false);
            }
            _players[i].pointState = pointState[i];
        }

        mask.Reload();

        if(coroutine != null)
        {
            StopCoroutine(coroutine);
        }
        record = DateTime.Now.ToString("MMdd_HHmmss");
        coroutine = StartCoroutine(Run());

        gameRunning = true;
    }
    IEnumerator Run()
    {
        while(num < 3)
        {
            time += Time.deltaTime;
            if(time >= captureTime)
            {
                num += 1;
                string file = folder + "/" + record + $"__{num}" + ".png";
                ScreenCapture.CaptureScreenshot(file);
                captureTime += interval;
            }
            yield return null;
        }
        _stop?.Invoke();
    }
    void GameStop()
    {
        gameRunning = false;
        StopCoroutine(coroutine);
        StopCoroutine(controller.coroutine);

        #if UNITY_EDITOR
        AssetDatabase.Refresh();
        #endif
    }
}
