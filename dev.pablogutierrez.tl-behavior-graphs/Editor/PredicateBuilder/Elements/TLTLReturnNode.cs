using TLTLPredicateBuilder.Utils;
using TLTLPredicateBuilder.Windows;
using TLTLUnity.Data;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Orientation = UnityEditor.Experimental.GraphView.Orientation;

namespace TLTLPredicateBuilder.Elements
{
    public class TLTLReturnNode : Node
    {
        // --------------------------------------------------------------------
        //                      Propiedades públicas del nodo
        // --------------------------------------------------------------------
        #region Propiedades públicas del nodo
        public TLTLInputConnectionData Connection { get; set; }
        #endregion

        // --------------------------------------------------------------------
        //                      Propiedades privadas del nodo
        // --------------------------------------------------------------------
        #region Propiedades privadas del nodo
        protected TLTLPredicateGraphView _graphView;
        private Color _defaultBackgroundColor = new Color(133f / 255f, 195f / 255f, 80f / 255f);
        #endregion

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Disconnect Input Ports", actionEvent => DisconnectInputPorts());

            base.BuildContextualMenu(evt);
        } // BuildContextualMenu

        public virtual void Initialize(TLTLPredicateGraphView tltlGraphView, Vector2 position)
        {
            Connection = new TLTLInputConnectionData();

            SetPosition(new Rect(position, Vector2.zero));

            // Limitación de las capacidades del nodo para que no sea posible copiarlo, borrarlo o cambiar su nombre.
            capabilities &= ~Capabilities.Copiable;
            capabilities &= ~Capabilities.Deletable;
            capabilities &= ~Capabilities.Renamable;

            _graphView = tltlGraphView;

            mainContainer.AddToClassList("tltl-predicate-node__main-container");
            extensionContainer.AddToClassList("tltl-predicate-node__extension-container");
        } // Initialize

        public virtual void Draw()
        {
            /* TITLE CONTAINER */
            TLTLElementUtils.PopulateNodeTitleContainer(titleContainer, "Return", _defaultBackgroundColor, "logout.png");
            titleButtonContainer.style.display = DisplayStyle.None;
            outputContainer.style.display = DisplayStyle.None;

            /* INPUT CONTAINER */
            Port inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            inputPort.portName = "Output";
            inputPort.userData = Connection;

            inputContainer.Add(inputPort);

            VisualElement customDataContainer = new VisualElement();
            extensionContainer.Add(customDataContainer);
            RefreshExpandedState();
        } // Draw


        private void DisconnectInputPorts()
        {
            DisconnectPorts(inputContainer);
        } // DisconnectInputPorts

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
    } // TLTLReturnNode

} // namespace TLTLPredicateBuilder.Elements
