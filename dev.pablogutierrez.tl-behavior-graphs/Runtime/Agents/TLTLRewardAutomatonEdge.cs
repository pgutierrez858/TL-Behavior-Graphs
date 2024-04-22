using System.Linq;
using TLTLCore.Framework;
using TLTLUnity.Data;

namespace TLTLUnity.Agents
{
    public class TLTLRewardAutomatonEdge
    {
        /// <summary>
        /// Predicado que define la transici�n representada por esta arista de la m�quina de estados.
        /// Una transici�n ser� escogida si el valor de robustez de su predicado asignado es el mayor
        /// de entre todos los valores de robustez de las aristas asociadas al estado actual.
        /// </summary>
        public readonly TLTLPredicate predicate;
        /// <summary>
        /// Estado final al que se enviar� a la m�quina de estados tras ejecutar la transici�n dada por esta
        /// arista (esto ocurrir� asumiendo que el predicado asignado es el m�s robusto de entre los disponibles
        /// en el estado actual).
        /// </summary>
        public readonly int endState;

        /// <summary>
        /// Tokens asociados a la transici�n. Estos s�lo tienen sentido desde el punto
        /// de vista de las condiciones de aceptaci�n de Rabin, que se traducen en que
        /// ciertos tokens deber�n ser visitados infinitamente durante la ejecuci�n, mientras
        /// que el resto s�lo podr�n ser visitados como mucho un n�mero finito de veces.
        /// </summary>
        public readonly string[] transitionTokens;

        public TLTLRewardAutomatonEdge(TLTLPredicate predicate, int endState, string[] transitionTokens)
        {
            this.predicate = predicate;
            this.endState = endState;
            this.transitionTokens = transitionTokens;
        }

        /// <summary>
        /// determina, dado un conjunto de condiciones de aceptaci�n, si la transici�n denota 
        /// una posible aceptaci�n. Esto se traduce en que exista una pareja de aceptaci�n de 
        /// Rabin cuyos tokens finitos no se est�n visitando en esta transici�n y sus tokens infinitos s�.
        /// </summary>
        public bool IsAcceptingTransition(RabinAcceptancePair[] acceptancePairs)
        {
            // para cada uno de los pares de aceptaci�n tenemos que verificar la
            // existencia de una transici�n af�n.
            foreach (var pair in acceptancePairs)
            {
                string[] finTokens = pair.FinTokens;
                string[] infTokens = pair.InfTokens;

                // condici�n de aceptaci�n: una transici�n tiene al menos un token que debe
                // ser visitado infinitamente y no contiene ninguno de los tokens que deben ser
                // visitados finitamente.
                if (infTokens.Intersect(transitionTokens).Count() > 0 &&
                    finTokens.Intersect(transitionTokens).Count() == 0) return true;
            }
            return false;
        } // IsAcceptingTransition

    } // TLTLRewardAutomatonEdge
} // namespace TLTLUnity.Agents
