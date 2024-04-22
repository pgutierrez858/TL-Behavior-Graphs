using System;
using System.Collections.Generic;
using System.Linq;
using TLTLPredicateBuilder.Utils;
using TLTLUnity.Data;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static System.Collections.Specialized.BitVector32;

namespace TLTLPredicateBuilder.Windows
{
    public class DictionaryEventArgs : EventArgs
    {

        public Type Type { get; }
        public List<string> Variables { get; }

        public string ChangedVariable { get; }

        public DictionaryEventArgs(Type type, List<string> variables, string changedVariable)
        {
            Type = type;
            Variables = variables;
            ChangedVariable = changedVariable;
        }
    } // DictionaryEventArgs

    public class TLTLBlackboardWindow : Blackboard
    {
        public event EventHandler<DictionaryEventArgs> EntryChanged;

        /// <summary>
        /// Diccionario que mantiene el conjunto de variables disponibles para cada uno 
        /// de los tipos soportados como parámetros de entrada de un predicado.
        /// </summary>
        private Dictionary<Type, List<string>> _availableVariables;

        /// <summary>
        /// Diccionario que mantiene las referencias a las secciones de cada uno de los tipos 
        /// de variable actualmente registrados en la pizarra.
        /// </summary>
        private Dictionary<Type, BlackboardSection> _blackboardSections;

        private Dictionary<BlackboardField, Tuple<Type, string>> _fieldVariables;

        public TLTLBlackboardWindow()
        {
            title = "Blackboard";
            subTitle = "Predicate variables";

            _availableVariables = new Dictionary<Type, List<string>>();
            _blackboardSections = new Dictionary<Type, BlackboardSection>();
            _fieldVariables = new Dictionary<BlackboardField, Tuple<Type, string>>();

            windowed = false;
            scrollable = true;
            SetPosition(new Rect(100, 50, 300, 400));
        }

        public void Initialize(List<TLTLBlackboardData.Entry> entries = null)
        {
            _availableVariables = new Dictionary<Type, List<string>>();
            _blackboardSections = new Dictionary<Type, BlackboardSection>();
            _fieldVariables = new Dictionary<BlackboardField, Tuple<Type, string>>();

            if (entries != null)
            {
                // rellenamos las variables disponibles con la información proporcionada
                foreach (var entry in entries)
                {
                    RegisterVariable(Type.GetType(entry.type), entry.name);
                }
            }

            this.AddStyleSheets("TLTLBlackboardStyles.uss");

            var addButton = panel.visualTree.Q<Button>(name: "addButton");
            if (addButton != null)
            {
                var cmManipulator = new ContextualMenuManipulator(ShowContextMenu);

                // evitar que el manipulador reaccione a nada (seremos nosotros los que decidamos cómo va a funcionar)
                cmManipulator.activators.Clear();
                // hacer que sólo reaccione a hacer click con el botón izquierdo del ratón
                cmManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                addButton.AddManipulator(cmManipulator);
            }

            editTextRequested += HandleEditText;

        } // Initialize

        private void HandleEditText(Blackboard _, VisualElement sourceElement, string newText)
        {
            // Note: sourceElement should be a BlackboardField
            BlackboardField blackboardField = sourceElement as BlackboardField;
            var fieldData = _fieldVariables[blackboardField];
            if (fieldData != null)
            {
                Type type = fieldData.Item1;
                string oldText = fieldData.Item2;
                _fieldVariables[blackboardField] = new Tuple<Type, string>(type, newText);
                blackboardField.text = newText;
                UpdateVariable(type, oldText, newText);
            }
        } // HandleEditText

        private void OnOptionSelected(Type type, string name)
        {
            RegisterVariable(type, name);
        } // OnOptionSelected

