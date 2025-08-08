using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Animation;

namespace Game.Managers
{
    public class GearsManager : MonoBehaviour
    {
        public static GearsManager Instance { get; private set; }

        [SerializeField] private GameObject gearButtonPrefab;
        [SerializeField] private Transform gearButtonsParent;
        [SerializeField] private Sprite xMarkSprite;
        [SerializeField] private Text specialAnimationText;
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private Button weaponButton;
        [SerializeField] private Button offhandButton;
        [SerializeField] private Image meleeImage;
        [SerializeField] private Image bowImage;
        [SerializeField] private Image staffImage;
        [SerializeField] private Image shieldImage;
        [SerializeField] private Image quiverImage;
        [SerializeField] private Image duelistOffhandImage;
        [SerializeField] private PlayerHero playerHero;
        [SerializeField] private List<Equipment> equipmentDatabase; // 装备数据

        private Dictionary<Gears, List<Equipment>> gearsAndEquipment = new Dictionary<Gears, List<Equipment>>();
        private Dictionary<Gears, int> currentChosenGears = new Dictionary<Gears, int>();
        private Gears currentClickedCategory = Gears.Armor;
        private string currentClickedCategoryString;
        private Dictionary<Jobs, Gears> jobsAndWeapons = new Dictionary<Jobs, Gears>();
        private Dictionary<Jobs, Gears> jobsAndOffhands = new Dictionary<Jobs, Gears>();

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
            InventoryIconsManager.Instance.StartMe();
            InitializeEquipmentDatabase();
            InitializeJobMappings();
            ChangeJob(Jobs.Warrior);
            ClickOnCategoryButton(Gears.Armor.ToString());
        }

        private void InitializeEquipmentDatabase()
        {
            // 初始化装备数据
            foreach (Gears gear in Enum.GetValues(typeof(Gears)))
            {
                gearsAndEquipment[gear] = equipmentDatabase.FindAll(e => e.type == gear);
                currentChosenGears[gear] = 0;
            }

            // 为Helmet和Shield添加“无装备”选项
            gearsAndEquipment[Gears.Helmet].Insert(0, CreateNoneEquipment(Gears.Helmet, xMarkSprite));
            gearsAndEquipment[Gears.Shield].Insert(0, CreateNoneEquipment(Gears.Shield, xMarkSprite));
            currentChosenGears[Gears.Helmet] = 3;
            currentChosenGears[Gears.Shield] = 1;
        }

        private Equipment CreateNoneEquipment(Gears gearType, Sprite icon)
        {
            Equipment none = ScriptableObject.CreateInstance<Equipment>();
            none.type = gearType;
            none.name = "None";
            none.icon = icon;
            none.skinId = 0;
            none.statBonuses = new Dictionary<string, float>();
            return none;
        }

        private void InitializeJobMappings()
        {
            jobsAndWeapons.Add(Jobs.Warrior, Gears.Melee);
            jobsAndOffhands.Add(Jobs.Warrior, Gears.Shield);
            jobsAndWeapons.Add(Jobs.Archer, Gears.Bow);
            jobsAndOffhands.Add(Jobs.Archer, Gears.Quiver);
            jobsAndWeapons.Add(Jobs.Elementalist, Gears.Staff);
            jobsAndWeapons.Add(Jobs.Duelist, Gears.Melee);
            jobsAndOffhands.Add(Jobs.Duelist, Gears.DuelistOffhand);
        }

        public void ChangeJob(Jobs newJob)
        {
          //  playerHero.SetJob(newJob);
            UpdateSpecialAnimationText(newJob);
            UpdateWeaponAndOffhandUI();
            ClickOnCategoryButton(currentClickedCategoryString);
            ApplySkinChanges();
        }

        private void UpdateSpecialAnimationText(Jobs job)
        {
            switch (job)
            {
                case Jobs.Warrior:
                    specialAnimationText.text = "Defence";
                    break;
                case Jobs.Archer:
                    specialAnimationText.text = "Shoot3";
                    break;
                case Jobs.Elementalist:
                    specialAnimationText.text = "Cast3";
                    break;
                case Jobs.Duelist:
                    specialAnimationText.text = "Attack 3";
                    break;
            }
        }

