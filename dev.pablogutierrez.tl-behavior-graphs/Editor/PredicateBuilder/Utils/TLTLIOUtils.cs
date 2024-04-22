using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using TLTLPredicateBuilder.Elements;
using System.Diagnostics;
using System.Text;
using TLTLCore.Framework;
using TLTLPredicateBuilder.Data;
using TLTLUnity.Data;
using TLTLPredicateBuilder.Windows;
using static TLTLPredicateBuilder.Data.TLTLGraphSaveData;
using UnityEditor.Graphs;
using Node = UnityEditor.Experimental.GraphView.Node;
using Edge = UnityEditor.Experimental.GraphView.Edge;

namespace TLTLPredicateBuilder.Utils
{
    public static class TLTLIOUtility
    {
        private static TLTLPredicateGraphView _graphView;

        /// <summary>
        /// Nombre del archivo de grafo cargado en el momento actual.
        /// </summary>
        private static string _graphFileName;
        /// <summary>
        /// Directorio raíz de la carpeta donde vamos a almacenar los ficheros 
        /// y scriptable objects de predicados.
        /// </summary>
        private static string _containerFolderPath;

        /// <summary>
        /// Directorio raíz de la carpeta donde vamos a almacenar los ficheros 
        /// y scriptable objects de grafos.
        /// </summary>
        private static string _graphDataFolderPath;

        /// <summary>
        /// Directorio raíz de la carpeta donde vamos a almacenar los ficheros
        /// y scriptable objects de autómatas
        /// </summary>
        private static string _automatonDataFolderPath;

        /// <summary>
        /// Listado de nodos del grafo actual.
        /// </summary>
        private static List<TLTLPredicateNode> _nodes;

        /// <summary>
        /// Nodo de retorno del grafo. Siempre debe haber uno y sólo uno de ellos para que tenga sentido la estructura.
        /// </summary>
        private static TLTLReturnNode _returnNode;

        private static Dictionary<string, Node> _loadedNodes;

        /// <summary>
        /// Inicializa el gestor de utilidades IO con la información de una vista de grafo y su nombre 
        /// asociado, creando nuevos diccionarios para nodos cargados y predicados creados, y estableciendo
        /// la ruta de almacenamiento de assets en el directorio correspondiente de la carpeta Assets.
        /// </summary>
        public static void Initialize(TLTLPredicateGraphView TLTLPredicateGraphView, string graphName)
        {
            _graphView = TLTLPredicateGraphView;

            _graphFileName = graphName;

            string builderPath = "Assets/TLTLPredicateBuilder";
            _containerFolderPath = $"{builderPath}/Predicates";
            _graphDataFolderPath = $"{builderPath}/Graphs";
            _automatonDataFolderPath = $"{builderPath}/Automata";

            _nodes = new List<TLTLPredicateNode>();

            _returnNode = null;

            _loadedNodes = new Dictionary<string, Node>();
        } // Initialize

        public static void Save()
        {
            CreateDefaultFolders();

            GetElementsFromGraphView();

            // asset para almacenar la información del grafo (configuración gráfica y de editor incluída)
            TLTLGraphSaveData graphAsset = CreateAsset<TLTLGraphSaveData>(_graphDataFolderPath, _graphFileName);
            graphAsset.Initialize(_graphFileName);

            // asset para almacenar la información "pura" del grafo, sin datos del editor
            TLTLPredicateDataAsset predicateAsset = CreateAsset<TLTLPredicateDataAsset>(_containerFolderPath, _graphFileName);
            predicateAsset.Initialize();

            graphAsset.PredicateAsset = predicateAsset;

            SaveReturnNode(graphAsset, predicateAsset);
            SaveNodes(graphAsset, predicateAsset);

            SaveBlackboardData(predicateAsset);

            SaveAsset(graphAsset);
            SaveAsset(predicateAsset);
        } // Save

