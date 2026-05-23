using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine.UI;
using System;

public class Webcam : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private int port; // Replace with your desired port number
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private Thread thread;
    private byte[] image;
    private byte[] lastImage;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private Texture2D imageTexture;
    private bool loaded;
    void Awake()
    {
        imageTexture = new Texture2D(2, 2);
        rawImage.texture = imageTexture;

        udpClient = new UdpClient(port);
        serverEndPoint = new IPEndPoint(IPAddress.Any, port);
        thread = new Thread(new ThreadStart(reciveImage));
        thread.IsBackground = true;
        thread.Start();
    }
    void reciveImage()
    {
        while (true)
        {
            try
            {
                if(udpClient.Available > 0)
                {
                    image = udpClient.Receive(ref serverEndPoint);
                    lock (this)
                    {
                        lastImage = image;
                        loaded = true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("Image loading failed");
            }
        }
    }
    void Update()
    {
        if (loaded)
        {
            lock (this)
            {
                imageTexture.LoadImage(lastImage);
                loaded = false;
            }
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
