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
using System.Drawing;
using System.Threading.Tasks;

public class YOLODatas : MonoBehaviour
{
    [SerializeField] private int port; // Replace with your desired port number
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private Thread thread;
    private byte[] data;
    private DatasToJson json = new DatasToJson();
    public List<Vector2>[] points = new List<Vector2>[4];
    public Tracked tracked;
    private GameSystem gameSystem;
    private List<bool>[] pointState = new List<bool>[4];
    private List<Vector2> pre = new List<Vector2>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        udpClient = new UdpClient(port);
        serverEndPoint = new IPEndPoint(IPAddress.Any, port);
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new List<Vector2>();
        }

        for (int i = 0; i < pointState.Length; i++)
        {
            pointState[i] = new List<bool>();
            for(int j = 0; j < 17; j++){
                pointState[i].Add(false);
            }
        }
    }
    void Start()
    {
        gameSystem = FindFirstObjectByType<GameSystem>();
        Debug.Log(serverEndPoint);
    }

    void reciveDatas()
    {
        while (true)
        {
            if (udpClient.Available > 0)
            {
                data = udpClient.Receive(ref serverEndPoint);
                lock(this)
                {
                    string input = Encoding.UTF8.GetString(data);
                    string wrappedJson = "{\"people\":" + input + "}";

                    // json.input = wrappedJson;
                    // json.saveJsonToFile(Application.dataPath + "/data.json");

                    try
                    {
                        tracked = JsonUtility.FromJson<Tracked>(wrappedJson);

                        // Debug.Log("Number of tracked people: " + allTracked.trackedPeople.Length);

                        for (int i = 0; i < tracked.people.Length; i++)
                        {
                            Person person = tracked.people[i];
                            points[i].Clear();

                            for (int j = 0; j < person.keypoints.Length; j++)
                            {
                                Vector2 point = new Vector2(person.keypoints[j].position[0], person.keypoints[j].position[1]);
                                points[i].Add(point);
                            }
                            // points[i] = pre;
                        }
                    }
                    catch (Exception e) 
                    {
                        Debug.LogError("Data Lose" + e.Message);
                    }
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
                lock (this)
                {
                    pointState = gameSystem.pointState;
                    if (tracked.people == null) continue;
                    for (int i = 0; i < tracked.people.Length; i++)
                    {
                        log += $"player_id:{tracked.people[i].person_id}, ";

                        for (int j = 0; j < points[i].Count; j++)
                        {
                            Vector2 point = points[i][j];
                            try
                            {
                                log += $"point_id:{j}(position:[{point.x}, {point.y}], be_seleted:[{pointState[i][j]}]), ";
                            }
                            catch(Exception e)
                            {
                                Debug.Log(e);
                            }
                        }
                        log += "\n";
                    }
                    // jsonDatas.jsonString = log;
                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string filePath = Path.Combine(desktopPath, "LandmarkPoints_Export.json");
                    File.WriteAllText(filePath, log);
                }
            }
            catch(IOException e) 
            {
                Debug.LogError("Save failed" + e.Message);
            }
            // AssetDatabase.ImportAsset("Assets/landmarkpoints.json");
            yield return null;
        }
    }
    [Serializable] public class DatasToJson
    {
        public string input;        
        public void saveJsonToFile(string filePath)
        {
            File.WriteAllText(filePath, input);
        }
    }
    [Serializable] public class Keypoints
    {
        public int point_id;
        public int[] position;
        public Keypoints(int point_id, int[] position)        
        {
            this.point_id = point_id;
            this.position = position;
        }
    }
    [Serializable] public class Person
    {
        public int person_id;
        public Keypoints[] keypoints;
        public Person(int person_id = 0, Keypoints[] keypoints = null)
        {
            this.person_id = person_id;
            this.keypoints = keypoints ?? new Keypoints[17];
        }
    }
    [Serializable] public class Tracked
    {
        public Person[] people;
        public Tracked(Person[] people = null)
        {
            this.people = people ?? new Person[4];
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
    public Task StartLoad()
    {
        if(thread == null)
        {
            thread = new Thread(new ThreadStart(reciveDatas));
            thread.IsBackground = true;
            thread.Start();
            // Start the coroutine normally; do not await a Coroutine.
            StartCoroutine(WriteData());
        }
        return Task.CompletedTask;
    }
}
