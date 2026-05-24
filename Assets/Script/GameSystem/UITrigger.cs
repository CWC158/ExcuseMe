using UnityEngine;
using System;
using System.Linq;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.InputSystem;
public class UITrigger : MonoBehaviour, IObserver<InputControl>
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject[] UI;
    [SerializeField] private GameObject[] readybutton;
    public int triggerNum;
    private GameManager gameManager;
    public PlayableDirector director;
    private IDisposable anyButtonPressListener;

    void OnEnable()
    {
        anyButtonPressListener = InputSystem.onAnyButtonPress.Subscribe(this);
    }

    void OnDisable()
    {
        if (anyButtonPressListener != null)
        {
            anyButtonPressListener.Dispose();
        }
    }

    public void OnNext(InputControl control)
    {
        if(triggerNum == 0 && control.device is Gamepad && gameManager.isSetup)
        {
            triggerNum++;
            UI[0].SetActive(false);
            UI[1].SetActive(true);
            gameManager.PlayerShow();
        }
    }

    public void OnCompleted() { }
    public void OnError(Exception error) { }

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        director = FindFirstObjectByType<PlayableDirector>();
        triggerNum = 0;
    }

    // Update is called once per frame
    void Update()
    {
        switch(triggerNum)
        {
            case 0:
                GameMenu();
                break;
            case 1:
                ReadyButton();
                break;
            case 2:
                OnGameStart();
                break;
            case 3:
                OnGameStop();
                break;
        }
        if(triggerNum == 1)
        {
            ReadyButton();
        }
    }
    private void GameMenu()
    {
        UI[0].SetActive(true);
        UI[1].SetActive(false);
        UI[2].SetActive(false);
        UI[3].SetActive(false);
    }
    private void ReadyButton()
    {
        for(int i = 0; i < gameManager._ready.Length; i++)
        {
            if(gameManager._ready[i] == true)
            {
                readybutton[i].SetActive(true);
            }
            else
            {
                readybutton[i].SetActive(false);
            }
        }
    }
    public void OnGameStart()
    {
        UI[1].SetActive(false);
        UI[2].SetActive(true);
        director.Play();
    }
    public void OnGameStop()
    {
        UI[2].SetActive(false);
        UI[3].SetActive(true);
        director.Stop();
    }

}
