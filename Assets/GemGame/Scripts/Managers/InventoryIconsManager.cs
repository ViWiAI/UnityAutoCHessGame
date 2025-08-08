using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Core;

namespace Game.Managers
{
    public class InventoryIconsManager : MonoBehaviour
    {
        public static InventoryIconsManager Instance { get; private set; }

        [SerializeField] private Image meleeImage;
        [SerializeField] private Image bowImage;
        [SerializeField] private Image staffImage;
        [SerializeField] private Image duelistOffhandImage;
        [SerializeField] private Image shieldImage;
        [SerializeField] private Image quiverImage;
        [SerializeField] private Image armorImage;
        [SerializeField] private Image helmetImage;
        [SerializeField] private Image shoulderImage;
        [SerializeField] private Image armImage;
        [SerializeField] private Image feetImage;
        [SerializeField] private Image hairImage;
        [SerializeField] private Image faceImage;

        private Vector2 buttonSize = new Vector2(100, 100);
        private float offset = 7f;
        private Dictionary<Gears, Image> inventoryGearImages = new Dictionary<Gears, Image>();

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

        public void StartMe()
        {
            inventoryGearImages.Add(Gears.Melee, meleeImage);
            inventoryGearImages.Add(Gears.Bow, bowImage);
            inventoryGearImages.Add(Gears.Staff, staffImage);
            inventoryGearImages.Add(Gears.DuelistOffhand, duelistOffhandImage);
            inventoryGearImages.Add(Gears.Shield, shieldImage);
            inventoryGearImages.Add(Gears.Quiver, quiverImage);
            inventoryGearImages.Add(Gears.Armor, armorImage);
            inventoryGearImages.Add(Gears.Helmet, helmetImage);
            inventoryGearImages.Add(Gears.Shoulder, shoulderImage);
            inventoryGearImages.Add(Gears.Arm, armImage);
            inventoryGearImages.Add(Gears.Feet, feetImage);
            inventoryGearImages.Add(Gears.Hair, hairImage);
            inventoryGearImages.Add(Gears.Face, faceImage);

            foreach (var image in inventoryGearImages.Values)
            {
                if (image == null)
                {
                    Debug.LogError("未配置部分装备图标，请检查 Inspector 设置！");
                }
            }
        }

        public void ChangeIcon(Gears gear, Sprite gearSprite)
        {
            if (!inventoryGearImages.ContainsKey(gear) || inventoryGearImages[gear] == null)
            {
                Debug.LogWarning($"无效的装备类型 {gear} 或未配置图标！");
                return;
            }

            Image iconImage = inventoryGearImages[gear];
            RectTransform imageRT = iconImage.GetComponent<RectTransform>();
            iconImage.sprite = gearSprite;

            iconImage.SetNativeSize();
            if (imageRT.sizeDelta.x > buttonSize.x - offset)
            {
                imageRT.sizeDelta *= (buttonSize.x - offset) / imageRT.sizeDelta.x;
            }
            if (imageRT.sizeDelta.y > buttonSize.y - offset)
            {
                imageRT.sizeDelta *= (buttonSize.y - offset) / imageRT.sizeDelta.y;
            }
        }
    }
}