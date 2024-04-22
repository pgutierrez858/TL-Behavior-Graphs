
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using TLTLCore;

namespace TLTLUnity
{

    /// <summary>
    /// Clase que almacena los datos que terminarán en una pizarra de
    /// TL Reward Automaton pero compatible con los prefabs y la serialización de Unity.
    /// </summary>
    /// Se utiliza para los componentes de ejecución y pizarra de
    /// Unity, que guardan un atributo de esta clase.
    /// Existe un PropertyDrawer para poder mostrar su contenido
    /// en el inspector.
    [System.Serializable]
    public class UnityBlackboard
    {

        /// <summary>
        /// Hace las veces de constructor; llamado por el editor cuando se construye
        /// un componente nuevo y se añade a un objeto.
        /// </summary>
        public UnityBlackboard()
        {
            intParams = new List<int>(); intParamsNames = new List<string>(); intParamsLostDefaultValue = new List<bool>();
            boolParams = new List<bool>(); boolParamsNames = new List<string>(); boolParamsLostDefaultValue = new List<bool>();
            floatParams = new List<float>(); floatParamsNames = new List<string>(); floatParamsLostDefaultValue = new List<bool>();
            stringParams = new List<string>(); stringParamsNames = new List<string>(); stringParamsLostDefaultValue = new List<bool>();
            colorParams = new List<Color>(); colorParamsNames = new List<string>(); colorParamsLostDefaultValue = new List<bool>();
            objectParams = new List<UnityEngine.Object>(); objectParamsNames = new List<string>(); objectParamsLostDefaultValue = new List<bool>();
            layerMaskParams = new List<LayerMask>(); layerMaskParamsNames = new List<string>(); layerMaskParamsLostDefaultValue = new List<bool>();
            enumParams = new List<Enum>(); enumParamsNames = new List<string>(); enumParamsLostDefaultValue = new List<bool>();
            vector2Params = new List<Vector2>(); vector2ParamsNames = new List<string>(); vector2ParamsLostDefaultValue = new List<bool>();
            vector3Params = new List<Vector3>(); vector3ParamsNames = new List<string>(); vector3ParamsLostDefaultValue = new List<bool>();
            vector4Params = new List<Vector4>(); vector4ParamsNames = new List<string>(); vector4ParamsLostDefaultValue = new List<bool>();
            rectParams = new List<Rect>(); rectParamsNames = new List<string>(); rectParamsLostDefaultValue = new List<bool>();
            animationCurveParams = new List<AnimationCurve>(); animationCurveParamsNames = new List<string>(); animationCurveParamsLostDefaultValue = new List<bool>();
            boundsParams = new List<Bounds>(); boundsParamsNames = new List<string>(); boundsParamsLostDefaultValue = new List<bool>();
            gradientParams = new List<Gradient>(); gradientParamsNames = new List<string>(); gradientParamsLostDefaultValue = new List<bool>();
            quaternionParams = new List<Quaternion>(); quaternionParamsNames = new List<string>(); quaternionParamsLostDefaultValue = new List<bool>();

            buildGenericList();
        }


        /// <summary>
        /// Método llamado cuando el comportamiento al que representa
        /// la pizarra cambia y hay que actualizar los parámetros
        /// que almacena.
        /// </summary>
        /// <param name="predicateParams"></param>
        public void updateParams(InParamValues predicateParams)
        {
            this.predicateParams = predicateParams;
            if (predicateParams == null)
            {
                // Borramos todo y terminamos
                foreach (var v in this.allParamLists)
                {
                    v.Clear();
                }
                return;
            }

            // Proceso en dos pasos.
            //    - Añadir los parámetros nuevos que no tengamos.
            //    - Borrar los parámetros que sobren.

            // Añadimos los nuevos
            foreach (var p in predicateParams.values)
            {
                putParamInLists(p.Key, p.Value);
            }

            // Borramos los que no estén
            foreach (ListInfo l in allParamLists)
            {
                for (int i = l.paramNames.Count - 1; i >= 0; --i)
                {
                    if (!predicateParams.values.ContainsKey(l.paramNames[i]))
                    {
                        // No está
                        l.removeItem(i);
                    }
                }
            }
        }


        public Blackboard BuildBlackboard()
        {
            Blackboard ret = new Blackboard();

            foreach (var l in allParamLists)
            {
                for (int i = 0; i < l.paramNames.Count; ++i)
                {
                    // El tipo de la lista puede ser UnityEngine.Object, pero
                    // el comportamiento espera un objeto de un tipo concreto. Lo
                    // Guardamos con ese tipo.
                    Type expectedType = this.predicateParams.values[l.paramNames[i]].type;
                    ret.Set(l.paramNames[i], expectedType, l.get(i));
                    //ret.Set(l.paramNames[i], l.t, l.get(i));
                }
            }

            return ret;
        }

        /// <summary>
        /// Parámetros en el comportamiento al que representamos.
        /// Es una referencia a los parámetros del TL Reward Automaton.
        /// </summary>
        /// Se utiliza únicamente desde el editor para mostrar los parámetros
        /// en el mismo orden que aparecen en el TL Reward Automaton.
        /// Se utiliza desde el PropertyDrawer
        [HideInInspector]
        [NonSerialized]
        public InParamValues predicateParams = new InParamValues();

