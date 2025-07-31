using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Game.Core;

namespace Game.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        [SerializeField] private GameObject worldUI;
        [SerializeField] private GameObject battleUI;
        [SerializeField] private GameObject teamUI;
        [SerializeField] private GameObject pvpUI;
        [SerializeField] private GameObject countdownUI;


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
            
        }

        private void Update()
        {
            
        }

        

        public void ShowBattleUI(bool isBattle)
        {
            //worldUI.SetActive(!isBattle);
            //battleUI.SetActive(isBattle);
        }

        public void UpdateTeamUI(List<object> teamMembers)
        {
            teamUI.SetActive(true);
            Debug.Log($"队伍成员: {string.Join(", ", teamMembers)}");
        }

        public void ShowPVPUI()
        {
            pvpUI.SetActive(true);
        }

        public void ShowCountdown(int seconds)
        {
            countdownUI.SetActive(true);
            Debug.Log($"战斗倒计时: {seconds} 秒");
        }

        public void ShowTreasurePrompt(string message)
        {
            Debug.Log(message);
        }

        private void OnDestroy()
        {
            
        }
    }
}