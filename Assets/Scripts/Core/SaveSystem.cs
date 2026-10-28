using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>Salvamento local simples (PlayerPrefs). Fase 1: apenas configurações.</summary>
    public static class SaveSystem
    {
        const string Prefix = "pm_";

        public static float GetFloat(string key, float def) { return PlayerPrefs.GetFloat(Prefix + key, def); }
        public static void SetFloat(string key, float value) { PlayerPrefs.SetFloat(Prefix + key, value); }
        public static int GetInt(string key, int def) { return PlayerPrefs.GetInt(Prefix + key, def); }
        public static void SetInt(string key, int value) { PlayerPrefs.SetInt(Prefix + key, value); }
        public static void Save() { PlayerPrefs.Save(); }
    }
}