        private void ShowContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Int", _ => OnOptionSelected(typeof(int), "Int"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Boolean", _ => OnOptionSelected(typeof(bool), "Boolean"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Float", _ => OnOptionSelected(typeof(float), "Float"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("String", _ => OnOptionSelected(typeof(string), "String"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Color", _ => OnOptionSelected(typeof(Color), "Color"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Object", _ => OnOptionSelected(typeof(UnityEngine.Object), "Object"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Layer Mask", _ => OnOptionSelected(typeof(LayerMask), "LayerMask"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Enum", _ => OnOptionSelected(typeof(Enum), "Enum"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Vector2", _ => OnOptionSelected(typeof(Vector2), "Vector2"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Vector3", _ => OnOptionSelected(typeof(Vector3), "Vector3"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Vector4", _ => OnOptionSelected(typeof(Vector4), "Vector4"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Rect", _ => OnOptionSelected(typeof(Rect), "Rect"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Animation Curve", _ => OnOptionSelected(typeof(AnimationCurve), "AnimationCurve"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Bounds", _ => OnOptionSelected(typeof(Bounds), "Bounds"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Gradient", _ => OnOptionSelected(typeof(Gradient), "Gradient"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Quaternion", _ => OnOptionSelected(typeof(Quaternion), "Quaternion"), DropdownMenuAction.AlwaysEnabled);
        } // ShowContextMenu

        /// <summary>
        /// Trata de añadir una nueva sección a la pizarra, si es que no existiera todavía.
        /// Devuelve true si la sección no existiera y se pudo añadir con éxito.
        /// </summary>
        private bool RegisterSection(Type type)
        {
            if (!_blackboardSections.ContainsKey(type))
            {
                // la sección no existía antes, la añadimos
                BlackboardSection section = new BlackboardSection();
                section.title = $"{type.Name} Params";
                section.headerVisible = true;
                Add(section);

                _blackboardSections.Add(type, section);
                return true;
            }
            return false;
        } // RegisterSection

        // TODO: Chapuza
        public List<KeyValuePair<Type, List<string>>> Entries { get { return _availableVariables.ToList(); } }

        public List<string> GetAvailableVariablesOfType(Type type)
        {
            return _availableVariables.ContainsKey(type) ? _availableVariables[type] : new List<string>();
        } // GetAvailableVariablesOfType

        /// <summary>
        /// Registra una nueva variable del tipo especificado en la pizarra.
        /// </summary>
        /// <returns>true si la variable pudo añadirse con éxito</returns>
        public bool RegisterVariable(Type type, string name)
        {
            if (!_availableVariables.ContainsKey(type))
            {
                // el tipo de la variable no existía con anterioridad,
                // necesitamos añadir una nueva entrada tanto en la lista
                // de variables disponibles como en la lista de secciones.
                _availableVariables.Add(type, new List<string>());

                // vamos a tratar de añadir una sección asociada a esta variable
                // en el caso en el que ya existiera una sección dedicada a este 
                // tipo de variable, el método no hará nada (la comprobación está
                // incluída).
                RegisterSection(type);
            }

            // el nombre podría encontrarse en uso, en cuyo caso lo modificamos con un contador
            // formato: nombre (1), nombre (2), etc
            int copyCounter = 1;

            while (_availableVariables[type].Contains(name))
            {
                name = $"{name} ({copyCounter})";
                copyCounter++;
            }
            // la variable no existía antes, necesitamos crear una nueva entrada
            // en la lista de variables disponibles.
            _availableVariables[type].Add(name);

            // añadimos a la sección una nueva fila con los contenidos de la variable
            BlackboardSection section = _blackboardSections[type];
            Texture2D iconTexture = EditorGUIUtility.Load("Packages/dev.pablogutierrez.tl-behavior-graphs/Editor/Images/radio-button.png") as Texture2D;
            BlackboardField field = new BlackboardField(iconTexture, name, type.Name);
            _fieldVariables.Add(field, new Tuple<Type, string>(type, name));
            BlackboardRow row = new BlackboardRow(field, null);
            section.Add(row);

            OnEntryChanged(type, _availableVariables[type], null);
            return true;
        } // RegisterVariable

        /// <summary>
        /// Elimina la variable con nombre y tipo dados de la pizarra.
        /// </summary>
        /// <returns>true si la variable existía y pudo ser borrada con éxito</returns>
        public bool RemoveVariable(Type type, string name)
        {
            if (!_availableVariables.ContainsKey(type))
            {
                // no existía siquiera una entrada asociada al tipo de la variable.
                return false;
            }

            // intento de borrado de la variable
            if (!_availableVariables[type].Remove(name)) return false;

            // la variable fue eliminada con éxito, podemos proceder a eliminarla
            // de su sección. NOTA: si la sección queda vacía tras eliminar esta variable,
            // la eliminamos también.
            if (_availableVariables[type].Count == 0) _availableVariables.Remove(type);

            _blackboardSections[type].RemoveFromHierarchy();
            if (_blackboardSections[type].childCount == 0) _blackboardSections.Remove(type);

            OnEntryChanged(type, _availableVariables[type], name);
            return true;
        } // RemoveVariable

        public void UpdateVariable(Type type, string oldName, string newName)
        {
            // no estaba registrada la variable de tipo type y nombre oldName, nada que actualizar
            if (!_availableVariables.ContainsKey(type)) return;

            int varIndex = _availableVariables[type].FindIndex(v => v == oldName);
            if (varIndex == -1) return;

            // la nueva variable podría existir ya en la lista de variables, en cuyo caso no es válida
            if (_availableVariables[type].Contains(newName)) return;

            // está registrada la variable dentro de un tipo, actualizamos
            _availableVariables[type][varIndex] = newName;

            OnEntryChanged(type, _availableVariables[type], oldName);
        } // UpdateVariable

        /// <summary>
        /// Notifica del evento de cambio de una entrada en el diccionario de la pizarra.
        /// </summary>
        /// <param name="type">Categoría en la que se encuentra clasificado el cambio</param>
        /// <param name="variables">Nuevo valor de la lista de variables de la categoría</param>
        /// <param name="entryChanged">Variable que ha sido modificada o eliminada (si es añadida esto es null al no haber una "entrada previa"</param>
        protected virtual void OnEntryChanged(Type type, List<string> variables, string entryChanged)
        {
            // Create an instance of DictionaryEventArgs
            var eventArgs = new DictionaryEventArgs(type, new List<string>(variables), entryChanged);

            // Raise the event
            EntryChanged?.Invoke(this, eventArgs);
        } // OnValueAdded

    } // TLTLBlackboardWindow

} // namespace TLTLPredicateBuilder.Windows
