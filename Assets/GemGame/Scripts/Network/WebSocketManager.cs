using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.WebSockets;
using Best.WebSockets.Implementations;
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
        private Queue<BufferSegment> messageQueue = new Queue<BufferSegment>();
        private const float RECONNECT_INTERVAL = 5f; // 重连间隔缩短为5秒
        private const float CONNECTION_CHECK_INTERVAL = 2f; // 连接检查间隔
        private DateTime lastConnectionAttemptTime = DateTime.MinValue;
        private bool isManualDisconnect;

        public Action<MessageType, byte[]> OnMessageReceived;

        public enum MessageType : byte
        {
            Connect = 0,
            PlayerLogin = 1,
            PlayerOnline = 2,
            MoveRequest = 3,
            MoveConfirmed = 4,
            PlayerMap = 5,
            TeamJoin = 6,
            PvPMatch = 7,
            CreateCharacter = 8,
            CharacterList = 9,
            Error = 255
        }

        public bool IsConnected => ws != null && ws.IsOpen;

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
            if (UnityMainThreadDispatcher.Instance() == null)
            {
                Debug.LogError("UnityMainThreadDispatcher 未初始化");
            }
            StartCoroutine(ConnectionMonitor());
        }

        private IEnumerator ConnectionMonitor()
        {
            while (true)
            {
                yield return new WaitForSeconds(CONNECTION_CHECK_INTERVAL);

                if (isManualDisconnect) continue;

                // 检查是否需要连接或重连
                if (!IsConnected && !isConnecting &&
                    (DateTime.Now - lastConnectionAttemptTime).TotalSeconds >= RECONNECT_INTERVAL)
                {
                    StartCoroutine(Connect());
                }
            }
        }

        private IEnumerator Connect()
        {
            if (isConnecting) yield break;

            isConnecting = true;
            lastConnectionAttemptTime = DateTime.Now;

            // 清理旧连接
            if (ws != null)
            {
                ws.OnOpen -= OnWebSocketOpen;
                ws.OnBinary -= OnWebSocketBinary;
                ws.OnClosed -= OnWebSocketClosed;
                ws.Close();
                ws = null;
            }

            Debug.Log("正在连接WebSocket服务器...");
            ws = new WebSocket(new Uri("ws://localhost:7272"));

            // 注册回调
            ws.OnOpen += OnWebSocketOpen;
            ws.OnBinary += OnWebSocketBinary;
            ws.OnClosed += OnWebSocketClosed;

            // 开始连接
            ws.Open();

            // 等待连接完成或超时(10秒)
            float timeout = 10f;
            float elapsed = 0f;
            while (isConnecting && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (elapsed >= timeout)
            {
                Debug.LogWarning("WebSocket连接超时");
                if (ws != null)
                {
                    ws.Close();
                    ws = null;
                }
            }

            isConnecting = false;
        }

        private void OnWebSocketOpen(WebSocket webSocket)
        {
            Debug.Log("WebSocket连接成功");
            isConnecting = false;
            isManualDisconnect = false;

            // 发送队列中的消息
            while (messageQueue.Count > 0)
            {
                var message = messageQueue.Dequeue();
                SendInternal(message);
            }
        }

        private void OnWebSocketBinary(WebSocket webSocket, BufferSegment buffer)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                try
                {
                    if (buffer.Count < 5)
                    {
                        Debug.LogWarning($"收到无效消息，长度不足: {buffer.Count}");
                        return;
                    }

                    byte[] data = new byte[buffer.Count];
                    Array.Copy(buffer.Data, buffer.Offset, data, 0, buffer.Count);

                    MessageType msgType = (MessageType)data[0];
                    int payloadLength = BitConverter.ToInt32(data, 1);
                    if (BitConverter.IsLittleEndian)
                    {
                        payloadLength = System.Net.IPAddress.NetworkToHostOrder(payloadLength);
                    }

                    if (payloadLength > data.Length - 5)
                    {
                        Debug.LogWarning($"Payload长度不匹配: 期望{payloadLength}, 实际{data.Length - 5}");
                        return;
                    }

                    byte[] payload = new byte[payloadLength];
                    Array.Copy(data, 5, payload, 0, payloadLength);
                    OnMessageReceived?.Invoke(msgType, payload);
                }
                catch (Exception e)
                {
                    Debug.LogError($"二进制消息解析失败: {e.Message}");
                }
            });
        }

        private void OnWebSocketClosed(WebSocket webSocket, WebSocketStatusCodes code, string message)
        {
            Debug.Log($"WebSocket关闭: {message} (Code: {code})");
            isConnecting = false;

            // 清理回调
            if (ws != null)
            {
                ws.OnOpen -= OnWebSocketOpen;
                ws.OnBinary -= OnWebSocketBinary;
                ws.OnClosed -= OnWebSocketClosed;
                ws = null;
            }

            if (!isManualDisconnect && code != WebSocketStatusCodes.NormalClosure)
            {
                Debug.LogWarning($"WebSocket异常关闭，将尝试重连: Code={code}, Message={message}");
            }
        }

        public void Send(MessageType msgType, BufferSegment message)
        {
            if (IsConnected)
            {
                SendInternal(message);
            }
            else
            {
                messageQueue.Enqueue(message);
                Debug.Log($"WebSocket未连接，消息已加入队列: msgType={msgType}");
            }
        }

        private void SendInternal(BufferSegment message)
        {
            try
            {
                ws.SendAsBinary(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"发送消息失败: {e.Message}");
                messageQueue.Enqueue(message); // 重新加入队列
                if (ws != null)
                {
                    ws.Close();
                    ws = null;
                }
            }
        }

        public void Disconnect()
        {
            isManualDisconnect = true;
            if (ws != null)
            {
                ws.Close();
                ws = null;
            }
            messageQueue.Clear();
        }

        public void Reconnect()
        {
            isManualDisconnect = false;
            if (!IsConnected && !isConnecting)
            {
                StartCoroutine(Connect());
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }
}