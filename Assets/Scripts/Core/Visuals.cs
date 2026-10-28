using System.Collections.Generic;
using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>Criação de materiais e primitivas para os modelos provisórios.</summary>
    public static class Visuals
    {
        static Shader shader;
        static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();

        public static Material Mat(Color color)
        {
            Color32 c = color;
            int key = (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;

            if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            m = new Material(shader);
            m.color = color;
            cache[key] = m;
            return m;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent,
            Vector3 localPos, Vector3 localScale, Color color, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider col = go.GetComponent<Collider>();
            if (!keepCollider && col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            return go;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
        }
    }
}
