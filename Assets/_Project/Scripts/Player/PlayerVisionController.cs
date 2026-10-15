using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace EscapeUNIFRANZ.Player
{
    /// <summary>
    /// Keeps a soft near light and a shadow-aware forward light aligned with the
    /// player's last valid visual direction. It has no encounter-specific logic.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerVisualController))]
    public sealed class PlayerVisionController : MonoBehaviour
    {
        [SerializeField] private PlayerVisualController visualController;
        [SerializeField] private Light2D immediateLight;
        [SerializeField] private Light2D forwardLight;

        [Header("Immediate visibility")]
        [SerializeField, Min(0.1f)] private float immediateRadius = 1.1f;
        [SerializeField, Min(0f)] private float immediateIntensity = 0.38f;
        [SerializeField, Range(0f, 1f)] private float immediateFalloff = 0.72f;

        [Header("Forward visibility")]
        [SerializeField, Min(0.5f)] private float forwardRange = 6.8f;
        [SerializeField, Range(1f, 179f)] private float forwardAngle = 88f;
        [SerializeField, Range(0f, 179f)] private float innerAngle = 60f;
        [SerializeField, Min(0f)] private float forwardIntensity = 1.15f;
        [SerializeField, Range(0f, 1f)] private float forwardFalloff = 0.55f;
        [Header("Emitter relative to the player's feet")]
        [SerializeField] private Vector3 emitterOrigin = new Vector3(0f, 0.3f, 0f);
        [SerializeField, Min(0f)] private float forwardOffset = 0.18f;
        [SerializeField] private Color lightColor = new Color(0.72f, 0.84f, 1f, 1f);
        private bool flashlightEquipped;
        private bool flashlightOn;
        private float interferenceUntil;
        public bool FlashlightActive => flashlightEquipped && flashlightOn && Time.time>=interferenceUntil;
        private readonly List<RaycastHit2D> emitterHits=new List<RaycastHit2D>();
        public void SetFlashlightEquipped(bool equipped) { SetFlashlightState(equipped,equipped); }
        public void SetFlashlightState(bool owned,bool on) { flashlightEquipped=owned; flashlightOn=on; ApplyLightSettings(); }
        public void InterfereFor(float seconds) { interferenceUntil=Time.time+Mathf.Clamp(seconds,0f,0.2f); ApplyLightSettings(); }
        public bool CanSeePoint(Vector2 point,Transform target=null)
        {
            Vector2 origin=transform.position; float distance=Vector2.Distance(origin,point);
            bool roomLit=EscapeUNIFRANZ.World.RoomLightingController.IsAmbientlyVisible(origin,point);
            if (!roomLit && distance>immediateRadius && (!FlashlightActive || !EscapeUNIFRANZ.Hazards.DetectionSensor2D.IsInField(origin,point,DirectionVector(CurrentDirection),forwardRange,forwardAngle))) return false;
            return EscapeUNIFRANZ.Hazards.DetectionSensor2D.HasLineOfSight(origin,point,~0,transform,target,emitterHits);
        }

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

            ApplyLightConfiguration();
        }

        private void LateUpdate()
        {
            if (forwardLight!=null && forwardLight.enabled!=FlashlightActive) ApplyLightSettings();
            if (immediateLight!=null) immediateLight.transform.localPosition=SafeEmitterPosition(emitterOrigin);
            ApplyDirection(CurrentDirection);
        }

        private void OnValidate()
        {
            immediateRadius = Mathf.Max(0.1f, immediateRadius);
            forwardRange = Mathf.Max(0.5f, forwardRange);
            forwardAngle = Mathf.Clamp(forwardAngle, 1f, 179f);
            innerAngle = Mathf.Clamp(innerAngle, 0f, forwardAngle);
            forwardOffset = Mathf.Max(0f, forwardOffset);

            if (Application.isPlaying)
            {
                if (immediateLight != null) immediateLight.transform.localPosition = emitterOrigin;
                ApplyLightSettings();
                ApplyDirection(CurrentDirection);
            }
        }

        public static float ResolveRotationDegrees(
            PlayerVisualController.FacingDirection direction)
        {
            // URP's point-light shader emits a narrow cone along local +Y.
            // Rotate that axis onto the same facing used by the sprite and emitter offset.
            return Vector2.SignedAngle(Vector2.up, DirectionVector(direction));
        }

        /// <summary>Shared setup for Awake and editor builders; never simulates gameplay.</summary>
        public void ApplyLightConfiguration()
        {
            if (visualController == null) visualController = GetComponent<PlayerVisualController>();
            EnsureLights();
            immediateLight.transform.localPosition = emitterOrigin;
            immediateLight.transform.localRotation = Quaternion.identity;
            ApplyLightSettings();
            ApplyDirection(CurrentDirection);
        }

        private void ApplyDirection(PlayerVisualController.FacingDirection direction)
        {
            if (forwardLight == null)
            {
                return;
            }

            float rotation = ResolveRotationDegrees(direction);
            forwardLight.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            forwardLight.transform.localPosition = SafeEmitterPosition(emitterOrigin + DirectionVector(direction) * forwardOffset);
        }
        private Vector3 SafeEmitterPosition(Vector3 localPosition)
        {
            Vector2 origin=transform.TransformPoint(new Vector3(0f,0.13f,0f));
            Vector2 desired=transform.TransformPoint(localPosition), delta=desired-origin;
            var filter=ContactFilter2D.noFilter; filter.useTriggers=false;
            emitterHits.Clear(); Physics2D.Raycast(origin,delta.normalized,filter,emitterHits,delta.magnitude);
            float distance=delta.magnitude;
            foreach(var hit in emitterHits)
                if (hit.collider!=null && !hit.collider.transform.IsChildOf(transform)) distance=Mathf.Min(distance,Mathf.Max(0f,hit.distance-0.025f));
            return transform.InverseTransformPoint(origin+delta.normalized*distance);
        }

        private void ApplyLightSettings()
        {
            ConfigurePointLight(
                immediateLight,
                immediateRadius,
                360f,
                360f,
                immediateIntensity,
                immediateFalloff,
                true);
            ConfigurePointLight(
                forwardLight,
                forwardRange,
                forwardAngle,
                Mathf.Min(forwardAngle,innerAngle+(flashlightEquipped ? 4f : 0f)),
                forwardIntensity+(flashlightEquipped ? 0.12f : 0f),
                forwardFalloff,
                true);
            if (forwardLight!=null) { forwardLight.enabled=FlashlightActive; if (!FlashlightActive) forwardLight.intensity=0f; }
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
            float falloff,
            bool shadows)
        {
            if (light == null)
            {
                return;
            }

            light.lightType = Light2D.LightType.Point;
            light.color = lightColor;
            light.intensity = lightIntensity;
            light.falloffIntensity = falloff;
            light.pointLightInnerRadius = outerAngle < 360f ? 0.4f : radius * 0.45f;
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