        private void UpdateWeaponAndOffhandUI()
        {
            Jobs job = playerHero.GetComponent<GearEquipper>().Job;
            meleeImage.gameObject.SetActive(false);
            bowImage.gameObject.SetActive(false);
            staffImage.gameObject.SetActive(false);
            shieldImage.gameObject.SetActive(false);
            quiverImage.gameObject.SetActive(false);
            duelistOffhandImage.gameObject.SetActive(false);
            offhandButton.interactable = true;
            offhandButton.gameObject.SetActive(true);

            switch (job)
            {
                case Jobs.Warrior:
                    meleeImage.gameObject.SetActive(true);
                    shieldImage.gameObject.SetActive(true);
                    break;
                case Jobs.Archer:
                    bowImage.gameObject.SetActive(true);
                    quiverImage.gameObject.SetActive(true);
                    break;
                case Jobs.Elementalist:
                    staffImage.gameObject.SetActive(true);
                    offhandButton.interactable = false;
                    offhandButton.gameObject.SetActive(false);
                    break;
                case Jobs.Duelist:
                    meleeImage.gameObject.SetActive(true);
                    duelistOffhandImage.gameObject.SetActive(true);
                    break;
            }
        }

        public void ClickOnCategoryButton(string categoryName)
        {
            currentClickedCategoryString = categoryName;
            if (categoryName == "Weapon")
            {
                currentClickedCategory = jobsAndWeapons[playerHero.GetComponent<GearEquipper>().Job];
            }
            else if (categoryName == "OffHand")
            {
                currentClickedCategory = jobsAndOffhands[playerHero.GetComponent<GearEquipper>().Job];
            }
            else
            {
                currentClickedCategory = (Gears)Enum.Parse(typeof(Gears), categoryName);
            }

            ListGears(currentClickedCategory);
        }

        public void ChooseThisGear(Gears category, int gearId)
        {
            currentChosenGears[category] = gearId;
            Equipment selectedEquipment = gearsAndEquipment[category][gearId];
            playerHero.Equip(selectedEquipment);
            InventoryIconsManager.Instance.ChangeIcon(category, selectedEquipment.icon);
            ApplySkinChanges();
        }

        public void ChooseRandomGears()
        {
            foreach (var kvp in currentChosenGears)
            {
                Gears gear = kvp.Key;
                int randomId = UnityEngine.Random.Range(0, gearsAndEquipment[gear].Count);
                currentChosenGears[gear] = randomId;
                Equipment selectedEquipment = gearsAndEquipment[gear][randomId];
                playerHero.Equip(selectedEquipment);
                InventoryIconsManager.Instance.ChangeIcon(gear, selectedEquipment.icon);
            }
            ApplySkinChanges();
        }

        public void ApplySkinChanges()
        {
            GearEquipper gearEquipper = playerHero.GetComponent<GearEquipper>();
            foreach (var kvp in currentChosenGears)
            {
                switch (kvp.Key)
                {
                    case Gears.Melee: gearEquipper.Melee = kvp.Value; break;
                    case Gears.Shield: gearEquipper.Shield = kvp.Value; break;
                    case Gears.Bow: gearEquipper.Bow = kvp.Value; break;
                    case Gears.Quiver: gearEquipper.Quiver = kvp.Value; break;
                    case Gears.Staff: gearEquipper.Staff = kvp.Value; break;
                    case Gears.DuelistOffhand: gearEquipper.DuelistOffhand = kvp.Value; break;
                    case Gears.Armor: gearEquipper.Armor = kvp.Value; break;
                    case Gears.Helmet: gearEquipper.Helmet = kvp.Value; break;
                    case Gears.Shoulder: gearEquipper.Shoulder = kvp.Value; break;
                    case Gears.Arm: gearEquipper.Arm = kvp.Value; break;
                    case Gears.Feet: gearEquipper.Feet = kvp.Value; break;
                    case Gears.Hair: gearEquipper.Hair = kvp.Value; break;
                    case Gears.Face: gearEquipper.Face = kvp.Value; break;
                }
            }
            gearEquipper.ApplySkinChanges();
        }

        private void ListGears(Gears gear)
        {
            foreach (Transform child in gearButtonsParent)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < gearsAndEquipment[gear].Count; i++)
            {
                GameObject newButton = Instantiate(gearButtonPrefab, gearButtonsParent);
                GearButtonClicker clicker = newButton.GetComponent<GearButtonClicker>();
                clicker.TakeInfo(gear, gearsAndEquipment[gear][i].icon, i);
            }

            float newScrollContentHeight = 30 + (Mathf.CeilToInt((float)gearsAndEquipment[gear].Count / 5)) * 140;
            scrollContent.sizeDelta = new Vector2(scrollContent.sizeDelta.x, newScrollContentHeight);
        }
    }
}