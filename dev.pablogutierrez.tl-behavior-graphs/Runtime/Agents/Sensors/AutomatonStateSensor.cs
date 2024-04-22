using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace TLTLUnity.Agents
{
    /// <summary>
    /// Sensor básico para generar observaciones de goal dirigidas por el estado
    /// actual de la máquina de estados de premio utilizada para definir el comportamiento
    /// del agente entrenado. Las observaciones de este tipo vienen representadas por
    /// un vector en formato one-hot, de la misma longitud que el número de estados de la
    /// máquina de premios.
    /// 
    /// Adicionalmente, para cada GameObject que se introduzca como parámetro en la pizarra,
    /// se añade una observación de posición relativa en XYZ al objeto agente normalizada por
    /// el tamaño del nivel, dado por un objeto bounds.
    /// </summary>
    public class AutomatonStateSensor : ISensor
    {
        public TLTLRewardAutomaton rewardAutomaton;
        public bool treatStatesAsGoalSignals;
        public int stateCount;
        public List<Transform> observablePositionalEntities;
        public List<ObservableComponentField> observableFields;
        public Transform agentTransform;
        public Bounds envBounds;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="automaton">Autómata de premios del que extraer el estado en el que se encuentra el agente dentro de la especificación</param>
        /// <param name="agentTransform">Transform del objeto agente, necesaria para calcular las posiciones relativas del agente a los objetos de interés de la escena.</param>
        /// <param name="treatStatesAsGoalSignals">Si se marca como true y se habilita la opción de HyperNetworks en el archivo de configuración, establece que la señal de los estados
        /// del autómata se trate como Goal en la red.</param>
        /// <param name="stateCount">Valor al que forzar, si se especifica, la cuenta de estados del autómata (esto está aquí para soportar cargas en caliente de autómatas)</param>
        public AutomatonStateSensor(
            TLTLRewardAutomaton automaton,
            List<Transform> observablePositionalEntities,
            List<ObservableComponentField> observableFields,
            Transform agentTransform,
            Bounds envBounds,
            bool treatStatesAsGoalSignals,
            int stateCount = -1)
        {
            rewardAutomaton = automaton;
            this.treatStatesAsGoalSignals = treatStatesAsGoalSignals;
            this.stateCount = stateCount < -1 ? rewardAutomaton.StateCount : stateCount;

            this.observableFields = observableFields;
            this.observablePositionalEntities = observablePositionalEntities;

            this.envBounds = envBounds;
            this.agentTransform = agentTransform;
        }

        public int Write(ObservationWriter writer)
        {
            // la observación tendrá los siguientes parámetros:
            // - una one-hot-encoding del estado actual del autómata
            // - una entrada con 3 valores representando la posición relativa del agente a cada uno de los objetos 
            // de interés en el predicado (X, Y, Z).
            // - las entradas booleanas de los elementos observables (0 para false y 1 para true)
            var res = new float[rewardAutomaton.StateCount + 3 * observablePositionalEntities.Count + observableFields.Count];
            // Vamos a representar la observación de estado como un one-hot-encoding en el que tendremos un vector
            // con tantos elementos como estados en el autómata de premios, todos 0's salvo un único 1
            // en la posición del estado actualmente activo.
            if (rewardAutomaton.ActiveStateIndex >= 0)
                res[rewardAutomaton.ActiveStateIndex] = 1f;

            // vamos moviendo el offset donde empezar a escribir observaciones en función de cuánto hayamos
            // avanzado en el vector res.
            int offset = rewardAutomaton.StateCount;
            // Por otro lado, para cada uno de los elementos de interés en la escena, vamos a introducir 
            // una observación con la posición relativa a ellos desde la perspectiva del agente.
            foreach (var target in observablePositionalEntities)
            {
                // En concreto, vamos a representar cada observación como un vector de 3 elementos para las posiciones X, Y y Z
                // cada una de ellas hecha clamp en el intervalo min-max especificado y después dividida entre la
                // longitud de dicho intervalo de posiciones válidas.

                // en principio si envBounds está bien hecho, todas las posiciones naturales de objetos y agente en la escena
                // deberían dar valores de entre -1 y 1 en estos cálculos. El signo importa aquí para entender la orientación.
                // cualquier valor que se exceda (esto es, objetos que se separen entre sí más que el tamaño del nivel) será capado
                // en este intervalo.
                float x = Mathf.Clamp((target.position.x - agentTransform.position.x) / envBounds.size.x, -1f, 1f);
                float y = Mathf.Clamp((target.position.y - agentTransform.position.y) / envBounds.size.y, -1f, 1f);
                float z = Mathf.Clamp((target.position.z - agentTransform.position.z) / envBounds.size.z, -1f, 1f);
                //Debug.Log($"[{target.name}] ({x}, {y}, {z})");
                res[offset] = x; res[offset + 1] = y; res[offset + 2] = z;
                offset += 3;
            }

            foreach (var target in observableFields)
            {
                // para cada uno de los campos observables añadimos una entrada explícita casteada a float
                // NOTA: aquí el valor se está extrayendo de una propiedad establecida a mano por el usuario 
                // desde el inspector. En AutomatonStateSensorComponent nos aseguramos de que aquí sólo metemos
                // propiedades válidas que sean de tipos bool, int o float, pero no se procesa de ningún otro modo
                // así que es responsabilidad del que mete estas referencias de que estas tengan sentido.
                // Ej: si una propiedad de tipo int vale 30, eso se mete a la red neuronal directamente, lo cual
                // raramente va a tener sentido... usar con PRECAUCIÓN.
                if (target.propertyInfo.FieldType == typeof(bool))
                {
                    res[offset] = (bool)target.CurrentValue ? 1 : 0;
                }
                else if (target.propertyInfo.FieldType == typeof(float) || target.propertyInfo.FieldType == typeof(int))
                {
                    res[offset] = (float)target.CurrentValue;
                }
                offset++;
            }


            writer.AddList(res);

            // el método exige devolver el número de elementos escritos para saber por qué índice continuar
            // al combinar todos los sensores.
            return res.Length;
        }

        public byte[] GetCompressedObservation()
        {
            // If you don't want to use compressed observations, return CompressionSpec.Default() from GetCompressionSpec().
            return null;
        }

        public void Update()
        {
            // Purposefully left empty
            // La razón por la que no es necesario hacer nada en el Update es porque
            // en realidad el autómata de premios ya se va a ir actualizando sólo con sus
            // propios ticks, y aquí lo único que estaremos interesados en hacer será
            // acceder al estado actual y escribirlo como observación en one-hot-encoding.
        }

        public void Reset()
        {
            // Purposefully left empty
        }

        public CompressionSpec GetCompressionSpec()
        {
            // If you don't want to use compressed observations, return CompressionSpec.Default() from GetCompressionSpec().
            return CompressionSpec.Default();
        }

        public ObservationSpec GetObservationSpec()
        {
            /**
             * Aquí estamos interesados en tratar las observaciones del sensor como señales
             * de objetivo, de manera que el estado específico del autómata tenga un efecto
             * explícito sobre la política de la red. En concreto, nos interesará que la política
             * sea potencialmente muy distinta en función del estado en el que nos encontremos,
             * al poder tener este una serie de objetivos que no tengan nada que ver con los de otros
             * estados. La forma de especificar esto en MLAgents es indicando que nuestro vector de 
             * estados en one-hot-encoding es una señal de goal, lo que permitirá que por detrás
             * se usen HyperNetworks para modificar los pesos de la red de política en función
             * del estado en el que nos encontremos (https://arxiv.org/abs/1609.09106).
             */
            var spec = ObservationSpec.Vector(stateCount + 3 * observablePositionalEntities.Count + observableFields.Count, treatStatesAsGoalSignals ? ObservationType.GoalSignal : ObservationType.Default);
            return spec;
        }

        public string GetName()
        {
            return "Automaton State";
        }
    } // AutomatonStateSensor

} // namespace TLTLUnity.Agents
