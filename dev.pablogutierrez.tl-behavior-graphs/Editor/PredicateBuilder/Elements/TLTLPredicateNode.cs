using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using TLTLPredicateBuilder.Utils;
using TLTLPredicateBuilder.Windows;
using System.Reflection;
using TLTLUnity.Data;
using TLTLCore.Framework;
using UnityEditor;

namespace TLTLPredicateBuilder.Elements
{
    public class TLTLPredicateNode : Node
    {
        // --------------------------------------------------------------------
        //                      Propiedades públicas del nodo
        // --------------------------------------------------------------------
        #region Propiedades públicas del nodo
        public string ID { get; set; }
        public List<TLTLInputConnectionData> Connections { get; set; }
        public List<TLTLInputParamData> InputParams { get; set; }
        public Type PredicateType { get; set; }
        #endregion

        // --------------------------------------------------------------------
        //                      Propiedades privadas del nodo
        // --------------------------------------------------------------------
        #region Propiedades privadas del nodo
        protected TLTLPredicateGraphView _graphView;
        private Color _compositeBackgroundColor = new Color(189f / 255f, 122f / 255f, 203f / 255f);
        private Color _leafBackgroundColor = new Color(252f / 255f, 136f / 255f, 11f / 255f);
        #endregion

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Disconnect Input Ports", actionEvent => DisconnectInputPorts());
            evt.menu.AppendAction("Disconnect Output Ports", actionEvent => DisconnectOutputPorts());

            base.BuildContextualMenu(evt);
        } // BuildContextualMenu

        public virtual void Initialize(TLTLPredicateGraphView tltlGraphView, Type predicateType, Vector2 position)
        {
            ID = Guid.NewGuid().ToString();
            Connections = new List<TLTLInputConnectionData>();
            InputParams = new List<TLTLInputParamData>();

            PredicateType = predicateType;

            SetPosition(new Rect(position, Vector2.zero));

            _graphView = tltlGraphView;

            mainContainer.AddToClassList("tltl-predicate-node__main-container");
            extensionContainer.AddToClassList("tltl-predicate-node__extension-container");
        } // Initialize

        /// <summary>
        /// Returns the display name for this node.
        /// Defaults to the predicate type if not overwritten.
        /// </summary>
        public virtual string GetDisplayName()
        {
            string[] nameParts = TLTLPredicate.GetPredicateDisplayName(PredicateType).Split('/');
            return nameParts[nameParts.Length-1];
        } // GetNodeName

        public virtual void Draw()
        {
            titleButtonContainer.style.display = DisplayStyle.None;

            /* OUTPUT CONTAINER */
            Port outputPort = this.CreatePort(typeof(float), GetOutputPortName(), Orientation.Horizontal, Direction.Output, Port.Capacity.Multi);
            outputContainer.Add(outputPort);

            /* PARAMETERS CONTAINER */
            VisualElement customDataContainer = new VisualElement();
            var inProperties = TLTLPredicate.GetInProperties(PredicateType);

            // aquí tenemos dos casos a tratar: por un lado es posible que estemos pintando con las conexiones
            // ya inicializadas porque estamos leyendo de un fichero, y por otro podría ser que este sea un nodo
            // nuevo, en cuyo caso no hay conexiones con información en este momento.
            bool initializeConnections = Connections.Count == 0;
            bool initializeInputParams = InputParams.Count == 0;
            bool isConditionNode = true;

            foreach (var property in inProperties)
            {
                InParam param = property.GetCustomAttribute<InParam>();

                if (property.PropertyType == typeof(TLTLPredicate))
                {
                    isConditionNode = false;
                    // Necesitamos un puerto de entrada
                    if (initializeConnections)
                    {
                        this.CreateAndAddInputPort(param.Name, property.Name);
                    }
                    else
                    {
                        Port inputPort = this.CreatePort(typeof(float), param.Name, Orientation.Horizontal, Direction.Input, Port.Capacity.Single);
                        // buscamos la conexión que se corresponda con el parámetro de entrada correspondiente
                        TLTLInputConnectionData connectionData = Connections.Find(c => c.InParamName == property.Name);
                        if (connectionData != null)
                        {
                            //  conexión encontrada, así que la asociamos al puerto de entrada que acabamos de crear
                            inputPort.userData = connectionData;
                        }
                        inputContainer.Add(inputPort);
                    }
                }
                else
                {
                    var availableVariables = _graphView.BlackboardWindow.GetAvailableVariablesOfType(property.PropertyType);
                    // necesitamos un parámetro a especificar en la sección de custom data del nodo
                    StringDropdown dropdown = new StringDropdown(param.Name, availableVariables, (evt) => OnPropertySelected(property.Name, evt));
                    dropdown.AddParameterButtonClicked += () => _graphView.BlackboardWindow.RegisterVariable(property.PropertyType, property.Name);
                    // TODO: mucho cleanup
                    _graphView.BlackboardWindow.EntryChanged += (_, evt) => {
                        if (evt.Type == property.PropertyType) {
                            // Update Options mantiene la selección actual de parámetro
                            // y contempla el caso en el que desaparece la variable seleccionada,
                            // pero no es capaz de reflejar el caso en el que la variable actualmente
                            // seleccionada es modificada con un nuevo nombre.
                            dropdown.UpdateOptions(evt.Variables);
                        }
                    };
                    customDataContainer.Add(dropdown);

                    if (initializeInputParams)
                    {
                        // necesitamos una entrada vacía en la lista de variables para este input
                        // esto es: con el nombre de la propiedad pero sin entrada de la pizarra asociada.
                        InputParams.Add(new TLTLInputParamData() { InParamName = property.Name });
                    }
                    else
                    {
                        // ya hay información sobre qué variable está cableada a este input desde la pizarra
                        var inputParam = InputParams.Find(p => p.InParamName == property.Name);

                        if (inputParam != null && inputParam.BlackboardParamName != null && inputParam.BlackboardParamName.Length > 0)
                        {
                            // hay un parámetro registrado con el mismo nombre que el que necesitamos (debería ser siempre el caso)
                            dropdown.SetSelection(inputParam.BlackboardParamName);
                        }
                        else
                        {
                            dropdown.SetSelection(StringDropdown.NO_SELECTION);
                        }
                    }
                }
            }
            extensionContainer.Add(customDataContainer);

            /* TITLE CONTAINER */
            // El estilo depende de si es un nodo de condición o no.
            if (isConditionNode)
                TLTLElementUtils.PopulateNodeTitleContainer(titleContainer, GetDisplayName(), _leafBackgroundColor, "function.png");
            else
                TLTLElementUtils.PopulateNodeTitleContainer(titleContainer, GetDisplayName(), _compositeBackgroundColor, "consolidate.png");

            // Importante para pintar extensionContainer
            RefreshExpandedState();
        } // Draw

