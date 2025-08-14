using Game.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;


namespace Game.Managers
{
    [System.Serializable]
    public class RoleDescriptionData
    {
        public string job;
        public string description;
        public string skin;
        public string endPhrase;
    }

    [System.Serializable]
    public class LanguageData
    {
        public string language;
        public List<RoleDescriptionData> descriptions;
    }

    public class LocalizationManager : MonoBehaviour
    {
        // ����ģʽ������ȫ�ַ���
        public static LocalizationManager Instance { get; private set; }

        [SerializeField] private TextAsset[] languageFiles; // ����zh-CN.json, en-US.json
        private Dictionary<string, Dictionary<HeroRole, RoleDescriptionData>> languageMap;
        private string currentLanguage = "zh-CN"; // Ĭ������

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

            languageMap = new Dictionary<string, Dictionary<HeroRole, RoleDescriptionData>>();
            foreach (var file in languageFiles)
            {
                var data = JsonUtility.FromJson<LanguageData>(file.text);
                var jobMap = new Dictionary<HeroRole, RoleDescriptionData>();
                foreach (var desc in data.descriptions)
                {
                    if (System.Enum.TryParse<HeroRole>(desc.job, out var job))
                    {
                        jobMap[job] = desc;
                    }
                }
                languageMap[data.language] = jobMap;
            }
        }

        // ��ȡְҵ���
        public string GetJobDescription(HeroRole job)
        {
            if (languageMap.TryGetValue(currentLanguage, out var jobMap) &&
                jobMap.TryGetValue(job, out var data))
            {
                string coloredSkin = FormatSkinWithColors(data.skin, currentLanguage);
                Debug.Log($"ԭʼƤ������: {data.skin}");
                Debug.Log($"��ʽ����Ƥ������: {coloredSkin}");
                return $"{data.description}\n\n{data.endPhrase}\n\n{coloredSkin}";
            }
            Debug.LogWarning($"δ�ҵ�ְҵ {job} �ļ�飨����: {currentLanguage}��");
            return "���޼�顣";
        }

        private string FormatSkinWithColors(string skinText, string language)
        {
            // ����ϡ�ж�����ɫ��ӳ��
            var colorMap = new Dictionary<string, string>
            {
                { language == "zh-CN" ? "��ͨ" : "Common", "#FFFFFF" }, // ��ɫ
                { language == "zh-CN" ? "ϡ��" : "Rare", "#00FF00" },   // ��ɫ
                { language == "zh-CN" ? "ʷʫ" : "Epic", "#0000FF" },   // ��ɫ
                { language == "zh-CN" ? "����" : "Legendary", "#800080" }, // ��ɫ
                { language == "zh-CN" ? "��" : "Mythic", "#FFA500" }    // ��ɫ
            };

            // ������ʽ�������á�����Ӣ����[]
            string pattern = language == "zh-CN" ? @"(��ͨ|ϡ��|ʷʫ|����|��)\s*��([^��]*)��" : @"(Common|Rare|Epic|Legendary|Mythic)\s*\[([^\]]*)\]";
            string result = Regex.Replace(skinText, pattern, match =>
            {
                string rarity = match.Groups[1].Value;
                string skinName = match.Groups[2].Value;
                Debug.Log($"ƥ�䵽ϡ�ж�: {rarity}, Ƥ������: {skinName}");
                return $"<color={colorMap[rarity]}>{rarity} [{skinName}]</color>";
            });

            // ����Ƿ�ƥ��
            if (result == skinText)
            {
                Debug.LogWarning($"������ʽδƥ���κ�Ƥ������: {skinText}������: {language}");
            }

            return result;
        }

        // �л�����
        public void SetLanguage(string language)
        {
            if (languageMap.ContainsKey(language))
            {
                currentLanguage = language;
                Debug.Log($"�����л�Ϊ: {language}");
            }
            else
            {
                Debug.LogWarning($"��֧�ֵ�����: {language}");
            }
        }
    }
}