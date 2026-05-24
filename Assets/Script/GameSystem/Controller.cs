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
    public Coroutine coroutine;
    public ManualResetEventSlim pauseEvent;
    //private DatasToJson json = new DatasToJson();
    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }

    // Capture the current cursor's Position value for each player and store it in the mousePos array
    void CursorMovement(int gamepadIndex)
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
    // Calculate the size and position of the selection box based on the start and current mouse positions for each player
    void SelectionBox(int gamepadIndex)
    {
        if (!_state[gamepadIndex]) return ;

        Vector2 selectionSize = cursorPos[gamepadIndex] - cursorStartPos[gamepadIndex];

        _selectionbox[gamepadIndex].GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Abs(selectionSize.x), Mathf.Abs(selectionSize.y));
        _selectionbox[gamepadIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(cursorStartPos[gamepadIndex].x + selectionSize.x / 2f, cursorStartPos[gamepadIndex].y + selectionSize.y / 2f);
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
        if(gamePads[gamepadIndex].buttonNorth.wasReleasedThisFrame && _state[gamepadIndex] == true)
        {
            _state[gamepadIndex] = false;

            cursorEndPos[gamepadIndex] = cursorPos[gamepadIndex];

            Debug.Log("Player " + (gamepadIndex + 1) + " cancelled selection.");

            gameManager.CalculatePointState(gamepadIndex);
            _selectionbox[gamepadIndex].GetComponent<RawImage>().enabled = false;
        }
    }
    // Continuously check for player input and update the cursor position, selection points, and selection box for each player
    IEnumerator ControllerCoroutine()
    {
        while (true)
        {
            for(int i = 0; i < gamePads.Length; i++)
            {
                CursorMovement(i);
                StartedSelecting(i);
                CancelledSelecting(i);
                SelectionBox(i);

                if (gamePads[i].buttonEast.wasPressedThisFrame)
                {
                    gameManager._ready[i] = !gameManager._ready[i];
                }
                if (gamePads[i].buttonWest.wasPressedThisFrame)
                {
                    gameManager.ReloadPlayerData();
                }
            }
            yield return null;
        }
    }
    // Update the playersData array with the current cursor, selection box, selecting state, selection points, and landmark be selected for each player
    public void Reload()
    {
        gamePads = Gamepad.all.ToArray();
        Debug.Log("Gamepad.all.Count: " + Gamepad.all.Count);
        Debug.Log(gamePads.Length);

        cursorPos = new Vector2[gamePads.Length];
        cursorStartPos = new Vector2[gamePads.Length];
        cursorEndPos = new Vector2[gamePads.Length];
        _state = new bool[gamePads.Length];
        pauseEvent = new ManualResetEventSlim(true);
        if(coroutine != null)
        {
            StopCoroutine(coroutine);
        }
        coroutine = StartCoroutine(ControllerCoroutine());
    }
}
