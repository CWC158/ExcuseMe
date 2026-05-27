using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Threading;
using System;
using UnityEditor;

public class Controller : MonoBehaviour
{   
    [Tooltip("gamepads of each player")]
    [SerializeField] public Gamepad[] gamePads;

    [Tooltip("cursors of each player")]
    [SerializeField] public GameObject[] _cursors;
    [SerializeField] private float cursorSpeed = 100f;

    [Tooltip("Selection boxes of each player")]
    [SerializeField] public GameObject[] _selectionbox;
    [SerializeField] public bool[] _state;
    //---------------------------------------------------------
    private Vector2[] cursorPos;
    private Vector2[] cursorStartPos;
    private Vector2[] cursorEndPos;
    //---------------------------------------------------------
    private GameManager gameManager;
    public ManualResetEventSlim pauseEvent;
    [SerializeField] private float[] pressingTime;
    //private DatasToJson json = new DatasToJson();
    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }
    void Update()
    {
        if (gamePads == null || gamePads.Length == 0)
        {
            Debug.LogWarning("Waiting for Gamepads...");
            return;
        }
        
        for(int i = 0; i < gamePads.Length; i++)
        {
            try
            {
                CursorMovement(i);
                StartedSelecting(i);
                CancelledSelecting(i);
                SelectionBox(i);

                if (gamePads[i].buttonEast.isPressed)
                {
                    pressingTime[i] += Mathf.Clamp01(Time.deltaTime);
                    if (pressingTime[i] >= 1f)
                    {
                        gameManager._ready[i] = !gameManager._ready[i];
                        pressingTime[i] = 0f;
                    }
                }
                else
                {
                    pressingTime[i] = 0f;
                }
            }
            catch(Exception e)
            {
                Debug.Log($"Gamepads {i} is not available...：" + e.Message);
            }
        }
    }
    private void CursorMovement(int gamepadIndex)
    {
        Gamepad gamePad = gamePads[gamepadIndex];
        Vector2 leftStick = gamePad.leftStick.ReadValue();
        _cursors[gamepadIndex].GetComponent<RectTransform>().anchoredPosition += leftStick * cursorSpeed * Time.deltaTime;

        _cursors[gamepadIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(
            Mathf.Clamp(_cursors[gamepadIndex].GetComponent<RectTransform>().anchoredPosition.x, 0, Screen.width),
            Mathf.Clamp(_cursors[gamepadIndex].GetComponent<RectTransform>().anchoredPosition.y, 0, Screen.height)
            );

        cursorPos[gamepadIndex] = _cursors[gamepadIndex].GetComponent<RectTransform>().anchoredPosition;
    }
    private void SelectionBox(int gamepadIndex)
    {
        if (!_state[gamepadIndex]) return ;

        Vector2 selectionSize = cursorPos[gamepadIndex] - cursorStartPos[gamepadIndex];

        _selectionbox[gamepadIndex].GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Abs(selectionSize.x), Mathf.Abs(selectionSize.y));
        _selectionbox[gamepadIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(cursorStartPos[gamepadIndex].x + selectionSize.x / 2f, cursorStartPos[gamepadIndex].y + selectionSize.y / 2f);
    }
    private void StartedSelecting(int gamepadIndex)
    {
        if(gamePads[gamepadIndex].buttonNorth.wasPressedThisFrame && _state[gamepadIndex] == false)
        {

            _state[gamepadIndex] = true;

            cursorStartPos[gamepadIndex] = cursorPos[gamepadIndex];
            _selectionbox[gamepadIndex].GetComponent<RawImage>().enabled = true;

            Debug.Log("Player " + (gamepadIndex + 1) + " is selecting.");
        }
    }
    private void CancelledSelecting(int gamepadIndex)
    {
        if(gamePads[gamepadIndex].buttonNorth.wasReleasedThisFrame && _state[gamepadIndex] == true)
        {
            _state[gamepadIndex] = false;

            cursorEndPos[gamepadIndex] = cursorPos[gamepadIndex];

            Debug.Log("Player " + (gamepadIndex + 1) + " cancelled selection.");

            CalculatePointState(gamepadIndex);
            _selectionbox[gamepadIndex].GetComponent<RawImage>().enabled = false;
        }
    }
    private void CalculatePointState(int gamepadIndex)
    {
        Vector3[] corners = new Vector3[4];
        gameManager._players[gamepadIndex].selectionBox.GetComponent<RectTransform>().GetWorldCorners(corners);

        for (int n = 0; n < gameManager._tracked.tracked.people.Length; n++)
        {
            int index = Array.IndexOf(gameManager._playerId, gameManager._tracked.tracked.people[n].person_id);
            if (index == -1) continue;

            // if (index == gamepadIndex) continue;
            for(int m = 0; m < gameManager._players[index].pointState.Count; m++)
            {
                Debug.Log("Test");
                Vector2 position = gameManager._players[index].points[m];

                float xRange = Math.Abs(corners[2].x - corners[1].x);
                float yRange = Math.Abs(corners[1].y - corners[0].y);
                Vector2 selectionCenter = gameManager._players[gamepadIndex].selectionBox.GetComponent<RectTransform>().anchoredPosition;

                if(Math.Abs(position.x - selectionCenter.x) <= xRange / 2f && Math.Abs(position.y - selectionCenter.y) <= yRange / 2f)
                {
                    gameManager._players[index].pointState[m] = true;
                }
            }
        }
    }
    public void Reload()
    {
        gamePads = Gamepad.all.ToArray();
        Debug.Log("Gamepad.all.Count: " + Gamepad.all.Count);
        Debug.Log(gamePads.Length);
        Debug.Log("Gamepads: " + string.Join(", ", gamePads.Select(g => g.name)));

        cursorPos = new Vector2[gamePads.Length];
        cursorStartPos = new Vector2[gamePads.Length];
        cursorEndPos = new Vector2[gamePads.Length];
        _state = new bool[gamePads.Length];
        pressingTime = new float[gamePads.Length];
        pauseEvent = new ManualResetEventSlim(true);
    }
}
