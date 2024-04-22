using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace TLTLPredicateBuilder.Windows
{
    using System;
    using System.Linq;
    using TLTLCore.Framework;
    using TLTLPredicateBuilder.Elements;

    public class CategoryTree
    {
        public string categoryName;
        public Type categoryType = null; // en nodos hoja, tipo que representan a modo de userData
        public Dictionary<string, CategoryTree> subcategories = new Dictionary<string, CategoryTree>();
    } // TreeCategory

    public class TLTLSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private TLTLPredicateGraphView _graphView;
        /*
         * Un problema de las search windows es que por defecto no a�aden ning�n tipo de espacio
         * o tabulaci�n al comienzo de entradas "hoja" (entradas que no contienen m�s anidamientos).
         * Una forma de solucionar esto es a�adir un icono de identaci�n justo al comienzo de la GUI,
         * lo cual puede conseguirse a�adiendo una textura en el constructor del GUIContent que usamos
         * para representar la entrada del �rbol de b�squeda (como los iconos de Unity en el editor).
         * Esto se especifica en el m�todo Initialize.
         */
        private Texture2D _indentationIcon;

        public void Initialize(TLTLPredicateGraphView tltlPredicateGraphView)
        {
            _graphView = tltlPredicateGraphView;

            /*
             * Creamos una nueva textura de tama�o 1 a colocar delante de cada una de las entradas
             * hoja del �rbol de b�squeda para solucionar el problema de falta de espacio. Notamos 
             * aqu� que esto podr�a ser cualquier cosa, y de hecho podr�amos tener un icono espec�fico
             * para cada una de las entradas. Por defecto, esto va a ser blanco, pero podemos usar el m�todo
             * SetPixel para modificar esta opci�n y usar un color transparente (Color.clear). Esto no va
             * a verse reflejado hasta que no solicitemos una acci�n de upload mediante una llamada a Apply.
             * Las llamadas a Apply son particularmente caras, por lo que si en alg�n momento queremos hacer
             * cosas m�s complejas, se aconseja acumular todos los cambios posibles antes de actualizar.
             */
            _indentationIcon = new Texture2D(1, 1);
            _indentationIcon.SetPixel(0, 0, Color.clear);
            _indentationIcon.Apply();
        } // Initialize

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            /*
                Este m�todo nos permite especificar la estructura del men� de b�squeda, as�
                como los elementos que estar�n disponibles en cada uno de los bloques del men�.
                En particular, esto se hace indicando una lista de SearchTreeEntries, 
             */
            CategoryTree categoryTree = new CategoryTree();
            categoryTree.categoryName = "Create Element";

            foreach (var subclass in TLTLPredicate.AvailableSubclasses)
            {
                string[] classParts = TLTLPredicate.GetPredicateDisplayName(subclass).Split('/');

                CategoryTree aux = categoryTree;
                for (int i = 0; i < classParts.Length; ++i)
                {
                    string key = classParts[i];
                    // nos movemos a la nueva subcategor�a
                    if (!aux.subcategories.ContainsKey(key))
                    {
                        aux.subcategories.Add(key, new CategoryTree());
                    }

                    aux = aux.subcategories[classParts[i]];
                    aux.categoryName = classParts[i];
                }
                aux.categoryType = subclass;
            }

            return GenerateSearchTreeEntriesFromCategoryTree(categoryTree);
        } // CreateSearchTree

        private List<SearchTreeEntry> GenerateSearchTreeEntriesFromCategoryTree(CategoryTree categoryTree)
        {
            List<SearchTreeEntry> searchTreeEntries = new List<SearchTreeEntry>();

            // Expandir categor�as de forma recursiva
            ExpandSearchTreeCategories(searchTreeEntries, categoryTree);

            return searchTreeEntries;
        } // GenerateSearchTreeEntriesFromCategoryTree

        /// <summary>
        /// Expande de forma recursiva todas las categor�as del �rbol de categor�as actual
        /// </summary>
        /// <param name="searchTreeEntries"></param>
        /// <param name="categoryTree"></param>
        private void ExpandSearchTreeCategories(List<SearchTreeEntry> searchTreeEntries, CategoryTree categoryTree, int level = 0)
        {
            // Caso base: Nodo hoja sin hijos; a�adimos un nodo con el icono de identaci�n y una entrada no agrupada.
            // + El tipo que necesitamos a modo de userData cuando seleccionemos esta opci�n
            if (categoryTree.subcategories.Count == 0)
            {
                searchTreeEntries.Add(new SearchTreeEntry(new GUIContent(categoryTree.categoryName, _indentationIcon))
                {
                    level = level,
                    userData = categoryTree.categoryType
                });
            }
            // caso recursivo: hay hijos: expandimos hijo a hijo con un nivel de profundidad adicional
            else
            {
                // A�ade la categor�a actual como grupo con el nivel introducido
                searchTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent(categoryTree.categoryName), level));
                foreach (var subcategory in categoryTree.subcategories.Values)
                {
                    // expansi�n de la categor�a
                    ExpandSearchTreeCategories(searchTreeEntries, subcategory, level + 1);
                }
            }
        } // ExpandSearchTreeCategories

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            Vector2 localMousePosition = _graphView.GetLocalMousePosition(context.screenMousePosition, true);

            Type userData = (Type)SearchTreeEntry.userData;
            if (userData != null && typeof(TLTLPredicate).IsAssignableFrom(userData)){
                TLTLPredicateNode node = _graphView.CreateNode(userData, localMousePosition);
                _graphView.AddElement(node);
                return true;
            }
            return false;
        } // OnSelectEntry

    } // TLTLSearchWindow

} // namespace TLTLPredicateBuilder.Windows
