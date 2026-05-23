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

public class GameSystem : MonoBehaviour
{
    public PlayerData[] _players;
    private int[] _playerId;
    
    [Tooltip("gamepads of each player")]
    [SerializeField] private Gamepad[] gamePads;
    private int[] _padId;

    [Tooltip("cursors of each player")]
    [SerializeField]private RectTransform[] _cursors;
    [SerializeField] private float cursorSpeed = 100f;

    [Tooltip("Selection boxes of each player")]
    [SerializeField] private RectTransform[] _selectionbox;
    private bool[] _state;
    //---------------------------------------------------------
    private Vector2[] cursorPos;
    private Vector2[] cursorStartPos;
    private Vector2[] cursorEndPos;
    //---------------------------------------------------------
    private YOLODatas _tracked;
    private List<Vector2>[] points = new List<Vector2>[4];
    private Thread thread;
    public List<bool>[] pointState = new List<bool>[4];
    //---------------------------------------------------------
    private Mask mask;
    //---------------------------------------------------------
    private Coroutine[] _controllers;
    private int[] _ready;
    private int ready;
    public float time;
    [SerializeField] private float interval;
    private float captureTime;
    [SerializeField]private string folder;
    public static event Action _start;
    public static event Action _stop;
    //---------------------------------------------------------
    private string record;
    private int num;
    private Coroutine coroutine;
    //---------------------------------------------------------
    //private DatasToJson json = new DatasToJson();
    void Awake()
    {
        _tracked = FindFirstObjectByType<YOLODatas>();
        mask = FindFirstObjectByType<Mask>();
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
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReloadPlayerData();
        }
        try
        {
            Ready();
        }
        catch(Exception e)
        {
            Debug.Log("Not Yet Load Player Data");
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
        int r = 0;
        for(int i = 0; i < _ready.Length; i++)
        {
            r += _ready[i];
        }
        if(r == ready)
        {
            _start?.Invoke();
        }
    }
    // Capture the current cursor's Position value for each player and store it in the mousePos array
    void CursorMovement(int gamepadIndex)
    {
        Gamepad gamePad = gamePads[gamepadIndex];
        Vector2 leftStick = gamePad.leftStick.ReadValue();
        _cursors[gamepadIndex].anchoredPosition += leftStick * cursorSpeed * Time.deltaTime;

        _cursors[gamepadIndex].anchoredPosition = new Vector2(
            Mathf.Clamp(_cursors[gamepadIndex].anchoredPosition.x, 0, Screen.width),
            Mathf.Clamp(_cursors[gamepadIndex].anchoredPosition.y, 0, Screen.height)
            );

        cursorPos[gamepadIndex] = _cursors[gamepadIndex].anchoredPosition;
    }
    // Calculate the size and position of the selection box based on the start and current mouse positions for each player
    void SelectionBox(int gamepadIndex)
    {
        if (!_players[gamepadIndex].isSelected) return ;

        Vector2 selectionSize = cursorPos[gamepadIndex] - cursorStartPos[gamepadIndex];

        _selectionbox[gamepadIndex].sizeDelta = new Vector2(Mathf.Abs(selectionSize.x), Mathf.Abs(selectionSize.y));
        _selectionbox[gamepadIndex].anchoredPosition = new Vector2(cursorStartPos[gamepadIndex].x + selectionSize.x / 2f, cursorStartPos[gamepadIndex].y + selectionSize.y / 2f);
    }
    // Capture the cursor's Position value of start for each player and store it in the mouseStartPos array
    void StartedSelecting(int gamepadIndex)
    {
        if(gamePads[gamepadIndex].buttonNorth.wasPressedThisFrame && _state[gamepadIndex] == false)
        {

            _state[gamepadIndex] = true;

            cursorStartPos[gamepadIndex] = cursorPos[gamepadIndex];
            _selectionbox[gamepadIndex].GetComponent<RawImage>().enabled = true;

            Debug.Log("Player " + (gamepadIndex + 1) + " is selecting.");
        }
    }
    // Capture the cursor's Position value of End for each player and store it in the mouseEndPos array
    void CancelledSelecting(int gamepadIndex)
    {
        if(gamePads[gamepadIndex].buttonEast.wasReleasedThisFrame && _state[gamepadIndex] == true)
        {
            _state[gamepadIndex] = false;

            cursorEndPos[gamepadIndex] = cursorPos[gamepadIndex];

            Debug.Log("Player " + (gamepadIndex + 1) + " cancelled selection.");

            CalculatePointState(gamepadIndex);
            _selectionbox[gamepadIndex].GetComponent<RawImage>().enabled = false;
        }
    }
    // Continuously check for player input and update the cursor position, selection points, and selection box for each player
    IEnumerator ControllerCoroutine(int gamepadIndex)
    {
        while (true)
        {
            try
            {
                UpdatePlayerData();
                Debug.Log("Player " + (gamepadIndex + 1) + " is using Gamepad: " + gamePads[gamepadIndex].name);

                CursorMovement(gamepadIndex);
                StartedSelecting(gamepadIndex);
                CancelledSelecting(gamepadIndex);
                SelectionBox(gamepadIndex);
            }
            catch (Exception e)
            {
                Debug.Log("Waiting Player " + (gamepadIndex + 1));
            }
            if (gamePads[gamepadIndex].buttonNorth.wasPressedThisFrame)
            {
                _ready[gamepadIndex] = 1;
            }
            yield return null;
        }
    }
    // Check if the landmark points of other players are within the selection area of the current player and update the landmarkSelected list accordingly
    void CalculatePointState(int gamepadIndex)
    {
        Vector3[] corners = new Vector3[4];
        _selectionbox[gamepadIndex].GetComponent<RectTransform>().GetWorldCorners(corners);
        // corners[0] = 左下, corners[1] = 左上, corners[2] = 右上, corners[3] = 右下

        // 如果有 Canvas，用 Canvas 的相機或螢幕大小來抓標準邊界
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        for (int n = 0; n < _tracked.tracked.people.Length; n++)
        {
            int index = Array.IndexOf(_playerId, _tracked.tracked.people[n].person_id);
            Debug.Log(index);

            // if (index == gamepadIndex) continue;
            for(int m = 0; m < pointState[index].Count; m++)
            {
                Vector2 position = points[n][m];

                float xRange = Math.Abs(corners[2].x - corners[1].x);
                float yRange = Math.Abs(corners[1].y - corners[0].y);

                Vector2 selectionCenter = _selectionbox[gamepadIndex].anchoredPosition;
                if(Math.Abs(position.x - selectionCenter.x) <= xRange / 2f && Math.Abs(1080f - position.y - selectionCenter.y) <= yRange / 2f)
                {
                    pointState[index][m] = true;
                    Debug.Log(pointState[index][m]);
                }
            }
        }
    }
    // Continuously update the landmark points of all tracked people and store them in the landmarkPoints list
    void UpdateKeypoints()
    {
        while(_tracked != null)
        {
            for(int i = 0; i < _playerId.Length; i++)
            {
                for (int j = 0; j < _tracked.tracked.people.Length; j++)
                {
                    if(_playerId[i] == _tracked.tracked.people[j].person_id)
                    {
                        points[i] = _tracked.points[j];
                    }
                }
            }
            // string log = "";
            // for (int i = 0; i < points.Length; i++)
            // {
            //     for (int j = 0; j < points[i].Count; j++)
            //     {
            //         Vector2 point = points[i][j];
            //         try
            //         {
            //             log += $"point_id:{j}(position:[{point.x}, {point.y}]";
            //         }
            //         catch(Exception e)
            //         {
            //             Debug.Log(e);
            //         }
            //     }
            //     log += "\n";
            // }
            // json.input = log;
            // json.saveJsonToFile(Application.dataPath + "/data_01.json");
        }
    }
    // Update the playersData array with the current cursor, selection box, selecting state, selection points, and landmark be selected for each player
    void UpdatePlayerData()
    {
        for (int i = 0; i < _players.Length; i++)
        {
            // _players[i].cursor = _cursors[i];
            // _players[i].selectionBox = _selectionbox[i];
            _players[i].isSelected = _state[i];
            _players[i].pointState = pointState[i];
        }
        for(int i = 0; i < _players.Length; i++)
        {
            if(_tracked.tracked.people == null) continue;
            for(int j = 0; j < _tracked.tracked.people.Length; j++)
            {
                if(_tracked.tracked.people[j].person_id == _players[i].playerId)
                {
                    Debug.Log(_tracked.tracked.people[j].person_id);
                    Debug.Log(_players[i].playerId);
                    _players[i].points = points[j];
                }
            }
        }
    }
    private void ReloadPlayerData()
    {
        _tracked.StartLoad();
        gamePads = Gamepad.all.ToArray();
        Debug.Log("Gamepad.all.Count: " + Gamepad.all.Count);
        Debug.Log(gamePads.Length);

        time = 0f;
        captureTime = interval;
        num = 0;
        ready = gamePads.Length;

        _controllers = new Coroutine[gamePads.Length];
        _playerId = new int[gamePads.Length];
        _padId = new int[gamePads.Length];
        cursorPos = new Vector2[gamePads.Length];
        cursorStartPos = new Vector2[gamePads.Length];
        cursorEndPos = new Vector2[gamePads.Length];
        _state = new bool[gamePads.Length];
        _players = new PlayerData[gamePads.Length];
        _ready = new int[4];

        for (int i = 0; i < _playerId.Length; i++)
        {
            _playerId[i] = _tracked.tracked.people[i].person_id;
        }

        for (int i = 0; i < _padId.Length; i++)
        {
            _padId[i] = gamePads[i].deviceId;
        }

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new List<Vector2>();
        }