        /// <summary>
        /// Dado un nodo <see cref="TLTLPredicateNode"/>, la lista de nodos en el grafo, y un nombre de propiedad,
        /// devuelve el nodo del grafo que se conecta al parámetro de entrada del nodo indicado con el nombre propertyName.
        /// </summary>
        public static TLTLPredicateNode ExtractInputNodeFromPropertyName(TLTLPredicateNode node, List<TLTLPredicateNode> nodes, string propertyName)
        {
            // obtención de las propiedades de entrada del predicado actual
            var inProperties = TLTLPredicate.GetInProperties(node.PredicateType);

            // propiedad con el nombre especificado
            var property = inProperties.Find(p => p.Name == propertyName);

            // la propiedad debe existir, y DEBE ser de tipo TLTLPredicate (son las únicas vinculadas a un nodo de entrada,
            // las demás se gestionan por medio de variables de pizarra. Esto no es un error ni requiere un mensaje de warning
            // porque es de esperar que podamos pedir una propiedad que no es válida, simplemente diremos que no existe una propiedad
            // adecuada.
            if (property == null || property.PropertyType != typeof(TLTLPredicate)) return null;

            // conexión de entrada del nodo que se corresponde con el nombre de la propiedad (es la que tiene el ID)
            TLTLInputConnectionData inputConnection = node.Connections.Find(c => c.InParamName == property.Name);

            // debe existir dicha conexión, si no es que hemos inicializado mal el nodo,
            // y debe tener asignado un nodo de entrada, si no el grafo está incompleto
            if (inputConnection == null)
            {
                UnityEngine.Debug.LogWarningFormat("La propiedad {0} del nodo con ID {1} no tiene una conexión de entrada asociada", propertyName, node.ID);
                return null;
            }
            else if (inputConnection.NodeID == null)
            {
                UnityEngine.Debug.LogWarningFormat("La propiedad {0} del nodo con ID {1} está inicializada, pero no está conectada a ningún otro nodo", propertyName, node.ID);
                return null;
            }

            // nodo de entrada vinculado con la conexión anterior
            TLTLPredicateNode inputNode = nodes.Find(n => n.ID == inputConnection.NodeID);

            return inputNode;
        } // ExtractInputNodeFromPropertyName

