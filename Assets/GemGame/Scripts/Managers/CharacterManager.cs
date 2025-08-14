using Game.Animation;
using Game.Managers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Game.Data;

public class CharacterManager : MonoBehaviour
{
    // 单例模式，方便全局访问
    public static CharacterManager Instance { get; private set; }

    private Dictionary<string, Dictionary<int, GameObject>> skinPrefabs = new Dictionary<string, Dictionary<int, GameObject>>();

    private float spriteHeightOffset = -0.2f; // 默认值，可在Inspector中调整

    // 存储当前实例化的皮肤对象
    private List<GameObject> instantiatedSkins = new List<GameObject>();

    public string selectRole { get; set; }


    // 辅助类，用于解析JSON
    [System.Serializable]
    private class ServerData
    {
        public List<Character> characters;
        public List<Job> jobs;
        public List<Skin> skins;
    }

    // 可选：SkinData组件，用于存储皮肤元数据
    public class SkinData : MonoBehaviour
    {
        public int SkinId { get; set; }
        public string Role { get; set; }
    }

    // 角色数据结构
    [System.Serializable]
    public class Character
    {
        public string name;
        public long id; // 角色唯一ID
        public string job; // 职业（Warrior, Mage, Hunter, Rogue, Priest）
        public int skinId; // 使用的皮肤ID
        // 可根据需要添加其他角色相关字段，如等级、装备等
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
    private List<Character> playerCharacters = new List<Character>(); // 玩家已创建的角色
    private List<Job> jobData = new List<Job>(); // 职业表
    private Dictionary<string, List<Skin>> skinData = new Dictionary<string, List<Skin>>(); // 按职业分类的皮肤表

    // 事件，用于通知UI更新
    public Action<List<Character>> OnCharacterListUpdated;
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
        InitRoleCharacter(HeroRole.Warrior);
    }

    public void InitRoleCharacter(HeroRole role)
    {
        // 清空之前创建的角色对象
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

    

    // 初始化服务器返回的数据（扩展以加载预制件）
    public void InitializeData(string jsonData)
    {
        try
        {
            var data = JsonUtility.FromJson<ServerData>(jsonData);

            // 初始化玩家角色列表
            playerCharacters = data.characters ?? new List<Character>();
            OnCharacterListUpdated?.Invoke(playerCharacters);

            // 初始化职业数据
            jobData = data.jobs ?? new List<Job>();
            OnJobListUpdated?.Invoke(jobData);

            // 初始化皮肤数据，按职业分类
            skinData.Clear();
            foreach (var skin in data.skins)
            {
                if (!skinData.ContainsKey(skin.job))
                {
                    skinData[skin.job] = new List<Skin>();
                }
                skinData[skin.job].Add(skin);
            }

            // 通知UI更新皮肤列表
            foreach (var job in jobData)
            {
                if (skinData.ContainsKey(job.job))
                {
                    OnSkinListUpdated?.Invoke(job.job, skinData[job.job]);
                }
            }

            Debug.Log("CharacterManager initialized successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize CharacterManager: {e.Message}");
        }
    }

    // 获取玩家已创建的角色列表
    public List<Character> GetPlayerCharacters()
    {
        return playerCharacters;
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
        StartCoroutine(SendCreateCharacterRequest(job, skinId, callback));
    }

    // 模拟服务器请求（实际项目中替换为真实的网络请求）
    private System.Collections.IEnumerator SendCreateCharacterRequest(string job, int skinId, Action<bool, string> callback)
    {
        // 模拟网络延迟
        yield return new WaitForSeconds(1f);

        // 模拟服务器创建角色成功
        Character newCharacter = new Character
        {
            id = playerCharacters.Count + 1, // 模拟自增ID
            job = job,
            skinId = skinId
        };

        playerCharacters.Add(newCharacter);
        OnCharacterListUpdated?.Invoke(playerCharacters);
        callback?.Invoke(true, "Character created successfully!");
    }

    // 选择角色进入游戏
    public void SelectCharacter(long characterId, Action<bool, Character> callback)
    {
        Character selected = playerCharacters.Find(c => c.id == characterId);
        if (selected == null)
        {
            callback?.Invoke(false, null);
            Debug.LogError($"Character with ID {characterId} not found.");
            return;
        }

        // 通知游戏进入角色选择状态，加载角色数据
        callback?.Invoke(true, selected);
        Debug.Log($"Selected character: {selected.job} with skin ID: {selected.skinId}");
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