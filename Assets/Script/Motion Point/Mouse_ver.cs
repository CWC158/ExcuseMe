using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Threading;

public class ControllerSystemMouse : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private Vector2 mousePos;
    private Vector2 mouseStartPos;
    private Vector2 mouseEndPos;

    [Tooltip("state of mouse")]
    [SerializeField] bool isSelecting = false;

    [Tooltip("cursor of mouse")]
    [SerializeField] private RectTransform mouseCursor;

    [Tooltip("Selection boxes of mouse")]
    [SerializeField] private RectTransform selectionBox;
    LineRenderer lineRenderer;
    private Vector2 mouseDelta;
    InputSystem_Actions inputAction;
    private List<Vector2> selectionPoints = new List<Vector2>();
    public PlayerData playerData;
    private YOLODatas pointsDatas;
    private List<Vector2>[] points = new List<Vector2>[4];
    public List<bool> pointState = new List<bool>();
    private YOLODatas.Tracked tracked;
    private int playerIdRecord;
    private int padIdRecord;
    void Awake()
    {        
        inputAction = new InputSystem_Actions();

        inputAction.Player.MousePos.performed += ctx =>
        {
            mousePos = ctx.ReadValue<Vector2>();
            mouseCursor.anchoredPosition = mousePos;
        };

        inputAction.Player.MouseL.started += ctx =>
        {
            selectionPoints.Clear();
            isSelecting = true;
            mouseStartPos = mousePos;
            selectionBox.gameObject.GetComponent<RawImage>().enabled = true;

            Debug.Log("MouseStart Position: " + mouseStartPos);
        };

        inputAction.Player.MouseL.canceled += ctx =>
        {
            isSelecting = false;
            mouseEndPos = mousePos;

            Debug.Log("MouseEnd Position: " + mouseEndPos);

            PointState();
            selectionBox.gameObject.GetComponent<RawImage>().enabled = false;
        };

        inputAction.Player.MouseDelta.performed += ctx =>
        {
            mouseDelta = ctx.ReadValue<Vector2>();
        };

        pointsDatas = FindFirstObjectByType<YOLODatas>();
        tracked = pointsDatas.tracked;
        new Thread(UpdateLandmarkPoints).Start();
    }
    void Start()
    {
        playerIdRecord = tracked.people[0].person_id;

        for(int j = 0; j < 17; j++)// the landmarks upload too late, so have to set the array length in 33 
        {
            pointState.Add(false);
        }

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new List<Vector2>();
        }
        playerData = new PlayerData(playerIdRecord, padIdRecord, mouseCursor, selectionBox, isSelecting, selectionPoints);
    }
    void OnEnable()
    {
        inputAction.Player.Enable();
    }
    void OnDisable()
    {
        inputAction.Player.Disable();
    }
    // Update is called once per frame
    void Update()
    {
        UpdatePlayerData();
        if (isSelecting)
        {
            selection();
        }
        // playerSelectionPoints(isSelecting);
    }
    void selection()
    {
        Vector2 selectionSize = mousePos - mouseStartPos;

        selectionBox.sizeDelta = new Vector2(Mathf.Abs(selectionSize.x), Mathf.Abs(selectionSize.y));
        selectionBox.anchoredPosition = new Vector2(mouseStartPos.x + selectionSize.x / 2f, mouseStartPos.y + selectionSize.y / 2f);
    }
    void PointState()
    {
        Debug.Log("f");
        Debug.Log(tracked.people.Length);
        for (int n = 0; n < tracked.people.Length; n++)
        {
            int index = -1;
            Debug.Log("f");
            // if (tracked.people[n].person_id == playersData[gamepadIndex].playerId) continue;
            int trackedId = tracked.people[0].person_id;
            int playerId = playerData.playerId;
            Debug.Log(playerId);
            Debug.Log(trackedId);

            if(trackedId == playerId)
            {
                index = n;
                for(int m = 0; m < points[n].Count; m++)
                {
                    Vector2 position = points[n][m];

                    float xRange = Math.Abs(mouseStartPos.x - mouseEndPos.x);
                    float yRange = Math.Abs(mouseStartPos.y - mouseEndPos.y);

                    Vector2 selectionCenter = playerData.selectionBox.anchoredPosition;
                    Debug.Log("c");
                    if(Math.Abs(position.x - selectionCenter.x) <= xRange && Math.Abs(1080f - position.y - selectionCenter.y) <= yRange)
                    {
                        Debug.Log("k");
                        pointState[m] = true;
                        Debug.Log(pointState[m]);
                    }
                }
            }
        }
    }
    public class PlayerData
    {
        public int playerId;
        public int padId;
        public RectTransform cursor;
        public RectTransform selectionBox;
        public bool isSelecting;
        public List<Vector2> selectionPoints;
        public List<Vector2> points;
        public List<bool> pointState;
        public PlayerData(int playerId, int padId, RectTransform cursor, RectTransform selectionBox, bool isSelecting = false, List<Vector2> selectionPoints = null, List<Vector2> points = null, List<bool> poinState = null)
        {
            this.playerId = playerId;
            this.padId = padId;
            this.cursor = cursor;
            this.selectionBox = selectionBox;
            this.isSelecting = isSelecting;
            this.selectionPoints = selectionPoints ?? new List<Vector2>();
            this.points = points ?? new List<Vector2>();
            this.pointState = poinState ?? new List<bool>();
        }
    }
    void UpdateLandmarkPoints()
    {
        while(pointsDatas != null)
        {
            points = pointsDatas.points;
            tracked = pointsDatas.tracked;
        }
    }
    void UpdatePlayerData()
    {
        playerData.cursor = mouseCursor;
        playerData.selectionBox = selectionBox;
        playerData.isSelecting = isSelecting;
        playerData.selectionPoints = selectionPoints;
        playerData.pointState = pointState;
        if(tracked.people != null)
        {
            for(int i = 0; i < tracked.people.Length; i++)
            {
                if(tracked.people[i].person_id == playerData.playerId)
                {
                    Debug.Log(tracked.people[i].person_id);
                    Debug.Log(playerData.playerId);
                    playerData.points = points[i];
                }
            }
        }
    }
    // void playerSelectionPoints(bool isSelecting)
    // {
    //     if (!isSelecting) return ;

    //     Vector2 currentPos = mousePos;
    //     selectionPoints.Add(mouseStartPos);

    //     if (Vector2.Distance(currentPos,selectionPoints[^1]) > 15f)
    //     {
    //         selectionPoints.Add(currentPos);
    //         Debug.Log("Selection Point: " + selectionPoints[^1]);
    //     }
    // }
}
