using Game.Animation;
using Game.Managers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Game.Data;

public class CharacterManager : MonoBehaviour
{
    // ����ģʽ������ȫ�ַ���
    public static CharacterManager Instance { get; private set; }

    private Dictionary<string, Dictionary<int, GameObject>> skinPrefabs = new Dictionary<string, Dictionary<int, GameObject>>();

    private float spriteHeightOffset = -0.2f; // Ĭ��ֵ������Inspector�е���

    // �洢��ǰʵ������Ƥ������
    private List<GameObject> instantiatedSkins = new List<GameObject>();

    public string selectRole { get; set; }


    // �����࣬���ڽ���JSON
    [System.Serializable]
    private class ServerData
    {
        public List<Character> characters;
        public List<Job> jobs;
        public List<Skin> skins;
    }

    // ��ѡ��SkinData��������ڴ洢Ƥ��Ԫ����
    public class SkinData : MonoBehaviour
    {
        public int SkinId { get; set; }
        public string Role { get; set; }
    }

    // ��ɫ���ݽṹ
    [System.Serializable]
    public class Character
    {
        public string name;
        public long id; // ��ɫΨһID
        public string job; // ְҵ��Warrior, Mage, Hunter, Rogue, Priest��
        public int skinId; // ʹ�õ�Ƥ��ID
        // �ɸ�����Ҫ���������ɫ����ֶΣ���ȼ���װ����
    }

    // ְҵ���ݽṹ
    [System.Serializable]
    public class Job
    {
        public long id;
        public string job; // ְҵ����
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

    // Ƥ�����ݽṹ
    [System.Serializable]
    public class Skin
    {
        public long id;
        public string skinName;
        public int skinId;
        public string rarity; // common, rare, epic, legendary, mythic
        public string job; // ��Ӧְҵ
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

    // �洢���������ص�����
    private List<Character> playerCharacters = new List<Character>(); // ����Ѵ����Ľ�ɫ
    private List<Job> jobData = new List<Job>(); // ְҵ��
    private Dictionary<string, List<Skin>> skinData = new Dictionary<string, List<Skin>>(); // ��ְҵ�����Ƥ����

    // �¼�������֪ͨUI����
    public Action<List<Character>> OnCharacterListUpdated;
    public Action<List<Job>> OnJobListUpdated;
    public Action<string, List<Skin>> OnSkinListUpdated;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ���ֵ����ڳ����л�ʱ������
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Ԥ��������Ƥ��Ԥ�Ƽ�
        PreloadSkinPrefabs();
        InitRoleCharacter(HeroRole.Warrior);
    }

    public void InitRoleCharacter(HeroRole role)
    {
        // ���֮ǰ�����Ľ�ɫ����
        foreach (var skin in instantiatedSkins)
        {
            if (skin != null)
            {
                Destroy(skin);
            }
        }
        instantiatedSkins.Clear();

        // ��ö��ֵת��Ϊ�ַ���������Ԥ�Ƽ�·����CharacterManager
        selectRole = role.ToString();
        if (string.IsNullOrEmpty(selectRole))
        {
            Debug.LogError("InitJobCharacter: ��Ч��ְҵö��ֵ��");
            return;
        }

        // ����ְҵȷ��Ƥ��IDǰ׺
        int skinIdPrefix = role switch
        {
            HeroRole.Warrior => 1000,
            HeroRole.Mage => 2000,
            HeroRole.Hunter => 3000,
            HeroRole.Rogue => 4000,
            HeroRole.Priest => 5000,
            _ => throw new ArgumentException($"δְ֪ҵ: {selectRole}")
        };

        // ����6��Ƥ����Tilemap�������꣨����������ã�
        Vector3Int[] skinPositions = new Vector3Int[]
        {
            new Vector3Int(0, 0, 0), // Ƥ��1
            new Vector3Int(-4, 2, 0), // Ƥ��2
            new Vector3Int(-2, 2, 0), // Ƥ��3
            new Vector3Int(0, 2, 0),  // Ƥ��4
            new Vector3Int(2, 2, 0),  // Ƥ��5
            new Vector3Int(4, 2, 0)   // Ƥ��6
        };

        // ��ȡTilemap
        var tilemap = MapManager.Instance.GetTilemap();
        if (tilemap == null)
        {
            Debug.LogError("InitJobCharacter: δ�ҵ�MapManager�е�Tilemap��");
            return;
        }

        // ʵ����6��Ƥ��Ԥ�Ƽ�
        for (int skinId = 1; skinId <= 6; skinId++)
        {
            int actualSkinId = skinIdPrefix + skinId; // ����ʵ��Ƥ��ID������1001
            GameObject skinPrefab = CharacterManager.Instance.GetSkinPrefab(selectRole, actualSkinId);
            if (skinPrefab == null)
            {
                Debug.LogWarning($"δ�ҵ�ְҵ {selectRole} ��Ƥ��Ԥ�Ƽ���Ƥ��ID: {actualSkinId}");
                continue;
            }

            // ��Tilemap��������ת��Ϊ��������
            Vector3 worldPos = tilemap.GetCellCenterWorld(skinPositions[skinId - 1]);
            worldPos += new Vector3(0, spriteHeightOffset, 0);

            // �ڼ�����������괦ʵ����Ƥ��Ԥ�Ƽ�
            GameObject playerObj = Instantiate(skinPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"{selectRole}_Skin_{actualSkinId}"; // ����Ψһ���ƣ����ڵ���

            // ��ӵ�ʵ�����б�
            instantiatedSkins.Add(playerObj);

            // ��ѡ�����SkinData����洢Ƥ��Ԫ����
            var skinData = playerObj.AddComponent<SkinData>();
            skinData.SkinId = actualSkinId;
            skinData.Role = selectRole;

            // ��ѡ�����ʹ��Animator������Ĭ�϶���
            var animator = playerObj.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play("Idle"); // �滻Ϊ��Ķ���״̬����
            }
        }

    }

    

