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

public class ControllerSystemYOLO : MonoBehaviour
{
    [Tooltip("gamepads of each player")]
    [SerializeField] private Gamepad[] gamePads;
    [Tooltip("cursors of each player")]
    [SerializeField]private RectTransform[] playerCursors;
    [SerializeField] private float cursorSpeed = 100f;

    [Tooltip("Selection boxes of each player")]
    [SerializeField] private RectTransform[] selectionBox;
    private Vector2[] cursorPos;
    private Vector2[] cursorStartPos;
    private Vector2[] cursorEndPos;

    [Tooltip("Selecting state of each player")]
    [SerializeField] private bool[] selectedState;

    public PlayerData[] _players;
    // private List<Vector2>[] selectionPoints;
    [SerializeField] private float minPointDistance = 10f;
    private YOLODatas _tracked;
    private List<Vector2>[] points = new List<Vector2>[4];
    public List<bool>[] pointState = new List<bool>[4];
    // private YOLODatas.Tracked tracked;
    private int[] _playerId;
    private int[] _padId;
    private Mask mask;
    //private DatasToJson json = new DatasToJson();
    private Thread thread;
    public static event Action _go;
    void Awake()
    {
        _tracked = FindFirstObjectByType<YOLODatas>();
        mask = FindFirstObjectByType<Mask>();
    }
    void Start()
    {
        StartCoroutine(playerControlCoroutine());
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReLoadPlayerDatas();
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
    // Capture the current cursor's Position value for each player and store it in the mousePos array
    void playerCursorMovement(int gamepadIndex)
    {
        Gamepad gamePad = gamePads[gamepadIndex];
        Vector2 leftStick = gamePad.leftStick.ReadValue();
        playerCursors[gamepadIndex].anchoredPosition += leftStick * cursorSpeed * Time.deltaTime;

        playerCursors[gamepadIndex].anchoredPosition = new Vector2(
            Mathf.Clamp(playerCursors[gamepadIndex].anchoredPosition.x, 0, Screen.width),
            Mathf.Clamp(playerCursors[gamepadIndex].anchoredPosition.y, 0, Screen.height)
            );

        cursorPos[gamepadIndex] = playerCursors[gamepadIndex].anchoredPosition;
    }
    // Calculate the size and position of the selection box based on the start and current mouse positions for each player
    void SelectionArea(int gamepadIndex)
    {
        if (!_players[gamepadIndex].isSelected) return ;

        Vector2 selectionSize = cursorPos[gamepadIndex] - cursorStartPos[gamepadIndex];

        selectionBox[gamepadIndex].sizeDelta = new Vector2(Mathf.Abs(selectionSize.x), Mathf.Abs(selectionSize.y));
        selectionBox[gamepadIndex].anchoredPosition = new Vector2(cursorStartPos[gamepadIndex].x + selectionSize.x / 2f, cursorStartPos[gamepadIndex].y + selectionSize.y / 2f);
    }
    // Calculate the size and position of the selection box based on the start and current mouse positions for each player
    void SelectionBox(int gamepadIndex)
    {
        if (!_players[gamepadIndex].isSelected) return ;

        Vector2 selectionSize = cursorPos[gamepadIndex] - cursorStartPos[gamepadIndex];

        selectionBox[gamepadIndex].sizeDelta = new Vector2(Mathf.Abs(selectionSize.x), Mathf.Abs(selectionSize.y));
        selectionBox[gamepadIndex].anchoredPosition = new Vector2(cursorStartPos[gamepadIndex].x + selectionSize.x / 2f, cursorStartPos[gamepadIndex].y + selectionSize.y / 2f);
    }
    // Capture the cursor's Position value of start for each player and store it in the mouseStartPos array
    void playerStartedSelecting(int gamepadIndex)
    {
        if(gamePads[gamepadIndex].buttonNorth.wasPressedThisFrame && selectedState[gamepadIndex] == false)
        {

            selectedState[gamepadIndex] = true;

            cursorStartPos[gamepadIndex] = cursorPos[gamepadIndex];
            selectionBox[gamepadIndex].GetComponent<RawImage>().enabled = true;

            Debug.Log("Player " + (gamepadIndex + 1) + " is selecting.");
        }
    }
    // Capture the cursor's Position value of End for each player and store it in the mouseEndPos array
    void playerCancelledSelecting(int gamepadIndex)
    {
        if(gamePads[gamepadIndex].buttonEast.wasReleasedThisFrame && selectedState[gamepadIndex] == true)
        {
            selectedState[gamepadIndex] = false;

            cursorEndPos[gamepadIndex] = cursorPos[gamepadIndex];

            Debug.Log("Player " + (gamepadIndex + 1) + " cancelled selection.");

            CalculatePointState(gamepadIndex);
            selectionBox[gamepadIndex].GetComponent<RawImage>().enabled = false;
        }
    }
    // Continuously check for player input and update the cursor position, selection points, and selection box for each player
    IEnumerator playerControlCoroutine()
    {
        while (true)
        {
            try
            {
                UpdatePlayerData();
                if (gamePads.Length != 0)
                {
                    for (int i = 0; i < gamePads.Length; i++)
                    {
                        Debug.Log("Player " + (i + 1) + " is using Gamepad: " + gamePads[i].name);

                        playerCursorMovement(i);
                        playerStartedSelecting(i);
                        playerCancelledSelecting(i);
                        SelectionBox(i);
                    }
                }
                else
                {
                    Debug.Log("No gamepad connected.");
                }
            }
            catch (Exception e)
            {
                Debug.Log("Waiting");
            }
            yield return null;
        }
    }
    // Check if the landmark points of other players are within the selection area of the current player and update the landmarkSelected list accordingly
    void CalculatePointState(int gamepadIndex)
    {
        for (int n = 0; n < _tracked.tracked.people.Length; n++)
        {
            int index = -1;
            // if (_tracked.tracked.people[n].person_id == _players[gamepadIndex].playerId) continue;
            for(int k = 0; k < _players.Length; k++)
            {
                int trackedId = _tracked.tracked.people[n].person_id;
                int playerId = _players[k].playerId;

                if(trackedId == playerId)
                {
                    index = k;
                    for(int m = 0; m < pointState[index].Count; m++)
                    {
                        Vector2 position = points[n][m];

                        float xRange = Math.Abs(cursorStartPos[gamepadIndex].x - cursorEndPos[gamepadIndex].x);
                        float yRange = Math.Abs(cursorStartPos[gamepadIndex].y - cursorEndPos[gamepadIndex].y);

                        Vector2 selectionCenter = _players[gamepadIndex].selectionBox.anchoredPosition;
                        if(Math.Abs(position.x - selectionCenter.x) <= xRange && Math.Abs(1080f - position.y - selectionCenter.y) <= yRange)
                        {
                            pointState[index][m] = true;
                            Debug.Log(pointState[index][m]);
                        }
                    }
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
            _players[i].cursor = playerCursors[i];
            _players[i].selectionBox = selectionBox[i];
            _players[i].isSelected = selectedState[i];
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
    private void ReLoadPlayerDatas()
    {
        _tracked.StartLoad();
        gamePads = Gamepad.all.ToArray();
        Debug.Log("Gamepad.all.Count: " + Gamepad.all.Count);
        Debug.Log(gamePads.Length);
        _playerId = new int[gamePads.Length];
        _padId = new int[gamePads.Length];
        cursorPos = new Vector2[gamePads.Length];
        cursorStartPos = new Vector2[gamePads.Length];
        cursorEndPos = new Vector2[gamePads.Length];
        selectedState = new bool[gamePads.Length];
        _players = new PlayerData[gamePads.Length];

        for (int i = 0; i < gamePads.Length; i++)
        {
            _padId[i] = gamePads[i].deviceId;
        }

        for (int i = 0; i < _playerId.Length; i++)
        {
            _playerId[i] = _tracked.tracked.people[i].person_id;
        }

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new List<Vector2>();
        }

        for(int i = 0; i < points.Length; i++)
        {
            pointState[i] = new List<bool>();
            Debug.Log(pointState[i]);
            for(int j = 0; j < 17; j++)// the landmarks upload too late, so have to set the array length in 33 
            {
                pointState[i].Add(false);
            }
        }
        for (int i = 0; i < _players.Length; i++)
        {
            _players[i] = new PlayerData(_playerId[i], _padId[i], playerCursors[i], selectionBox[i], selectedState[i], points[i], pointState[i]);
        }

        if(thread == null)
        {
            Debug.Log("Start");
            thread = new Thread(new ThreadStart(UpdateKeypoints));
            thread.IsBackground = true;
            thread.Start();
        }

        mask.LoadDatas();
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