        /// <summary>
        /// Construye una fórmula LTL a partir de un predicado; para cada uno de los predicados atómicos encontrados
        /// en el predicado de entrada, el método asigna un nuevo GUID que pasa a estar registrado en el mapa guidToPredicate,
        /// que vincula el guid asignado con el predicado original de cara a poder recuperarlo cuando sea necesario. Esto
        /// es un requisito para poder abstraer las operaciones de transformación de predicados de las particularidades 
        /// de cada predicado en Unity: en estas operaciones sólo nos interesa saber que hay un predicado A, B, C, pero
        /// en ningún momento nos vamos a preocupar de qué significa ni de su grado de robustez, estas son preocupaciones
        /// del lado de ejecución.
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="guidToPredicate"></param>
        /// <returns></returns>
        private static LTLFormula BuildFormulaFromPredicateNode(TLTLPredicateNode node, List<TLTLPredicateNode> nodes, List<RabinAutomatonAtomicPropositionData> atomicPropositions)
        {
            // Para cada nodo: obtener el tipo definido en su especificación.
            Type nodeType = node.PredicateType;

            if (!typeof(TLTLPredicate).IsAssignableFrom(nodeType))
            {
                UnityEngine.Debug.LogWarningFormat("¡CUIDADO! Tratando de construir fórmula a partir de un tipo no asignable a TLTLPredicate ({0})", nodeType);
                return null;
            }

            //--------------------------------------------------------
            //           CASOS PARTICULARES TIPO A TIPO
            //--------------------------------------------------------

            // Definimos esta función local por comodidad para no tener que andar escribiendo lo mismo en cada caso y mejorar la legibilidad
            LTLFormula RecursiveBuildFormula(TLTLPredicateNode node)
            {
                return BuildFormulaFromPredicateNode(node, nodes, atomicPropositions);
            } // RecursiveBuildFormula

            //--------------------------------------------------------
            //                          THEN
            //--------------------------------------------------------
            if (nodeType.IsAssignableFrom(typeof(ThenPredicate)))
            {
                // Esperamos dos propiedades de entrada (A y B)
                var A = ExtractInputNodeFromPropertyName(node, nodes, "A");
                var B = ExtractInputNodeFromPropertyName(node, nodes, "B");

                if (A == null | B == null) return null;

                // Un poco más complejo: este queremos que se reemplace por !A|B
                return new LTLDisjunction(new LTLNegation(RecursiveBuildFormula(A)), RecursiveBuildFormula(B));
            }

            //--------------------------------------------------------
            //                         ALWAYS
            //--------------------------------------------------------
            if (nodeType.IsAssignableFrom(typeof(AlwaysPredicate)))
            {
                // Esperamos una propiedad de Entrada (Predicate)
                var Predicate = ExtractInputNodeFromPropertyName(node, nodes, "Predicate");

                if (Predicate == null) return null;

                // G P -> LTLGlobally(expand(P))
                return new LTLGlobally(RecursiveBuildFormula(Predicate));
            }

            //--------------------------------------------------------
            //                        EVENTUALLY
            //--------------------------------------------------------
            if (nodeType.IsAssignableFrom(typeof(EventuallyPredicate)))
            {
                // Esperamos una propiedad de Entrada (Predicate)
                var Predicate = ExtractInputNodeFromPropertyName(node, nodes, "Predicate");

                if (Predicate == null) return null;

                // F P -> LTLEventually(expand(P))
                return new LTLEventually(RecursiveBuildFormula(Predicate));
            }

            //--------------------------------------------------------
            //                         UNTIL
            //--------------------------------------------------------
            if (nodeType.IsAssignableFrom(typeof(UntilPredicate)))
            {
                // Esperamos dos propiedades de entrada (P y Q)
                var P = ExtractInputNodeFromPropertyName(node, nodes, "P");
                var Q = ExtractInputNodeFromPropertyName(node, nodes, "Q");

                if (P == null || Q == null) return null;

                // P U Q -> LTLUntil(expand(P), expand(Q))
                return new LTLUntil(RecursiveBuildFormula(P), RecursiveBuildFormula(Q));
            }

            //--------------------------------------------------------
            //                          NEXT
            //--------------------------------------------------------
            if (nodeType.IsAssignableFrom(typeof(NextPredicate)))
            {
                // Esperamos una propiedad de Entrada (Predicate)
                var Predicate = ExtractInputNodeFromPropertyName(node, nodes, "Predicate");

                if (Predicate == null) return null;

                // X P -> LTLNext(expand(P))
                return new LTLNext(RecursiveBuildFormula(Predicate));
            }

            //--------------------------------------------------------
            //                          NOT
            //--------------------------------------------------------
            if (nodeType.IsAssignableFrom(typeof(NotPredicate)))
            {
                // Esperamos una propiedad de Entrada (Predicate)
                var Predicate = ExtractInputNodeFromPropertyName(node, nodes, "Predicate");

                if (Predicate == null) return null;

                // !X -> LTLNegation(expand(X))
                return new LTLNegation(RecursiveBuildFormula(Predicate));
            }

            //--------------------------------------------------------
            //                          AND
            //--------------------------------------------------------
            if (nodeType.IsAssignableFrom(typeof(AndPredicate)))
            {
                // Esperamos dos propiedades de entrada (A y B)
                var A = ExtractInputNodeFromPropertyName(node, nodes, "A");
                var B = ExtractInputNodeFromPropertyName(node, nodes, "B");

                if (A == null || B == null) return null;

                // A & B -> LTLConjunction(expand(A), expand(B))
                return new LTLConjunction(RecursiveBuildFormula(A), RecursiveBuildFormula(B));
            }

            //--------------------------------------------------------
            //                          OR
            //--------------------------------------------------------
            if (nodeType.IsAssignableFrom(typeof(OrPredicate)))
            {
                // Esperamos dos propiedades de entrada (A y B)
                var A = ExtractInputNodeFromPropertyName(node, nodes, "A");
                var B = ExtractInputNodeFromPropertyName(node, nodes, "B");

                if (A == null || B == null) return null;

                // A & B -> LTLDisjunction(expand(A), expand(B))
                return new LTLDisjunction(RecursiveBuildFormula(A), RecursiveBuildFormula(B));
            }


            //--------------------------------------------------------
            //                     USER PREDICATE
            //--------------------------------------------------------
            // predicado especificado por el usuario, lo consideramos atómico con el identificador dado por un nuevo GUID
            // y registramos esta asociación en el diccionario de etiquetas.
            RabinAutomatonAtomicPropositionData newAP = new RabinAutomatonAtomicPropositionData();
            // Las proposiciones se introducen en la lista con el índice que tienen en la misma como ID
            // pero deben ir siempre precedidas de una letra, por lo que añadimos una p al principio
            string ID = $"p{atomicPropositions.Count()}";
            newAP.InputParams = node.InputParams.ToArray();
            newAP.PredicateType = node.PredicateType.AssemblyQualifiedName;
            atomicPropositions.Add(newAP);

            return new LTLAtomicProposition(ID);
        } // BuildFormulaFromNodeData

