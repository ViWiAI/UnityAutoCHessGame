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
        public int skinId; // ��ӦGearEquipper��Ƥ��ID
        public Dictionary<string, float> statBonuses; // �� {"attack": 10, "armor": 5}
    }
}