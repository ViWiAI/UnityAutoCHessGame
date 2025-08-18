using Game.Animation;
using Game.Data;
using Game.Managers;
using System;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterManager : MonoBehaviour
{
    // 单例模式，方便全局访问
    public static CharacterManager Instance { get; private set; }

    private Dictionary<string, Dictionary<int, GameObject>> skinPrefabs = new Dictionary<string, Dictionary<int, GameObject>>();

    private float spriteHeightOffset = -0.2f; // 默认值，可在Inspector中调整

    // 存储当前实例化的皮肤对象
    private List<GameObject> instantiatedSkins = new List<GameObject>();

    private List<GameObject> instantiatedCharacter = new List<GameObject>();

    public string selectRole { get; set; }


    // 可选：SkinData组件，用于存储皮肤元数据
    public class SkinData : MonoBehaviour
    {
        public int SkinId { get; set; }
        public string Role { get; set; }
    }

    // 职业数据结构
    [System.Serializable]
    public class Job
    {
        public long id;
        public string job; // 职业名称
        public float lucky;
        public float hp;
        public float mp;
        public float ac;
        public float mc;
        public float atkDmg;
        public float atkSpeed;
        public float spellPower;
        public float mpRegen;
        public float hpRegen;
        public float critRate;
        public float critDmg;
        public float dodgeRate;
        public float moveSpeed;
        public float ranged;
        public float str;
        public float sta;
        public float dex;
        public float intel;
        public float spi;
    }

    // 皮肤数据结构
    [System.Serializable]
    public class Skin
    {
        public long id;
        public string skinName;
        public int skinId;
        public string rarity; // common, rare, epic, legendary, mythic
        public string job; // 对应职业
        public float lucky;
        public float hp;
        public float mp;
        public float ac;
        public float mc;
        public float atkDmg;
        public float atkSpeed;
        public float spellPower;
        public float mpRegen;
        public float hpRegen;
        public float critRate;
        public float critDmg;
        public float dodgeRate;
        public float moveSpeed;
        public float ranged;
        public float str;
        public float sta;
        public float dex;
        public float intel;
        public float spi;
        public int mintCount;
        public int count;
    }

    // 存储服务器返回的数据
    private List<PlayerCharacterInfo> playerCharacters = new List<PlayerCharacterInfo>(); // 玩家已创建的角色
    private List<Job> jobData = new List<Job>(); // 职业表
    private Dictionary<string, List<Skin>> skinData = new Dictionary<string, List<Skin>>(); // 按职业分类的皮肤表

    public PlayerCharacterInfo selectPLayerCharacter;

    // 事件，用于通知UI更新
    public Action<List<PlayerCharacterInfo>> OnCharacterListUpdated;
    public Action<List<Job>> OnJobListUpdated;
    public Action<string, List<Skin>> OnSkinListUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 保持单例在场景切换时不销毁
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 预加载所有皮肤预制件
        PreloadSkinPrefabs();
    }

    public GameObject InitPlayerObj(HeroRole role, int skinId)
    {
        // 根据职业确定皮肤ID前缀
        int skinIdPrefix = role switch
        {
            HeroRole.Warrior => 1000,
            HeroRole.Mage => 2000,
            HeroRole.Hunter => 3000,
            HeroRole.Rogue => 4000,
            HeroRole.Priest => 5000,
            _ => throw new ArgumentException($"未知职业: {selectRole}")
        };

        int actualSkinId = skinIdPrefix + skinId; // 计算实际皮肤ID，例如1001


        string roleName = RoleToString(skinIdPrefix/1000-1);
        GameObject skinPrefab = CharacterManager.Instance.GetSkinPrefab(roleName, actualSkinId);
        if (skinPrefab == null)
        {
            Debug.LogWarning($"未找到职业 {selectRole} 的皮肤预制件，皮肤ID: {actualSkinId}");
        }
        return skinPrefab;
    }

    public void InitRoleCharacter(HeroRole role)
    {
        // 清空之前创建的角色对象
        foreach (var skin in instantiatedCharacter)
        {
            if (skin != null)
            {
                Destroy(skin);
            }
        }
        instantiatedCharacter.Clear();
        // 清空之前创建的skins角色对象
        foreach (var skin in instantiatedSkins)
        {
            if (skin != null)
            {
                Destroy(skin);
            }
        }
        instantiatedSkins.Clear();

        // 将枚举值转换为字符串，用于预制件路径和CharacterManager
        selectRole = role.ToString();
        if (string.IsNullOrEmpty(selectRole))
        {
            Debug.LogError("InitJobCharacter: 无效的职业枚举值。");
            return;
        }

        // 根据职业确定皮肤ID前缀
        int skinIdPrefix = role switch
        {
            HeroRole.Warrior => 1000,
            HeroRole.Mage => 2000,
            HeroRole.Hunter => 3000,
            HeroRole.Rogue => 4000,
            HeroRole.Priest => 5000,
            _ => throw new ArgumentException($"未知职业: {selectRole}")
        };

        // 定义6个皮肤的Tilemap格子坐标（保持你的设置）
        Vector3Int[] skinPositions = new Vector3Int[]
        {
            new Vector3Int(0, 0, 0), // 皮肤1
            new Vector3Int(-4, 2, 0), // 皮肤2
            new Vector3Int(-2, 2, 0), // 皮肤3
            new Vector3Int(0, 2, 0),  // 皮肤4
            new Vector3Int(2, 2, 0),  // 皮肤5
            new Vector3Int(4, 2, 0)   // 皮肤6
        };

        // 获取Tilemap
        var tilemap = MapManager.Instance.GetTilemap();
        if (tilemap == null)
        {
            Debug.LogError("InitJobCharacter: 未找到MapManager中的Tilemap。");
            return;
        }

        // 实例化6个皮肤预制件
        for (int skinId = 1; skinId <= 6; skinId++)
        {
            int actualSkinId = skinIdPrefix + skinId; // 计算实际皮肤ID，例如1001
            GameObject skinPrefab = CharacterManager.Instance.GetSkinPrefab(selectRole, actualSkinId);
            if (skinPrefab == null)
            {
                Debug.LogWarning($"未找到职业 {selectRole} 的皮肤预制件，皮肤ID: {actualSkinId}");
                continue;
            }

            // 将Tilemap格子坐标转换为世界坐标
            Vector3 worldPos = tilemap.GetCellCenterWorld(skinPositions[skinId - 1]);
            worldPos += new Vector3(0, spriteHeightOffset, 0);

            // 在计算的世界坐标处实例化皮肤预制件
            GameObject playerObj = Instantiate(skinPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"{selectRole}_Skin_{actualSkinId}"; // 设置唯一名称，便于调试

            // 添加到实例化列表
            instantiatedSkins.Add(playerObj);

            // 可选：添加SkinData组件存储皮肤元数据
            var skinData = playerObj.AddComponent<SkinData>();
            skinData.SkinId = actualSkinId;
            skinData.Role = selectRole;

            // 可选：如果使用Animator，触发默认动画
            var animator = playerObj.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play("Idle"); // 替换为你的动画状态名称
            }
        }

    }

    public void InitPlayerCharacterList()
    {
        // 清空之前创建的skins角色对象
        foreach (var skin in instantiatedSkins)
        {
            if (skin != null)
            {
                Destroy(skin);
            }
        }
        instantiatedSkins.Clear();
        // 清空之前创建的角色对象
        foreach (var skin in instantiatedCharacter)
        {
            if (skin != null)
            {
                Destroy(skin);
            }
        }
        instantiatedCharacter.Clear();

        // 获取Tilemap
        var tilemap = MapManager.Instance.GetTilemap();
        if (tilemap == null)
        {
            Debug.LogError("InitPlayerCharacterList: 未找到MapManager中的Tilemap。");
            return;
        }

        // 获取玩家角色列表
        var characters = GetPlayerCharacters();
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("InitPlayerCharacterList: 玩家角色列表为空。");
            return;
        }

        // 定义起始坐标和间距
        Vector3Int startPos = new Vector3Int(-4, 2, 0); // 起始坐标
        int spacing = 2; // 每个角色之间的X轴间距（单位：Tilemap格子）

        // 遍历角色列表并实例化
        for (int i = 0; i < characters.Count; i++)
        {
            var character = characters[i];
            
            string roleName = RoleToString(character.Role-1); // 将Role编号转换为职业名称
            Debug.LogWarning($"角色职业数字ID {character.Role} -- {roleName}");
            if (string.IsNullOrEmpty(roleName))
            {
                Debug.LogWarning($"角色 {character.CharacterId} 的职业编号 {character.Role} 无效。");
                continue;
            }

            // 根据职业确定皮肤ID前缀
            int skinIdPrefix = roleName switch
            {
                "Warrior" => 1000,
                "Mage" => 2000,
                "Hunter" => 3000,
                "Rogue" => 4000,
                "Priest" => 5000,
                _ => throw new ArgumentException($"未知职业: {selectRole}")
            };

            // 获取皮肤预制件
            GameObject skinPrefab = GetSkinPrefab(roleName, skinIdPrefix + character.SkinId);
            if (skinPrefab == null)
            {
                Debug.LogWarning($"未找到职业 {roleName} 的皮肤预制件，皮肤ID: {character.SkinId}");
                continue;
            }

            // 计算当前角色的Tilemap格子坐标（向右排列）
            Vector3Int tilePos = new Vector3Int(startPos.x + i * spacing, startPos.y, startPos.z);

            // 将Tilemap格子坐标转换为世界坐标
            Vector3 worldPos = tilemap.GetCellCenterWorld(tilePos);
            worldPos += new Vector3(0, spriteHeightOffset, 0); // 应用高度偏移

            // 实例化皮肤预制件
            GameObject playerObj = Instantiate(skinPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"{roleName}_Character_{character.CharacterId}"; // 设置唯一名称

            // 添加到实例化列表
            instantiatedCharacter.Add(playerObj);

            // 添加SkinData组件存储元数据
            var skinData = playerObj.AddComponent<SkinData>();
            skinData.SkinId = character.SkinId;
            skinData.Role = roleName;

            // 播放默认动画（如果有Animator组件）
            var animator = playerObj.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play("Idle"); // 替换为你的动画状态名称
            }

            Debug.Log($"实例化角色: ID={character.CharacterId}, Name={character.Name}, Role={roleName}, SkinId={character.SkinId}, Position={worldPos}");
        }
    }

    public string RoleToString(int role)
    {
        return role switch
        {
            (int)HeroRole.Warrior => "Warrior",
            (int)HeroRole.Mage => "Mage",
            (int)HeroRole.Hunter => "Hunter",
            (int)HeroRole.Rogue => "Rogue",
            (int)HeroRole.Priest => "Priest",
            _ => string.Empty // 无效职业返回空字符串
        };
    }


    public void AddCharacterList(PlayerCharacterInfo PlayerCharacterInfo)
    {
        playerCharacters.Add(PlayerCharacterInfo);
    }

    // 获取玩家已创建的角色列表
    public List<PlayerCharacterInfo> GetPlayerCharacters()
    {
        return playerCharacters;
    }
    
    public void CleanPlayerCharacters()
    {
        playerCharacters.Clear();
    }

    public void SetSelectCharacter(int index)
    {
        if(playerCharacters.Count >= (index+1))
        {
            UIManager.Instance.ShowStartGameButton(true);
            selectPLayerCharacter = playerCharacters[index];
            Debug.LogWarning($"当前选择的角色：{selectPLayerCharacter.CharacterId} --{selectPLayerCharacter.Name} -- {selectPLayerCharacter.Level} -- {selectPLayerCharacter.Role} -- {selectPLayerCharacter.SkinId} -- {selectPLayerCharacter.MapId} -- {selectPLayerCharacter.X} -- {selectPLayerCharacter.Y}");
        }
        
    }

    // 获取职业列表
    public List<Job> GetJobList()
    {
        return jobData;
    }

    // 获取某职业的皮肤列表
    public List<Skin> GetSkinsForJob(string job)
    {
        return skinData.ContainsKey(job) ? skinData[job] : new List<Skin>();
    }

    // 创建新角色
    public void CreateCharacter(string job, int skinId, Action<bool, string> callback)
    {
        // 验证职业和皮肤是否有效
        Job selectedJob = jobData.Find(j => j.job == job);
        if (selectedJob == null)
        {
            callback?.Invoke(false, $"Invalid job: {job}");
            return;
        }

        Skin selectedSkin = skinData.ContainsKey(job) ? skinData[job].Find(s => s.skinId == skinId) : null;
        if (selectedSkin == null)
        {
            callback?.Invoke(false, $"Invalid skin ID: {skinId} for job: {job}");
            return;
        }

        // 模拟发送请求到服务器创建角色
        // 这里假设服务器返回新创建的角色数据
       
    }

 

    // 选择角色进入游戏
    public void SelectCharacter(long characterId, Action<bool, PlayerCharacterInfo> callback)
    {
        //PlayerCharacterInfo selected = playerCharacters.Find(c => c.id == characterId);
        //if (selected == null)
        //{
        //    callback?.Invoke(false, null);
        //    Debug.LogError($"Character with ID {characterId} not found.");
        //    return;
        //}

        //// 通知游戏进入角色选择状态，加载角色数据
        //callback?.Invoke(true, selected);
        //Debug.Log($"Selected character: {selected.Role} with skin ID: {selected.SkinId}");
    }

    // 获取皮肤的Sprite（用于UI展示）
    // 修改：扩展 GetSkinSprite 以支持从预制件中提取 Sprite（可选）
    public Sprite GetSkinSprite(string job, int skinId)
    {
        GameObject prefab = GetSkinPrefab(job, skinId);
        if (prefab != null)
        {
            SpriteRenderer spriteRenderer = prefab.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                return spriteRenderer.sprite;
            }
            else
            {
                Debug.LogWarning($"No SpriteRenderer found on skin prefab: {job}/{skinId}");
            }
        }
        return null;
    }

    // 新增：预加载所有皮肤预制件
    private void PreloadSkinPrefabs()
    {
        string[] jobs = { "Warrior", "Mage", "Hunter", "Rogue", "Priest" };
        int[] skinIdPrefixes = { 1000, 2000, 3000, 4000, 5000 }; // 每种职业的皮肤ID前缀

        for (int i = 0; i < jobs.Length; i++)
        {
            string job = jobs[i];
            int prefix = skinIdPrefixes[i];

            if (!skinPrefabs.ContainsKey(job))
            {
                skinPrefabs[job] = new Dictionary<int, GameObject>();
            }

            // 每种职业有6种皮肤，skinId 从 prefix+1 到 prefix+6（例如1001-1006）
            for (int skinId = 1; skinId <= 6; skinId++)
            {
                int actualSkinId = prefix + skinId; // 实际皮肤ID，例如1001, 1002, ..., 1006
                string path = $"Prefabs/Skins/{job}/{actualSkinId}";
                GameObject prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    skinPrefabs[job][actualSkinId] = prefab; // 使用实际的skinId（如1001）存储
                    Debug.Log($"Loaded skin prefab: {path}");
                }
                else
                {
                    Debug.LogWarning($"Skin prefab not found at: {path}");
                }
            }
        }
    }

    // 新增：获取指定职业和皮肤ID的预制件
    public GameObject GetSkinPrefab(string job, int skinId)
    {
        Debug.Log($"GetSkinPrefab: {job} -- {skinId}");
        if (skinPrefabs.ContainsKey(job) && skinPrefabs[job].ContainsKey(skinId))
        {
            return skinPrefabs[job][skinId];
        }

        // 如果未预加载，尝试即时加载
        string path = $"Prefabs/Skins/{job}/{skinId}";
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"Skin prefab not found at path: {path}");
        }
        else
        {
            // 缓存预制件
            if (!skinPrefabs.ContainsKey(job))
            {
                skinPrefabs[job] = new Dictionary<int, GameObject>();
            }
            skinPrefabs[job][skinId] = prefab;
        }
        return prefab;
    }


}