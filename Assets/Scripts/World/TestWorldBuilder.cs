using UnityEngine;
using UnityEngine.Rendering;

namespace ProjetoMoto
{
    /// <summary>
    /// Mundo de teste provisório (Fase 1): estacionamento amplo cercado por prédios, com
    /// rampas, lombadas, cones e caixotes para testar física, grau e câmera.
    /// A cidade conectada vem nas fases seguintes.
    /// </summary>
    public static class TestWorldBuilder
    {
        public static void Build()
        {
            Color sky = new Color(0.55f, 0.75f, 0.95f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = sky;
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 280f;

            GameObject sunGo = new GameObject("Sol");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.0f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.shadows = LightShadows.None;
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Transform root = new GameObject("Mundo").transform;

            Visuals.Prim(PrimitiveType.Cube, "Chao", root, new Vector3(0f, -0.5f, 0f), new Vector3(700f, 1f, 700f),
                new Color(0.30f, 0.55f, 0.28f), true);
            Visuals.Prim(PrimitiveType.Cube, "Asfalto", root, new Vector3(0f, 0.01f, 0f), new Vector3(236f, 0.02f, 236f),
                new Color(0.22f, 0.22f, 0.24f));

            Color white = new Color(0.92f, 0.92f, 0.92f);
            for (float z = -110f; z <= 110f; z += 8f)
                Visuals.Prim(PrimitiveType.Cube, "Faixa", root, new Vector3(0f, 0.03f, z), new Vector3(0.3f, 0.01f, 3f), white);
            for (float x = -110f; x <= 110f; x += 8f)
                if (Mathf.Abs(x) > 6f)
                    Visuals.Prim(PrimitiveType.Cube, "Faixa", root, new Vector3(x, 0.03f, 0f), new Vector3(3f, 0.01f, 0.3f), white);

            // Muros-prédio que cercam a área
            Color wall = new Color(0.78f, 0.74f, 0.66f);
            Visuals.Prim(PrimitiveType.Cube, "Predio N", root, new Vector3(0f, 7f, 123f), new Vector3(250f, 14f, 10f), wall, true);
            Visuals.Prim(PrimitiveType.Cube, "Predio S", root, new Vector3(0f, 7f, -123f), new Vector3(250f, 14f, 10f), wall, true);
            Visuals.Prim(PrimitiveType.Cube, "Predio L", root, new Vector3(123f, 7f, 0f), new Vector3(10f, 14f, 250f), wall, true);
            Visuals.Prim(PrimitiveType.Cube, "Predio O", root, new Vector3(-123f, 7f, 0f), new Vector3(10f, 14f, 250f), wall, true);

            // Prédios e casas espalhados nos quadrantes (longe das ruas e do ponto de partida)
            Color[] palette =
            {
                new Color(0.85f, 0.55f, 0.35f), new Color(0.9f, 0.85f, 0.55f), new Color(0.6f, 0.75f, 0.85f),
                new Color(0.75f, 0.6f, 0.8f), new Color(0.65f, 0.8f, 0.6f), new Color(0.88f, 0.88f, 0.85f)
            };
            System.Random rng = new System.Random(7);
            int placed = 0;
            int guard = 0;
            while (placed < 16 && guard < 400)
            {
                guard++;
                float x = (float)(rng.NextDouble() * 190.0 - 95.0);
                float z = (float)(rng.NextDouble() * 190.0 - 95.0);
                if (Mathf.Abs(x) < 16f || Mathf.Abs(z) < 16f) continue;
                float w = 8f + (float)rng.NextDouble() * 10f;
                float d = 8f + (float)rng.NextDouble() * 10f;
                float h = 5f + (float)rng.NextDouble() * 20f;
                Visuals.Prim(PrimitiveType.Cube, "Predio", root, new Vector3(x, h * 0.5f, z), new Vector3(w, h, d),
                    palette[rng.Next(palette.Length)], true);
                placed++;
            }

            // Rampas
            Ramp(root, new Vector3(0f, 0f, -20f), 0f, 10f, 5f, 12f);
            Ramp(root, new Vector3(50f, 0f, 40f), 180f, 10f, 5f, 12f);
            Ramp(root, new Vector3(-50f, 0f, 60f), 90f, 8f, 4f, 15f);

            // Lombadas
            Color yellow = new Color(0.9f, 0.8f, 0.1f);
            for (int i = 0; i < 2; i++)
            {
                float z = i == 0 ? -45f : 25f;
                GameObject bump = Visuals.Prim(PrimitiveType.Cube, "Lombada", root, new Vector3(0f, 0f, z),
                    new Vector3(8f, 0.3f, 0.3f), yellow, true);
                bump.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            }

            // Cones para slalom
            Color orange = new Color(1f, 0.45f, 0.05f);
            for (int i = 0; i < 10; i++)
            {
                GameObject cone = Visuals.Prim(PrimitiveType.Cylinder, "Cone", root,
                    new Vector3(i % 2 == 0 ? 10f : 16f, 0.4f, -55f + i * 5f), new Vector3(0.4f, 0.4f, 0.4f), orange, true);
                Rigidbody crb = cone.AddComponent<Rigidbody>();
                crb.mass = 3f;
            }

            // Caixotes para bater
            for (int i = 0; i < 6; i++)
            {
                GameObject crate = Visuals.Prim(PrimitiveType.Cube, "Caixote", root,
                    new Vector3(-12f - i * 1.1f, 0.5f + (i % 2) * 1f, -40f + i * 2f), Vector3.one,
                    new Color(0.6f, 0.42f, 0.2f), true);
                Rigidbody rb = crate.AddComponent<Rigidbody>();
                rb.mass = 20f;
            }
        }

        static void Ramp(Transform parent, Vector3 pos, float yaw, float length, float width, float angle)
        {
            const float thickness = 0.4f;
            float rad = angle * Mathf.Deg2Rad;
            float cy = (length * 0.5f) * Mathf.Sin(rad) - (thickness * 0.5f) * Mathf.Cos(rad);
            GameObject go = Visuals.Prim(PrimitiveType.Cube, "Rampa", parent, Vector3.zero,
                new Vector3(width, thickness, length), new Color(0.55f, 0.55f, 0.6f), true);
            go.transform.SetPositionAndRotation(new Vector3(pos.x, cy, pos.z),
                Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-angle, 0f, 0f));
        }
    }
}