        public static void CompileAutomaton()
        {
            GetElementsFromGraphView();

            List<RabinAutomatonAtomicPropositionData> atomicPropositions = new List<RabinAutomatonAtomicPropositionData>();
            TLTLPredicateNode rootNode = _nodes.Find(n => n.ID == _returnNode.Connection.NodeID);

            if (rootNode == null)
            {
                UnityEngine.Debug.LogWarningFormat("No se encontró un nodo raíz conectado con el nodo de retorno.");
                return;
            }

            LTLFormula formula = BuildFormulaFromPredicateNode(rootNode, _nodes, atomicPropositions);

            if (formula == null)
            {
                UnityEngine.Debug.LogWarningFormat("No se pudo generar una fórmula a partir del grafo del editor.");
                return;
            }

            // assset para almacenar la información del autómata (proposiciones atómicas y estados con transiciones).
            RabinAutomatonDataAsset automatonAsset = CreateAsset<RabinAutomatonDataAsset>(_automatonDataFolderPath, _graphFileName);

            // Specify the path to the command-line executable
            // Unity Editor: <path to project folder>/Assets (https://docs.unity3d.com/ScriptReference/Application-dataPath.html)
            string rabinizerPath = $"Packages/dev.pablogutierrez.tl-behavior-graphs/Editor/PredicateBuilder/Utils/External/rabinizer3.1.jar";

            // Como argumentos, especificamos la fórmula generada en el paso anterior.
            string arguments = $"-jar \"{rabinizerPath}\" -format=hoa -auto=tr -silent -out=std \"{formula}\"";
            UnityEngine.Debug.Log(arguments);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Start the process
            Process process = new Process();
            process.StartInfo = startInfo;

            UnityEngine.Debug.Log(process.StartInfo);

            StringBuilder outputBuilder = new StringBuilder();
            StringBuilder errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            string output = outputBuilder.ToString();

            UnityEngine.Debug.Log(output);
            string error = errorBuilder.ToString();

            if (error != null && error.Length > 0)
            {
                UnityEngine.Debug.LogError("Error:\n" + error);
                UnityEngine.Debug.LogError("Could not compile automaton from predicate. Please check above error messages.");
                return;
            }

            automatonAsset.InitializeFromSpecs(output, atomicPropositions);

            SaveBlackboardData(automatonAsset);
            SaveAsset(automatonAsset);

        } // CompileAutomaton

        private static void SaveBlackboardData(TLTLPredicateDataAsset predicateAsset)
        {
            List<TLTLBlackboardData.Entry> entries = new List<TLTLBlackboardData.Entry>();
            foreach (var typeEntry in _graphView.BlackboardWindow.Entries)
            {
                foreach (var entry in typeEntry.Value)
                {
                    entries.Add(new TLTLBlackboardData.Entry() { name = entry, type = typeEntry.Key.AssemblyQualifiedName });
                }
            }
            predicateAsset.BlackboardData.Parameters = entries;
        } // SaveBlackboardData

        private static void SaveBlackboardData(RabinAutomatonDataAsset automatonAsset)
        {
            List<TLTLBlackboardData.Entry> entries = new List<TLTLBlackboardData.Entry>();
            foreach (var typeEntry in _graphView.BlackboardWindow.Entries)
            {
                foreach (var entry in typeEntry.Value)
                {
                    entries.Add(new TLTLBlackboardData.Entry() { name = entry, type = typeEntry.Key.AssemblyQualifiedName });
                }
            }
            automatonAsset.BlackboardData.Parameters = entries;
        } // SaveBlackboardData

        /// <summary>
        /// Almacena la información del nodo de retorno en los assets de guardado del grafo en versión editor (posición del nodo)
        /// y de guardado de información de predicado pura (conexiones).
        /// </summary>
        private static void SaveReturnNode(TLTLGraphSaveData graphAsset, TLTLPredicateDataAsset predicateAsset)
        {
            graphAsset.ReturnNodePosition = _returnNode.GetPosition().position;
            predicateAsset.ReturnNodeConnectionID = _returnNode.Connection.NodeID;
        } // SaveReturnNode

