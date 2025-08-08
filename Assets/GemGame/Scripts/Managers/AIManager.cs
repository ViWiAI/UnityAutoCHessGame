using UnityEngine;
using System.Collections.Generic;
using Game.Core;

namespace Game.Managers
{
    public class AIManager : MonoBehaviour
    {
        public static AIManager Instance { get; private set; }

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

        //public void UpdateAI(List<Hero> playerUnits, List<Monster> enemyUnits)
        //{
        //    foreach (var unit in playerUnits)
        //    {
        //        if (unit.heroType == HeroType.Pet || (unit.heroType == HeroType.Player && BattleManager.Instance.isBattleStarted))
        //        {
        //            UpdateUnitAI(unit);
        //        }
        //    }
        //    foreach (var enemy in enemyUnits)
        //    {
        //        UpdateUnitAI(enemy);
        //    }
        //}

        //private void UpdateUnitAI(Hero unit)
        //{
        //    if (!unit.isMoving && !unit.isAttacking)
        //    {
        //        Hero target = FindNearestTarget(unit);
        //        if (target != null)
        //        {
        //            Vector3Int targetCell = unit.GetCurrentTilemap().WorldToCell(target.transform.position);
        //            unit.Attack(target.gameObject, targetCell, true);
        //        }
        //    }
        //}

        //private Hero FindNearestTarget(Hero unit)
        //{
        //    Hero nearest = null;
        //    float minDistance = float.MaxValue;
        //    Collider2D[] hits = Physics2D.OverlapCircleAll(unit.transform.position, 10f);
        //    foreach (Collider2D hit in hits)
        //    {
        //        Hero target = hit.GetComponent<Hero>();
        //        if (target != null && !target.isDead)
        //        {
        //            if ((unit.heroType == HeroType.Monster && (target.heroType == HeroType.Player || target.heroType == HeroType.Pet)) ||
        //                ((unit.heroType == HeroType.Player || unit.heroType == HeroType.Pet) && target.heroType == HeroType.Monster))
        //            {
        //                float distance = Vector3.Distance(unit.transform.position, target.transform.position);
        //                if (distance < minDistance)
        //                {
        //                    minDistance = distance;
        //                    nearest = target;
        //                }
        //            }
        //        }
        //    }
        //    return nearest;
        //}
    }
}