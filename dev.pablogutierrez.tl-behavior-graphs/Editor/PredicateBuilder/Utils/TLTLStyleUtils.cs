using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TLTLPredicateBuilder.Utils
{
    public static class TLTLStyleUtils
    {
        /// <summary>
        /// Método de conveniencia para añadir un número arbitrario de nombres de clase a un elemento visual.
        /// </summary>
        public static VisualElement AddClasses(this VisualElement element, params string[] classNames)
        {
            foreach (string className in classNames)
            {
                element.AddToClassList(className);
            }

            return element;
        } // AddClasses

        /// <summary>
        /// Método de conveniencia para añadir un número arbitrario de hojas de estilo a un elemento visual.
        /// </summary>
        public static VisualElement AddStyleSheets(this VisualElement element, params string[] styleSheetNames)
        {
            foreach (string styleSheetName in styleSheetNames)
            {
                StyleSheet nodeStyleSheet = (StyleSheet) EditorGUIUtility.Load($"Packages/dev.pablogutierrez.tl-behavior-graphs/Editor/Styles/{styleSheetName}");
                element.styleSheets.Add(nodeStyleSheet);
            }

            return element;
        } // AddStyleSheets

    } // TLTLStyleUtils

} // namespace TLTLPredicateBuilder.Utils