        /// <summary>
        /// Almacena la información de los nodos creados en edición sobre los ficheros de assets de grafo y de predicado puro.
        /// </summary>
        /// <param name="graphAsset"></param>
        /// <param name="predicateAsset"></param>
        private static void SaveNodes(TLTLGraphSaveData graphAsset, TLTLPredicateDataAsset predicateAsset)
        {
            List<string> nodeNames = new List<string>();

            foreach (TLTLPredicateNode node in _nodes)
            {
                SaveNodeToGraph(node, graphAsset, predicateAsset);
                nodeNames.Add(node.ID);
            }
        } // SaveNodes

        /// <summary>
        /// Método para almacenar un nodo de predicado en el sentido de nodo específico dentro del grafo
        /// del editor visual en un scriptable object que contenga la información correspondiente sobre el 
        /// nodo en el grafo y así poder guardarlo y cargarlo en el futuro.
        /// </summary>
        private static void SaveNodeToGraph(TLTLPredicateNode node, TLTLGraphSaveData graphAsset, TLTLPredicateDataAsset predicateAsset)
        {
            List<TLTLInputConnectionData> connections = CloneNodeConnections(node.Connections);

            TLTLNodeData nodeData = new TLTLNodeData()
            {
                ID = node.ID,
                Connections = connections,
                InputParams = node.InputParams,
                PredicateType = node.PredicateType.AssemblyQualifiedName,
            };

            predicateAsset.Nodes.Add(nodeData);
            graphAsset.NodePositions.Add(new NodePositionEntry() { nodeId = node.ID, position = node.GetPosition().position });
        } // SaveNodeToGraph

        /// <summary>
        /// Carga un grafo completo en base al parámetro _graphFileName especificado en la inicialización.
        /// Este se busca en el directorio Assets/TLTLPredicateBuilder/Graphs. En caso de éxito, se actualiza
        /// el nombre de archivo mostrado en la ventana del editor de predicados, y se "pueblan" las listas de nodos y 
        /// de conexiones de nodos de esta clase con la información del grafo cargado.
        /// </summary>
        public static void Load()
        {
            TLTLGraphSaveData graphAsset = LoadAsset<TLTLGraphSaveData>(_graphDataFolderPath, _graphFileName);

            if (graphAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Could not find the graph asset!",
                    "The file at the following path could not be found:\n\n" +
                    $"\"{_graphDataFolderPath}/{_graphFileName}\".\n\n" +
                    "Make sure you chose the right file and it's placed at the folder path mentioned above.",
                    "Thanks!"
                );

                return;
            }

            LoadGraphFromSave(graphAsset);

        } // Load

        public static void LoadGraphFromSave(TLTLGraphSaveData graphAsset)
        {
            TLTLPredicateDataAsset predicateAsset = LoadAsset<TLTLPredicateDataAsset>(_containerFolderPath, _graphFileName);

            if (predicateAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Could not find the predicate asset!",
                    "The file at the following path could not be found:\n\n" +
                    $"\"{_containerFolderPath}/{_graphFileName}\".\n\n" +
                    "Make sure you chose the right file and it's placed at the folder path mentioned above.",
                    "Thanks!"
                );

                return;
            }
            TLTLPredicateBuilderEditorWindow.UpdateFileName(graphAsset.FileName);

