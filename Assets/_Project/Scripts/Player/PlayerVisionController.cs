using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EscapeUNIFRANZ.Player
{
    /// <summary>
    /// Keeps a soft near light and a shadow-aware forward light aligned with the
    /// player's last valid visual direction. It has no encounter-specific logic.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerVisualController))]
    public sealed class PlayerVisionController : MonoBehaviour
    {
        [SerializeField] private PlayerVisualController visualController;
        [SerializeField] private Light2D immediateLight;
        [SerializeField] private Light2D forwardLight;

        [Header("Immediate visibility")]
        [SerializeField, Min(0.1f)] private float immediateRadius = 1.25f;
        [SerializeField, Min(0f)] private float immediateIntensity = 0.72f;

        [Header("Forward visibility")]
        [SerializeField, Min(0.5f)] private float forwardRange = 4.4f;
        [SerializeField, Range(1f, 179f)] private float forwardAngle = 84f;
        [SerializeField, Range(0f, 179f)] private float innerAngle = 56f;
        [SerializeField, Min(0f)] private float forwardIntensity = 1.05f;
        [SerializeField] private Color lightColor = new Color(0.72f, 0.84f, 1f, 1f);

        public PlayerVisualController.FacingDirection CurrentDirection =>
            visualController != null
                ? visualController.CurrentDirection
                : PlayerVisualController.FacingDirection.Front;

        private void Awake()
        {
            if (visualController == null)
            {
                visualController = GetComponent<PlayerVisualController>();
            }

            EnsureLights();
            ApplyLightSettings();
            ApplyDirection(CurrentDirection);
        }

        private void LateUpdate()
        {
            ApplyDirection(CurrentDirection);
        }

        private void OnValidate()
        {
            immediateRadius = Mathf.Max(0.1f, immediateRadius);
            forwardRange = Mathf.Max(0.5f, forwardRange);
            forwardAngle = Mathf.Clamp(forwardAngle, 1f, 179f);
            innerAngle = Mathf.Clamp(innerAngle, 0f, forwardAngle);

            if (Application.isPlaying)
            {
                ApplyLightSettings();
                ApplyDirection(CurrentDirection);
            }
        }

        public static float ResolveRotationDegrees(
            PlayerVisualController.FacingDirection direction)
        {
            switch (direction)
            {
                case PlayerVisualController.FacingDirection.Back:
                    return 180f;
                case PlayerVisualController.FacingDirection.Left:
                    return -90f;
                case PlayerVisualController.FacingDirection.Right:
                    return 90f;
                default:
                    return 0f;
            }
        }

        private void ApplyDirection(PlayerVisualController.FacingDirection direction)
        {
            if (forwardLight == null)
            {
                return;
            }

            float rotation = ResolveRotationDegrees(direction);
            forwardLight.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            forwardLight.transform.localPosition = DirectionVector(direction) * 0.16f;
        }

        private void ApplyLightSettings()
        {
            ConfigurePointLight(
                immediateLight,
                immediateRadius,
                360f,
                360f,
                immediateIntensity,
                false);
            ConfigurePointLight(
                forwardLight,
                forwardRange,
                forwardAngle,
                innerAngle,
                forwardIntensity,
                true);
        }

        private void EnsureLights()
        {
            if (immediateLight == null)
            {
                immediateLight = FindOrCreateLight("ImmediateVisionLight");
            }

            if (forwardLight == null)
            {
                forwardLight = FindOrCreateLight("ForwardVisionLight");
            }
        }

        private Light2D FindOrCreateLight(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                child = new GameObject(childName).transform;
                child.SetParent(transform, false);
            }

            Light2D light = child.GetComponent<Light2D>();
            return light != null ? light : child.gameObject.AddComponent<Light2D>();
        }

        private void ConfigurePointLight(
            Light2D light,
            float radius,
            float outerAngle,
            float lightInnerAngle,
            float lightIntensity,
            bool shadows)
        {
            if (light == null)
            {
                return;
            }

            light.lightType = Light2D.LightType.Point;
            light.color = lightColor;
            light.intensity = lightIntensity;
            light.falloffIntensity = 0.72f;
            light.pointLightInnerRadius = shadows ? 0.15f : radius * 0.45f;
            light.pointLightOuterRadius = radius;
            light.pointLightOuterAngle = outerAngle;
            light.pointLightInnerAngle = Mathf.Min(lightInnerAngle, outerAngle);
            light.shadowsEnabled = shadows;
            light.shadowIntensity = shadows ? 0.93f : 0f;
            light.shadowSoftness = shadows ? 0.28f : 0f;
        }

        private static Vector3 DirectionVector(
            PlayerVisualController.FacingDirection direction)
        {
            switch (direction)
            {
                case PlayerVisualController.FacingDirection.Back:
                    return Vector3.up;
                case PlayerVisualController.FacingDirection.Left:
                    return Vector3.left;
                case PlayerVisualController.FacingDirection.Right:
                    return Vector3.right;
                default:
                    return Vector3.down;
            }
        }
    }
}
