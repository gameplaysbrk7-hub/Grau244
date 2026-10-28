using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjetoMoto
{
    /// <summary>
    /// Alterna entre as câmeras e traduz gestos (arrastar = olhar, pinça = zoom) sem interferir
    /// nos botões: só dedos que começam fora da interface controlam a câmera.
    /// Editor: botão direito do mouse arrasta, roda do mouse dá zoom.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        public ThirdPersonCameraController thirdPerson;
        public FirstPersonCameraController firstPerson;
        public BikeRig rig;

        readonly List<int> fingers = new List<int>();

        public bool IsFirstPerson { get; private set; }

        public static CameraManager Create(Camera cam, BikeRig rig)
        {
            CameraManager m = cam.gameObject.AddComponent<CameraManager>();
            m.rig = rig;
            MotorcycleController bike = rig.GetComponent<MotorcycleController>();

            m.thirdPerson = cam.gameObject.AddComponent<ThirdPersonCameraController>();
            m.thirdPerson.target = rig.transform;
            m.thirdPerson.bike = bike;

            m.firstPerson = cam.gameObject.AddComponent<FirstPersonCameraController>();
            m.firstPerson.target = rig.transform;
            m.firstPerson.point = rig.firstPersonPoint;
            m.firstPerson.bike = bike;

            m.SetFirstPerson(false);
            return m;
        }

        public void SetFirstPerson(bool fp)
        {
            IsFirstPerson = fp;
            thirdPerson.enabled = !fp;
            firstPerson.enabled = fp;
            if (rig != null && rig.hiddenInFirstPerson != null)
            {
                for (int i = 0; i < rig.hiddenInFirstPerson.Length; i++)
                    if (rig.hiddenInFirstPerson[i] != null) rig.hiddenInFirstPerson[i].enabled = !fp;
            }
        }

        public void ToggleView() { SetFirstPerson(!IsFirstPerson); }

        public void Recenter()
        {
            thirdPerson.Recenter();
            firstPerson.Recenter();
        }

        void Update()
        {
            if (Time.timeScale <= 0f) { fingers.Clear(); return; }

            float dx = 0f, dy = 0f, pinch = 0f;
            int count = Input.touchCount;

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began)
                    {
                        if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(t.fingerId))
                            fingers.Add(t.fingerId);
                    }
                    else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        fingers.Remove(t.fingerId);
                    }
                }

                int found = 0;
                Touch t0 = default(Touch);
                Touch t1 = default(Touch);
                for (int i = 0; i < count; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (!fingers.Contains(t.fingerId)) continue;
                    if (found == 0) t0 = t; else if (found == 1) t1 = t;
                    found++;
                }

                if (found == 1)
                {
                    dx = t0.deltaPosition.x;
                    dy = t0.deltaPosition.y;
                }
                else if (found >= 2)
                {
                    float prev = ((t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition)).magnitude;
                    float cur = (t0.position - t1.position).magnitude;
                    pinch = cur - prev;
                }
            }
            else
            {
                fingers.Clear();
                if (Input.GetMouseButton(1))
                {
                    dx = Input.GetAxis("Mouse X") * 10f;
                    dy = Input.GetAxis("Mouse Y") * 10f;
                }
                pinch = Input.mouseScrollDelta.y * 20f;
            }

            float k = 0.12f * (1080f / Mathf.Max(Screen.height, 1)) * GameSettings.CameraSensitivity;
            float yawDelta = dx * k;
            float pitchDelta = -dy * k;

            if (yawDelta != 0f || pitchDelta != 0f)
            {
                if (IsFirstPerson) firstPerson.AddLook(yawDelta, pitchDelta);
                else thirdPerson.AddLook(yawDelta, pitchDelta);
            }
            if (pinch != 0f && !IsFirstPerson) thirdPerson.AddZoom(-pinch * 0.004f);
        }
    }
}
