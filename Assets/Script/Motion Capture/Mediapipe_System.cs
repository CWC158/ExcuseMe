using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Threading;
using System.IO;
using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

public class MediapipekDatas : MonoBehaviour
{
    [SerializeField] private int port; // Replace with your desired port number
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private Thread thread;
    private byte[] landmarkDatas;
    private DatasToJson jsonDatas = new DatasToJson();
    public List<Vector2>[] landmarkPoints = new List<Vector2>[4];
    public AllTracked allTracked;
    private ControllerSystem controllerSystem;
    private List<bool>[] landmarkSelectedState = new List<bool>[4];
    private List<Vector2> pre = new List<Vector2>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        udpClient = new UdpClient(port);
        serverEndPoint = new IPEndPoint(IPAddress.Any, port);
        thread = new Thread(new ThreadStart(reciveDatas));
        thread.IsBackground = true;
        thread.Start();
        for (int i = 0; i < landmarkPoints.Length; i++)
        {
            landmarkPoints[i] = new List<Vector2>();
        }

        for (int i = 0; i < landmarkSelectedState.Length; i++)
        {
            landmarkSelectedState[i] = new List<bool>();
        }
    }
    void Start()
    {
        controllerSystem = FindFirstObjectByType<ControllerSystem>();
        Debug.Log(serverEndPoint);
        StartCoroutine(WriteData());
    }

    // Update is called once per frame
    void Update()
    {
        landmarkSelectedState = controllerSystem.landmarkState;
    }
    void reciveDatas()
    {
        while (true)
        {
            if (udpClient.Available > 0)
            {
                landmarkDatas = udpClient.Receive(ref serverEndPoint);
                string landmarkString = Encoding.UTF8.GetString(landmarkDatas);
                jsonDatas.jsonString = landmarkString;
                jsonDatas.saveJsonToFile(Application.dataPath + "/landmarkDatas.json");
                allTracked = JsonUtility.FromJson<AllTracked>(landmarkString);
                Debug.Log(allTracked.trackedPeople.Length);
                // Debug.Log("Number of tracked people: " + allTracked.trackedPeople.Length);

                for (int i = 0; i < allTracked.trackedPeople.Length; i++)
                {
                    TrackedPeople person = allTracked.trackedPeople[i];
                    pre.Clear();

                    for (int j = 0; j < person.landmarkPoints.Length; j++)
                    {
                        Vector2 point = new Vector2(person.landmarkPoints[j].position[0], person.landmarkPoints[j].position[1]);
                        pre.Add(point);
                    }
                    landmarkPoints[i] = pre;
                }
            }
        }
    }
    IEnumerator WriteData()
    {
        while (true)
        {
            try
            {
                string log = "";
                for (int i = 0; i < allTracked.trackedPeople.Length; i++)
                {
                    if (allTracked.trackedPeople[i] == null) continue;
                    log += $"player_id:{allTracked.trackedPeople[i].trackId}, ";

                    for (int j = 0; j < landmarkPoints[i].Count; j++)
                    {
                        Vector2 point = landmarkPoints[i][j];
                        log += $"landmark_id:{j}(position:[{point.x}, {1080f - point.y}], be_seleted:[{landmarkSelectedState[i][j]}]), ";
                    }
                    log += "\n";
                }
                // jsonDatas.jsonString = log;
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, "LandmarkPoints_Export.json");
                File.WriteAllText(filePath, log);
            }
            catch(IOException e) 
            {
                Debug.LogError("保存失败，文件仍被占用: " + e.Message);
            }
            // AssetDatabase.ImportAsset("Assets/landmarkpoints.json");
            yield return null;
        }
    }
    [Serializable] public class DatasToJson
    {
        public string jsonString;        
        public void saveJsonToFile(string filePath)
        {
            File.WriteAllText(filePath, jsonString);
        }
    }
    [Serializable] public class LandmarkPoint
    {
        public int landmarkId;
        public float[] position;
        public LandmarkPoint(int landmarkId, float[] position)        
        {
            this.landmarkId = landmarkId;
            this.position = position;
        }
    }
    [Serializable] public class TrackedPeople
    {
        public int trackId;
        public LandmarkPoint[] landmarkPoints;
        public TrackedPeople(int trackId = 0, LandmarkPoint[] landmarkPoints = null)
        {
            this.trackId = trackId;
            this.landmarkPoints = landmarkPoints ?? new LandmarkPoint[33];
        }
    }
    [Serializable] public class AllTracked
    {
        public TrackedPeople[] trackedPeople;
        public AllTracked(TrackedPeople[] trackedPeople = null)
        {
            this.trackedPeople = trackedPeople ?? new TrackedPeople[4];
        }
    }
    void OApplicationQuit()
    {
        if(thread != null && thread.IsAlive)
        {
            thread.Abort();
        }
        if(udpClient != null)
        {
            udpClient.Close();
        }
    }
}
