using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Threading;
using System;

public class ControllerSystem : MonoBehaviour
{
    [Tooltip("gamepads of each player")]
    [SerializeField] private Gamepad[] gamePads;
    [Tooltip("cursors of each player")]
    [SerializeField]private RectTransform[] playerCursors;
    [SerializeField] private float cursorSpeed = 100f;

    [Tooltip("Selection boxes of each player")]
    [SerializeField] private RectTransform[] selectionBox;
    private Vector2[] mousePos;
    private Vector2[] mouseStartPos;
    private Vector2[] mouseEndPos;

    [Tooltip("Selecting state of each player")]
    [SerializeField] private List<bool> playerSelected;

    private PlayerData[] playersData;
    private List<Vector2>[] selectionPoints;
    [SerializeField] private float minPointDistance = 10f;
    private MediapipekDatas landmarkDatas;
    private List<Vector2>[] landmarkPoints = new List<Vector2>[4];
    public List<bool>[] landmarkState = new List<bool>[4];
    private MediapipekDatas.AllTracked allTracked;
    private int[] playerIdRecord = new int[4];
    private int[] padIdRecord = new int[4];
    void Awake()
    {
        gamePads = Gamepad.all.ToArray();

        for (int i = 0; i < gamePads.Length; i++)
        {
            padIdRecord[i] = gamePads[i].deviceId;
        }

        landmarkDatas = FindFirstObjectByType<MediapipekDatas>();
        allTracked = landmarkDatas.allTracked;
        new Thread(UpdateLandmarkPoints).Start();
        // allTracked = landmarkDatas.allTracked;

        // for (int i = 0; i < allTracked.trackedPeople.Length; i++)
        // {
        //     playerIdRecord[i] = allTracked.trackedPeople[i].trackId;
        // }

        // for (int i = 0; i < landmarkPoints.Length; i++)
        // {
        //     landmarkPoints[i] = new List<Vector2>();
        // }
        // new Thread(UpdateLandmarkPoints).Start();

        // for (int i = 0; i < landmarkSelected.Length; i++)
        // {
        //     landmarkSelected[i] = new List<bool>();
        //     for(int k = 0; k < allTracked.trackedPeople.Length; k++)
        //     {
        //         if(allTracked.trackedPeople[k].trackId == playerIdRecord[i])
        //         {
        //             for(int m = 0; m < landmarkPoints[k].Count; m++)
        //             {
        //                 landmarkSelected[i].Add(false);
        //             }
        //         }
        //     }
        // }

        mousePos = new Vector2[gamePads.Length];
        mouseStartPos = new Vector2[gamePads.Length];
        mouseEndPos = new Vector2[gamePads.Length];
        playerSelected = new List<bool>(new bool[gamePads.Length]);
        playersData = new PlayerData[gamePads.Length];

        selectionPoints = new List<Vector2>[gamePads.Length];
        for (int i = 0; i < selectionPoints.Length; i++)
        {
            selectionPoints[i] = new List<Vector2>();
        }
    }
    void Start()
    {
        Debug.Log("Gamepad.all.Count: " + Gamepad.all.Count);
        Debug.Log(allTracked.trackedPeople.Length);
        for (int i = 0; i < allTracked.trackedPeople.Length; i++)
        {
            playerIdRecord[i] = allTracked.trackedPeople[i].trackId;
        }
        Debug.Log(allTracked.trackedPeople.Length);
        for (int i = 0; i < landmarkPoints.Length; i++)
        {
            landmarkPoints[i] = new List<Vector2>();
        }
        
        for(int i = 0; i < landmarkPoints.Length; i++)
        {
            landmarkState[i] = new List<bool>();
            for(int j = 0; j < 33; j++)// the landmarks upload too late, so have to set the array length in 33 
            {
                landmarkState[i].Add(false);
            }
        }

        for (int i = 0; i < playersData.Length; i++)
        {
            playersData[i] = new PlayerData(playerIdRecord[i], padIdRecord[i], playerCursors[i], selectionBox[i], playerSelected[i], selectionPoints[i]);
        }

        StartCoroutine(playerControlCoroutine());
    }
    void Update()
    {
        UpdatePlayerData();
    }
    // Define a Player class to store player information such as player ID, gamepad ID, and cursor reference
    public class PlayerData
    {
        public int playerId;
        public int padId;
        public RectTransform cursor;
        public RectTransform selectionBox;
        public bool isSelecting;
        public List<Vector2> selectionPoints;
        public List<Vector2> points;
        public List<bool> landmarkState;
        public PlayerData(int playerId, int padId, RectTransform cursor, RectTransform selectionBox, bool isSelecting = false, List<Vector2> selectionPoints = null, List<Vector2> points = null, List<bool> landmarkBeSelected = null)
        {
            this.playerId = playerId;
            this.padId = padId;
            this.cursor = cursor;
            this.selectionBox = selectionBox;
            this.isSelecting = isSelecting;
            this.selectionPoints = selectionPoints ?? new List<Vector2>();
            this.points = points ?? new List<Vector2>();
            this.landmarkState = landmarkBeSelected ?? new List<bool>();
        }
    }
    // Capture the current cursor's Position value for each player and store it in the mousePos array
    void playerCursorMovement(int gamepadIndex)
    {
        Gamepad gamePad = gamePads[gamepadIndex];
        Debug.Log(allTracked.trackedPeople.Length);
        Vector2 leftStick = gamePad.leftStick.ReadValue();
        playerCursors[gamepadIndex].anchoredPosition += leftStick * cursorSpeed * Time.deltaTime;

        playerCursors[gamepadIndex].anchoredPosition = new Vector2(
            Mathf.Clamp(playerCursors[gamepadIndex].anchoredPosition.x, 0, Screen.width),
            Mathf.Clamp(playerCursors[gamepadIndex].anchoredPosition.y, 0, Screen.height)
            );

        mousePos[gamepadIndex] = playerCursors[gamepadIndex].anchoredPosition;
    }
    // Calculate the size and position of the selection box based on the start and current mouse positions for each player
    void SelectionArea(int gamepadIndex)
    {
        if (!playersData[gamepadIndex].isSelecting) return ;

        Vector2 selectionSize = mousePos[gamepadIndex] - mouseStartPos[gamepadIndex];

        selectionBox[gamepadIndex].sizeDelta = new Vector2(Mathf.Abs(selectionSize.x), Mathf.Abs(selectionSize.y));
        selectionBox[gamepadIndex].anchoredPosition = new Vector2(mouseStartPos[gamepadIndex].x + selectionSize.x / 2f, mouseStartPos[gamepadIndex].y + selectionSize.y / 2f);
    }
    // Calculate the size and position of the selection box based on the start and current mouse positions for each player
    void SelectionBox(int gamepadIndex)
    {
        if (!playersData[gamepadIndex].isSelecting) return ;

        Vector2 selectionSize = mousePos[gamepadIndex] - mouseStartPos[gamepadIndex];
        // Debug.Log("Mouse Position: " + mousePos[gamepadIndex]);

        selectionBox[gamepadIndex].sizeDelta = new Vector2(Mathf.Abs(selectionSize.x), Mathf.Abs(selectionSize.y));
        selectionBox[gamepadIndex].anchoredPosition = new Vector2(mouseStartPos[gamepadIndex].x + selectionSize.x / 2f, mouseStartPos[gamepadIndex].y + selectionSize.y / 2f);
        // Debug.Log("SelectionBox Size: " + selectionBox[gamepadIndex].sizeDelta);
    }
    // Capture the real-time cursor's Position value while the player is selecting and store it in the selectionPoints list
    void playerSelectionPoints(int gamepadIndex)
    {
        if (!playersData[gamepadIndex].isSelecting) return ;

        Gamepad gamepad = gamePads[gamepadIndex];
        Vector2 currentPos = gamepad.leftStick.ReadValue();
        if(selectionPoints[gamepadIndex].Count == 0)
        {
            selectionPoints[gamepadIndex].Add(mouseStartPos[gamepadIndex]);
        }

        if (Vector2.Distance(currentPos, playersData[gamepadIndex].selectionPoints[^1]) > minPointDistance)
        {
            selectionPoints[gamepadIndex].Add(currentPos);
        }
    }
    // Capture the cursor's Position value of start for each player and store it in the mouseStartPos array
    void playerStartedSelecting(int gamepadIndex)
    {
        if(gamePads[gamepadIndex].buttonNorth.wasPressedThisFrame)
        {
            selectionPoints[gamepadIndex].Clear();

            playerSelected[gamepadIndex] = true;

            mouseStartPos[gamepadIndex] = mousePos[gamepadIndex];
            selectionBox[gamepadIndex].gameObject.GetComponent<RawImage>().enabled = true;

            Debug.Log("Player " + (gamepadIndex + 1) + " is selecting.");
                // Debug.Log("Mouse Start Position: " + mouseStartPos[gamepadIndex]);
        }
    }
    // Capture the cursor's Position value of End for each player and store it in the mouseEndPos array
    void playerCancelledSelecting(int gamepadIndex)
    {
        if(gamePads[gamepadIndex].buttonEast.wasReleasedThisFrame)
        {
            playerSelected[gamepadIndex] = false;

            mouseEndPos[gamepadIndex] = mousePos[gamepadIndex];
            selectionBox[gamepadIndex].gameObject.GetComponent<RawImage>().enabled = false;

            Debug.Log("Player " + (gamepadIndex + 1) + " cancelled selection.");
                // Debug.Log("Mouse End Position: " + mouseEndPos[gamepadIndex]);

            LandmarkBeSelected(gamepadIndex);
                // WheatherSelectedLandmarks(gamepadIndex);
        }
    }
    // Continuously check for player input and update the cursor position, selection points, and selection box for each player
    IEnumerator playerControlCoroutine()
    {
        while (true)
        {
            if (gamePads.Length != 0)
            {
                for (int i = 0; i < gamePads.Length; i++)
                {
                    Debug.Log("Player " + (i + 1) + " is using Gamepad: " + gamePads[i].name);

                    playerCursorMovement(i);
                    playerSelectionPoints(i);
                    playerStartedSelecting(i);
                    playerCancelledSelecting(i);
                    //SelectionArea(i);
                    SelectionBox(i);
                }
            }
            else
            {
                Debug.Log("No gamepad connected.");
            }
            yield return null;
        }
    }
    // Check if the landmark points of other players are within the selection area of the current player and update the landmarkSelected list accordingly
    void WheatherSelectedLandmarks(int gamepadIndex)
    {
        int pointCount = playersData[gamepadIndex].selectionPoints.Count;

        for (int n = 0; n < allTracked.trackedPeople.Length; n++)
        {
            int index = -1;
            // if (allTracked.trackedPeople[n].trackId == playersData[gamepadIndex].playerId) continue;
            for(int k = 0; k < playerIdRecord.Length; k++)
            {
                if(allTracked.trackedPeople[n].trackId == playerIdRecord[k])
                {
                    index = k;
                }
            }
            for(int m = 0; m < landmarkPoints[n].Count; m++)
            {
                Vector2 position = landmarkPoints[n][m];

                for(int i = 0, j = pointCount - 1; i < pointCount; j = i++)
                {
                    Vector2 pointA = playersData[gamepadIndex].selectionPoints[i];
                    Vector2 pointB = playersData[gamepadIndex].selectionPoints[j];

                    if(landmarkState[index][m]) continue;

                    if(((pointA.y > position.y) != (pointB.y > position.y)) && (position.x < ((pointA.x - pointB.x) * (position.y - pointB.y) / (pointA.y - pointB.y)) + pointB.x))
                    {
                        landmarkState[index][m] = !landmarkState[index][m];
                    }
                }
            }
        }
    }
    void LandmarkBeSelected(int gamepadIndex)
    {
        Debug.Log("f");
        Debug.Log(allTracked.trackedPeople.Length);
        for (int n = 0; n < allTracked.trackedPeople.Length; n++)
        {
            int index = -1;
            Debug.Log("f");
            if (allTracked.trackedPeople[n].trackId == playersData[gamepadIndex].playerId) continue;
            for(int k = 0; k < playersData.Length; k++)
            {
                int trackedId = allTracked.trackedPeople[n].trackId;
                int playerId = playersData[k].playerId;
                Debug.Log(playerId);
                Debug.Log(trackedId);

                if(trackedId == playerId)
                {
                    index = k;
                    Debug.Log(index);
                    for(int m = 0; m < landmarkState[index].Count; m++)
                    {
                        Vector2 position = landmarkPoints[n][m];

                        float xRange = Math.Abs(mouseStartPos[gamepadIndex].x - mouseEndPos[gamepadIndex].x);
                        float yRange = Math.Abs(mouseStartPos[gamepadIndex].y - mouseEndPos[gamepadIndex].y);

                        Vector2 selectionCenter = playersData[gamepadIndex].selectionBox.anchoredPosition;
                        Debug.Log("c");
                        if(Math.Abs(position.x - selectionCenter.x) <= xRange && Math.Abs(1080f - position.y - selectionCenter.y) <= yRange)
                        {
                            Debug.Log("k");
                            landmarkState[index][m] = true;
                            Debug.Log(landmarkState[index][m]);
                        }
                    }
                }
            }
        }
    }
    // Continuously update the landmark points of all tracked people and store them in the landmarkPoints list
    void UpdateLandmarkPoints()
    {
        while(landmarkDatas != null)
        {
            landmarkPoints = landmarkDatas.landmarkPoints;
            allTracked = landmarkDatas.allTracked;
        }
    }
    // Update the playersData array with the current cursor, selection box, selecting state, selection points, and landmark be selected for each player
    void UpdatePlayerData()
    {
        for (int i = 0; i < playersData.Length; i++)
        {
            playersData[i].cursor = playerCursors[i];
            playersData[i].selectionBox = selectionBox[i];
            playersData[i].isSelecting = playerSelected[i];
            playersData[i].selectionPoints = selectionPoints[i];
            playersData[i].landmarkState = landmarkState[i];
        }
    }
}
