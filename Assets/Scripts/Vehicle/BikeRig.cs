using UnityEngine;

namespace ProjetoMoto
{
    /// <summary>Referências úteis da moto montada (câmera em 1ª pessoa, peças a esconder).</summary>
    public class BikeRig : MonoBehaviour
    {
        public Transform firstPersonPoint;
        public Renderer[] hiddenInFirstPerson;
    }
}
