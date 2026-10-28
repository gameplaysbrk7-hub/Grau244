using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>
    /// Câmera na posição da cabeça do piloto. Suaviza o ângulo para não apontar demais para o
    /// céu no grau nem girar bruscamente nas curvas; aceita olhar ao redor (volta sozinha).
    /// </summary>
    public class FirstPersonCameraController : MonoBehaviour
    {
        public Transform target;
        public Transform point;
        public MotorcycleController bike;

        public float releaseDelay = 1.5f;
        public float returnSharpness = 3f;
        public float maxYaw = 80f;
        public float maxPitch = 35f;

        Camera cam;
        float manualYaw, manualPitch, lastManualTime;
        bool needSnap = true;
        Vector3 lastFlat = Vector3.forward;

        void Awake() { cam = GetComponent<Camera>(); }

        void OnEnable() { needSnap = true; }

        public void AddLook(float yawDelta, float pitchDelta)
        {
            manualYaw = Mathf.Clamp(manualYaw + yawDelta, -maxYaw, maxYaw);
            manualPitch = Mathf.Clamp(manualPitch + pitchDelta, -maxPitch, maxPitch);
            lastManualTime = Time.time;
        }

        public void Recenter() { manualYaw = 0f; manualPitch = 0f; }

        void LateUpdate()
        {
            if (target == null || point == null || bike == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (Time.time - lastManualTime > releaseDelay)
            {
                float k = 1f - Mathf.Exp(-returnSharpness * dt);
                manualYaw = Mathf.Lerp(manualYaw, 0f, k);
                manualPitch = Mathf.Lerp(manualPitch, 0f, k);
            }

            Vector3 flat = Vector3.ProjectOnPlane(target.forward, Vector3.up);
            if (flat.sqrMagnitude < 0.01f) flat = lastFlat; else flat.Normalize();
            lastFlat = flat;

            Vector3 fwd = Vector3.Slerp(flat, target.forward, 0.55f);
            Vector3 up = Vector3.Slerp(Vector3.up, target.up, 0.5f);
            Quaternion baseRot = Quaternion.LookRotation(fwd, up);
            Quaternion rot = baseRot * Quaternion.Euler(-manualPitch, manualYaw, 0f);

            transform.position = point.position;
            transform.rotation = needSnap ? rot : Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-18f * dt));
            needSnap = false;

            float speed01 = Mathf.Clamp01(bike.Speed / 40f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 70f + speed01 * 12f, 1f - Mathf.Exp(-3f * dt));
        }
    }
}