        for(int i = 0; i < points.Length; i++)
        {
            pointState[i] = new List<bool>();
            Debug.Log(pointState[i]);
            for(int j = 0; j < points[i].Count; j++)// the landmarks upload too late, so have to set the array length in 33 
            {
                pointState[i].Add(false);
            }
        }
        for (int i = 0; i < _players.Length; i++)
        {
            _players[i] = new PlayerData(_playerId[i], _padId[i], _cursors[i], _selectionbox[i], _state[i], points[i], pointState[i]);
        }

        for (int i = 0; i < _ready.Length; i++)
        {
            _ready[i] = 0;
        }

        if(thread == null)
        {
            Debug.Log("Start");
            thread = new Thread(new ThreadStart(UpdateKeypoints));
            thread.IsBackground = true;
            thread.Start();
        }

        for (int i = 0; i < _controllers.Length; i++)
        {
            if(_controllers[i] == null)
            {
                _controllers[i] = StartCoroutine(ControllerCoroutine(i));
            }
        }

        mask.LoadDatas();
    }
    public void GameStart()
    {
        if(coroutine != null)
        {
            StopCoroutine(coroutine);
        }
        record = DateTime.Now.ToString("MMdd_HHmmss");
        coroutine = StartCoroutine(Run());
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
        StopCoroutine(Run());
        for(int i = 0; i < _ready.Length; i++)
        {
            _ready[i] = 0;
        }
        for (int i = 0; i < _controllers.Length; i++)
        {
            if(_controllers[i] != null)
            {
                StopCoroutine(_controllers[i]);
            }
        }

        #if UNITY_EDITOR
        AssetDatabase.Refresh();
        #endif
    }
    // [Serializable] public class DatasToJson
    // {
    //     public string input;        
    //     public void saveJsonToFile(string filePath)
    //     {
    //         File.WriteAllText(filePath, input);
    //     }
    // }
}
