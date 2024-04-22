using System;
using System.Collections.Generic;
using System.Reflection;
using TLTLUnity.Data;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace TLTLUnity.Agents
{
    [Serializable]
    public class ObservableParameter
    {
        [SerializeField]
        public GameObject gameObject;

        [SerializeField]
        public string targetComponentName;

        [SerializeField]
        public string parameterName;
    }

    [Serializable]
    public class ObservableComponentField
    {
        /// <summary>
        /// Componente espec�fico de la escena al que queremos hacer referencia.
        /// </summary>
        [SerializeField]
        public Component component;

        /// <summary>
        /// Informaci�n de la propiedad espec�fica del componente a la que queremos hacer referencia.
        /// </summary>
        [SerializeField]
        public FieldInfo propertyInfo;

        /// <summary>
        /// Valor actual de la propiedad sobre la instancia concreta del componente sobre el que estamos trabajando.
        /// </summary>
        public object CurrentValue { get { return propertyInfo.GetValue(component); } }
    }

    [RequireComponent(typeof(TLTLRewardAutomatonComponent))]
    public class AutomatonStateSensorComponent : SensorComponent
    {
        [SerializeField]
        private bool treatStatesAsGoalSignals = false;

        [SerializeField]
        private MeshRenderer environmentMesh;

        /// <summary>
        /// Lista de entidades de la escena que queremos tener en cuenta para las observaciones del agente.
        /// (Por el momento, s�lo se a�aden las posiciones relativas del agente a dichas entidades).
        /// </summary>
        public List<Transform> observablePositionalEntities = new List<Transform>();

        /// <summary>
        /// Lista de triplas entidad/ componente/ par�metro que queremos observar 
        /// </summary>
        public List<ObservableParameter> observableParameters = new List<ObservableParameter>();

        void OnValidate()
        {
            ValidateComponentFields();
        }

        List<ObservableComponentField> ValidateComponentFields()
        {
            List<ObservableComponentField> validatedComponentFields = new List<ObservableComponentField>();
            foreach (var param in observableParameters)
            {
                if (param.targetComponentName != null)
                {
                    // Acceso al componente de la entrada...
                    Component component = param.gameObject.GetComponent(param.targetComponentName);

                    // PRIMERA COMPROBACI�N: el objeto especificado debe tener una instancia del componente
                    // referenciado por targetComponentName
                    if (component == null)
                    {
                        Debug.LogError($"Component of type {param.targetComponentName} not found in game object {param.gameObject.name}.");
                        continue;
                    }

                    // SEGUNDA COMPROBACI�N: usando reflexi�n, confirmamos que el componente dado tiene
                    // verdaderamente una propiedad con el nombre especificado como par�metro.

                    // empezamos consiguiendo el tipo del componente
                    Type componentType = component.GetType();

                    // a partir de este tipo podemos empezar a extraer propiedades por nombre
                    FieldInfo propertyInfo = componentType.GetField(param.parameterName);

                    // y confirmar que efectivamente tenemos la propiedad con el nombre especificado.
                    if (propertyInfo == null)
                    {
                        Debug.LogError($"Property {param.parameterName} not found in component {param.targetComponentName}.");
                        continue;
                    }

                    // TERCERA COMPROBACI�N: con la informaci�n de esta propiedad, vamos a verificar que tiene un tipo v�lido
                    // (int, float o bool por el momento).
                    if (propertyInfo.FieldType != typeof(int) &&
                        propertyInfo.FieldType != typeof(float) &&
                        propertyInfo.FieldType != typeof(bool))
                    {
                        Debug.LogError($"Property {param.parameterName} does not have a compatible type ({propertyInfo.FieldType}, must be float, int or bool).");
                        continue;
                    }


                    // BUENA PINTA: parece que todo ha ido bien y estamos en condiciones de a�adir el par�metro a nuestra lista de componentes
                    // validados que ser�n observados durante la ejecuci�n. OJO con quitar componentes sobre la marcha, no damos soporte a�n para esto...
                    validatedComponentFields.Add(new ObservableComponentField()
                    {
                        component = component,
                        propertyInfo = propertyInfo
                    });
                }
            }
            return validatedComponentFields;
        } // ValidateComponentFields

        /// <summary>
        /// Creates a BasicSensor.
        /// </summary>
        public override ISensor[] CreateSensors()
        {
            TLTLRewardAutomaton rewardAutomaton = GetComponent<TLTLRewardAutomatonComponent>().rewardAutomaton;
            List<ObservableComponentField> observableFields = ValidateComponentFields();

            return new ISensor[] {
                new AutomatonStateSensor(
                    rewardAutomaton,
                    observablePositionalEntities,
                    observableFields,
                    transform,
                    environmentMesh.bounds,
                    treatStatesAsGoalSignals,
                    rewardAutomaton.StateCount
                ) };
        }

    } // AutomatonStateSensorComponent
} // namespace TLTLUnity.Agents
