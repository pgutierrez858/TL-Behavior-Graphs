using System.Linq;
using TLTLCore.Framework;
using TLTLUnity.Data;

namespace TLTLUnity.Agents
{
    public class TLTLRewardAutomatonEdge
    {
        /// <summary>
        /// Predicado que define la transición representada por esta arista de la máquina de estados.
        /// Una transición será escogida si el valor de robustez de su predicado asignado es el mayor
        /// de entre todos los valores de robustez de las aristas asociadas al estado actual.
        /// </summary>
        public readonly TLTLPredicate predicate;
        /// <summary>
        /// Estado final al que se enviará a la máquina de estados tras ejecutar la transición dada por esta
        /// arista (esto ocurrirá asumiendo que el predicado asignado es el más robusto de entre los disponibles
        /// en el estado actual).
        /// </summary>
        public readonly int endState;

        /// <summary>
        /// Tokens asociados a la transición. Estos sólo tienen sentido desde el punto
        /// de vista de las condiciones de aceptación de Rabin, que se traducen en que
        /// ciertos tokens deberán ser visitados infinitamente durante la ejecución, mientras
        /// que el resto sólo podrán ser visitados como mucho un número finito de veces.
        /// </summary>
        public readonly string[] transitionTokens;

        public TLTLRewardAutomatonEdge(TLTLPredicate predicate, int endState, string[] transitionTokens)
        {
            this.predicate = predicate;
            this.endState = endState;
            this.transitionTokens = transitionTokens;
        }

        /// <summary>
        /// determina, dado un conjunto de condiciones de aceptación, si la transición denota 
        /// una posible aceptación. Esto se traduce en que exista una pareja de aceptación de 
        /// Rabin cuyos tokens finitos no se estén visitando en esta transición y sus tokens infinitos sí.
        /// </summary>
        public bool IsAcceptingTransition(RabinAcceptancePair[] acceptancePairs)
        {
            // para cada uno de los pares de aceptación tenemos que verificar la
            // existencia de una transición afín.
            foreach (var pair in acceptancePairs)
            {
                string[] finTokens = pair.FinTokens;
                string[] infTokens = pair.InfTokens;

                // condición de aceptación: una transición tiene al menos un token que debe
                // ser visitado infinitamente y no contiene ninguno de los tokens que deben ser
                // visitados finitamente.
                if (infTokens.Intersect(transitionTokens).Count() > 0 &&
                    finTokens.Intersect(transitionTokens).Count() == 0) return true;
            }
            return false;
        } // IsAcceptingTransition

    } // TLTLRewardAutomatonEdge
} // namespace TLTLUnity.Agents
