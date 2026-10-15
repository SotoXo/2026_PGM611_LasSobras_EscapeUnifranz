using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EscapeUNIFRANZ.UI
{
    /// <summary>
    /// Plays short, irregular lighting failures over still hallway images.
    /// It intentionally avoids a fixed frame rate so the result reads as faulty
    /// electricity rather than a looping animation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HallwayFlicker : MonoBehaviour
    {
        [SerializeField] private Image primaryImage;
        [SerializeField] private Image blendImage;
        [SerializeField] private Sprite[] lightingStates;

        [Header("Timing")]
        [SerializeField] private Vector2 restingDuration = new Vector2(2.5f, 5f);
        [SerializeField] private Vector2 shortFailureDuration = new Vector2(0.08f, 0.15f);
        [SerializeField] private Vector2 rapidFailureDuration = new Vector2(0.05f, 0.12f);
        [SerializeField] private Vector2 recoveryDuration = new Vector2(0.08f, 0.2f);
        [SerializeField, Range(0f, 0.08f)] private float crossfadeDuration = 0.035f;
        [SerializeField, Range(0f, 1f)] private float rapidBurstChance = 0.42f;

        private readonly System.Random random = new System.Random(2309);
        private Coroutine flickerRoutine;
        private int currentState;

        public int CurrentState => currentState;

        private void OnEnable()
        {
            ShowImmediately(0);
            flickerRoutine = StartCoroutine(FlickerLoop());
        }

        private void OnDisable()
        {
            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
                flickerRoutine = null;
            }

            ShowImmediately(0);
        }

        private IEnumerator FlickerLoop()
        {
            while (isActiveAndEnabled)
            {
                yield return new WaitForSecondsRealtime(RandomRange(restingDuration));

                int firstFailure = RandomFailureState(1);
                yield return CrossfadeTo(firstFailure);
                yield return new WaitForSecondsRealtime(RandomRange(shortFailureDuration));

                if (random.NextDouble() <= rapidBurstChance)
                {
                    int secondFailure = RandomFailureState(firstFailure);
                    yield return CrossfadeTo(secondFailure);
                    yield return new WaitForSecondsRealtime(RandomRange(rapidFailureDuration));

                    int thirdFailure = RandomFailureState(secondFailure);
                    yield return CrossfadeTo(thirdFailure);
                    yield return new WaitForSecondsRealtime(RandomRange(recoveryDuration));
                }

                yield return CrossfadeTo(0);
            }
        }

        private IEnumerator CrossfadeTo(int stateIndex)
        {
            if (!HasUsableState(stateIndex) || primaryImage == null)
            {
                yield break;
            }

            Sprite next = lightingStates[stateIndex];
            if (blendImage == null || crossfadeDuration <= 0f)
            {
                primaryImage.sprite = next;
                currentState = stateIndex;
                yield break;
            }

            blendImage.sprite = next;
            SetAlpha(blendImage, 0f);
            blendImage.enabled = true;

            float elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(blendImage, Mathf.Clamp01(elapsed / crossfadeDuration));
                yield return null;
            }

            primaryImage.sprite = next;
            currentState = stateIndex;
            SetAlpha(blendImage, 0f);
            blendImage.enabled = false;
        }

        private int RandomFailureState(int stateToAvoid)
        {
            if (lightingStates == null || lightingStates.Length <= 1)
            {
                return 0;
            }

            int candidate = random.Next(1, lightingStates.Length);
            if (lightingStates.Length > 2 && candidate == stateToAvoid)
            {
                candidate = candidate % (lightingStates.Length - 1) + 1;
            }
            return candidate;
        }

        private void ShowImmediately(int stateIndex)
        {
            currentState = HasUsableState(stateIndex) ? stateIndex : 0;
            if (primaryImage != null && HasUsableState(currentState))
            {
                primaryImage.sprite = lightingStates[currentState];
            }

            if (blendImage != null)
            {
                SetAlpha(blendImage, 0f);
                blendImage.enabled = false;
            }
        }

        private bool HasUsableState(int stateIndex)
        {
            return lightingStates != null &&
                stateIndex >= 0 &&
                stateIndex < lightingStates.Length &&
                lightingStates[stateIndex] != null;
        }

        private float RandomRange(Vector2 range)
        {
            float minimum = Mathf.Max(0f, Mathf.Min(range.x, range.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
