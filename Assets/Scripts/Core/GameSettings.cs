using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>Preferências do jogador, salvas localmente.</summary>
    public static class GameSettings
    {
        static readonly float[] Opacities = { 0.35f, 0.5f, 0.7f, 0.85f, 1f };
        static readonly float[] Scales = { 0.8f, 0.9f, 1f, 1.15f, 1.3f };
        static readonly float[] Sensitivities = { 0.5f, 0.75f, 1f, 1.5f, 2f };

        public static float ControlOpacity { get { return Opacities[Index("opacity", 2, Opacities.Length)]; } }
        public static float ControlScale { get { return Scales[Index("scale", 2, Scales.Length)]; } }
        public static float CameraSensitivity { get { return Sensitivities[Index("sens", 2, Sensitivities.Length)]; } }
        public static bool Vibration { get { return SaveSystem.GetInt("vibration", 1) == 1; } }

        public static void CycleOpacity() { Cycle("opacity", 2, Opacities.Length); }
        public static void CycleScale() { Cycle("scale", 2, Scales.Length); }
        public static void CycleSensitivity() { Cycle("sens", 2, Sensitivities.Length); }

        public static void ToggleVibration()
        {
            SaveSystem.SetInt("vibration", Vibration ? 0 : 1);
            SaveSystem.Save();
        }

        public static void Vibrate()
        {
            if (Vibration && Application.isMobilePlatform) Handheld.Vibrate();
        }

        static int Index(string key, int def, int count)
        {
            return Mathf.Clamp(SaveSystem.GetInt(key, def), 0, count - 1);
        }

        static void Cycle(string key, int def, int count)
        {
            int next = (SaveSystem.GetInt(key, def) + 1) % count;
            SaveSystem.SetInt(key, next);
            SaveSystem.Save();
        }
    }
}
