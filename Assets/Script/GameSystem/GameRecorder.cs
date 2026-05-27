using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.IO;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine.Video;
using UnityEditor;

public class GameRecorder : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]private string pictureFolder;
    private string record;
    private int phase;
    [SerializeField] private RawImage[] pictures;
    [SerializeField] private VideoPlayer videoPlayer;
    private UITrigger uiTrigger;
    private GameManager gameManager;
    [SerializeField]private string movieFolder;
    private RecorderController recorderController;
    private RecorderControllerSettings controllerSettings;
    private MovieRecorderSettings movieRecorderSettings;
    private bool isRecording = false;
    void Awake()
    {
        uiTrigger = FindFirstObjectByType<UITrigger>();
        gameManager = FindFirstObjectByType<GameManager>();
        recorderController = CreateNewRecorder();
    }
    void Start()
    {
        pictureFolder = "Assets/Screenshots";
        movieFolder = "Assets/Videos";

        if (!Directory.Exists(pictureFolder))
        {
            Directory.CreateDirectory(pictureFolder);
        }

        if (!Directory.Exists(movieFolder))
        {
            Directory.CreateDirectory(movieFolder);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public IEnumerator Timeline()
    {
        string pictureFile = ""; 
        byte[] pic = null;
        Texture2D loadedTexture;
        phase = 0;
        record = DateTime.Now.ToString("MMdd_HHmmss");
        
        while(true)
        {
            switch (phase)
            {
                case 0:
                    if (uiTrigger.director.time >= 6f)
                    {
                        Debug.Log("Take Screenshot 1");
                        pictureFile = $"{pictureFolder}/{record}__1.png";
                        ScreenCapture.CaptureScreenshot(pictureFile);

                        yield return null;

                        loadedTexture = new Texture2D(1, 1);
                        pic = File.ReadAllBytes(pictureFile);
                        loadedTexture.LoadImage(pic);
                        pictures[0].texture = loadedTexture;

                        phase = 1;
                    }
                    break;

                case 1:
                    if (uiTrigger.director.time >= 17f)
                    {
                        Debug.Log("Start!!!");
                        StartRecording();
                        gameManager.PlayerShow();
                        phase = 2;
                    }
                    break;

                case 2:
                    if (uiTrigger.director.time >= 28.25f)
                    {
                        Debug.Log("Take Screenshot 2");
                        pictureFile = $"{pictureFolder}/{record}__2.png";
                        ScreenCapture.CaptureScreenshot(pictureFile);

                        yield return null;

                        loadedTexture = new Texture2D(1, 1);
                        pic = File.ReadAllBytes(pictureFile);
                        loadedTexture.LoadImage(pic);
                        pictures[1].texture = loadedTexture;

                        phase = 3;
                    }
                    break;

                case 3:
                    if (uiTrigger.director.time >= 38.5f)
                    {
                        Debug.Log("Take Screenshot 3");
                        pictureFile = $"{pictureFolder}/{record}__3.png";
                        ScreenCapture.CaptureScreenshot(pictureFile);
                        
                        yield return null;

                        loadedTexture = new Texture2D(1, 1);
                        pic = File.ReadAllBytes(pictureFile);
                        loadedTexture.LoadImage(pic);
                        pictures[2].texture = loadedTexture;

                        phase = 4;
                    }
                    break;

                case 4:
                    if (uiTrigger.director.time >= uiTrigger.director.duration)
                    {
                        Debug.Log("Take Screenshot 4");
                        pictureFile = $"{pictureFolder}/{record}__4.png";
                        ScreenCapture.CaptureScreenshot(pictureFile);
                        StopRecording();
                        gameManager.GameStop(out gameManager.gameRunning);

                        yield return null;

                        loadedTexture = new Texture2D(1, 1);
                        pic = File.ReadAllBytes(pictureFile);
                        loadedTexture.LoadImage(pic);
                        pictures[3].texture = loadedTexture;

                        // VideoClip recordedClip = AssetDatabase.LoadAssetAtPath<VideoClip>($"{movieFolder}/{record}.mp4");
                        videoPlayer.url = $"file://C:/Users/RHA/ExcuseMe/{movieFolder}/{record}.mp4";
                        videoPlayer.controlledAudioTrackCount = 0;
                        videoPlayer.Prepare();
                        videoPlayer.Play();

                        phase = 5;
                        
                        Debug.Log("Game Stop by Timeline");
                        yield break;
                    }
                    break;
            }
            yield return new WaitForSeconds(0.05f); 
        }
    }
    public void StopRecording()
    {
        if (isRecording && recorderController != null)
        {
            recorderController.StopRecording();
            isRecording = false;
            Debug.Log("Stop Recording...");
        }
    }
    public void StartRecording()
    {
        if(!isRecording && recorderController != null)
        {
            record = DateTime.Now.ToString("MMdd_HHmmss");
            movieRecorderSettings.OutputFile = $"{movieFolder}/{record}";
            recorderController.PrepareRecording();
            recorderController.StartRecording();
            isRecording = true;
            Debug.Log("Start Recording...");
        }
    }
    private RecorderController CreateNewRecorder()
    {
        controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        
        movieRecorderSettings.name = "My Video Recorder";
        movieRecorderSettings.Enabled = true;

        // movieRecorderSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;

        movieRecorderSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };

        controllerSettings.AddRecorderSettings(movieRecorderSettings);
        controllerSettings.SetRecordModeToManual(); 
        controllerSettings.FrameRate = 60; 

        return new RecorderController(controllerSettings);
    }
}
