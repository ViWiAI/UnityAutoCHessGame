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
    // ����ģʽ������ȫ�ַ���
    public static CharacterManager Instance { get; private set; }

    private Dictionary<string, Dictionary<int, GameObject>> skinPrefabs = new Dictionary<string, Dictionary<int, GameObject>>();

    private float spriteHeightOffset = -0.2f; // Ĭ��ֵ������Inspector�е���

    // �洢��ǰʵ������Ƥ������
    private List<GameObject> instantiatedSkins = new List<GameObject>();

    private List<GameObject> instantiatedCharacter = new List<GameObject>();

    public string selectRole { get; set; }


    // ��ѡ��SkinData��������ڴ洢Ƥ��Ԫ����
    public class SkinData : MonoBehaviour
    {
        public int SkinId { get; set; }
        public string Role { get; set; }
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
    private List<PlayerCharacterInfo> playerCharacters = new List<PlayerCharacterInfo>(); // ����Ѵ����Ľ�ɫ
    private List<Job> jobData = new List<Job>(); // ְҵ��
    private Dictionary<string, List<Skin>> skinData = new Dictionary<string, List<Skin>>(); // ��ְҵ�����Ƥ����

    public PlayerCharacterInfo selectPLayerCharacter;

    // �¼�������֪ͨUI����
    public Action<List<PlayerCharacterInfo>> OnCharacterListUpdated;
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
    }

    public GameObject InitPlayerObj(HeroRole role, int skinId)
    {
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

        int actualSkinId = skinIdPrefix + skinId; // ����ʵ��Ƥ��ID������1001


        string roleName = RoleToString(skinIdPrefix/1000-1);
        GameObject skinPrefab = CharacterManager.Instance.GetSkinPrefab(roleName, actualSkinId);
        if (skinPrefab == null)
        {
            Debug.LogWarning($"δ�ҵ�ְҵ {selectRole} ��Ƥ��Ԥ�Ƽ���Ƥ��ID: {actualSkinId}");
        }
        return skinPrefab;
    }

    public void InitRoleCharacter(HeroRole role)
    {
        // ���֮ǰ�����Ľ�ɫ����
        foreach (var skin in instantiatedCharacter)
        {
            if (skin != null)
            {
                Destroy(skin);
            }
        }
        instantiatedCharacter.Clear();
        // ���֮ǰ������skins��ɫ����
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

    public void InitPlayerCharacterList()
    {
        // ���֮ǰ������skins��ɫ����
        foreach (var skin in instantiatedSkins)
        {
            if (skin != null)
            {
                Destroy(skin);
            }
        }
        instantiatedSkins.Clear();
        // ���֮ǰ�����Ľ�ɫ����
        foreach (var skin in instantiatedCharacter)
        {
            if (skin != null)
            {
                Destroy(skin);
            }
        }
        instantiatedCharacter.Clear();

        // ��ȡTilemap
        var tilemap = MapManager.Instance.GetTilemap();
        if (tilemap == null)
        {
            Debug.LogError("InitPlayerCharacterList: δ�ҵ�MapManager�е�Tilemap��");
            return;
        }

        // ��ȡ��ҽ�ɫ�б�
        var characters = GetPlayerCharacters();
        if (characters == null || characters.Count == 0)
        {
            Debug.LogWarning("InitPlayerCharacterList: ��ҽ�ɫ�б�Ϊ�ա�");
            return;
        }

        // ������ʼ����ͼ��
        Vector3Int startPos = new Vector3Int(-4, 2, 0); // ��ʼ����
        int spacing = 2; // ÿ����ɫ֮���X���ࣨ��λ��Tilemap���ӣ�

        // ������ɫ�б�ʵ����
        for (int i = 0; i < characters.Count; i++)
        {
            var character = characters[i];
            
            string roleName = RoleToString(character.Role-1); // ��Role���ת��Ϊְҵ����
            Debug.LogWarning($"��ɫְҵ����ID {character.Role} -- {roleName}");
            if (string.IsNullOrEmpty(roleName))
            {
                Debug.LogWarning($"��ɫ {character.CharacterId} ��ְҵ��� {character.Role} ��Ч��");
                continue;
            }

            // ����ְҵȷ��Ƥ��IDǰ׺
            int skinIdPrefix = roleName switch
            {
                "Warrior" => 1000,
                "Mage" => 2000,
                "Hunter" => 3000,
                "Rogue" => 4000,
                "Priest" => 5000,
                _ => throw new ArgumentException($"δְ֪ҵ: {selectRole}")
            };

            // ��ȡƤ��Ԥ�Ƽ�
            GameObject skinPrefab = GetSkinPrefab(roleName, skinIdPrefix + character.SkinId);
            if (skinPrefab == null)
            {
                Debug.LogWarning($"δ�ҵ�ְҵ {roleName} ��Ƥ��Ԥ�Ƽ���Ƥ��ID: {character.SkinId}");
                continue;
            }

            // ���㵱ǰ��ɫ��Tilemap�������꣨�������У�
            Vector3Int tilePos = new Vector3Int(startPos.x + i * spacing, startPos.y, startPos.z);

            // ��Tilemap��������ת��Ϊ��������
            Vector3 worldPos = tilemap.GetCellCenterWorld(tilePos);
            worldPos += new Vector3(0, spriteHeightOffset, 0); // Ӧ�ø߶�ƫ��

            // ʵ����Ƥ��Ԥ�Ƽ�
            GameObject playerObj = Instantiate(skinPrefab, worldPos, Quaternion.identity);
            playerObj.name = $"{roleName}_Character_{character.CharacterId}"; // ����Ψһ����

            // ��ӵ�ʵ�����б�
            instantiatedCharacter.Add(playerObj);

            // ���SkinData����洢Ԫ����
            var skinData = playerObj.AddComponent<SkinData>();
            skinData.SkinId = character.SkinId;
            skinData.Role = roleName;

            // ����Ĭ�϶����������Animator�����
            var animator = playerObj.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play("Idle"); // �滻Ϊ��Ķ���״̬����
            }

            Debug.Log($"ʵ������ɫ: ID={character.CharacterId}, Name={character.Name}, Role={roleName}, SkinId={character.SkinId}, Position={worldPos}");
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
            _ => string.Empty // ��Чְҵ���ؿ��ַ���
        };
    }


    public void AddCharacterList(PlayerCharacterInfo PlayerCharacterInfo)
    {
        playerCharacters.Add(PlayerCharacterInfo);
    }

    // ��ȡ����Ѵ����Ľ�ɫ�б�
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
            Debug.LogWarning($"��ǰѡ��Ľ�ɫ��{selectPLayerCharacter.CharacterId} --{selectPLayerCharacter.Name} -- {selectPLayerCharacter.Level} -- {selectPLayerCharacter.Role} -- {selectPLayerCharacter.SkinId} -- {selectPLayerCharacter.MapId} -- {selectPLayerCharacter.X} -- {selectPLayerCharacter.Y}");
        }
        
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
       
    }

 

    // ѡ���ɫ������Ϸ
    public void SelectCharacter(long characterId, Action<bool, PlayerCharacterInfo> callback)
    {
        //PlayerCharacterInfo selected = playerCharacters.Find(c => c.id == characterId);
        //if (selected == null)
        //{
        //    callback?.Invoke(false, null);
        //    Debug.LogError($"Character with ID {characterId} not found.");
        //    return;
        //}

        //// ֪ͨ��Ϸ�����ɫѡ��״̬�����ؽ�ɫ����
        //callback?.Invoke(true, selected);
        //Debug.Log($"Selected character: {selected.Role} with skin ID: {selected.SkinId}");
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
        Debug.Log($"GetSkinPrefab: {job} -- {skinId}");
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