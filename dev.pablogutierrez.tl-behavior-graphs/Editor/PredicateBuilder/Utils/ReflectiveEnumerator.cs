using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class SubclassUtils
{
    public static List<Type> GetNonAbstractSubclasses(Type baseClass)
    {
        // accedemos a la assembly actualmente en ejecución (en realidad podría estar en otro sitio, pero
        // por el momento vamos a buscar sólo aquí).
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] types = assembly.GetTypes();
        // nos interesa quedarnos con todos los tipos que no sean abstractos y que hereden de nuestra clase base
        IEnumerable<Type> subclasses = types.Where(t => t.IsSubclassOf(baseClass) && !t.IsAbstract);

        /*
         * Si quisiéramos buscar en todas las assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.IsClass && !type.IsAbstract && baseClass.IsAssignableFrom(type))
                {
                    subclasses.Add(type);
                }
            }
        }
        */

        return subclasses.ToList();
    }
}