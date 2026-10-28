using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjetoMoto.EditorTools
{
    /// <summary>Criação da cena e build Android (usado pelo GitHub Actions e pelo menu do Editor).</summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Projeto Moto/Gerar cena de teste")]
        public static void CreateTestScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Projeto Moto/Compilar APK Android")]
        public static void BuildAndroid()
        {
            try
            {
                CreateTestScene();
                ConfigurePlayer();
                EnsureShadersIncluded();

                string path = GetArg("-customBuildPath");
                if (string.IsNullOrEmpty(path)) path = "build/Android/ProjetoMoto3D";
                if (!path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)) path += ".apk";
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                EditorUserBuildSettings.buildAppBundle = false;

                BuildPlayerOptions options = new BuildPlayerOptions();
                options.scenes = new[] { ScenePath };
                options.locationPathName = path;
                options.target = BuildTarget.Android;
                options.options = BuildOptions.None;

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                Debug.Log("[BuildScript] Resultado: " + summary.result + " | tamanho: " + summary.totalSize + " bytes | erros: " + summary.totalErrors);

                if (summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError("[BuildScript] Build Android falhou.");
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[BuildScript] Exceção: " + e);
                EditorApplication.Exit(1);
            }
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "ProjetoMoto";
            PlayerSettings.productName = "Projeto Moto 3D";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.projetomoto.moto3d");
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        }

        // Garante que os shaders usados em tempo de execução (Shader.Find) entram no APK.
        static void EnsureShadersIncluded()
        {
            try
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
                if (assets == null || assets.Length == 0) return;
                SerializedObject so = new SerializedObject(assets[0]);
                SerializedProperty list = so.FindProperty("m_AlwaysIncludedShaders");
                string[] names = { "Legacy Shaders/Diffuse", "Sprites/Default", "UI/Default" };
                foreach (string n in names)
                {
                    Shader sh = Shader.Find(n);
                    if (sh == null) continue;
                    bool found = false;
                    for (int i = 0; i < list.arraySize; i++)
                    {
                        if (list.GetArrayElementAtIndex(i).objectReferenceValue == sh) { found = true; break; }
                    }
                    if (!found)
                    {
                        list.arraySize++;
                        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = sh;
                    }
                }
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BuildScript] Não foi possível ajustar os shaders incluídos: " + e.Message);
            }
        }

        static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
