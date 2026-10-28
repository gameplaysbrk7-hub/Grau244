using System.Collections.Generic;
using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>
    /// Monta a moto e o piloto provisórios a partir de primitivas (fácil de substituir por modelos finais)
    /// e liga todos os componentes. O objeto fica inativo durante a montagem para que os Awake
    /// rodem só depois de todas as referências estarem preenchidas.
    /// </summary>
    public static class BikeBuilder
    {
        const float WheelRestOffset = 0.214f; // distância do centro da roda até o ponto de montagem em repouso

        public static GameObject Build(Vector3 position, Quaternion rotation, Color bodyColor)
        {
            GameObject go = new GameObject("Moto");
            go.SetActive(false);
            go.transform.SetPositionAndRotation(position, rotation);
            Transform root = go.transform;

            go.AddComponent<Rigidbody>();
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.18f, 0f);
            box.size = new Vector3(0.35f, 0.5f, 1.7f);
            PhysicMaterial pm = new PhysicMaterial("Corpo");
            pm.dynamicFriction = 0.5f;
            pm.staticFriction = 0.5f;
            pm.bounciness = 0f;
            pm.frictionCombine = PhysicMaterialCombine.Minimum;
            box.sharedMaterial = pm;

            Color dark = new Color(0.12f, 0.12f, 0.13f);
            Color silver = new Color(0.7f, 0.72f, 0.75f);

            // Carenagem e chassi
            Visuals.Prim(PrimitiveType.Cube, "Corpo", root, new Vector3(0f, 0.12f, 0.05f), new Vector3(0.30f, 0.34f, 0.95f), bodyColor);
            Visuals.Prim(PrimitiveType.Cube, "Tanque", root, new Vector3(0f, 0.30f, 0.15f), new Vector3(0.28f, 0.20f, 0.40f), bodyColor);
            Visuals.Prim(PrimitiveType.Cube, "Banco", root, new Vector3(0f, 0.27f, -0.25f), new Vector3(0.26f, 0.08f, 0.55f), dark);
            Visuals.Prim(PrimitiveType.Cube, "Lanterna", root, new Vector3(0f, 0.34f, -0.58f), new Vector3(0.14f, 0.08f, 0.08f), new Color(0.8f, 0.05f, 0.05f));
            Visuals.Prim(PrimitiveType.Cube, "Motor", root, new Vector3(0f, -0.10f, 0.0f), new Vector3(0.22f, 0.28f, 0.40f), new Color(0.35f, 0.35f, 0.38f));
            Visuals.Prim(PrimitiveType.Cube, "Balanca", root, new Vector3(0f, -0.16f, -0.40f), new Vector3(0.08f, 0.06f, 0.64f), dark);

            GameObject exhaust = Visuals.Prim(PrimitiveType.Cylinder, "Escape", root, new Vector3(0.17f, -0.08f, -0.35f), new Vector3(0.07f, 0.35f, 0.07f), silver);
            exhaust.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Garfo e roda dianteira (giram com a direção)
            Transform fork = new GameObject("PivoGarfo").transform;
            fork.SetParent(root, false);
            fork.localPosition = new Vector3(0f, 0.05f, 0.72f);
            Visuals.Prim(PrimitiveType.Cylinder, "GarfoE", fork, new Vector3(-0.08f, 0.12f, 0f), new Vector3(0.045f, 0.33f, 0.045f), silver);
            Visuals.Prim(PrimitiveType.Cylinder, "GarfoD", fork, new Vector3(0.08f, 0.12f, 0f), new Vector3(0.045f, 0.33f, 0.045f), silver);
            GameObject bar = Visuals.Prim(PrimitiveType.Cylinder, "Guidao", fork, new Vector3(0f, 0.46f, -0.05f), new Vector3(0.03f, 0.35f, 0.03f), dark);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Visuals.Prim(PrimitiveType.Sphere, "Farol", fork, new Vector3(0f, 0.36f, 0.14f), new Vector3(0.20f, 0.20f, 0.20f), new Color(1f, 0.97f, 0.8f));

            Transform frontWheel = new GameObject("RodaDianteira").transform;
            frontWheel.SetParent(fork, false);
            frontWheel.localPosition = new Vector3(0f, -WheelRestOffset, 0f);
            BuildWheel(frontWheel);

            // Roda traseira
            Transform rearMount = new GameObject("MontagemTraseira").transform;
            rearMount.SetParent(root, false);
            rearMount.localPosition = new Vector3(0f, 0.05f, -0.72f);
            Transform rearWheel = new GameObject("RodaTraseira").transform;
            rearWheel.SetParent(root, false);
            rearWheel.localPosition = rearMount.localPosition + new Vector3(0f, -WheelRestOffset, 0f);
            BuildWheel(rearWheel);

            // Pontos de apoio do piloto
            Transform gripL = MakePoint("ManoplaE", fork, new Vector3(-0.33f, 0.46f, -0.05f));
            Transform gripR = MakePoint("ManoplaD", fork, new Vector3(0.33f, 0.46f, -0.05f));
            Transform pegL = MakePoint("PedalE", root, new Vector3(-0.20f, -0.20f, -0.10f));
            Transform pegR = MakePoint("PedalD", root, new Vector3(0.20f, -0.20f, -0.10f));

            // Piloto
            Transform hips = new GameObject("Piloto").transform;
            hips.SetParent(root, false);
            hips.localPosition = new Vector3(0f, 0.32f, -0.22f);
            Transform torso = new GameObject("PivoTronco").transform;
            torso.SetParent(hips, false);

            Color jacket = new Color(0.15f, 0.3f, 0.75f);
            Color pants = new Color(0.12f, 0.14f, 0.2f);
            Visuals.Prim(PrimitiveType.Cube, "Quadril", hips, Vector3.zero, new Vector3(0.30f, 0.16f, 0.28f), pants);
            Visuals.Prim(PrimitiveType.Capsule, "Tronco", torso, new Vector3(0f, 0.30f, 0.10f), new Vector3(0.32f, 0.30f, 0.26f), jacket);
            GameObject head = Visuals.Prim(PrimitiveType.Sphere, "Capacete", torso, new Vector3(0f, 0.74f, 0.16f), new Vector3(0.24f, 0.26f, 0.28f), new Color(0.9f, 0.9f, 0.9f));
            Transform shoulderL = MakePoint("OmbroE", torso, new Vector3(-0.19f, 0.50f, 0.10f));
            Transform shoulderR = MakePoint("OmbroD", torso, new Vector3(0.19f, 0.50f, 0.10f));
            Transform hipL = MakePoint("QuadrilE", hips, new Vector3(-0.12f, 0f, 0f));
            Transform hipR = MakePoint("QuadrilD", hips, new Vector3(0.12f, 0f, 0f));

            RiderAnimationController.Limb[] limbs = new RiderAnimationController.Limb[4];
            limbs[0] = MakeLimb(root, "BracoE", shoulderL, gripL, 0.09f, jacket);
            limbs[1] = MakeLimb(root, "BracoD", shoulderR, gripR, 0.09f, jacket);
            limbs[2] = MakeLimb(root, "PernaE", hipL, pegL, 0.12f, pants);
            limbs[3] = MakeLimb(root, "PernaD", hipR, pegR, 0.12f, pants);

            // Componentes
            MotorcycleInput input = go.AddComponent<MotorcycleInput>();

            BikeRig rig = go.AddComponent<BikeRig>();
            rig.firstPersonPoint = MakePoint("PontoPrimeiraPessoa", root, new Vector3(0f, 1.0f, 0.22f));
            rig.hiddenInFirstPerson = new Renderer[] { head.GetComponent<Renderer>() };

            MotorcycleController bike = go.AddComponent<MotorcycleController>();
            bike.frontMount = fork;
            bike.rearMount = rearMount;
            bike.frontWheel = frontWheel;
            bike.rearWheel = rearWheel;

            WheelieController wheelie = go.AddComponent<WheelieController>();

            RiderAnimationController rider = go.AddComponent<RiderAnimationController>();
            rider.torsoPivot = torso;
            rider.limbs = limbs;
            rider.bike = bike;
            rider.controls = input;
            rider.wheelie = wheelie;

            Visuals.SetLayerRecursive(go, MotorcycleController.BikeLayer);
            go.SetActive(true);
            return go;
        }

        static void BuildWheel(Transform pivot)
        {
            GameObject tire = Visuals.Prim(PrimitiveType.Cylinder, "Pneu", pivot, Vector3.zero, new Vector3(0.64f, 0.07f, 0.64f), new Color(0.07f, 0.07f, 0.07f));
            tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            GameObject rim = Visuals.Prim(PrimitiveType.Cylinder, "Aro", pivot, Vector3.zero, new Vector3(0.42f, 0.075f, 0.42f), new Color(0.75f, 0.75f, 0.78f));
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            // Raios cruzados para a rotação ficar visível
            Visuals.Prim(PrimitiveType.Cube, "RaioA", pivot, Vector3.zero, new Vector3(0.06f, 0.50f, 0.04f), new Color(0.3f, 0.3f, 0.32f));
            Visuals.Prim(PrimitiveType.Cube, "RaioB", pivot, Vector3.zero, new Vector3(0.06f, 0.04f, 0.50f), new Color(0.3f, 0.3f, 0.32f));
        }

        static Transform MakePoint(string name, Transform parent, Vector3 localPos)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        static RiderAnimationController.Limb MakeLimb(Transform root, string name, Transform from, Transform to, float thickness, Color color)
        {
            GameObject mesh = Visuals.Prim(PrimitiveType.Cylinder, name, root, Vector3.zero, new Vector3(thickness, 0.3f, thickness), color);
            RiderAnimationController.Limb l = new RiderAnimationController.Limb();
            l.from = from;
            l.to = to;
            l.mesh = mesh.transform;
            l.thickness = thickness;
            return l;
        }
    }
}
