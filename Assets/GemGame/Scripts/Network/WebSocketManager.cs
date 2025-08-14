using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.WebSockets;
using Best.WebSockets.Implementations;
using PimDeWitte.UnityMainThreadDispatcher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Network
{
    public class WebSocketManager : MonoBehaviour
    {
        public static WebSocketManager Instance { get; private set; }

        private WebSocket ws;
        private bool isConnecting;
        private Queue<BufferSegment> messageQueue = new Queue<BufferSegment>();
        private const float RECONNECT_INTERVAL = 10f; // 重连间隔，10秒

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
            Error = 255
        }

        public bool IsConnected()
        {
            return ws != null && ws.IsOpen;
        }

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
            // 确保 UnityMainThreadDispatcher 已初始化
            if (UnityMainThreadDispatcher.Instance() == null)
            {
                Debug.LogError("UnityMainThreadDispatcher 未初始化");
            }
            StartCoroutine(Connect());
        }

        private IEnumerator Connect()
        {
            while (true)
            {
                // 检查 WebSocket 是否需要连接或重连
                if (ws == null || ws.State == WebSocketStates.Closed || ws.State == WebSocketStates.Closing)
                {
                    if (ws != null)
                    {
                        Debug.Log($"WebSocket 状态: {ws.State}, 准备重连...");
                        ws.Close(); // 确保旧连接关闭
                        ws = null;
                        // 等待重连间隔
                        Debug.Log($"等待 {RECONNECT_INTERVAL} 秒后重连...");
                        yield return new WaitForSeconds(RECONNECT_INTERVAL);
                    }

                    isConnecting = true;
                    ws = new WebSocket(new Uri("ws://localhost:7272"));

                    // 注册回调
                    ws.OnOpen += OnWebSocketOpen;
                    ws.OnBinary += OnWebSocketBinary;
                    ws.OnClosed += OnWebSocketClosed;
                    // 移除 OnMessage，避免处理文本消息
                    // ws.OnMessage += OnMessageReceivedJson;

                    Debug.Log("尝试连接 WebSocket...");
                    ws.Open();

                    // 等待连接结果
                    while (isConnecting && ws.State == WebSocketStates.Connecting)
                    {
                        yield return null;
                    }
                }
                // 每秒检查一次连接状态，避免协程过于频繁
                yield return new WaitForSeconds(5f);
            }
        }

        private void OnWebSocketOpen(WebSocket webSocket)
        {
            Debug.Log("WebSocket 连接成功");
            isConnecting = false;

            // 处理消息队列
            while (messageQueue.Count > 0)
            {
                Debug.Log("处理消息队列");
                BufferSegment message = messageQueue.Dequeue();
                ws.SendAsBinary(message);
            }
        }

        private void OnWebSocketBinary(WebSocket webSocket, BufferSegment buffer)
        {
            Debug.Log($"OnWebSocketBinary: 收到消息，长度={buffer.Count}, 数据={BitConverter.ToString(buffer.Data, buffer.Offset, buffer.Count)}");
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
                    Debug.Log($"解析消息: msgType={msgType}");
                    byte[] lengthBytes = new byte[4];
                    Array.Copy(data, 1, lengthBytes, 0, 4);
                    if (BitConverter.IsLittleEndian)
                    {
                        lengthBytes = lengthBytes.Reverse().ToArray();
                    }
                    int payloadLength = BitConverter.ToInt32(lengthBytes, 0);
                    if (payloadLength > data.Length - 5)
                    {
                        Debug.LogWarning($"Payload 长度不匹配: 期望 {payloadLength}, 实际 {data.Length - 5}");
                        return;
                    }
                    byte[] payload = new byte[payloadLength];
                    Array.Copy(data, 5, payload, 0, payloadLength);
                    Debug.Log($"消息详情: payloadLength={payloadLength}, payload={BitConverter.ToString(payload)}");
                    OnMessageReceived?.Invoke(msgType, payload);
                }
                catch (Exception e)
                {
                    Debug.LogError($"二进制消息解析失败: {e.Message}, 数据: {BitConverter.ToString(buffer.Data, buffer.Offset, buffer.Count)}");
                }
            });
        }

        private void OnWebSocketClosed(WebSocket webSocket, WebSocketStatusCodes code, string message)
        {
            Debug.Log($"WebSocket 关闭: {message} (Code: {code})");
            isConnecting = false;
            ws = null; // 确保 ws 重置，触发重连
            if (code != WebSocketStatusCodes.NormalClosure)
            {
                Debug.LogWarning($"WebSocket 异常关闭: Code={code}, Message={message}");
            }
        }

        public void Send(MessageType msgType, BufferSegment message)
        {
            if (ws != null && ws.IsOpen)
            {
                ws.SendAsBinary(message);
                Debug.Log($"发送消息: msgType={msgType}, 长度={message.Count}");
            }
            else
            {
                messageQueue.Enqueue(message);
                Debug.Log($"WebSocket 未连接，消息已加入队列: msgType={msgType}");
            }
        }

        private void Update()
        {
            // 定期日志连接状态（可选）
            //if (ws != null)
            //{
            //    Debug.Log($"WebSocket 状态: {ws.State}, 未发送数据量: {ws.BufferedAmount}");
            //}
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