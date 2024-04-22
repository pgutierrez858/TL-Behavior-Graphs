using UnityEngine;

namespace TLTLUnity.Data
{
    using System;

    [Serializable]
    public class TLTLInputParamData
    {
        /// <summary>
        /// Nombre del par�metro de entrada del nodo enlazado a este objeto.
        /// </summary>
        [field: SerializeField] public string InParamName { get; set; }
        /// <summary>
        /// Nombre de la variable de la pizarra que se usar� para proporcionar input a este par�metro en ejecuci�n.
        /// </summary>
        [field: SerializeField] public string BlackboardParamName { get; set; }
    } // TLTLInputParamData

} // namespace TLTLUnity.Data
