using UnityEngine;

namespace TLTLUnity.Data
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class TLTLNodeData
    {
        [field: SerializeField] public string ID { get; set; }
        [field: SerializeField] public List<TLTLInputConnectionData> Connections { get; set; }
        [field: SerializeField] public List<TLTLInputParamData> InputParams { get; set; }
        /// <summary>
        /// Nombre de la clase que representa el tipo de predicado codificado por este nodo (la que implementa el usuario).
        /// </summary>
        [field: SerializeField] public string PredicateType { get; set; }

    } // TLTLNodeData

} // namespace TLTLUnity.Data
