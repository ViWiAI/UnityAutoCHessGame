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
        // 单例模式，方便全局访问
        public static LocalizationManager Instance { get; private set; }

        [SerializeField] private TextAsset[] languageFiles; // 分配zh-CN.json, en-US.json
        private Dictionary<string, Dictionary<HeroRole, RoleDescriptionData>> languageMap;
        private string currentLanguage = "zh-CN"; // 默认语言

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

        // 获取职业简介
        public string GetJobDescription(HeroRole job)
        {
            if (languageMap.TryGetValue(currentLanguage, out var jobMap) &&
                jobMap.TryGetValue(job, out var data))
            {
                string coloredSkin = FormatSkinWithColors(data.skin, currentLanguage);
                Debug.Log($"原始皮肤描述: {data.skin}");
                Debug.Log($"格式化后皮肤描述: {coloredSkin}");
                return $"{data.description}\n\n{data.endPhrase}\n\n{coloredSkin}";
            }
            Debug.LogWarning($"未找到职业 {job} 的简介（语言: {currentLanguage}）");
            return "暂无简介。";
        }

        private string FormatSkinWithColors(string skinText, string language)
        {
            // 定义稀有度与颜色的映射
            var colorMap = new Dictionary<string, string>
            {
                { language == "zh-CN" ? "普通" : "Common", "#FFFFFF" }, // 白色
                { language == "zh-CN" ? "稀有" : "Rare", "#00FF00" },   // 绿色
                { language == "zh-CN" ? "史诗" : "Epic", "#0000FF" },   // 蓝色
                { language == "zh-CN" ? "传奇" : "Legendary", "#800080" }, // 紫色
                { language == "zh-CN" ? "神话" : "Mythic", "#FFA500" }    // 橙色
            };

            // 正则表达式：中文用【】，英文用[]
            string pattern = language == "zh-CN" ? @"(普通|稀有|史诗|传奇|神话)\s*【([^】]*)】" : @"(Common|Rare|Epic|Legendary|Mythic)\s*\[([^\]]*)\]";
            string result = Regex.Replace(skinText, pattern, match =>
            {
                string rarity = match.Groups[1].Value;
                string skinName = match.Groups[2].Value;
                Debug.Log($"匹配到稀有度: {rarity}, 皮肤名称: {skinName}");
                return $"<color={colorMap[rarity]}>{rarity} [{skinName}]</color>";
            });

            // 检查是否匹配
            if (result == skinText)
            {
                Debug.LogWarning($"正则表达式未匹配任何皮肤描述: {skinText}，语言: {language}");
            }

            return result;
        }

        // 切换语言
        public void SetLanguage(string language)
        {
            if (languageMap.ContainsKey(language))
            {
                currentLanguage = language;
                Debug.Log($"语言切换为: {language}");
            }
            else
            {
                Debug.LogWarning($"不支持的语言: {language}");
            }
        }
    }
}