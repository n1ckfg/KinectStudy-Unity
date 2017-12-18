﻿using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using MessagePack;

namespace sugi.cc
{
    public class WebsocketDataServer : SingletonMonoBehaviour<WebsocketDataServer>
    {
        WebSocketServer server { get { if (_server == null) _server = new WebSocketServer(listenPort); return _server; } }
        WebSocketServer _server;

        public int listenPort = 3000;
        public bool useLZ4;

        private void Start()
        {
            if (Instance != this) Destroy(gameObject);
            var dataBehaviours = FindObjectsOfType<WebSocketDataBehaviour>();
            foreach (var db in dataBehaviours)
                db.AddService();
            server.Start();
        }

        public void AddService<T>(string path)
        {
            server.AddWebSocketService<DataGetter<T>>(path);
        }

        public class DataGetter<T> : WebSocketBehavior
        {
            public static Queue<T> recievedData = new Queue<T>();
            protected override void OnMessage(MessageEventArgs e)
            {
                var str = e.Data;
                T data = default(T);
                if (e.IsPing)
                    return;
                else if (e.IsBinary)
                {
                    if (Instance.useLZ4)
                        data = LZ4MessagePackSerializer.Deserialize<T>(e.RawData);
                    else
                        data = MessagePackSerializer.Deserialize<T>(e.RawData);
                }
                else if (e.IsText)
                    data = JsonUtility.FromJson<T>(str);

                lock (recievedData)
                    recievedData.Enqueue(data);
            }
        }
    }
}