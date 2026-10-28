using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>
    /// Estado de entrada da moto. Os botões touch escrevem nos campos Touch*;
    /// no Editor/PC o teclado também funciona (W/S/Espaço/A/D/Shift/Ctrl/R).
    /// </summary>
    public class MotorcycleInput : MonoBehaviour
    {
        [HideInInspector] public float TouchThrottle;
        [HideInInspector] public float TouchBrakeFront;
        [HideInInspector] public float TouchBrakeRear;
        [HideInInspector] public float TouchSteer;
        [HideInInspector] public bool TouchWheelie;
        [HideInInspector] public bool TouchTrick;

        public bool enableKeyboard = true;

        public float Throttle { get; private set; }
        public float BrakeFront { get; private set; }
        public float BrakeRear { get; private set; }
        public float Steer { get; private set; }
        public bool Wheelie { get; private set; }
        public bool Trick { get; private set; }

        void Update()
        {
            float t = TouchThrottle;
            float bf = TouchBrakeFront;
            float br = TouchBrakeRear;
            float s = TouchSteer;
            bool w = TouchWheelie;
            bool tr = TouchTrick;

            if (enableKeyboard)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) t = 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) bf = 1f;
                if (Input.GetKey(KeyCode.Space)) br = 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) s -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) s += 1f;
                if (Input.GetKey(KeyCode.LeftShift)) w = true;
                if (Input.GetKey(KeyCode.LeftControl)) tr = true;
            }

            s = Mathf.Clamp(s, -1f, 1f);
            float dt = Time.deltaTime;
            Throttle = Mathf.MoveTowards(Throttle, t, dt * 4f);
            BrakeFront = Mathf.MoveTowards(BrakeFront, bf, dt * 6f);
            BrakeRear = Mathf.MoveTowards(BrakeRear, br, dt * 6f);
            float steerRate = Mathf.Abs(s) > 0.01f ? 3.5f : 6f;
            Steer = Mathf.MoveTowards(Steer, s, dt * steerRate);
            Wheelie = w;
            Trick = tr;
        }
    }
}
