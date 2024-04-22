using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TLTLCore.Framework
{
    /// <summary>
    /// Clase base para la implementaci�n de predicados en TLTL que soporten una sem�ntica cuantitativa.
    /// Cada predicado que herede de esta clase deber� necesariamente incluir una implementaci�n de la funci�n
    /// EvaluateRobustness que se encargue de calcular el grado de "robustez" del predicado en el momento actual.
    /// 
    /// Por otro lado, se entiende que un predicado por lo general har� uso de par�metros de entrada para representar
    /// las entidades involucradas en el c�mputo de la robustez. Estos pueden marcarse mediante el atributo InParam("nombre")
    /// especificando el nombre del atributo dentro de la clase.
    /// </summary>
    public abstract class TLTLPredicate
    {
        /// <summary>
        /// Clases disponibles que hereden de TLTLPredicate.
        /// </summary>
        private static List<Type> _availableSubclasses;

        /// <summary>
        /// Eval�a la robustez del predicado por medio de un valor decimal ( > 0 para denotar predicados satisfechos )
        /// </summary>
        public abstract float EvaluateRobustness();

        /// <summary>
        /// Devuelve el predicado a su estado original. Este m�todo se llama al comienzo de cada run del aut�mata de premios
        /// de manera que si un predicado almacena informaci�n a lo largo de la ejecuci�n pueda contar con un mecanismo
        /// para limpiar dicha informaci�n de cara a empezar una nueva palabra. Aqu� s�lo es necesario implementar el cleanup
        /// del estado interno del predicado, sin tocar ninguno de los inparams en ning�n momento.
        /// </summary>
        public virtual void Reset()
        {

        } // Reset

        /// <summary>
        /// Devuelve el predicado a su estado original. Este m�todo resetea primero todos los predicados compuestos que
        /// pueda llegar a tener este predicado como InParams para posteriormente llamar a la implementaci�n espec�fica 
        /// de Reset para la clase actual.
        /// </summary>
        public void ResetPredicate()
        {
            List<PropertyInfo> properties = GetInProperties(GetType());
            foreach (PropertyInfo property in properties)
            {
                if (property.GetType().IsSubclassOf(typeof(TLTLPredicate)))
                {
                    ((TLTLPredicate)property.GetValue(this))?.Reset();
                }
            }

            Reset();
        } // ResetPredicate

        public static List<Type> AvailableSubclasses
        {
            get
            {
                if (_availableSubclasses == null)
                {
                    // accedemos a la assembly actualmente en ejecuci�n (en realidad podr�a estar en otro sitio, pero
                    // por el momento vamos a buscar s�lo aqu�).
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    Type[] types = assembly.GetTypes();
                    // nos interesa quedarnos con todos los tipos que no sean abstractos y que hereden de nuestra clase base
                    IEnumerable<Type> subclasses = types.Where(t => t.IsSubclassOf(typeof(TLTLPredicate)) && !t.IsAbstract);

                    _availableSubclasses = subclasses.ToList();
                }

                return _availableSubclasses;
            }
        } // AvailableSubclasses

        // Helper method to retrieve the list of properties with InParams for a given class
        public static List<PropertyInfo> GetInProperties(Type classType)
        {
            var inParams = new List<PropertyInfo>();

            var properties = classType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<InParam>();
                if (attribute != null)
                {
                    inParams.Add(property);
                }
            }

            return inParams;
        } // GetInProperties

        // Helper method to retrieve the output expression for a given class
        public static string GetOutputExpression(Type classType)
        {
            var predicate = classType.GetCustomAttribute<Predicate>();

            if (predicate != null)
            {
                return predicate.OutputExpression;
            }

            return "Output";
        } // GetOutputExpression

        // Helper method to retrieve the display name for a given predicate class
        public static string GetPredicateDisplayName(Type classType)
        {
            var predicate = classType.GetCustomAttribute<Predicate>();

            if (predicate != null)
            {
                return predicate.Name;
            }

            return "Evaluate Condition";
        } // GetPredicateDisplayName

    } // TLTLPredicate

}
