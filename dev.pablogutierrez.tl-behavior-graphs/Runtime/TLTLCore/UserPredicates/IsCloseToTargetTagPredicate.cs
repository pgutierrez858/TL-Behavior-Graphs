using UnityEngine;

namespace TLTLCore.Framework
{
    /// <summary>
    /// Comprueba si el objeto especificado <see cref="Object"/> se encuentra a menos de <see cref="MaxDistance"/>
    /// de un objetivo con tag <see cref="Tag"/>.
    /// </summary>
    [Predicate("Conditions/Tag Is Close Condition", "True?")]
    public class IsCloseToTargetTagPredicate : TLTLPredicate
    {
        [InParam("Object")]
        public GameObject Object { get; set; }

        [InParam("Tag")]
        public string Tag { get; set; }

        [InParam("CheckDistance")]
        public float CheckDistance { get; set; }

        [InParam("CloseDistance")]
        public float CloseDistance { get; set; }

        public override float EvaluateRobustness()
        {
            Collider[] hitColliders = Physics.OverlapSphere(Object.transform.position, CheckDistance);

            foreach (Collider collider in hitColliders)
            {
                // Check if the collider belongs to the target tag
                if (collider.gameObject.CompareTag(Tag))
                {
                    // vamos a aplicar una transformación para que este predicado tenga una robustez
                    // con mayor pendiente cerca de la región objetivo y menor cuando se aleja de ella.
                    // podemos expresar esto por medio de una ecuación cuadrática p = m*d^2 + n, que debe
                    // pasar por los puntos (CloseDistance, 0), (MaxDistance, -1).
                    // La primera condición impone que:
                    //  0 = m * CloseDistance^2 + n
                    // La segunda impone:
                    //  -1 = m * MaxDistance^2 + n
                    // Restando ambas tenemos:
                    // 1 = (CloseDistance^2 - MaxDistance^2) * m
                    // por lo que m = 1 / (CloseDistance^2 - MaxDistance^2)
                    // y n = - m * CloseDistance^2.
                    float m = 1f / (CloseDistance * CloseDistance - CheckDistance * CheckDistance);
                    float n = -m * CloseDistance * CloseDistance;
                    // Calculate the distance between the current object and the target object
                    float d = Vector3.Distance(Object.transform.position, collider.transform.position);
                    float p = m * d * d + n;
                    return Mathf.Clamp(p, -1, 1);
                }
            }

            return -1f;
        }
    } // IsCloseToTargetTagPredicate
} // namespace TLTLCore.Framework
