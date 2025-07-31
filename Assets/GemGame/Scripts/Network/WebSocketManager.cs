using NativeWebSocket;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Network
{
    public class WebSocketManager : MonoBehaviour
    {
        public static WebSocketManager Instance { get; private set; }
        private WebSocket ws;
        private bool isConnecting;
        private Queue<string> messageQueue = new Queue<string>(); // 消息队列
        private const float RECONNECT_INTERVAL = 5f;

        public Action<Dictionary<string, object>> OnMessageReceived;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartCoroutine(Connect());
        }

        private IEnumerator Connect()
        {
            while (true)
            {
                if (ws == null || ws.State == WebSocketState.Closed)
                {
                    isConnecting = true;
                    ws = new WebSocket("ws://localhost:8282");
                    ws.OnOpen += () =>
                    {
                        Debug.Log("WebSocket 连接成功");
                        isConnecting = false;
                        while (messageQueue.Count > 0)
                        {
                            ws.SendText(messageQueue.Dequeue());
                        }
                    };
                    ws.OnMessage += (bytes) =>
                    {
                        string message = System.Text.Encoding.UTF8.GetString(bytes);
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            try
                            {
                                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                                OnMessageReceived?.Invoke(data);
                            }
                            catch (Exception e)
                            {
                                Debug.Log($"消息解析失败: {message}, 错误: {e.Message}");
                            }
                        });
                    };
                    ws.OnError += (e) => Debug.Log($"WebSocket 错误: {e}");
                    ws.OnClose += (e) =>
                    {
                        Debug.Log($"WebSocket 关闭: {e}");
                        isConnecting = false;
                    };
                    yield return ws.Connect();
                }
                else if (ws.State == WebSocketState.Closed || ws.State == WebSocketState.Closing)
                {
                    Debug.Log("WebSocket 断开，尝试重连...");
                    yield return new WaitForSeconds(RECONNECT_INTERVAL);
                }
                yield return null;
            }
        }

        public void Send(Dictionary<string, object> message)
        {
            string json = JsonConvert.SerializeObject(message);
            if (ws != null && ws.State == WebSocketState.Open)
            {
                ws.SendText(json);
            }
            else
            {
                messageQueue.Enqueue(json);
                Debug.Log($"WebSocket 未连接，消息已加入队列: {json}");
            }
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            ws?.DispatchMessageQueue();
#endif
        }

        private void OnDestroy()
        {
            if (ws != null)
            {
                ws.Close();
                ws = null;
            }
        }
    }
}