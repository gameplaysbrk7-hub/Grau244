using System;
using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>
    /// Animação procedural provisória do piloto: tronco inclina nas curvas, para trás no grau
    /// e para frente na frenagem; braços e pernas são esticados entre ombros/guidão e quadril/pedais.
    /// </summary>
    public class RiderAnimationController : MonoBehaviour
    {
        [Serializable]
        public class Limb
        {
            public Transform from;
            public Transform to;
            public Transform mesh;
            public float thickness = 0.1f;
        }

        public Transform torsoPivot;
        public Limb[] limbs;
        public MotorcycleController bike;
        public MotorcycleInput controls;
        public WheelieController wheelie;

        void LateUpdate()
        {
            if (bike == null || torsoPivot == null) return;

            float dt = Time.deltaTime;
            float roll = -bike.LeanAngle * 0.4f;
            float pitch = 22f + controls.BrakeFront * 10f - controls.Throttle * 3f;
            if (wheelie != null && wheelie.IsWheelie) pitch -= 14f;
            if (bike.IsCrashed) { pitch = 50f; roll = 0f; }

            Quaternion target = Quaternion.Euler(pitch, 0f, roll);
            torsoPivot.localRotation = Quaternion.Slerp(torsoPivot.localRotation, target, 1f - Mathf.Exp(-8f * dt));

            if (limbs == null) return;
            for (int i = 0; i < limbs.Length; i++)
            {
                Limb l = limbs[i];
                if (l == null || l.from == null || l.to == null || l.mesh == null) continue;
                Vector3 a = l.from.position;
                Vector3 b = l.to.position;
                Vector3 d = b - a;
                float len = d.magnitude;
                if (len < 0.001f) continue;
                l.mesh.position = (a + b) * 0.5f;
                l.mesh.rotation = Quaternion.FromToRotation(Vector3.up, d / len);
                l.mesh.localScale = new Vector3(l.thickness, len * 0.5f, l.thickness);
            }
        }
    }
}