        private void OnPropertySelected(string propertyName, ChangeEvent<string> evt)
        {
            // buscamos el parámetro de entrada sobre el que se ha realizado la selección de variable de la pizarra
            var inParam = InputParams.Find(p => p.InParamName == propertyName);
            if (inParam == null) return;

            // El parámetro de nombre propertyName indexado bajo paramType ahora está asociado a la variable evt.newValue del blackboard
            // Se ha cancelado la selección
            if (evt.newValue == StringDropdown.NO_SELECTION)
            {
                inParam.BlackboardParamName = null;
            }
            else
            {
                inParam.BlackboardParamName = evt.newValue;
            }
        } // OnPropertySelected

        /// <summary>
        /// Crea un nuevo puerto de input con el nombre dado, capacidad Single, de tipo float, y lo inicializa
        /// con un nuevo objeto userData. Adicionalmente, añade los datos de la conexión a la lista de conexiones
        /// del nodo en el caso en el que userData sea null (si userData es null, no había un registro previo a cargar,
        /// mientras que si tiene valor denota que la conexión ya se encontraba presente en los datos del nodo).
        /// Devuelve el puerto creado como resultado de la función.
        /// </summary>
        protected virtual Port CreateAndAddInputPort(string paramName, string propertyName, object userData = null)
        {
            // por defecto este objeto de datos de conexión va a tener un NodeID vacío al 
            // no estar conectado con ningún otro nodo, SALVO QUE se haya introducido un valor
            // explícito para userData como parámetro (por ejemplo, si estuviéramos pintando
            // un nodo al cargar el grafo con datos ya conocidos sobre las conexiones).
            TLTLInputConnectionData connectionData = userData != null ? (TLTLInputConnectionData) userData : new TLTLInputConnectionData() { InParamName = propertyName };

            // Connections albergará los datos de todos los puertos de entrada asociados a 
            // este nodo.
            if (userData == null)
            {
                // userData == null denota que no disponíamos de un registro previo de conexión para este puerto.
                Connections.Add(connectionData);
            }

            // creamos un nuevo puerto de entrada con el nombre especificado y las propiedades prestablecidas.
            Port inputPort = this.CreatePort(typeof(float), paramName, Orientation.Horizontal, Direction.Input, Port.Capacity.Single);

            // ese nuevo puerto necesita tener acceso a la información de la conexión que representa.
            inputPort.userData = connectionData;

            // finalmente añadimos el puerto recién creado al contenedor de inputs del nodo
            inputContainer.Add(inputPort);

            // y devolvemos una referencia
            return inputPort;
        } // CreateAndAddInputPort

        /// <summary>
        /// Método para especificar
        /// qué quieren mostrar como nombre de output del predicado. Por ejemplo, en un nodo NOT,
        /// podríamos querer poner como label del output ~X y X para el input por claridad.
        /// </summary>
        protected virtual string GetOutputPortName()
        {
            return TLTLPredicate.GetOutputExpression(PredicateType);
        } // GetOutputPortName

        public void DisconnectAllPorts()
        {
            DisconnectInputPorts();
            DisconnectOutputPorts();
        } // DisconnectAllPorts

        private void DisconnectInputPorts()
        {
            DisconnectPorts(inputContainer);
        } // DisconnectInputPorts

        private void DisconnectOutputPorts()
        {
            DisconnectPorts(outputContainer);
        } // DisconnectOutputPorts

        private void DisconnectPorts(VisualElement container)
        {
            foreach (Port port in container.Children())
            {
                if (!port.connected)
                {
                    continue;
                }

                _graphView.DeleteElements(port.connections);
            }
        } // DisconnectPorts

    } // TLTLPredicateNode

} // namespace TLTLPredicateBuilder.Elements

