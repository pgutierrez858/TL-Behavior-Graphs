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
        //                      Propiedades p�blicas del nodo
        // --------------------------------------------------------------------
        #region Propiedades p�blicas del nodo
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

            // aqu� tenemos dos casos a tratar: por un lado es posible que estemos pintando con las conexiones
            // ya inicializadas porque estamos leyendo de un fichero, y por otro podr�a ser que este sea un nodo
            // nuevo, en cuyo caso no hay conexiones con informaci�n en este momento.
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
                        // buscamos la conexi�n que se corresponda con el par�metro de entrada correspondiente
                        TLTLInputConnectionData connectionData = Connections.Find(c => c.InParamName == property.Name);
                        if (connectionData != null)
                        {
                            //  conexi�n encontrada, as� que la asociamos al puerto de entrada que acabamos de crear
                            inputPort.userData = connectionData;
                        }
                        inputContainer.Add(inputPort);
                    }
                }
                else
                {
                    var availableVariables = _graphView.BlackboardWindow.GetAvailableVariablesOfType(property.PropertyType);
                    // necesitamos un par�metro a especificar en la secci�n de custom data del nodo
                    StringDropdown dropdown = new StringDropdown(param.Name, availableVariables, (evt) => OnPropertySelected(property.Name, evt));
                    dropdown.AddParameterButtonClicked += () => _graphView.BlackboardWindow.RegisterVariable(property.PropertyType, property.Name);
                    // TODO: mucho cleanup
                    _graphView.BlackboardWindow.EntryChanged += (_, evt) => {
                        if (evt.Type == property.PropertyType) {
                            // Update Options mantiene la selecci�n actual de par�metro
                            // y contempla el caso en el que desaparece la variable seleccionada,
                            // pero no es capaz de reflejar el caso en el que la variable actualmente
                            // seleccionada es modificada con un nuevo nombre.
                            dropdown.UpdateOptions(evt.Variables);
                        }
                    };
                    customDataContainer.Add(dropdown);

                    if (initializeInputParams)
                    {
                        // necesitamos una entrada vac�a en la lista de variables para este input
                        // esto es: con el nombre de la propiedad pero sin entrada de la pizarra asociada.
                        InputParams.Add(new TLTLInputParamData() { InParamName = property.Name });
                    }
                    else
                    {
                        // ya hay informaci�n sobre qu� variable est� cableada a este input desde la pizarra
                        var inputParam = InputParams.Find(p => p.InParamName == property.Name);

                        if (inputParam != null && inputParam.BlackboardParamName != null && inputParam.BlackboardParamName.Length > 0)
                        {
                            // hay un par�metro registrado con el mismo nombre que el que necesitamos (deber�a ser siempre el caso)
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
            // El estilo depende de si es un nodo de condici�n o no.
            if (isConditionNode)
                TLTLElementUtils.PopulateNodeTitleContainer(titleContainer, GetDisplayName(), _leafBackgroundColor, "function.png");
            else
                TLTLElementUtils.PopulateNodeTitleContainer(titleContainer, GetDisplayName(), _compositeBackgroundColor, "consolidate.png");

            // Importante para pintar extensionContainer
            RefreshExpandedState();
        } // Draw

        private void OnPropertySelected(string propertyName, ChangeEvent<string> evt)
        {
            // buscamos el par�metro de entrada sobre el que se ha realizado la selecci�n de variable de la pizarra
            var inParam = InputParams.Find(p => p.InParamName == propertyName);
            if (inParam == null) return;

            // El par�metro de nombre propertyName indexado bajo paramType ahora est� asociado a la variable evt.newValue del blackboard
            // Se ha cancelado la selecci�n
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
        /// con un nuevo objeto userData. Adicionalmente, a�ade los datos de la conexi�n a la lista de conexiones
        /// del nodo en el caso en el que userData sea null (si userData es null, no hab�a un registro previo a cargar,
        /// mientras que si tiene valor denota que la conexi�n ya se encontraba presente en los datos del nodo).
        /// Devuelve el puerto creado como resultado de la funci�n.
        /// </summary>
        protected virtual Port CreateAndAddInputPort(string paramName, string propertyName, object userData = null)
        {
            // por defecto este objeto de datos de conexi�n va a tener un NodeID vac�o al 
            // no estar conectado con ning�n otro nodo, SALVO QUE se haya introducido un valor
            // expl�cito para userData como par�metro (por ejemplo, si estuvi�ramos pintando
            // un nodo al cargar el grafo con datos ya conocidos sobre las conexiones).
            TLTLInputConnectionData connectionData = userData != null ? (TLTLInputConnectionData) userData : new TLTLInputConnectionData() { InParamName = propertyName };

            // Connections albergar� los datos de todos los puertos de entrada asociados a 
            // este nodo.
            if (userData == null)
            {
                // userData == null denota que no dispon�amos de un registro previo de conexi�n para este puerto.
                Connections.Add(connectionData);
            }

            // creamos un nuevo puerto de entrada con el nombre especificado y las propiedades prestablecidas.
            Port inputPort = this.CreatePort(typeof(float), paramName, Orientation.Horizontal, Direction.Input, Port.Capacity.Single);

            // ese nuevo puerto necesita tener acceso a la informaci�n de la conexi�n que representa.
            inputPort.userData = connectionData;

            // finalmente a�adimos el puerto reci�n creado al contenedor de inputs del nodo
            inputContainer.Add(inputPort);

            // y devolvemos una referencia
            return inputPort;
        } // CreateAndAddInputPort

        /// <summary>
        /// M�todo para especificar
        /// qu� quieren mostrar como nombre de output del predicado. Por ejemplo, en un nodo NOT,
        /// podr�amos querer poner como label del output ~X y X para el input por claridad.
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

