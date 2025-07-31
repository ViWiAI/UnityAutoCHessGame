using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Game.Core;
using Game.Combat;

namespace Game.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerHero playerHero;
        public List<Pet> pets = new List<Pet>();
        public int gold = 100;
        private string worldMapScene = "WorldMap";
        private string battleMapScene = "BattleMap";

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

        public void StartGame()
        {
            SceneManager.LoadScene(worldMapScene);
        }

        public void EnterBattle(List<Monster> enemies)
        {
            // 保存当前世界地图状态
            SceneManager.LoadScene(battleMapScene);
            //SceneManager.sceneLoaded += (scene, mode) =>
            //{
            //    BattleManager.Instance.InitializeBattle(new List<Hero> { playerHero }, pets, enemies);
            //};
        }

        public void ExitBattle(bool victory)
        {
            if (victory)
            {
                // 发放奖励
                gold += Random.Range(50, 100);
             //   UIManager.Instance.ShowTreasureChest();
            }
            SceneManager.LoadScene(worldMapScene);
        }

        public void SaveGame()
        {
            PlayerPrefs.SetFloat("PlayerPosX", playerHero.transform.position.x);
            PlayerPrefs.SetFloat("PlayerPosY", playerHero.transform.position.y);
            PlayerPrefs.SetInt("Gold", gold);
            // 保存英雄属性、装备等
            PlayerPrefs.Save();
        }

        public void LoadGame()
        {
            if (PlayerPrefs.HasKey("PlayerPosX"))
            {
                Vector3 pos = new Vector3(
                    PlayerPrefs.GetFloat("PlayerPosX"),
                    PlayerPrefs.GetFloat("PlayerPosY"),
                    0);
                playerHero.transform.position = pos;
                gold = PlayerPrefs.GetInt("Gold");
            }
        }
    }
}