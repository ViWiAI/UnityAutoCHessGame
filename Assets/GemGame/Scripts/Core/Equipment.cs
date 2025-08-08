using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "NewEquipment", menuName = "Game/Equipment")]
    public class Equipment : ScriptableObject
    {
        public Gears type;
        public string equipmentName;
        public Sprite icon;
        public int skinId; // 对应GearEquipper的皮肤ID
        public Dictionary<string, float> statBonuses; // 如 {"attack": 10, "armor": 5}
    }
}