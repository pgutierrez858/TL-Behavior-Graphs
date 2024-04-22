using UnityEngine;

namespace TLTLCore.Framework
{
    /// <summary>
    /// Comprueba si el objeto especificado <see cref="Object"/> se encuentra a menos de <see cref="CloseDistance"/>
    /// de su objetivo <see cref="Target"/>. De cara a acotar la robustez del predicado entre -1 y 1, se establece una
    /// distancia máxima <see cref="MaxDistance"/> por encima de la cual consideramos que el objetivo está demasiado lejos
    /// (con robustez -1).
    /// </summary>
    [Predicate("Conditions/Close to Target Condition", "True?")]
    public class IsCloseToTargetPredicate : TLTLPredicate
    {
        [InParam("Object")]
        public GameObject Object { get; set; }

        [InParam("Target")]
        public GameObject Target { get; set; }

        [InParam("CloseDistance")]
        public float CloseDistance { get; set; }

        [InParam("MaxDistance")]
        public float MaxDistance { get; set; }


        public override float EvaluateRobustness()
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
            float m = 1f / (CloseDistance * CloseDistance - MaxDistance * MaxDistance);
            float n = -m * CloseDistance * CloseDistance;
            float d = Vector3.Distance(Object.transform.position, Target.transform.position);
            float p = m * d * d + n;
            return Mathf.Clamp(p, -1, 1);
        }
    } // IsCloseToTargetPredicate
} // namespace TLTLCore.Framework
