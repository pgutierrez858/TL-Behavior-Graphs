using UnityEditor;
using UnityEngine.UIElements;

namespace TLTLPredicateBuilder.Windows
{
    using System.IO;
    using TLTLPredicateBuilder.Data;
    using TLTLPredicateBuilder.Utils;
    using UnityEditor.Experimental.GraphView;
    using UnityEditor.UIElements;
    using UnityEngine;

    public class TLTLPredicateBuilderEditorWindow : EditorWindow
    {
        // --------------------------------------------------------------------
        //                      Propiedades Privadas de la clase
        // --------------------------------------------------------------------
        #region Propiedades Privadas de la clase

        // Referencia a la vista que contiene en horizontal al grafo y al blackboard.
        private TwoPaneSplitView _graphEditorSplitView;
        // Referencia a la vista de grafo representada en esta ventana.
        private TLTLPredicateGraphView _graphView;
        // Referencia del blackboard a utilizar en esta ventana
        private TLTLBlackboardWindow _blackboardWindow;
        private Blackboard _blackboard;
        // Nombre por defecto para los nuevos ficheros que serán creados a partir de esta ventana.
        private readonly string _defaultFileName = "PredicateFile";

        // Campo de texto representando el nombre del fichero donde se almacenará el grafo actual.
        private static TextField _fileNameTextField;
        // referencia al botón empleado para guardar los cambios en el fichero actual.
        private Button _saveButton;
        // referencia al botón empleado para compilar el fichero actual en un fichero de autómata.
        private Button _compileButton;

        public TLTLBlackboardWindow BlackboardWindow { get { return _blackboardWindow; } }

        #endregion

        // --------------------------------------------------------------------
        //          Configuración básica de la ventana e inicialización
        // --------------------------------------------------------------------
        #region Configuración básica de la ventana e inicialización

        [MenuItem("Window/AI Behavior Graphs/Behavior Graph Builder")]
        public static void Open()
        {
            GetWindow<TLTLPredicateBuilderEditorWindow>("Behavior Graph Builder");
        } // Open

        public static void Open(TLTLGraphSaveData graphData)
        {
            var window = GetWindow<TLTLPredicateBuilderEditorWindow>("Behavior Graph Builder");
            TLTLIOUtility.Initialize(window._graphView, _fileNameTextField.value);
            TLTLIOUtility.LoadGraphFromSave(graphData);
        } // Open

        public void OnEnable()
        {
            // _graphEditorSplitView = new TwoPaneSplitView(0, 1080f, TwoPaneSplitViewOrientation.Horizontal);
            // rootVisualElement.Add(_graphEditorSplitView);

            AddGraphView();
            AddToolbar();
            AddStyles();
        } // OnEnable

        private void AddGraphView()
        {
            _graphView = new TLTLPredicateGraphView(this);

            // ¡CUIDADO cuando se use con TwoPaneSplitView!
            _graphView.StretchToParentSize();

            rootVisualElement.Add(_graphView);
        } // AddGraphView

        private void AddToolbar()
        {
            Toolbar toolbar = new Toolbar();

            _fileNameTextField = TLTLElementUtils.CreateTextField(_defaultFileName, "File Name:", callback =>
            {
                _fileNameTextField.value = callback.newValue; // TODO: sanear el string
            });

            _saveButton = TLTLElementUtils.CreateButton("Save", () => Save());
            _compileButton = TLTLElementUtils.CreateButton("Compile Automaton", () => CompileAutomaton());

            Button loadButton = TLTLElementUtils.CreateButton("Load", () => Load());
            Button clearButton = TLTLElementUtils.CreateButton("Clear", () => Clear());
            Button resetButton = TLTLElementUtils.CreateButton("Reset", () => ResetGraph());


            toolbar.Add(_fileNameTextField);
            toolbar.Add(_saveButton);
            toolbar.Add(_compileButton);
            toolbar.Add(loadButton);
            toolbar.Add(clearButton);
            toolbar.Add(resetButton);

            toolbar.AddStyleSheets("TLTLToolbarStyles.uss");

            rootVisualElement.Add(toolbar);
        } // AddToolbar

        private void AddStyles()
        {
            // ¡CUIDADO!
            // El EditorWindow NO es un VisualElement; si queremos aplicar los estilos al contenido de 
            // esta ventana de editor necesitamos hacerlo sobre el nodo raíz de sus contenidos, por medio
            // de rootVisualElement.
            rootVisualElement.AddStyleSheets("TLTLPredicateVariables.uss");
        } // AddStyles

        #endregion

        // --------------------------------------------------------------------
        //          Métodos de utilidad para gestión del grafo
        // --------------------------------------------------------------------
        #region Métodos de utilidad para gestión del grafo
        private void Save()
        {
            if (string.IsNullOrEmpty(_fileNameTextField.value))
            {
                EditorUtility.DisplayDialog("Invalid file name.", "Please ensure the file name you've typed in is valid.", "OK");
                return;
            }

            TLTLIOUtility.Initialize(_graphView, _fileNameTextField.value);
            TLTLIOUtility.Save();
        } // Save

        private void CompileAutomaton()
        {
            if (string.IsNullOrEmpty(_fileNameTextField.value))
            {
                EditorUtility.DisplayDialog("Invalid file name.", "Please ensure the file name you've typed in is valid.", "OK");
                return;
            }

            TLTLIOUtility.Initialize(_graphView, _fileNameTextField.value);
            TLTLIOUtility.CompileAutomaton();
        } // CompileAutomaton

        private void Load()
        {
            string filePath = EditorUtility.OpenFilePanel("TLTL Predicate Graphs", "Assets/TLTLPredicateBuilder/Graphs", "asset");

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            Clear();

            TLTLIOUtility.Initialize(_graphView, Path.GetFileNameWithoutExtension(filePath));
            TLTLIOUtility.Load();
        } // Load

        private void Clear()
        {
            _graphView.ClearGraph();
        } // Clear

        private void ResetGraph()
        {
            Clear();

            UpdateFileName(_defaultFileName);
        } // ResetGraph

        public static void UpdateFileName(string newFileName)
        {
            _fileNameTextField.value = newFileName;
        } // UpdateFileName

        public void EnableSaving()
        {
            _saveButton.SetEnabled(true);
        } // EnableSaving

        public void DisableSaving()
        {
            _saveButton.SetEnabled(false);
        } // DisableSaving

        #endregion

    } // TLTLPredicateBuilderEditorWindow

} // namespace TLTLPredicateBuilder.Windows
