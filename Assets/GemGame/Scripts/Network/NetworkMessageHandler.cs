using Best.HTTP.Shared.PlatformSupport.Memory;
using Best.WebSockets;
using Game.Animation;
using Game.Combat;
using Game.Core;
using Game.Data;
using Game.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Game.Network
{
    public class NetworkMessageHandler : MonoBehaviour
    {
        public static NetworkMessageHandler Instance { get; private set; }

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
            WebSocketManager.Instance.OnMessageReceived += HandleServerMessage;
        }

        private void OnDestroy()
        {
            if (WebSocketManager.Instance != null)
            {
                WebSocketManager.Instance.OnMessageReceived -= HandleServerMessage;
            }
        }

        private void HandleServerMessage(WebSocketManager.MessageType msgType, byte[] payload)
        {
            try
            {
                switch (msgType)
                {
                    case WebSocketManager.MessageType.Connect:
                        HandleConnect(payload);
                        break;
                    case WebSocketManager.MessageType.PlayerLogin:
                        HandlePlayerLogin(payload);
                        break;
                    case WebSocketManager.MessageType.CreateCharacter:
                        HandleCreateCharacter(payload);
                        break;
                    case WebSocketManager.MessageType.CharacterList:
                        HandleCharacterList(payload);
                        break;
                    case WebSocketManager.MessageType.PlayerOnline:
                        HandlePlayerOnline(payload);
                        break;
                    case WebSocketManager.MessageType.MoveConfirmed:
                        HandleMoveConfirmed(payload);
                        break;
                    case WebSocketManager.MessageType.PlayerMap:
                        HandlePlayerMap(payload);
                        break;
                    case WebSocketManager.MessageType.TeamJoin:
                        HandleTeamJoin(payload);
                        break;
                    case WebSocketManager.MessageType.PvPMatch:
                        HandlePvPMatch(payload);
                        break;
                    case WebSocketManager.MessageType.Error:
                        HandleError(payload);
                        break;
                    default:
                        Debug.LogWarning($"未知消息类型: {msgType}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"消息处理失败: {e.Message}");
            }
        }

        private void HandleConnect(byte[] payload)
        {
            int offset = 0;
            try
            {
                string msg = BinaryProtocol.DecodeString(payload, ref offset);
                UIManager.Instance.ShowTipsMessage($"服务器连接成功");
                Debug.Log($"收到连接消息: {msg}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"HandleConnect 解析失败: {e.Message}, 数据: {BitConverter.ToString(payload)}");
            }
        }

        private void HandlePlayerLogin(byte[] payload)
        {
            if (payload.Length < 1)
            {
                Debug.LogWarning("PlayerLogin payload 长度不足");
                return;
            }
            byte status = payload[0];
            if (status == 0)
            {
                GameManager.Instance.SetLoginStatus(true);
                UIManager.Instance.ShowTipsMessage("登录成功");
                UIManager.Instance.Close_Login();
                UIManager.Instance.ShowUIButton(false);
                UIManager.Instance.ShowStartGameUI(true);
            }
            else if (status == 1)
            {
                UIManager.Instance.ShowErrorMessage("账号已被锁定，请联系客服");
            }
        }

        private void HandleCreateCharacter(byte[] payload)
        {
            int offset = 0;
            try
            {
                var status = BinaryProtocol.DecodeStatus(payload, ref offset);
                var msg = BinaryProtocol.DecodeString(payload, ref offset);
                if (status == 1)
                {
                    UIManager.Instance.ShowTipsMessage($"{msg}");
                   // UIManager.Instance.Close_CharacterCreation(); // Close character creation UI
                   // UIManager.Instance.ShowGameUI(true); // Show main game UI
                }
                else
                {
                    UIManager.Instance.ShowErrorMessage($" {msg}");
                }
                Debug.Log($"HandleCreateCharacter: status={status}, message={msg}, payload={BitConverter.ToString(payload)}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"HandleCreateCharacter 解析失败: {e.Message}, 数据: {BitConverter.ToString(payload)}");
                UIManager.Instance.ShowErrorMessage($"{e.Message}");
            }
        }

        private void HandleCharacterList(byte[] payload)
        {
            int offset = 0;
            try
            {
                int characterCount = BinaryProtocol.DecodeInt32(payload, ref offset);

                CharacterManager.Instance.CleanPlayerCharacters();
                for (int i = 0; i < characterCount; i++)
                {
                    var characterData = BinaryProtocol.DecodeCharacterInfo(payload, ref offset);
                    PlayerCharacterInfo character = new PlayerCharacterInfo
                    {
                        CharacterId = (int)characterData["characterId"],
                        Name = (string)characterData["name"],
                        Level = (int)characterData["level"],
                        Role = (int)characterData["role"],
                        SkinId = (int)characterData["skinId"],
                        MapId = (int)characterData["mapId"],
                        X = (int)characterData["x"],
                        Y = (int)characterData["y"]
                    };
                    CharacterManager.Instance.AddCharacterList(character);
                }

                // Update UI with character list
                UIManager.Instance.ShowCharacterUI(false);
                CharacterManager.Instance.InitPlayerCharacterList();
            }
            catch (Exception e)
            {
                Debug.LogError($"处理角色列表失败: {e.Message}");
            }
        }

        private void HandlePlayerOnline(byte[] payload)
        {
            int offset = 0;
            int playerId = BinaryProtocol.DecodeInt32(payload, ref offset);
            int mapId = BinaryProtocol.DecodeInt32(payload, ref offset);
            int role = BinaryProtocol.DecodeInt32(payload, ref offset);
            string roleStr = CharacterManager.Instance.RoleToString(role);
            Vector3Int position = BinaryProtocol.DecodePosition(payload, ref offset);

            if (playerId == 0 || mapId == 0 || role == 0)
            {
                Debug.LogWarning("PlayerOnline 消息缺少必要字段");
                return;
            }

            if (playerId != GameManager.Instance.GetLocalPlayer()?.GetPlayerId())
            {
                HeroRole heroJob = (HeroRole)Enum.Parse(typeof(HeroRole), roleStr);
                PlayerManager.Instance.AddPlayer(playerId, position, mapId, heroJob);
                Debug.Log($"收到其他玩家上线消息: {playerId}, 地图: {mapId}, 位置: {position}, 职业: {heroJob}");
            }
            else
            {
                Debug.Log($"本地玩家上线确认: {playerId}, 地图: {mapId}, 位置: {position}");
            }
        }

        private void HandleMoveConfirmed(byte[] payload)
        {
            int offset = 0;
            try
            {
                if (payload.Length != 8)
                {
                    throw new Exception($"MoveConfirmed payload 长度错误: 期望8字节，实际{payload.Length}");
                }
                Vector3Int position = BinaryProtocol.DecodePosition(payload, ref offset);
                Debug.Log($"MoveConfirmed: position=({position.x}, {position.y})");

                PlayerHero localPlayer = PlayerManager.Instance.GetLocalPlayer();
                if (localPlayer == null)
                {
                    Debug.LogWarning("MoveConfirmed: 本地玩家不存在");
                    return;
                }

                localPlayer.moveToBase(position);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"HandleMoveConfirmed 解析失败: {e.Message}, Payload: {BitConverter.ToString(payload)}");
                UIManager.Instance.ShowErrorMessage($"移动确认错误: {e.Message}，请重试");
            }
        }

        private void HandlePlayerMap(byte[] payload)
        {
            int offset = 0;
            int playerId = BinaryProtocol.DecodeInt32(payload, ref offset);
            int mapId = BinaryProtocol.DecodeInt32(payload, ref offset);
            Vector3Int position = BinaryProtocol.DecodePosition(payload, ref offset);

            if (playerId == 0 || mapId == 0)
            {
                Debug.LogWarning("PlayerMap 消息缺少必要字段");
                return;
            }

            PlayerHero localPlayer = GameManager.Instance.GetLocalPlayer();
            if (localPlayer == null)
            {
                Debug.LogWarning("本地玩家未初始化，无法处理 PlayerMap 消息");
                return;
            }

            localPlayer.SetCurrentMapId(mapId);
            localPlayer.transform.position = MapManager.Instance.GetTilemap().GetCellCenterWorld(position);
            Debug.Log($"玩家 {playerId} 初始化到地图 {mapId}，位置 {position}");
        }

        private void HandleTeamJoin(byte[] payload)
        {
            int offset = 0;
            string playerId = BinaryProtocol.DecodeString(payload, ref offset);
            string teamId = BinaryProtocol.DecodeString(payload, ref offset);
            List<string> teamMembers = BinaryProtocol.DecodeStringArray(payload, ref offset);

            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(teamId))
            {
                Debug.LogWarning("TeamJoin 消息缺少必要字段");
                return;
            }

            PlayerHero localPlayer = GameManager.Instance.GetLocalPlayer();
            if (localPlayer == null)
            {
                Debug.LogWarning("本地玩家未初始化，无法处理 TeamJoin 消息");
                return;
            }

            localPlayer.JoinTeam(teamId);
            Debug.Log($"玩家 {playerId} 加入队伍 {teamId}，成员: {string.Join(", ", teamMembers)}");
        }

        private void HandlePvPMatch(byte[] payload)
        {
            int offset = 0;
            int playerId = BinaryProtocol.DecodeInt32(payload, ref offset);
            int pvpMapId = BinaryProtocol.DecodeInt32(payload, ref offset);
            int pvpRoomId = BinaryProtocol.DecodeInt32(payload, ref offset);
            List<string> opponents = BinaryProtocol.DecodeStringArray(payload, ref offset);
            List<string> teamMembers = BinaryProtocol.DecodeStringArray(payload, ref offset);

            if (playerId == 0 || pvpMapId == 0 || pvpRoomId == 0)
            {
                Debug.LogWarning("PvPMatch 消息缺少必要字段");
                return;
            }

            MapManager.Instance?.SwitchMap(pvpMapId, pvpRoomId);
            List<Hero> opponentHeroes = new List<Hero>();
            foreach (var opponentId in opponents)
            {
                // 实现对手实例化逻辑
            }
            Debug.Log($"PVP 匹配: 地图 {pvpMapId}, 房间 {pvpRoomId}, 对手: {string.Join(", ", opponents)}");
        }

        private void HandleError(byte[] payload)
        {
            int offset = 0;
            string errorMessage = BinaryProtocol.DecodeString(payload, ref offset);
            Debug.LogWarning($"服务器错误: {errorMessage}");
            UIManager.Instance.ShowErrorMessage(errorMessage);
        }




        // 发送方法：登录请求
        public void SendLoginRequest(string username, string password)
        {
            Debug.Log("SendLoginRequest");
            BufferSegment usernameSegment = BinaryProtocol.EncodeString(username);
            BufferSegment passwordSegment = BinaryProtocol.EncodeString(password);

            int payloadLength = usernameSegment.Count + passwordSegment.Count;
            int totalLength = 5 + payloadLength;
            byte[] buffer = BufferPool.Get(totalLength, true);
            buffer[0] = (byte)WebSocketManager.MessageType.PlayerLogin;

            // 大端序编码长度(4字节)
            byte[] lengthBytes = new byte[4];
            lengthBytes[0] = (byte)(payloadLength >> 24);
            lengthBytes[1] = (byte)(payloadLength >> 16);
            lengthBytes[2] = (byte)(payloadLength >> 8);
            lengthBytes[3] = (byte)payloadLength;

            Array.Copy(lengthBytes, 0, buffer, 1, 4);

            int offset = 5;
            Array.Copy(usernameSegment.Data, usernameSegment.Offset, buffer, offset, usernameSegment.Count);
            offset += usernameSegment.Count;
            Array.Copy(passwordSegment.Data, passwordSegment.Offset, buffer, offset, passwordSegment.Count);

            BufferSegment payload = new BufferSegment(buffer, 0, totalLength);
            WebSocketManager.Instance.Send(WebSocketManager.MessageType.PlayerLogin, payload);

            BufferPool.Release(usernameSegment.Data);
            BufferPool.Release(passwordSegment.Data);
            Debug.Log($"SendLoginRequest payload: {BitConverter.ToString(payload.Data, payload.Offset, payload.Count)}");
        }

        // 发送方法：上线请求
        public void SendPlayerOnlineRequest(int playerId, int mapId, HeroRole job, Vector3Int position)
        {
            BufferSegment playerIdSegment = BinaryProtocol.EncodeInt32(playerId);
            BufferSegment mapIdSegment = BinaryProtocol.EncodeInt32(mapId);
            BufferSegment jobSegment = BinaryProtocol.EncodeString(job.ToString());
            BufferSegment positionSegment = BinaryProtocol.EncodePosition(position);

            int totalLength = playerIdSegment.Count + mapIdSegment.Count + jobSegment.Count + positionSegment.Count;
            byte[] buffer = BufferPool.Get(totalLength, true);
            int offset = 0;
            Array.Copy(playerIdSegment.Data, playerIdSegment.Offset, buffer, offset, playerIdSegment.Count);
            offset += playerIdSegment.Count;
            Array.Copy(mapIdSegment.Data, mapIdSegment.Offset, buffer, offset, mapIdSegment.Count);
            offset += mapIdSegment.Count;
            Array.Copy(jobSegment.Data, jobSegment.Offset, buffer, offset, jobSegment.Count);
            offset += jobSegment.Count;
            Array.Copy(positionSegment.Data, positionSegment.Offset, buffer, offset, positionSegment.Count);

            BufferSegment payload = new BufferSegment(buffer, 0, totalLength);
            WebSocketManager.Instance.Send(WebSocketManager.MessageType.PlayerOnline, payload);

            BufferPool.Release(playerIdSegment.Data);
            BufferPool.Release(mapIdSegment.Data);
            BufferPool.Release(jobSegment.Data);
            BufferPool.Release(positionSegment.Data);
            Debug.Log($"SendPlayerOnlineRequest payload: {BitConverter.ToString(payload.Data, payload.Offset, payload.Count)}");
        }

        // 发送方法：移动请求
        public void SendMoveRequest(int playerId, int mapId, Vector3Int position)
        {
            BufferSegment playerIdSegment = BinaryProtocol.EncodeInt32(playerId);
            BufferSegment mapIdSegment = BinaryProtocol.EncodeInt32(mapId);
            BufferSegment positionSegment = BinaryProtocol.EncodePosition(position);

            int payloadLength = playerIdSegment.Count + mapIdSegment.Count + positionSegment.Count;
            int totalLength = 5 + payloadLength; // 包含消息头
            byte[] buffer = BufferPool.Get(totalLength, true);
            buffer[0] = (byte)WebSocketManager.MessageType.MoveRequest;
            byte[] lengthBytes = BitConverter.GetBytes(payloadLength);
            if (BitConverter.IsLittleEndian)
                lengthBytes = lengthBytes.Reverse().ToArray();
            Array.Copy(lengthBytes, 0, buffer, 1, 4);
            int offset = 5;
            Array.Copy(playerIdSegment.Data, playerIdSegment.Offset, buffer, offset, playerIdSegment.Count);
            offset += playerIdSegment.Count;
            Array.Copy(mapIdSegment.Data, mapIdSegment.Offset, buffer, offset, mapIdSegment.Count);
            offset += mapIdSegment.Count;
            Array.Copy(positionSegment.Data, positionSegment.Offset, buffer, offset, positionSegment.Count);

            BufferSegment payload = new BufferSegment(buffer, 0, totalLength);
            WebSocketManager.Instance.Send(WebSocketManager.MessageType.MoveRequest, payload);

            BufferPool.Release(playerIdSegment.Data);
            BufferPool.Release(mapIdSegment.Data);
            BufferPool.Release(positionSegment.Data);
            Debug.Log($"SendMoveRequest: playerId={playerId}, mapId={mapId}, position=({position.x}, {position.y}, {position.z}), payload={BitConverter.ToString(payload.Data, payload.Offset, payload.Count)}");
        }

        public void SendCreateCharacter(string name, string role, string accountName)
        {
            BufferSegment nameSegment = BinaryProtocol.EncodeString(name);
            BufferSegment roleSegment = BinaryProtocol.EncodeString(role);
            BufferSegment accountNameSegment = BinaryProtocol.EncodeString(accountName);

            int payloadLength = nameSegment.Count + roleSegment.Count + accountNameSegment.Count;
            int totalLength = 5 + payloadLength;
            byte[] buffer = BufferPool.Get(totalLength, true);
            buffer[0] = (byte)WebSocketManager.MessageType.CreateCharacter;
            byte[] lengthBytes = BitConverter.GetBytes(payloadLength);
            if (BitConverter.IsLittleEndian)
                lengthBytes = lengthBytes.Reverse().ToArray();
            Array.Copy(lengthBytes, 0, buffer, 1, 4);
            int offset = 5;
            Array.Copy(nameSegment.Data, nameSegment.Offset, buffer, offset, nameSegment.Count);
            offset += nameSegment.Count;
            Array.Copy(roleSegment.Data, roleSegment.Offset, buffer, offset, roleSegment.Count);
            offset += roleSegment.Count;
            Array.Copy(accountNameSegment.Data, accountNameSegment.Offset, buffer, offset, accountNameSegment.Count);

            BufferSegment payload = new BufferSegment(buffer, 0, totalLength);
            WebSocketManager.Instance.Send(WebSocketManager.MessageType.CreateCharacter, payload);

            BufferPool.Release(nameSegment.Data);
            BufferPool.Release(roleSegment.Data);
            BufferPool.Release(accountNameSegment.Data);
            Debug.Log($"SendCreateCharacter: name={name}, role={role}, accountName={accountName}, payload={BitConverter.ToString(payload.Data, payload.Offset, payload.Count)}");
        }

        // 发送方法：获取玩家地图请求
        public void SendPlayerMapRequest(string playerId)
        {
            BufferSegment playerIdSegment = BinaryProtocol.EncodeString(playerId);
            WebSocketManager.Instance.Send(WebSocketManager.MessageType.PlayerMap, playerIdSegment);
            BufferPool.Release(playerIdSegment.Data);
            Debug.Log($"SendPlayerMapRequest payload: {BitConverter.ToString(playerIdSegment.Data, playerIdSegment.Offset, playerIdSegment.Count)}");
        }

        // 发送方法：加入队伍请求
        public void SendTeamJoinRequest(string playerId, string teamId)
        {
            BufferSegment playerIdSegment = BinaryProtocol.EncodeString(playerId);
            BufferSegment teamIdSegment = BinaryProtocol.EncodeString(teamId);

            int totalLength = playerIdSegment.Count + teamIdSegment.Count;
            byte[] buffer = BufferPool.Get(totalLength, true);
            int offset = 0;
            Array.Copy(playerIdSegment.Data, playerIdSegment.Offset, buffer, offset, playerIdSegment.Count);
            offset += playerIdSegment.Count;
            Array.Copy(teamIdSegment.Data, teamIdSegment.Offset, buffer, offset, teamIdSegment.Count);

            BufferSegment payload = new BufferSegment(buffer, 0, totalLength);
            WebSocketManager.Instance.Send(WebSocketManager.MessageType.TeamJoin, payload);

            BufferPool.Release(playerIdSegment.Data);
            BufferPool.Release(teamIdSegment.Data);
            Debug.Log($"SendTeamJoinRequest payload: {BitConverter.ToString(payload.Data, payload.Offset, payload.Count)}");
        }

        // 发送方法：PVP 匹配请求
        public void SendPvPMatchRequest(string playerId, string teamId, string mode)
        {
            BufferSegment playerIdSegment = BinaryProtocol.EncodeString(playerId);
            BufferSegment teamIdSegment = BinaryProtocol.EncodeString(teamId);
            BufferSegment modeSegment = BinaryProtocol.EncodeString(mode);

            int totalLength = playerIdSegment.Count + teamIdSegment.Count + modeSegment.Count;
            byte[] buffer = BufferPool.Get(totalLength, true);
            int offset = 0;
            Array.Copy(playerIdSegment.Data, playerIdSegment.Offset, buffer, offset, playerIdSegment.Count);
            offset += playerIdSegment.Count;
            Array.Copy(teamIdSegment.Data, teamIdSegment.Offset, buffer, offset, teamIdSegment.Count);
            offset += teamIdSegment.Count;
            Array.Copy(modeSegment.Data, modeSegment.Offset, buffer, offset, modeSegment.Count);

            BufferSegment payload = new BufferSegment(buffer, 0, totalLength);
            WebSocketManager.Instance.Send(WebSocketManager.MessageType.PvPMatch, payload);

            BufferPool.Release(playerIdSegment.Data);
            BufferPool.Release(teamIdSegment.Data);
            BufferPool.Release(modeSegment.Data);
            Debug.Log($"SendPvPMatchRequest payload: {BitConverter.ToString(payload.Data, payload.Offset, payload.Count)}");
        }
    }
}