    // ��ʼ�����������ص����ݣ���չ�Լ���Ԥ�Ƽ���
    public void InitializeData(string jsonData)
    {
        try
        {
            var data = JsonUtility.FromJson<ServerData>(jsonData);

            // ��ʼ����ҽ�ɫ�б�
            playerCharacters = data.characters ?? new List<Character>();
            OnCharacterListUpdated?.Invoke(playerCharacters);

            // ��ʼ��ְҵ����
            jobData = data.jobs ?? new List<Job>();
            OnJobListUpdated?.Invoke(jobData);

            // ��ʼ��Ƥ�����ݣ���ְҵ����
            skinData.Clear();
            foreach (var skin in data.skins)
            {
                if (!skinData.ContainsKey(skin.job))
                {
                    skinData[skin.job] = new List<Skin>();
                }
                skinData[skin.job].Add(skin);
            }

            // ֪ͨUI����Ƥ���б�
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

    // ��ȡ����Ѵ����Ľ�ɫ�б�
    public List<Character> GetPlayerCharacters()
    {
        return playerCharacters;
    }

    // ��ȡְҵ�б�
    public List<Job> GetJobList()
    {
        return jobData;
    }

    // ��ȡĳְҵ��Ƥ���б�
    public List<Skin> GetSkinsForJob(string job)
    {
        return skinData.ContainsKey(job) ? skinData[job] : new List<Skin>();
    }

    // �����½�ɫ
    public void CreateCharacter(string job, int skinId, Action<bool, string> callback)
    {
        // ��ְ֤ҵ��Ƥ���Ƿ���Ч
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

        // ģ�ⷢ�����󵽷�����������ɫ
        // �����������������´����Ľ�ɫ����
        StartCoroutine(SendCreateCharacterRequest(job, skinId, callback));
    }

    // ģ�����������ʵ����Ŀ���滻Ϊ��ʵ����������
    private System.Collections.IEnumerator SendCreateCharacterRequest(string job, int skinId, Action<bool, string> callback)
    {
        // ģ�������ӳ�
        yield return new WaitForSeconds(1f);

        // ģ�������������ɫ�ɹ�
        Character newCharacter = new Character
        {
            id = playerCharacters.Count + 1, // ģ������ID
            job = job,
            skinId = skinId
        };

        playerCharacters.Add(newCharacter);
        OnCharacterListUpdated?.Invoke(playerCharacters);
        callback?.Invoke(true, "Character created successfully!");
    }

    // ѡ���ɫ������Ϸ
    public void SelectCharacter(long characterId, Action<bool, Character> callback)
    {
        Character selected = playerCharacters.Find(c => c.id == characterId);
        if (selected == null)
        {
            callback?.Invoke(false, null);
            Debug.LogError($"Character with ID {characterId} not found.");
            return;
        }

        // ֪ͨ��Ϸ�����ɫѡ��״̬�����ؽ�ɫ����
        callback?.Invoke(true, selected);
        Debug.Log($"Selected character: {selected.job} with skin ID: {selected.skinId}");
    }

    // ��ȡƤ����Sprite������UIչʾ��
    // �޸ģ���չ GetSkinSprite ��֧�ִ�Ԥ�Ƽ�����ȡ Sprite����ѡ��
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

    // ������Ԥ��������Ƥ��Ԥ�Ƽ�
    private void PreloadSkinPrefabs()
    {
        string[] jobs = { "Warrior", "Mage", "Hunter", "Rogue", "Priest" };
        int[] skinIdPrefixes = { 1000, 2000, 3000, 4000, 5000 }; // ÿ��ְҵ��Ƥ��IDǰ׺

        for (int i = 0; i < jobs.Length; i++)
        {
            string job = jobs[i];
            int prefix = skinIdPrefixes[i];

            if (!skinPrefabs.ContainsKey(job))
            {
                skinPrefabs[job] = new Dictionary<int, GameObject>();
            }

            // ÿ��ְҵ��6��Ƥ����skinId �� prefix+1 �� prefix+6������1001-1006��
            for (int skinId = 1; skinId <= 6; skinId++)
            {
                int actualSkinId = prefix + skinId; // ʵ��Ƥ��ID������1001, 1002, ..., 1006
                string path = $"Prefabs/Skins/{job}/{actualSkinId}";
                GameObject prefab = Resources.Load<GameObject>(path);
                if (prefab != null)
                {
                    skinPrefabs[job][actualSkinId] = prefab; // ʹ��ʵ�ʵ�skinId����1001���洢
                    Debug.Log($"Loaded skin prefab: {path}");
                }
                else
                {
                    Debug.LogWarning($"Skin prefab not found at: {path}");
                }
            }
        }
    }

    // ��������ȡָ��ְҵ��Ƥ��ID��Ԥ�Ƽ�
    public GameObject GetSkinPrefab(string job, int skinId)
    {
        if (skinPrefabs.ContainsKey(job) && skinPrefabs[job].ContainsKey(skinId))
        {
            return skinPrefabs[job][skinId];
        }

        // ���δԤ���أ����Լ�ʱ����
        string path = $"Prefabs/Skins/{job}/{skinId}";
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"Skin prefab not found at path: {path}");
        }
        else
        {
            // ����Ԥ�Ƽ�
            if (!skinPrefabs.ContainsKey(job))
            {
                skinPrefabs[job] = new Dictionary<int, GameObject>();
            }
            skinPrefabs[job][skinId] = prefab;
        }
        return prefab;
    }


}