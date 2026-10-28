using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjetoMoto
{
    /// <summary>
    /// Monta a cena de teste em tempo de execução: mundo, moto, câmera e interface.
    /// A cena Assets/Scenes/Main.unity contém apenas este componente.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.01f;
            Physics.defaultSolverIterations = 8;
            Physics.defaultSolverVelocityIterations = 2;

            TestWorldBuilder.Build();

            GameObject bikeGo = BikeBuilder.Build(new Vector3(0f, 0.7f, -70f), Quaternion.identity,
                new Color(0.8f, 0.1f, 0.1f));
            BikeRig rig = bikeGo.GetComponent<BikeRig>();
            MotorcycleController bike = bikeGo.GetComponent<MotorcycleController>();
            WheelieController wheelie = bikeGo.GetComponent<WheelieController>();

            Camera cam = CreateCamera();
            CameraManager cams = CameraManager.Create(cam, rig);

            EnsureEventSystem();
            TouchInputController touch = TouchInputController.Create(bike, cams);
            UIManager.Create(bike, wheelie, cams, touch);
        }

        static Camera CreateCamera()
        {
            GameObject go = new GameObject("MainCamera");
            go.tag = "MainCamera";
            Camera cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            cam.fieldOfView = 60f;
            go.AddComponent<AudioListener>();
            return cam;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
