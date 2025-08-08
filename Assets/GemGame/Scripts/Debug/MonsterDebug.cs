using UnityEngine;
using Game.Core;

public class MonsterDebug : MonoBehaviour
{
    [SerializeField] private Monster monster;

    private void OnGUI()
    {
        if (monster == null) return;

        GUILayout.Label($"Monster: {monster.heroName}");
        bool autoSearch = GUILayout.Toggle(monster.IsAutoSearchEnabled(), "Auto Search");
        bool autoCounter = GUILayout.Toggle(monster.IsAutoCounterAttackEnabled(), "Auto Counter Attack");

        if (autoSearch != monster.IsAutoSearchEnabled())
        {
            monster.SetAutoSearchEnabled(autoSearch);
        }
        if (autoCounter != monster.IsAutoCounterAttackEnabled())
        {
            monster.SetAutoCounterAttackEnabled(autoCounter);
        }
    }
}