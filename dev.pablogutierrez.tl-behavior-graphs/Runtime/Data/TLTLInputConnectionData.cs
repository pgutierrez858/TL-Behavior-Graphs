using UnityEngine;

namespace TLTLUnity.Data
{
    using System;

    [Serializable]
    public class TLTLInputConnectionData
    {
        /// <summary>
        /// ID del nodo que entra en esta conexi�n. Se entiende que una conexi�n representa al nodo
        /// cuyo output presenta una arista con destino al nodo actual.
        /// </summary>
        [field: SerializeField] public string NodeID { get; set; }

        /// <summary>
        /// Nombre del par�metro de entrada del nodo enlazado a este objeto.
        /// </summary>
        [field: SerializeField] public string InParamName { get; set; }
    } // TLTLInputConnectionData

} // namespace TLTLUnity.Data