        #region Listas serializadas por Unity
        // ///
        // Arrays ad-hoc con los parámetros pasados al comportamiento.
        // Hay un array por cada tipo soportado (uno por cada
        // componente en SerializedPropertyType, que marca los tipos
        // que pueden tener las propiedades en el Inspector).
        // Eso sí, no se soportan las propiedades compuestas (clases
        // que son [Serializable] del usuario), pues son gestionadas
        // por Unity un poco al márgen del SerializedProperty y no
        // pueden meterse todas en un List<object> esperando que
        // el SerializedObject.FindProperty("name") funcione :(
        // En realidad la lista es algo distinta a los valores del
        // enumerado, pues la documentación discrepa de los valores
        // del ensamblado... El Vector4 está en la documentación,
        // pero no en el ensamblado, que se convierte en el tipo
        // Generic. El Quaternion no está en la documentación
        // pero sí en el ensamblado... En resumen, es posible que
        // haya otros tipos que queramos poder añadir que no sean
        // UnityEngine.Object y que habrá que ir añadiendo a mano
        // según nos los vayamos encontrando.
        // ///
        public List<int> intParams;
        public List<string> intParamsNames;
        public List<bool> intParamsLostDefaultValue;

        public List<bool> boolParams;
        public List<string> boolParamsNames;
        public List<bool> boolParamsLostDefaultValue;

        public List<float> floatParams;
        public List<string> floatParamsNames;
        public List<bool> floatParamsLostDefaultValue;

        public List<string> stringParams;
        public List<string> stringParamsNames;
        public List<bool> stringParamsLostDefaultValue;

        public List<Color> colorParams;
        public List<string> colorParamsNames;
        public List<bool> colorParamsLostDefaultValue;

        public List<UnityEngine.Object> objectParams;
        public List<string> objectParamsNames;
        public List<bool> objectParamsLostDefaultValue;

        public List<LayerMask> layerMaskParams;
        public List<string> layerMaskParamsNames;
        public List<bool> layerMaskParamsLostDefaultValue;

        public List<Enum> enumParams;
        public List<string> enumParamsNames;
        public List<bool> enumParamsLostDefaultValue;

        public List<Vector2> vector2Params;
        public List<string> vector2ParamsNames;
        public List<bool> vector2ParamsLostDefaultValue;

        public List<Vector3> vector3Params;
        public List<string> vector3ParamsNames;
        public List<bool> vector3ParamsLostDefaultValue;

        public List<Vector4> vector4Params;
        public List<string> vector4ParamsNames;
        public List<bool> vector4ParamsLostDefaultValue;

        public List<Rect> rectParams;
        public List<string> rectParamsNames;
        public List<bool> rectParamsLostDefaultValue;


        // TODO: plantearse el array... ¿podría
        // ayudar a que entraran las clases de usuario?

        /*
        // Las serialized pueden ser "Character", pero no sé a qué
        // tipo hace referencia... ¿CharacterController?
        public List<CharacterController> characterParams;
        public List<string> characterParamsNames;
         */

        public List<AnimationCurve> animationCurveParams;
        public List<string> animationCurveParamsNames;
        public List<bool> animationCurveParamsLostDefaultValue;

        public List<Bounds> boundsParams;
        public List<string> boundsParamsNames;
        public List<bool> boundsParamsLostDefaultValue;

        public List<Gradient> gradientParams;
        public List<string> gradientParamsNames;
        public List<bool> gradientParamsLostDefaultValue;

        public List<Quaternion> quaternionParams;
        public List<string> quaternionParamsNames;
        public List<bool> quaternionParamsLostDefaultValue;
        #endregion

        #region Gestión de las listas con los parámetros
        /// <summary>
        /// Estructura interna que aglutina todas las listas
        /// que están como atributos públicos.
        /// </summary>
        /// De esta forma es más fácil recorrerlas todas utilizando
        /// introspección. Se necesitan los atributos públicos para
        /// que Unity nos las serialice, y para poder utilizar las
        /// SerializedProperty en el editor y que se integre
        /// razonablemente bien con prefabs.
        public struct ListInfo
        {
            public Type t;
            public object listTParams; // Se supone que de tipo List<t>
            public List<string> paramNames;

            MethodInfo addMethod;
            MethodInfo getMethod;
            MethodInfo removeMethod;
            MethodInfo clearMethod;
            MethodInfo insertMethod;

            public static ListInfo create<T>(List<T> list, List<string> names)
            {

                ListInfo ret = new ListInfo();
                ret.t = typeof(T); ret.listTParams = list; ret.paramNames = names;
                ret.addMethod = list.GetType().GetMethod("Add", new Type[] { typeof(T) });
                ret.removeMethod = list.GetType().GetMethod("RemoveAt", new Type[] { typeof(int) });
                ret.clearMethod = list.GetType().GetMethod("Clear");
                ret.getMethod = list.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                            .Where(p => p.GetIndexParameters().Any())
                                            .Select(p => p.GetGetMethod())
                                            .Single();
                ret.insertMethod = list.GetType().GetMethod("Insert", new Type[] { typeof(int), typeof(T) });
                return ret;
            }

