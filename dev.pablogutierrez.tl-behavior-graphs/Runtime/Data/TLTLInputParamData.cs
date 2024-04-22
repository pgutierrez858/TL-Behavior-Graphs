using UnityEngine;

namespace TLTLUnity.Data
{
    using System;

    [Serializable]
    public class TLTLInputParamData
    {
        /// <summary>
        /// Nombre del parámetro de entrada del nodo enlazado a este objeto.
        /// </summary>
        [field: SerializeField] public string InParamName { get; set; }
        /// <summary>
        /// Nombre de la variable de la pizarra que se usará para proporcionar input a este parámetro en ejecución.
        /// </summary>
        [field: SerializeField] public string BlackboardParamName { get; set; }
    } // TLTLInputParamData

} // namespace TLTLUnity.Data
