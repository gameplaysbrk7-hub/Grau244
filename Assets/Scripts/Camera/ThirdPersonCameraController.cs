using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>
    /// Câmera em terceira pessoa: segue atrás e acima da moto com suavização, aceita rotação manual
    /// (que volta sozinha ao soltar), zoom, recentralização, enquadramento especial no grau e
    /// prevenção de colisão com o cenário (SphereCast).
    /// </summary>
    public class ThirdPersonCameraController : MonoBehaviour
    {
        public Transform target;
        public MotorcycleController bike;

        [Header("Enquadramento")]
        public float distance = 4.3f;
        public float height = 1.6f;
        public float lookHeight = 0.9f;
        public float zoom = 1f;
        public float minZoom = 0.6f;
        public float maxZoom = 1.8f;

        [Header("Suavização")]
        public float yawSharpness = 3.5f;
        public float horizontalSharpness = 14f;
        public float verticalSharpness = 6f;

        [Header("Controle manual")]
        public float releaseDelay = 1.8f;
        public float returnSharpness = 2.5f;
        public float minManualPitch = -12f;
        public float maxManualPitch = 40f;

        [Header("Colisão")]
        public float collisionRadius = 0.25f;

        Camera cam;
        float yaw, manualYaw, manualPitch, lastManualTime, fraction = 1f;
        bool recentering, needSnap = true;
        Vector3 pivot;
        Vector3 lastFlat = Vector3.forward;
        int mask;

        void Awake()
        {
            cam = GetComponent<Camera>();
            mask = ~(1 << MotorcycleController.BikeLayer);
        }

        void OnEnable() { needSnap = true; }

        public void AddLook(float yawDelta, float pitchDelta)
        {
            manualYaw += yawDelta;
            manualPitch = Mathf.Clamp(manualPitch + pitchDelta, minManualPitch, maxManualPitch);
            lastManualTime = Time.time;
            recentering = false;
        }

        public void AddZoom(float amount) { zoom = Mathf.Clamp(zoom + amount, minZoom, maxZoom); }

        public void Recenter() { recentering = true; }

        void LateUpdate()
        {
            if (target == null || bike == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 flat = Vector3.ProjectOnPlane(target.forward, Vector3.up);
            if (flat.sqrMagnitude < 0.01f) flat = lastFlat; else flat.Normalize();
            lastFlat = flat;
            float targetYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;

            Vector3 pivotTarget = target.position + Vector3.up * 0.2f;
            if (needSnap)
            {
                pivot = pivotTarget;
                yaw = targetYaw;
                manualYaw = 0f;
                manualPitch = 0f;
                fraction = 1f;
                needSnap = false;
            }

            yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-yawSharpness * dt));

            if (recentering)
            {
                float kr = 1f - Mathf.Exp(-8f * dt);
                manualYaw = Mathf.Lerp(manualYaw, 0f, kr);
                manualPitch = Mathf.Lerp(manualPitch, 0f, kr);
                yaw = Mathf.LerpAngle(yaw, targetYaw, kr);
                if (Mathf.Abs(manualYaw) < 0.5f && Mathf.Abs(manualPitch) < 0.5f) recentering = false;
            }
            else if (Time.time - lastManualTime > releaseDelay)
            {
                float k = 1f - Mathf.Exp(-returnSharpness * dt);
                manualYaw = Mathf.Lerp(manualYaw, 0f, k);
                manualPitch = Mathf.Lerp(manualPitch, 0f, k);
            }

            float wheelie = Mathf.Clamp01(bike.NoseUpAngle / 45f);
            float speed01 = Mathf.Clamp01(bike.Speed / 40f);
            float dist = distance * zoom + wheelie * 1.3f + speed01 * 0.6f;
            float h = height + wheelie * 0.6f;

            float kh = 1f - Mathf.Exp(-horizontalSharpness * dt);
            float kv = 1f - Mathf.Exp(-verticalSharpness * dt);
            pivot.x = Mathf.Lerp(pivot.x, pivotTarget.x, kh);
            pivot.z = Mathf.Lerp(pivot.z, pivotTarget.z, kh);
            pivot.y = Mathf.Lerp(pivot.y, pivotTarget.y, kv);

            Quaternion orbit = Quaternion.Euler(manualPitch, yaw + manualYaw, 0f);
            Vector3 desired = pivot + orbit * new Vector3(0f, h, -dist);
            Vector3 from = pivot + Vector3.up * 0.6f;
            Vector3 toDesired = desired - from;
            float len = toDesired.magnitude;
            if (len < 0.01f) return;
            Vector3 dir = toDesired / len;

            float allowed = len;
            RaycastHit hit;
            if (Physics.SphereCast(from, collisionRadius, dir, out hit, len, mask, QueryTriggerInteraction.Ignore))
                allowed = Mathf.Max(hit.distance - 0.05f, Mathf.Min(1f, len));

            float targetFraction = Mathf.Clamp01(allowed / len);
            if (targetFraction < fraction) fraction = targetFraction; // obstruída: aproxima na hora
            else fraction = Mathf.MoveTowards(fraction, targetFraction, 1.5f * dt); // livre: volta suave

            transform.position = from + dir * (len * fraction);

            Vector3 lookAt = pivot + Vector3.up * (lookHeight - wheelie * 0.3f);
            Vector3 look = lookAt - transform.position;
            if (look.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(look, Vector3.up);

            float targetFov = 58f + speed01 * 14f;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-3f * dt));
        }
    }
}