            _graphView.ClearGraph();
            LoadBlackboardData(predicateAsset);
            LoadReturnNode(graphAsset, predicateAsset);
            LoadNodes(graphAsset, predicateAsset);
            LoadNodesConnections();
        } // LoadGraphFromSave

        private static void LoadBlackboardData(TLTLPredicateDataAsset predicateAsset)
        {
            _graphView.BlackboardWindow.Initialize(predicateAsset.BlackboardData.Parameters);
        } // LoadBlackboardData

        private static void LoadReturnNode(TLTLGraphSaveData graphAsset, TLTLPredicateDataAsset predicateAsset)
        {
            TLTLReturnNode returnNode = _graphView.CreateReturnNode(graphAsset.ReturnNodePosition, false);

            returnNode.Connection = new TLTLInputConnectionData()
            {
                NodeID = predicateAsset.ReturnNodeConnectionID
            };

            returnNode.Draw();

            _graphView.AddReturnNode(returnNode);

            // En este caso no nos hace verdaderamente falta el asociar un ID al nodo de retorno
            // (cualquier nombre vale) porque jamás tendrá un output port y por lo tanto no puede
            // ser referenciado en una conexión por ID.
            _loadedNodes.Add("return", returnNode);
        } // LoadReturnNode

        private static void LoadNodes(TLTLGraphSaveData graphAsset, TLTLPredicateDataAsset predicateAsset)
        {
            foreach (TLTLNodeData nodeData in predicateAsset.Nodes)
            {
                List<TLTLInputConnectionData> connections = CloneNodeConnections(nodeData.Connections);
                List<TLTLInputParamData> inputParams = CloneNodeInputParams(nodeData.InputParams);

                // ¡OJO! Aquí estamos llamando a crear node con el flag de draw on creation set a false.
                // Esto lo hacemos para poder introducir la información de ID y conexiones ANTES de que se 
                // pinte; de este modo el nodo reflejará el estado correcto en el primer dibujado.
                TLTLPredicateNode node = _graphView.CreateNode(Type.GetType(nodeData.PredicateType), graphAsset.NodePositions.Find(entry => entry.nodeId == nodeData.ID).position, false);

                node.ID = nodeData.ID;
                node.Connections = connections;
                node.InputParams = inputParams;

                node.Draw();

                _graphView.AddElement(node);

                _loadedNodes.Add(node.ID, node);
            }
        } // LoadNodes

        private static void LoadNodesConnections()
        {
            foreach (KeyValuePair<string, Node> loadedNode in _loadedNodes)
            {
                // Una vez creado el nodo a partir del asset pertinente, podemos proceder a 
                // comprobar puerto a puerto en los puertos de entrada si deberían estar enlazados
                // a los puertos de salida de algún otro nodo del grafo.
                foreach (Port inputPort in loadedNode.Value.inputContainer.Children())
                {
                    // Extraemos la información de la conexión de los datos de usuario del puerto de entrada.
                    // Esto es básicamente con qué nodo (if any) está conectado el puerto actual.
                    TLTLInputConnectionData connectionData = (TLTLInputConnectionData)inputPort.userData;

                    if (string.IsNullOrEmpty(connectionData.NodeID))
                    {
                        continue;
                    }

                    // Esa información en concreto nos permite determinar cuál es el nodo de salida
                    // que se conecta con nuestro nodo actual, y podemos obtener una referencia
                    // indexando por ID en nuestro diccionario de nodos cargados.
                    Node sourceNode = _loadedNodes[connectionData.NodeID];

                    // Una vez disponemos del nodo del que este recibe una entrada, 
                    // podemos acceder a su puerto de salida correspondiente y conectarlo a esta entrada.
                    // NOTA: Esto funciona porque asumimos que todos los nodos tienen una única salida
                    // en forma de su robustez (valor compuesto).
                    Port sourceNodeOutputPort = (Port)sourceNode.outputContainer.Children().First();
                    Edge edge = inputPort.ConnectTo(sourceNodeOutputPort);

                    // Una vez creado el objeto de conexión, lo integramos en el grafo.
                    _graphView.AddElement(edge);
                    // NO OLVIDAR refrescar los puertos para que este cambio se refleje verdaderamente sobre la vista.
                    loadedNode.Value.RefreshPorts();
                }
            }
        } // LoadNodesConnections

        /// <summary>
        /// Genera las carpetas por defecto necesarias para almancenar los grafos, autómatas y predicados.
        /// </summary>
        private static void CreateDefaultFolders()
        {
            // creación de la carpeta del Predicate Builder
            CreateFolder("Assets", "TLTLPredicateBuilder");

            // Creación de las carpetas de datos para predicados, grafos y autómatass
            string builderPath = "Assets/TLTLPredicateBuilder";
            CreateFolder(builderPath, "Predicates");
            CreateFolder(builderPath, "Graphs");
            CreateFolder(builderPath, "Automata");
        } // CreateDefaultFolders

        /// <summary>
        /// Extrae los elementos de la vista de grafo asociada y puebla las correspondientes listas
        /// internas (de momento, sólo _nodes), con los elementos del mismo tipo. 
        /// </summary>
        private static void GetElementsFromGraphView()
        {
            if (_graphView.graphElements == null) return;

            _graphView.graphElements.ForEach(graphElement =>
            {
                if (graphElement is TLTLPredicateNode node)
                {
                    _nodes.Add(node);
                    return;
                }
                else if (graphElement is TLTLReturnNode returnNode)
                {
                    _returnNode = returnNode;
                    return;
                }
            });
        } // GetElementsFromGraphView

        /// <summary>
        /// Trata de crear un nuevo directorio bajo la ruta dada por parentFolderPath, con
        /// nombre newFolderName. Comprueba primero si ese nuevo directorio sería válido y, 
        /// en caso afirmativo, procede a la creación.
        /// </summary>
        public static void CreateFolder(string parentFolderPath, string newFolderName)
        {
            if (AssetDatabase.IsValidFolder($"{parentFolderPath}/{newFolderName}"))
            {
                return;
            }

            AssetDatabase.CreateFolder(parentFolderPath, newFolderName);
        } // CreateFolder

        /// <summary>
        /// Elimina el directorio introducido junto con su fichero .meta asociado.
        /// </summary>
        public static void RemoveFolder(string path)
        {
            FileUtil.DeleteFileOrDirectory($"{path}.meta");
            FileUtil.DeleteFileOrDirectory($"{path}/");
        } // RemoveFolder

        /// <summary>
        /// Genera un nuevo fichero .asset en el path indicado con el nombre assetName, asumiendo
        /// que no existiera previamente un fichero con el mismo nombre en dicho path.
        /// </summary>
        public static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            if (assetName == null || assetName.Length == 0)
            {
                return null;
            }

            string fullPath = $"{path}/{assetName}.asset";

            T asset = LoadAsset<T>(path, assetName);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();

                AssetDatabase.CreateAsset(asset, fullPath);
            }

            return asset;
        } // CreateAsset

        /// <summary>
        /// Carga el asset en la dirección path/assetName.asset y lo devuelve.
        /// </summary>
        public static T LoadAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            string fullPath = $"{path}/{assetName}.asset";

            return AssetDatabase.LoadAssetAtPath<T>(fullPath);
        } // LoadAsset

        /// <summary>
        /// Carga el asset en la dirección fullAssetPath (incluyendo extensión .asset) y lo devuelve.
        /// </summary>
        public static T LoadAsset<T>(string fullAssetPath) where T : ScriptableObject
        {
            return AssetDatabase.LoadAssetAtPath<T>(fullAssetPath);
        } // LoadAsset

        /// <summary>
        /// "Guarda" el asset especificado por parámetro, marcándolo como dirty en el editor
        /// y a cotinuación llamando a los métodos SaveAssets y Refresh del AssetDatabase. Nota: SaveAssets
        /// sólo tiene efecto sobre aquellos objetos marcados explícitamente como dirty.
        /// </summary>
        public static void SaveAsset(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        } // SaveAsset

        /// <summary>
        /// Elimina el asset especificado de la base de datos de assets.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="assetName"></param>
        public static void RemoveAsset(string path, string assetName)
        {
            AssetDatabase.DeleteAsset($"{path}/{assetName}.asset");
        } // RemoveAsset

        private static List<TLTLInputConnectionData> CloneNodeConnections(List<TLTLInputConnectionData> nodeConnections)
        {
            List<TLTLInputConnectionData> connections = new List<TLTLInputConnectionData>();

            foreach (TLTLInputConnectionData connection in nodeConnections)
            {
                TLTLInputConnectionData connectionData = new TLTLInputConnectionData()
                {
                    NodeID = connection.NodeID,
                    InParamName = connection.InParamName
                };

                connections.Add(connectionData);
            }

            return connections;
        } // CloneNodeConnections

        private static List<TLTLInputParamData> CloneNodeInputParams(List<TLTLInputParamData> nodeInputParams)
        {
            List<TLTLInputParamData> inputParams = new List<TLTLInputParamData>();

            foreach (TLTLInputParamData param in nodeInputParams)
            {
                TLTLInputParamData paramData = new TLTLInputParamData()
                {
                    InParamName = param.InParamName,
                    BlackboardParamName = param.BlackboardParamName
                };

                inputParams.Add(paramData);
            }

            return inputParams;
        } // CloneNodeInputParams

    } // TLTLIOUtils
} // namespace TLTLPredicateBuilder.Utilities