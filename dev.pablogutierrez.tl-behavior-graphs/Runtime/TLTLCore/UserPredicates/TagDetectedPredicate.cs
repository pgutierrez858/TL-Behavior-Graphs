using UnityEngine;

namespace TLTLCore.Framework
{
    /// <summary>
    /// Comprueba si el objeto especificado <see cref="Object"/> se encuentra a menos de <see cref="MaxDistance"/>
    /// de un objetivo con tag <see cref="Tag"/>, y lo tiene delante.
    /// </summary>
    [Predicate("Conditions/Tag In Sight Condition", "True?")]
    public class TagDetectedPredicate : TLTLPredicate
    {
        [InParam("Object")]
        public GameObject Object { get; set; }

        [InParam("Tag")]
        public string Tag { get; set; }

        [InParam("MaxDistance")]
        public float MaxDistance { get; set; }

        public override float EvaluateRobustness()
        {
            // Cast a ray forward from the GameObject's position
            Ray ray = new Ray(Object.transform.position, Object.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, MaxDistance))
            {
                // Check if the hit object has the target tag
                if (hit.collider.CompareTag(Tag))
                {
                    // objeto con el tag buscado encontrado delante de nosotros
                    // la robustez del predicado será 1f si hit.distance es 0 (está justo delante)
                    // y 0 si la distancia se corresponde con la máxima (lo más lejos aceptado).
                    return 1f - (hit.distance / MaxDistance);
                }
            }

            // en cualquier otro caso devolvemos -1f para indicar que no se encontró el objetivo. 
            return -1f;
        }
    } // TagDetectedPredicate
} // namespace TLTLCore.Framework
