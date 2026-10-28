using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>
    /// Assistência de grau. Não gira o modelo: aplica torque no Rigidbody, então a roda
    /// dianteira só sai do chão se a física permitir (tração + torque), e a moto cai se
    /// passar do ponto de equilíbrio. Acelerador sobe o ângulo; freio traseiro desce.
    /// </summary>
    [RequireComponent(typeof(MotorcycleController))]
    public class WheelieController : MonoBehaviour
    {
        [Header("Condições")]
        public float minSpeed = 2.5f;

        [Header("Ângulo desejado (graus)")]
        public float minTargetAngle = 22f;
        public float maxTargetAngle = 50f;
        public float crashAngle = 64f;

        [Header("Controle de equilíbrio")]
        public float kp = 22f;          // N·m por grau de erro
        public float kd = 7f;           // N·m por grau/s
        public float maxTorque = 1100f;
        public float popTorque = 450f;  // empurrão inicial para tirar a roda do chão
        public float feedforward = 0.85f;
        public float comFromRear = 0.6f;
        public float comHeight = 0.6f;
        public float releaseAssistStart = 35f;
        public float releaseAssistTorque = 400f;

        MotorcycleController bike;
        MotorcycleInput controls;
        Rigidbody rb;
        float grace;

        public float BalanceTarget { get; private set; }
        public float WheelieTime { get; private set; }
        public bool IsWheelie
        {
            get { return bike != null && !bike.IsCrashed && bike.NoseUpAngle > 8f && !bike.FrontGrounded; }
        }

        void Awake()
        {
            bike = GetComponent<MotorcycleController>();
            controls = GetComponent<MotorcycleInput>();
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            if (IsWheelie) { WheelieTime += dt; grace = 0.3f; }
            else { grace -= dt; if (grace <= 0f) WheelieTime = 0f; }

            if (bike.IsCrashed) return;

            float pitch = bike.NoseUpAngle;
            float pitchRate = -Vector3.Dot(rb.angularVelocity, transform.right) * Mathf.Rad2Deg; // + = subindo

            if (pitch > crashAngle) { bike.Crash(); return; }

            bool active = controls.Wheelie && bike.RearGrounded && bike.ForwardSpeed > minSpeed;

            if (active)
            {
                float knob = Mathf.Clamp01(0.4f + 0.6f * controls.Throttle - 0.7f * controls.BrakeRear);
                BalanceTarget = Mathf.Lerp(minTargetAngle, maxTargetAngle, knob);

                float error = BalanceTarget - pitch;
                float rad = pitch * Mathf.Deg2Rad;
                float ffScale = Mathf.Clamp01(pitch / 12f);
                float gravityTorque = rb.mass * 9.81f * (comFromRear * Mathf.Cos(rad) - comHeight * Mathf.Sin(rad));

                float torque = kp * error - kd * pitchRate + feedforward * ffScale * gravityTorque;
                torque = Mathf.Clamp(torque, -maxTorque, maxTorque);

                if (pitch < 10f && controls.Throttle > 0.3f) torque += popTorque * controls.Throttle;

                rb.AddTorque(-transform.right * torque);
            }
            else if (pitch > 2f && !bike.FrontGrounded)
            {
                // Pouso suave quando o botão é solto
                float torque = -0.5f * kd * pitchRate;
                if (pitch > releaseAssistStart)
                    torque -= releaseAssistTorque * Mathf.Clamp01((pitch - releaseAssistStart) / 20f);
                rb.AddTorque(-transform.right * torque);
            }
        }
    }
}
