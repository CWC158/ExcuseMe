using UnityEngine;
using System.Linq;
using UnityEngine.Playables;
using UnityEngine.UI;
public class UITrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject[] UI;
    [SerializeField] private GameObject[] readybutton;
    private int triggerNum;
    private GameManager gameManager;
    private Controller controller;
    public PlayableDirector director;
    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        controller = FindFirstObjectByType<Controller>();
        director.Stop();
    }
    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        controller = FindFirstObjectByType<Controller>();
        director = FindFirstObjectByType<PlayableDirector>();
        triggerNum = 0;
        UI[0].SetActive(true);
        UI[1].SetActive(false);
        UI[2].SetActive(false);
        UI[3].SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            triggerNum++;
            UI[0].SetActive(false);
            UI[1].SetActive(true);
        }

        if(triggerNum == 1)
        {
            ReadyButton();
        }
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
        Debug.Log(triggerNum);
        UI[1].SetActive(false);
        UI[2].SetActive(true);
        StopCoroutine(controller.coroutine);
        director.Play();
    }
    public void OnGameStop()
    {
        UI[2].SetActive(false);
        UI[3].SetActive(true);
        StopCoroutine(controller.coroutine);
        director.Stop();
    }

}
