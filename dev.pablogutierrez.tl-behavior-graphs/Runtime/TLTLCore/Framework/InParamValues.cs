using System;
using System.Collections.Generic;

namespace TLTLCore
{
    /// <summary>
    /// Clase base tanto de la clase InParamValue como de OutParamValue y LocalParamValue.
    /// Está vacía. Su único propósito es hacer menos chapucero
    /// el BrickParamsInfo que tiene una referencia a un InParamValue
    /// o a un OutParamValue dependiendo de si representa un
    /// parámetro de entrada o de salida.
    /// </summary>
    public class ParamValue
    {
        public Type type;
        public string entryName; // Null si es una constante

        protected ParamValue()
        {

        }

        protected ParamValue(string blackboardEntryName, Type expectedType)
        {
            this.type = expectedType;
            this.entryName = blackboardEntryName;
        }

        public virtual bool IsBlackboard()
        {
            return true;
        }
    }


    /// <summary>
    /// Contiene la información sobre el valor de un parámetro de entrada 
    /// de un nodo. Si el parámetro de entrada es una constante
    /// "cableada", contiene esa constante. Si es una entrada en
    /// la pizarra, contiene el nombre de la clave donde
    /// buscarla. Tiene además un método para leer el valor que
    /// recibe la pizarra y devuelve el valor concreto.
    /// </summary>
    public class InParamValue : ParamValue
    {
        // Constructor sin parámetros para serialización.
        public InParamValue() { }

        // Constructores a partir de constantes
        public InParamValue(bool value) : this(typeof(bool), value)
        {
        }

        public InParamValue(int value) : this(typeof(int), value)
        {
        }

        public InParamValue(float value) : this(typeof(float), value)
        {
        }

        public InParamValue(string value) : this(typeof(string), value)
        {
        }

        // ... TODO ... ¿hacer el de System.object?

        // Para constantes de objetos de tipo desconocido (ENTITY, ...)
        public InParamValue(Type type, object constValue)
        {
            this.type = type;
            this.constValue = constValue;
            this._isBlackboard = false;
        }

        // Constructor de referencia a pizarra

        public InParamValue(string blackboardEntryName, Type expectedType, object defaultValue = null) :
            base(blackboardEntryName, expectedType)
        {
            this._isBlackboard = true;
            this.constValue = defaultValue;
        }

        public override bool IsBlackboard()
        {
            return _isBlackboard;
        }

        public void SetIsBlackboard(bool value)
        {
            _isBlackboard = value;
        }

        // Método de acceso

        /// <summary>
        /// Devuelve el valor del atributo, convertido al tipo T.
        /// </summary>
        /// Si hay que leer de la pizarra se utilizará el tipo
        /// del parámetro declarado, y no el parámetro de tipo T.
        /// <typeparam name="T"></typeparam>
        /// <param name="b"></param>
        /// <returns></returns>
        public T getValue<T>(Blackboard b)
        {
            if (constValue != null)
                return (T)constValue;
            else
                return (T)b.Get(entryName, type);
        }

        /// <summary>
        /// Propiedad que obtiene el valor por defecto del parámetro.
        /// </summary>
        /// <returns>Object con el valor por defecto del parámetro.</returns>
        public object DefaultValue
        {
            get { return constValue; }
        }

        private bool _isBlackboard;
        // TODO: ¡hacer esto no público!
        public object constValue; // Null si está en la pizarra y no tiene un
                                  // valor por defecto. EOC entryName será null
    }

    /// <summary>
    /// Contiene los valores dados a los distintos parámetros
    /// de entrada de un nodo. Se serializan/deserializan hacia/desde
    /// el fichero con el comportamiento para luego establecerse
    /// en tiempo de ejecución en las acciones.
    /// </summary>
    public class InParamValues
    {
        // TODO! Hacer esto bien, por tu padre....
        public Dictionary<string, InParamValue> values = new Dictionary<string, InParamValue>();
    }
}
