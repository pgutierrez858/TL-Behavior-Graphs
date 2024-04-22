using UnityEngine;
using UnityEditor;
using TLTLUnity;
using TLTLCore;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(UnityBlackboard))]
public class UnityBlackboardPropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // Create property container element.
        var container = new VisualElement();

        // Vamos recorriendo los parámetros del comportamiento para escribir sus valores
        UnityBlackboard owner = (fieldInfo.GetValue(property.serializedObject.targetObject)) as UnityBlackboard;
        owner.updateParams(owner.predicateParams);

        if (owner.predicateParams == null) return container;
        Debug.Log(owner.predicateParams.values.Count);
        foreach (var v in owner.predicateParams.values)
        {
            // Buscamos el atributo en las listas serializadas "prefab-friendly"
            SerializedProperty valueProp = findProperty(property, v.Key);
            if (valueProp != null)
                container.Add(CreatePropertyVisualElement(v.Key, v.Value.type, valueProp));
        }

        return container;
    } // CreatePropertyGUI

    private VisualElement CreatePropertyVisualElement(string name, System.Type type, SerializedProperty property)
    {
        if (typeof(GameObject).IsAssignableFrom(type))
        {
            var objectField = new ObjectField(name);
            objectField.objectType = type;
            objectField.bindingPath = property.propertyPath;
            objectField.AddToClassList("unity-base-field__aligned");
            return objectField;
        }

        // PropertyField ya hace sólo el binding en su constructor,
        // por lo que no es necesario repetir ese paso después de llamarlo.
        var propertyField = new PropertyField(property, name);
        return propertyField;
    } // CreatePropertyVisualElement

    /// <summary>
    /// Busca el nombre del parámetro en las listas de parámetros guardadas
    /// en la pizarra y devuelve la SerializedProperty que representa el valor
    /// de ese parámetro.
    /// </summary>
    /// <param name="ownerProp"></param>
    /// <param name="paramName"></param>
    /// <returns></returns>
    private SerializedProperty findProperty(SerializedProperty ownerProp,
                                            string paramName)
    {
        foreach (var propName in propNames)
        {
            SerializedProperty names = ownerProp.FindPropertyRelative(propName + "Names");
            for (int i = 0; i < names.arraySize; ++i)
            {
                if (paramName == names.GetArrayElementAtIndex(i).stringValue)
                {
                    SerializedProperty values = ownerProp.FindPropertyRelative(propName);
                    if (values != null)
                        return ownerProp.FindPropertyRelative(propName).GetArrayElementAtIndex(i);
                }
            }
        }
        return null;
    }

    static string[] propNames = new string[] {
        "boolParams", "intParams", "floatParams", "stringParams",
        "colorParams", "objectParams", "layerMaskParams", "enumParams",
        "vector2Params", "vector3Params", "vector4Params", "rectParams",
        "animationCurveParams", "boundsParams", "gradientParams", "quaternionParams"
    };
}
