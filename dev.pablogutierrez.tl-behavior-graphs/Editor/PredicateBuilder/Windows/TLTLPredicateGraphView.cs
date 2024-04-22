using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TLTLPredicateBuilder.Windows
{
    using Elements;
    using TLTLUnity.Data;
    using Utils;

    public class TLTLPredicateGraphView : GraphView
    {
        public TLTLPredicateGraphView(TLTLPredicateBuilderEditorWindow editorWindow)
        {
            _editorWindow = editorWindow;

            AddManipulators();
            AddSearchWindow();
            AddGridBackground();

            OnGraphViewChanged();

            AddStyles();

            AddBlackboard();

            AddReturnNode();
            _returnNode.Draw();
        } // TLTLPredicateGraphView

        // --------------------------------------------------------------------
        //                      Propiedades Privadas de la clase
        // --------------------------------------------------------------------
        #region Propiedades Privadas de la clase

        private TLTLReturnNode _returnNode;
        private TLTLSearchWindow _searchWindow;
        private TLTLPredicateBuilderEditorWindow _editorWindow;
        private TLTLBlackboardWindow _blackboard;

        #endregion

        // --------------------------------------------------------------------
        //                      Manipuladores adicionales
        // --------------------------------------------------------------------
        #region Manipuladores adicionales

        private void AddManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        } // AddManipulators

        private void AddSearchWindow()
        {
            if (_searchWindow == null)
            {
                _searchWindow = ScriptableObject.CreateInstance<TLTLSearchWindow>();
                _searchWindow.Initialize(this);
            }

            /*
             * Este callback ocurre cada vez que pulsamos la tecla de espacio teniendo el focus sobre el grafo.
             * Tambi�n sucede si hacemos click derecho y seleccionamos Create Node como opci�n en el men� contextual,
             * por lo que es posible mantener de forma simultanea la ventana de b�squeda y el men� contextual.
             */
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
        } // AddSearchWindow


        private IManipulator CreateNodeContextualMenu(string actionTitle, Type predicateType)
        {
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(
                menuEvent => menuEvent.menu.AppendAction(actionTitle, actionEvent => AddElement(CreateNode(predicateType, GetLocalMousePosition(actionEvent.eventInfo.localMousePosition))))
            );

            return contextualMenuManipulator;
        } // CreateNodeContextualMenu

        /// <summary>
        /// Crea un nodo del tipo pasado como par�metro en la posici�n especificada de la vista.
        /// </summary>
        /// <param name="nodeType">Tipo de predicado que queremos crear.</param>
        /// <param name="position">Posici�n en la vista sobre la que queremos colocar el nuevo nodo.</param>
        public TLTLPredicateNode CreateNode(Type nodeType, Vector2 position, bool drawOnCreation = true)
        {
            TLTLPredicateNode node = new TLTLPredicateNode();
            node.Initialize(this, nodeType, position);

            if (drawOnCreation)
            {
                node.Draw();
            }

            return node;
        } // CreateNode

        /// <summary>
        /// Crea un nuevo nodo de retorno de predicado en la posici�n especificada de la vista.
        /// <param name="position">Posici�n de la vista sobre la que queremos colocar el nuevo nodo.</param>
        public TLTLReturnNode CreateReturnNode(Vector2 position, bool drawOnCreation = true)
        {
            TLTLReturnNode returnNode = new TLTLReturnNode();
            returnNode.Initialize(this, position);

            if (drawOnCreation)
            {
                returnNode.Draw();
            }

            return returnNode;
        } // CreateReturnNode

        /// <summary>
        /// A�ade un nodo de retorno al grafo de forma segura. Esto asegura que se elimine cualquier nodo
        /// de retorno previo que pudiera existir en el grafo anteriormente y que se cree un nuevo nodo
        /// con valores por defecto en el caso de que no se haya proporcionado un nodo por par�metro.
        /// NO llama a Draw a la hora de a�adir el nodo.
        /// </summary>
        /// <param name="node"></param>
        public void AddReturnNode(TLTLReturnNode node = null)
        {
            if (_returnNode != null)
            {
                // borramos el elemento anterior, si es que lo hubiera
                RemoveElement(_returnNode);
                _returnNode = null;
            }
            _returnNode = node ?? CreateReturnNode(new Vector2(800, 450), false);
            AddElement(_returnNode);
        } // ResetReturnNode

        public void AddBlackboard()
        {
            _blackboard = new TLTLBlackboardWindow();
            AddElement(_blackboard);
        } // AddBlackboard
        #endregion

        // --------------------------------------------------------------------
        //                  Estilos aplicados sobre la vista
        // --------------------------------------------------------------------
        #region Estilos aplicados sobre la vista

        /// <summary>
        /// Inserta un fondo de cuadr�cula en la primera posici�n de la vista, estableciendo su
        /// tama�o para que se ajuste a las medidas del contenedor padre.
        /// </summary>
        private void AddGridBackground()
        {
            GridBackground gridBackground = new GridBackground();
            gridBackground.StretchToParentSize();
            Insert(0, gridBackground);
        } // AddGridBackground

        /// <summary>
        /// Carga los estilos definidos en ficheros .uss de la carpeta "Editor Default Resources" y
        /// los aplica sobre la vista. Estos estilos se a�aden a los styleSheets de la ra�z, por lo que
        /// estar�n disponibles en todos los componentes anidados como hijos. En particular a�adimos las
        /// hojas de estilo para la GraphView y los nodos.
        /// </summary>
        private void AddStyles()
        {
            this.AddStyleSheets(
                "TLTLPredicateGraphViewStyle.uss", // graphViewStyleSheet
                "TLTLPredicateNodeStyle.uss" // nodeStyleSheet
            );
        } // AddStyles
        #endregion

        // --------------------------------------------------------------------
        //                  M�todos sobrescritos de GraphView
        // --------------------------------------------------------------------
        #region M�todos sobrescritos de GraphView
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            // GetCompatiblePorts es un m�todo llamado por el editor gr�fico cada vez que se pulsa sobre un puerto
            // para intentar conectarlo con otro de los puertos dentro de la vista. Por defecto, no permite la conexi�n
            // con ning�n otro puerto, y es ah� donde entra en juego la implementaci�n que hagamos de este m�todo para 
            // habilitar los puertos que queramos. En concreto este m�todo recibe como par�metro un startPort, que es
            // la referencia al puerto sobre el que se ha pulsado en el editor, y devuelve un listado de puertos de la vista
            // con los que permitimos una conexi�n. La clase GraphView proporciona una referencia al listado de puertos disponibles
            // en la vista por medio del atributo ports, que podemos utilizar como base para iterar y filtrar aquellos puertos
            // que queramos mostrar como disponibles.
            List<Port> compatiblePorts = new List<Port>();

            // Check de cada uno de los puertos disponibles en el grafo para determinar cu�les pueden conectarse con el puerto
            // de origen (startPort). Comprobamos en concreto orientaci�n y autoreferencias.
            ports.ForEach(port =>
            {
                // caso 1: el puerto de partida es este puerto => no permitir autoenlaces
                if (startPort == port) return;

                // caso 2: el puerto de partida y el de destino pertenecen al mismo nodo => no permitimos recurrencia
                if (startPort.node == port.node) return;

                // caso 3: el puerto de partida tiene la misma direcci�n (entrada/ salida) que el puerto de destino => no tiene sentido
                if (startPort.direction == port.direction) return;

                // cualquier otro caso es aceptable (un nodo de salida con uno de entrada o viceversa, entre nodos distintos)
                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        } // GetCompatiblePorts
        #endregion

        /// <summary>
        /// M�todo de conveniencia para configurar c�mo reacciona la vista ante cualquier modificaci�n
        /// realizada por el usuario en sus interacciones con ella. En particular sobrescribe el m�todo/ evento
        /// graphViewChanged.
        /// </summary>
        private void OnGraphViewChanged()
        {
            graphViewChanged = (changes) =>
            {
                // Una propiedad importante dentro del payload del cambio es edgesToCreate, que permite
                // fijarnos en qu� aristas se han decidido crear en esta modificaci�n.
                if (changes.edgesToCreate != null)
                {
                    // Para cada una de estas aristas, vamos a asegurarnos de que el objeto que almacena la informaci�n
                    // de la conexi�n de entrada del nodo destino guarda una referencia al ID del nodo de origen para poder
                    // recrear adecuadamente los edges del grafo.
                    foreach (Edge edge in changes.edgesToCreate)
                    {
                        // b�sicamente, nos limitamos a hidratar el campo NodeID asociado a los datos de conexi�n
                        // del puerto de entrada con el ID del nodo de origen.
                        TLTLPredicateNode sourceNode = (TLTLPredicateNode)edge.output.node;
                        TLTLInputConnectionData connectionData = (TLTLInputConnectionData)edge.input.userData;
                        connectionData.NodeID = sourceNode.ID;
                    }
                }

                // En el caso de que lo que se quiera sea eliminar un elemento de tipo arista del grafo,
                // basta con ir recorriendo elemento a elemento la lista de elementsToRemove buscando objetos
                // de tipo Edge (lamentablemente no existe un edgesToRemove en contraparte a edgesToCreate),
                // y asegurarnos de que los datos de conexi�n asociados a dicha arista se limpian, eliminando 
                // la referencia al nodo de origen de la misma.
                if (changes.elementsToRemove != null)
                {
                    Type edgeType = typeof(Edge);

                    foreach (GraphElement element in changes.elementsToRemove)
                    {
                        if (element.GetType() != edgeType)
                        {
                            continue;
                        }

                        Edge edge = (Edge)element;
                        TLTLInputConnectionData connectionData = (TLTLInputConnectionData)edge.input.userData;
                        // Borramos el ID del Nodo fuente.
                        connectionData.NodeID = "";
                    }
                }

                return changes;
            };
        } // OnGraphViewChanged


        // --------------------------------------------------------------------
        //                  M�todos de conveniencia
        // --------------------------------------------------------------------
        #region M�todos de conveniencia

        /// <summary>
        /// Transforma la posici�n global del panel de la vista (esto es, las coordenadas dentro de la
        /// ventana) en una posici�n local al grafo independiente de la global. Esto significa que si nuestra
        /// mousePosition es, por ejemplo (800, 600), desplazamos la vista hacia la derecha dejando el rat�n en 
        /// el mismo sitio, y volvemos a preguntar por la posici�n, volveremos a recibir ese (800, 600), cuando
        /// en realidad lo que nos har� falta a la hora de instanciar un nodo es saber la posici�n desde el origen 
        /// del grafo (local). Este m�todo se encarga de realizar esta transformaci�n realizando una llamada auxiliar
        /// al m�todo contentViewContainer.WorldToLocal.
        /// </summary>
        public Vector2 GetLocalMousePosition(Vector2 mousePosition, bool isSearchWindow = false)
        {
            Vector2 worldMousePosition = mousePosition;

            if (isSearchWindow)
            {
                worldMousePosition -= _editorWindow.position.position;
            }

            Vector2 localMousePosition = contentViewContainer.WorldToLocal(worldMousePosition);
            return localMousePosition;
        } // GetLocalMousePosition
        #endregion

        public void ClearGraph()
        {
            graphElements.ForEach(RemoveElement);
            AddReturnNode();
            _returnNode.Draw();
            AddBlackboard();
        } // ClearGraph

        public TLTLBlackboardWindow BlackboardWindow { get { return _blackboard; } }

    } // TLTLPredicateGraphView

} // namespace TLTLPredicateBuilder.Windows
