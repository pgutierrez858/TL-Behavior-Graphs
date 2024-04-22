using System;
using System.Collections.Generic;
using TLTLCore.Framework;
using UnityEngine;
using Blackboard = TLTLCore.Blackboard;

namespace TLTLUnity.Data
{
    public class TLTLPredicateDataAsset : ScriptableObject
    {
        #region Propiedades Serializables e inicialización
        [field: SerializeField] public List<TLTLNodeData> Nodes { get; set; }
        [field: SerializeField] public TLTLBlackboardData BlackboardData { get; set; }
        /// <summary>
        /// ID del nodo cuyo output está vinculado al nodo de return del grafo (o null)
        /// </summary>
        [field: SerializeField] public string ReturnNodeConnectionID { get; set; }

        public void Initialize()
        {
            Nodes = new List<TLTLNodeData>();
            BlackboardData = new TLTLBlackboardData();
            ReturnNodeConnectionID = null;
        } // Initialize
        #endregion

        #region Predicado en ejecución extraído del asset
        /// <summary>
        /// Predicado al que representa, en el formato
        /// utilizado por el motor de ejecución.
        /// </summary>
        /// Durante la edición, se guarda el predicado
        /// extendido con la información del editor.
        [System.NonSerialized]
        public TLTLPredicate predicate;

        public TLTLPredicate BuildPredicate(Blackboard blackboard)
        {
            // El nodo de retorno debe estar conectado a otro nodo
            if (ReturnNodeConnectionID == null) return null;

            // nodo raíz a partir del cual se construye el predicado
            TLTLNodeData rootNode = Nodes.Find(n => n.ID == ReturnNodeConnectionID);

            if (rootNode == null) return null;

            return BuildPredicateFromNodeData(rootNode, blackboard);
        } // BuildPredicate

        private TLTLPredicate BuildPredicateFromNodeData(TLTLNodeData nodeData, Blackboard blackboard)
        {
            // Para cada nodo: obtener el tipo definido en su especificación y
            // crear una nueva instancia de ese tipo.
            // Adicionalmente estaremos interesados en vincular las 
            // variables de la pizarra con las variables de entrada de los predicados
            // que vayamos creando.
            Type nodeType = Type.GetType(nodeData.PredicateType);
            TLTLPredicate predicateInstance = Activator.CreateInstance(nodeType) as TLTLPredicate;


            // obtención de las propiedades de entrada del predicado actual
            var inProperties = TLTLPredicate.GetInProperties(nodeType);

            // Para cada propiedad:
            // - Si es una propiedad de tipo Predicado TLTL, entonces realizamos una llamada recursiva
            // para generar un nodo EXPANDIDO. No asociamos ningún valor de predicado a una entrada
            // de tipo predicate salvo que el nodo correspondiente haya sido expandido previamente.
            // - Si es una propiedad de otro tipo, estará registrada en la pizarra, y debemos vincularla
            // directamente (es un caso base y no requiere ninguna llamada recursiva).
            foreach (var property in inProperties)
            {
                if (property.PropertyType == typeof(TLTLPredicate))
                {
                    // Caso recursivo: expandir nodo correspondiente, comprobar que es correcto
                    // y en caso afirmativo vincularlo a este parámetro de entrada.

                    // conexión de entrada del nodo que se corresponde con el parámetro que queremos rellenar
                    TLTLInputConnectionData inputConnection = nodeData.Connections.Find(c => c.InParamName == property.Name);
                    // nodo de entrada vinculado con la conexión anterior
                    TLTLNodeData inputNodeData = Nodes.Find(n => n.ID == inputConnection.NodeID);
                    property.SetValue(predicateInstance, BuildPredicateFromNodeData(inputNodeData, blackboard));
                }
                else
                {
                    // Caso base: buscamos la variable en la pizarra y la vinculamos de forma
                    // directa con la entrada.
                    // CUIDADO con la nomenclatura aquí:
                    // - dentro de un nodeData tenemos la lista de parámetros de entrada del NODO, que pueden tener nombres
                    // arbitrarios, especificados dentro de su BlackboardParamName (el que se establece en el editor de grafo).
                    // - cada uno de estos parámetros, no obstante, está vinculado a un InParamName, que es precisamente el nombre
                    // de la propiedad a la que va vinculado dentro de la clase en sí, y este nombre es fijo.
                    // Por tanto, para cada propiedad de la clase debemos encontrar aquella propiedad de los parámetros de entrada
                    // del nodo cuyo inParamName coincida con el nombre de la propiedad en cuestión.
                    TLTLInputParamData inputParamData = nodeData.InputParams.Find(p => p.InParamName == property.Name);
                    property.SetValue(predicateInstance, blackboard.Get(inputParamData.BlackboardParamName, property.PropertyType));
                }
            }
            return predicateInstance;
        } // BuildPredicateFromNodeData
        #endregion

    } // TLTLPredicateDataAsset

} // namespace TLTLUnity.Data
