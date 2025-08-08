using UnityEngine;
using System;
using System.Collections;

namespace Game.Combat
{
    public class SkillEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem effectParticleSystem; // 重命名为 effectParticleSystem
        [SerializeField] private float duration = 2f;
        [SerializeField] private bool useParticleDuration = true;
        [SerializeField] private bool useProjectile = false;
        [SerializeField] private float projectileSpeed = 5f;

        private Vector3 targetPosition;
        private Action onComplete;
        private bool isMoving;

        public void Initialize(Vector3 targetPos, Action callback)
        {
            if (effectParticleSystem == null)
            {
                Debug.LogWarning($"ParticleSystem is null on {gameObject.name}, using default duration.");
            }

            targetPosition = targetPos;
            onComplete = callback;

            float actualDuration = useParticleDuration && effectParticleSystem != null
                ? effectParticleSystem.main.duration
                : duration;

            if (useProjectile)
            {
                isMoving = true;
                StartCoroutine(MoveToTarget(actualDuration));
            }
            else
            {
                transform.position = targetPosition;
                PlayEffect(actualDuration);
            }
        }

        private void PlayEffect(float actualDuration)
        {
            if (effectParticleSystem != null)
            {
                effectParticleSystem.Play();
            }
            StartCoroutine(CompleteAfterDuration(actualDuration));
        }

        private IEnumerator MoveToTarget(float actualDuration)
        {
            Vector3 startPosition = transform.position;
            float elapsed = 0f;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * projectileSpeed / Vector3.Distance(startPosition, targetPosition);
                transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed);
                yield return null;
            }

            transform.position = targetPosition;
            isMoving = false;
            PlayEffect(actualDuration);
        }

        private IEnumerator CompleteAfterDuration(float actualDuration)
        {
            yield return new WaitForSeconds(actualDuration);
            if (effectParticleSystem != null)
            {
                effectParticleSystem.Stop();
            }
            onComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}