            /// <summary>
            /// Añade a la lista listTParams, haciendo antes el cast de tipos
            /// correspondiente para poder llamar al Add.
            /// </summary>
            /// <param name="o"></param>
            /// <returns></returns>
            public bool addToList(object o)
            {
                if ((o != null) && !t.IsAssignableFrom(o.GetType()))
                    return false;
                try
                {
                    addMethod.Invoke(listTParams, new object[] { o });
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }

            public bool assignToIndex(int i, object o)
            {
                removeMethod.Invoke(listTParams, new object[] { i });
                insertMethod.Invoke(listTParams, new object[] { i, o });
                return true;
            }

            /// <summary>
            /// Borra la entrada i (tanto el nombre como el valor)
            /// </summary>
            /// <param name="i"></param>
            /// <returns></returns>
            public void removeItem(int i)
            {
                if ((i < 0) || (paramNames.Count <= i))
                    return;
                paramNames.RemoveAt(i);
                removeMethod.Invoke(listTParams, new object[] { i });
            }

            public object get(int i)
            {
                if ((i < 0) || (paramNames.Count <= i))
                    return null;
                return getMethod.Invoke(listTParams, new object[] { i });
            }

            /// <summary>
            /// Borra la información (tanto la lista de nombres como valores)
            /// </summary>
            public void Clear()
            {
                paramNames.Clear();
                clearMethod.Invoke(listTParams, null);
            }
        };

        /// <summary>
        /// Lista con la información de todas las listas de
        /// parámetros.
        /// </summary>
        /// Se debe garantizar tras cada deserialización que
        /// se hace referencia a los atributos construidos.
        public List<ListInfo> allParamLists;

        private void buildGenericList()
        {
            if (allParamLists != null) return;
            allParamLists = new List<ListInfo>(20);

            allParamLists = new List<ListInfo>
            {
                ListInfo.create(intParams, intParamsNames),
                ListInfo.create(boolParams, boolParamsNames),
                ListInfo.create(floatParams, floatParamsNames),
                ListInfo.create(stringParams, stringParamsNames),
                ListInfo.create(colorParams, colorParamsNames),
                ListInfo.create(objectParams, objectParamsNames),
                ListInfo.create(layerMaskParams, layerMaskParamsNames),
                ListInfo.create(enumParams, enumParamsNames),
                ListInfo.create(vector2Params, vector2ParamsNames),
                ListInfo.create(vector3Params, vector3ParamsNames),
                ListInfo.create(vector4Params, vector4ParamsNames),
                ListInfo.create(rectParams, rectParamsNames),
                ListInfo.create(animationCurveParams, animationCurveParamsNames),
                ListInfo.create(boundsParams, boundsParamsNames),
                ListInfo.create(gradientParams, gradientParamsNames),
                ListInfo.create(quaternionParams, quaternionParamsNames),
            };
        }

        private bool putParamInLists(string paramName, InParamValue value)
        {
            if (allParamLists == null) buildGenericList();

            ListInfo where = new ListInfo();
            bool typeFound = false;
            foreach (ListInfo l in allParamLists)
            {
                if (l.t.IsAssignableFrom(value.type))
                {
                    where = l;
                    typeFound = true;
                    break;
                }
            }

            if (!typeFound)
                return false;

            List<string> names = where.paramNames;
            int pos;
            if ((pos = names.FindIndex(c => c == paramName)) == -1)
            {
                // Nos aseguramos de que el nombre no está en el resto de listas.
                //deleteParam(paramName);
            }

            if (pos == -1)
            {
                // Parámetro nuevo; no estaba ya en la lista
                where.paramNames.Add(paramName);
                where.addToList(value.DefaultValue);
            }
            else
            {
                // El parámetro ya lo teníamos. Dejamos el valor que había, porque a este método
                // se le puede llamar también cuando no hay cambios...
            }

            return true;
        }

        public bool SetBehaviorParam(string paramName, object value)
        {
            if (allParamLists == null) buildGenericList();

            if (value == null) return false;

            ListInfo where = new ListInfo();
            bool typeFound = false;
            foreach (ListInfo l in allParamLists)
            {
                if (l.t.IsAssignableFrom(value.GetType()))
                {
                    where = l;
                    typeFound = true;
                    break;
                }
            }

            if (!typeFound)
                return false;

            List<string> names = where.paramNames;
            int pos = names.FindIndex(c => c == paramName);

            if (pos >= 0)
            {
                where.assignToIndex(pos, value);
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Busca en las distintas listas el parámetro con el nombre indicado y
        /// si lo encuentra lo elimina.
        /// </summary>
        /// <param name="paramName"></param>
        private void deleteParam(string paramName)
        {
            foreach (var l in allParamLists)
            {
                for (int i = l.paramNames.Count - 1; i >= 0; --i)
                {
                    if (l.paramNames[i] == paramName)
                    {
                        l.removeItem(i);
                    }
                }
            }
        }
        #endregion
    }

}