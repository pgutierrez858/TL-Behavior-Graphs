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
         * Un problema de las search windows es que por defecto no añaden ningún tipo de espacio
         * o tabulación al comienzo de entradas "hoja" (entradas que no contienen más anidamientos).
         * Una forma de solucionar esto es añadir un icono de identación justo al comienzo de la GUI,
         * lo cual puede conseguirse añadiendo una textura en el constructor del GUIContent que usamos
         * para representar la entrada del árbol de búsqueda (como los iconos de Unity en el editor).
         * Esto se especifica en el método Initialize.
         */
        private Texture2D _indentationIcon;

        public void Initialize(TLTLPredicateGraphView tltlPredicateGraphView)
        {
            _graphView = tltlPredicateGraphView;

            /*
             * Creamos una nueva textura de tamaño 1 a colocar delante de cada una de las entradas
             * hoja del árbol de búsqueda para solucionar el problema de falta de espacio. Notamos 
             * aquí que esto podría ser cualquier cosa, y de hecho podríamos tener un icono específico
             * para cada una de las entradas. Por defecto, esto va a ser blanco, pero podemos usar el método
             * SetPixel para modificar esta opción y usar un color transparente (Color.clear). Esto no va
             * a verse reflejado hasta que no solicitemos una acción de upload mediante una llamada a Apply.
             * Las llamadas a Apply son particularmente caras, por lo que si en algún momento queremos hacer
             * cosas más complejas, se aconseja acumular todos los cambios posibles antes de actualizar.
             */
            _indentationIcon = new Texture2D(1, 1);
            _indentationIcon.SetPixel(0, 0, Color.clear);
            _indentationIcon.Apply();
        } // Initialize

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            /*
                Este método nos permite especificar la estructura del menú de búsqueda, así
                como los elementos que estarán disponibles en cada uno de los bloques del menú.
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
                    // nos movemos a la nueva subcategoría
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

            // Expandir categorías de forma recursiva
            ExpandSearchTreeCategories(searchTreeEntries, categoryTree);

            return searchTreeEntries;
        } // GenerateSearchTreeEntriesFromCategoryTree

        /// <summary>
        /// Expande de forma recursiva todas las categorías del árbol de categorías actual
        /// </summary>
        /// <param name="searchTreeEntries"></param>
        /// <param name="categoryTree"></param>
        private void ExpandSearchTreeCategories(List<SearchTreeEntry> searchTreeEntries, CategoryTree categoryTree, int level = 0)
        {
            // Caso base: Nodo hoja sin hijos; añadimos un nodo con el icono de identación y una entrada no agrupada.
            // + El tipo que necesitamos a modo de userData cuando seleccionemos esta opción
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
                // Añade la categoría actual como grupo con el nivel introducido
                searchTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent(categoryTree.categoryName), level));
                foreach (var subcategory in categoryTree.subcategories.Values)
                {
                    // expansión de la categoría
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
