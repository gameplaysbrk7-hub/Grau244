using System;
using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>
    /// Física da moto: suspensão por raycast em cada roda, aderência lateral,
    /// tração traseira, freios independentes, estabilização de inclinação e quedas.
    /// O grau e o stoppie surgem da física (forças aplicadas no ponto de contato
    /// dos pneus); o WheelieController adiciona apenas assistência de torque.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MotorcycleController : MonoBehaviour
    {
        public const int BikeLayer = 2; // "Ignore Raycast": a moto não enxerga a si mesma

        [Header("Massa e inércia")]
        public float mass = 180f;
        public Vector3 centerOfMass = new Vector3(0f, 0.12f, -0.12f);
        public Vector3 inertia = new Vector3(55f, 30f, 20f); // x = arfagem, y = guinada, z = rolagem

        [Header("Rodas e suspensão")]
        public Transform frontMount; // pivô do garfo (gira com a direção)
        public Transform rearMount;
        public Transform frontWheel;
        public Transform rearWheel;
        public float wheelRadius = 0.32f;
        public float suspensionTravel = 0.30f;
        public float springRate = 12000f;
        public float damperRate = 1300f;

        [Header("Motor e freios")]
        public float maxEngineForce = 2800f;
        public float maxSpeed = 38f; // m/s (~137 km/h)
        public float frontBrakeForce = 4500f;
        public float rearBrakeForce = 3000f;
        public float engineBrakeForce = 150f;
        public float dragCoefficient = 0.35f;

        [Header("Aderência")]
        public float frontGrip = 1.35f;
        public float rearGrip = 1.4f;
        public float lateralResponse = 0.5f;
        public float driftGripFactor = 0.3f;

        [Header("Direção e equilíbrio")]
        public float maxSteerAngle = 26f;
        public float minSteerAngle = 5f;
        public float steerFadeSpeed = 30f;
        public float maxLean = 38f;
        public float rollKp = 400f;
        public float rollKd = 20f;
        public float maxRollTorque = 1500f;

        [Header("Queda")]
        public float autoRecoverDelay = 3f;

        class WheelState
        {
            public bool grounded, wasGrounded;
            public Vector3 point, normal;
            public float distance, load, lastCompression, rollSpeed;
        }

        Rigidbody rb;
        MotorcycleInput controls;
        int groundMask;
        readonly WheelState front = new WheelState();
        readonly WheelState rear = new WheelState();
        float steerAngle, spinFront, spinRear, extFront, extRear, crashTime;
        Vector3 lastFlatForward = Vector3.forward;

        public bool IsCrashed { get; private set; }
        public bool Burnout { get; private set; }
        public bool FrontGrounded { get { return front.grounded; } }
        public bool RearGrounded { get { return rear.grounded; } }
        public float Speed { get; private set; }
        public float ForwardSpeed { get; private set; }
        public float NoseUpAngle { get; private set; }  // graus, positivo = roda dianteira para cima
        public float LeanAngle { get; private set; }    // graus, positivo = inclinada para a direita
        public float SteerAngle { get { return steerAngle; } }

        public event Action Crashed;
        public event Action Recovered;
        public event Action<float> Impact;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            controls = GetComponent<MotorcycleInput>();
            rb.mass = mass;
            rb.centerOfMass = centerOfMass;
            rb.inertiaTensor = inertia;
            rb.inertiaTensorRotation = Quaternion.identity;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.drag = 0f;
            rb.angularDrag = 0.4f;
            groundMask = ~(1 << BikeLayer);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            UpdateState();

            float steerIn = IsCrashed ? 0f : controls.Steer;
            float speedFactor = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / steerFadeSpeed);
            float maxSteer = Mathf.Lerp(maxSteerAngle, minSteerAngle, speedFactor);
            steerAngle = Mathf.MoveTowards(steerAngle, steerIn * maxSteer, 220f * dt);

            ProcessWheel(front, frontMount, true, dt);
            ProcessWheel(rear, rearMount, false, dt);
            ApplyDrag();

            if (IsCrashed)
            {
                crashTime += dt;
                if (crashTime > autoRecoverDelay) Recover();
            }
            else
            {
                ApplyRollControl();
                CheckCrash();
            }
        }

        void UpdateState()
        {
            Vector3 v = rb.velocity;
            Speed = v.magnitude;
            ForwardSpeed = Vector3.Dot(v, transform.forward);

            Vector3 gn = Vector3.up;
            if (rear.grounded) gn = rear.normal;
            else if (front.grounded) gn = front.normal;

            NoseUpAngle = Mathf.Asin(Mathf.Clamp(Vector3.Dot(transform.forward, gn), -1f, 1f)) * Mathf.Rad2Deg;
            LeanAngle = Mathf.Asin(Mathf.Clamp(-transform.right.y, -1f, 1f)) * Mathf.Rad2Deg;

            Vector3 flat = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flat.sqrMagnitude > 0.2f) lastFlatForward = flat.normalized;
        }

        void ProcessWheel(WheelState w, Transform mount, bool isFront, float dt)
        {
            Vector3 dir = Vector3.Slerp(-transform.up, Vector3.down, 0.7f).normalized;
            float rayLen = suspensionTravel + wheelRadius;
            RaycastHit hit;

            if (!Physics.Raycast(mount.position, dir, out hit, rayLen, groundMask, QueryTriggerInteraction.Ignore))
            {
                w.grounded = false;
                w.wasGrounded = false;
                w.load = 0f;
                w.distance = rayLen;
                if (!isFront) Burnout = false;
                return;
            }

            w.grounded = true;
            w.point = hit.point;
            w.normal = hit.normal;
            w.distance = hit.distance;

            // Suspensão (mola + amortecedor)
            float rawCompression = suspensionTravel - (hit.distance - wheelRadius);
            float comp = Mathf.Clamp(rawCompression, 0f, suspensionTravel);
            if (!w.wasGrounded)
            {
                w.lastCompression = comp;
                w.wasGrounded = true;
            }
            float compVel = (comp - w.lastCompression) / dt;
            w.lastCompression = comp;

            float force = springRate * comp + damperRate * compVel;
            if (rawCompression > suspensionTravel) force += springRate * 4f * (rawCompression - suspensionTravel);
            if (force < 0f) force = 0f;
            w.load = force;
            rb.AddForceAtPosition(w.normal * force, w.point);

            // Direção da roda projetada no solo
            Vector3 wheelForward = transform.forward;
            if (isFront) wheelForward = Quaternion.AngleAxis(steerAngle, transform.up) * transform.forward;
            Vector3 fwd = Vector3.ProjectOnPlane(wheelForward, w.normal);
            if (fwd.sqrMagnitude < 0.0001f) return;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(w.normal, fwd);

            Vector3 vel = rb.GetPointVelocity(w.point);
            float vLong = Vector3.Dot(vel, fwd);
            float vLat = Vector3.Dot(vel, right);
            w.rollSpeed = vLong;

            float throttle = IsCrashed ? 0f : controls.Throttle;
            float brakeFront = IsCrashed ? 0f : controls.BrakeFront;
            float brakeRear = IsCrashed ? 0f : controls.BrakeRear;
            bool trick = !IsCrashed && controls.Trick;

            float maxFriction = w.load * (isFront ? frontGrip : rearGrip);
            float shareMass = rb.mass * 0.5f;
            float longForce;
            bool slidingRear = false;

            if (isFront)
            {
                float cmd = brakeFront * frontBrakeForce;
                longForce = -Mathf.Sign(vLong) * Mathf.Min(cmd, Mathf.Abs(vLong) * shareMass / dt);
            }
            else
            {
                float engine = throttle * maxEngineForce * Mathf.Clamp01(1f - Mathf.Max(0f, vLong) / maxSpeed);
                float brakeCmd = brakeRear * rearBrakeForce + (throttle < 0.05f ? engineBrakeForce : 0f);
                float brake = -Mathf.Sign(vLong) * Mathf.Min(brakeCmd, Mathf.Abs(vLong) * shareMass / dt);
                longForce = engine + brake;

                Burnout = throttle > 0.6f && brakeRear > 0.6f && Mathf.Abs(vLong) < 9f;
                slidingRear = Burnout || trick;
            }

            longForce = Mathf.Clamp(longForce, -maxFriction, maxFriction);

            // Círculo de atrito: o que sobra da tração longitudinal vai para a lateral
            float latMax = Mathf.Sqrt(Mathf.Max(0f, maxFriction * maxFriction - longForce * longForce));
            latMax = Mathf.Max(latMax, maxFriction * 0.35f);
            if (slidingRear) latMax *= driftGripFactor;

            float latForce = Mathf.Clamp(-vLat * shareMass * lateralResponse / dt, -latMax, latMax);
            rb.AddForceAtPosition(fwd * longForce + right * latForce, w.point);
        }

        void ApplyDrag()
        {
            Vector3 v = rb.velocity;
            float s = v.magnitude;
            if (s > 0.1f) rb.AddForce(-v.normalized * dragCoefficient * s * s);
        }

        void ApplyRollControl()
        {
            float speed01 = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / 10f);
            float highSpeedReduction = 1f - Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / maxSpeed) * 0.4f;
            float target = controls.Steer * maxLean * speed01 * highSpeedReduction;
            float scale = (front.grounded || rear.grounded) ? 1f : 0.25f;
            if (scale < 1f) target = 0f;

            float leanRate = -Vector3.Dot(rb.angularVelocity, transform.forward) * Mathf.Rad2Deg;
            float torque = rollKp * (target - LeanAngle) - rollKd * leanRate;
            torque = Mathf.Clamp(torque, -maxRollTorque, maxRollTorque) * scale;
            rb.AddTorque(-transform.forward * torque);
        }

        void CheckCrash()
        {
            if (transform.up.y < 0.28f || NoseUpAngle < -60f || NoseUpAngle > 72f) Crash();
        }

        public void Crash()
        {
            if (IsCrashed) return;
            IsCrashed = true;
            crashTime = 0f;
            if (Crashed != null) Crashed();
        }

        public void Recover()
        {
            Vector3 flat = lastFlatForward;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = rb.position + Vector3.up * 0.6f;
            rb.rotation = Quaternion.LookRotation(flat, Vector3.up);
            steerAngle = 0f;
            IsCrashed = false;
            crashTime = 0f;
            if (Recovered != null) Recovered();
        }

        void OnCollisionEnter(Collision collision)
        {
            float impulse = collision.impulse.magnitude;
            if (impulse > 400f && Impact != null) Impact(impulse);

            if (!IsCrashed && collision.contactCount > 0 && collision.relativeVelocity.magnitude > 12f)
            {
                Vector3 n = collision.GetContact(0).normal;
                if (Mathf.Abs(n.y) < 0.5f) Crash(); // batida de frente em parede/obstáculo
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float k = 1f - Mathf.Exp(-25f * dt);
            float targetFront = front.grounded ? Mathf.Clamp(front.distance - wheelRadius, 0f, suspensionTravel) : suspensionTravel;
            float targetRear = rear.grounded ? Mathf.Clamp(rear.distance - wheelRadius, 0f, suspensionTravel) : suspensionTravel;
            extFront = Mathf.Lerp(extFront, targetFront, k);
            extRear = Mathf.Lerp(extRear, targetRear, k);

            if (frontWheel != null) frontWheel.localPosition = new Vector3(0f, -extFront, 0f);
            if (rearWheel != null && rearMount != null)
                rearWheel.localPosition = rearMount.localPosition + new Vector3(0f, -extRear, 0f);

            spinFront = (spinFront + front.rollSpeed / wheelRadius * Mathf.Rad2Deg * dt) % 360f;
            float rearSpin = Burnout ? 45f : rear.rollSpeed / wheelRadius;
            spinRear = (spinRear + rearSpin * Mathf.Rad2Deg * dt) % 360f;

            if (frontMount != null) frontMount.localRotation = Quaternion.Euler(0f, steerAngle, 0f);
            if (frontWheel != null) frontWheel.localRotation = Quaternion.Euler(spinFront, 0f, 0f);
            if (rearWheel != null) rearWheel.localRotation = Quaternion.Euler(spinRear, 0f, 0f);
        }
    }
}
