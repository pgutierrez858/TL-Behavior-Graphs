using System;
using System.Collections.Generic;

namespace TLTLCore
{
    /// <summary>
    /// Clase base tanto de la clase InParamValue como de OutParamValue y LocalParamValue.
    /// Est� vac�a. Su �nico prop�sito es hacer menos chapucero
    /// el BrickParamsInfo que tiene una referencia a un InParamValue
    /// o a un OutParamValue dependiendo de si representa un
    /// par�metro de entrada o de salida.
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
    /// Contiene la informaci�n sobre el valor de un par�metro de entrada 
    /// de un nodo. Si el par�metro de entrada es una constante
    /// "cableada", contiene esa constante. Si es una entrada en
    /// la pizarra, contiene el nombre de la clave donde
    /// buscarla. Tiene adem�s un m�todo para leer el valor que
    /// recibe la pizarra y devuelve el valor concreto.
    /// </summary>
    public class InParamValue : ParamValue
    {
        // Constructor sin par�metros para serializaci�n.
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

        // ... TODO ... �hacer el de System.object?

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

        // M�todo de acceso

        /// <summary>
        /// Devuelve el valor del atributo, convertido al tipo T.
        /// </summary>
        /// Si hay que leer de la pizarra se utilizar� el tipo
        /// del par�metro declarado, y no el par�metro de tipo T.
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
        /// Propiedad que obtiene el valor por defecto del par�metro.
        /// </summary>
        /// <returns>Object con el valor por defecto del par�metro.</returns>
        public object DefaultValue
        {
            get { return constValue; }
        }

        private bool _isBlackboard;
        // TODO: �hacer esto no p�blico!
        public object constValue; // Null si est� en la pizarra y no tiene un
                                  // valor por defecto. EOC entryName ser� null
    }

    /// <summary>
    /// Contiene los valores dados a los distintos par�metros
    /// de entrada de un nodo. Se serializan/deserializan hacia/desde
    /// el fichero con el comportamiento para luego establecerse
    /// en tiempo de ejecuci�n en las acciones.
    /// </summary>
    public class InParamValues
    {
        // TODO! Hacer esto bien, por tu padre....
        public Dictionary<string, InParamValue> values = new Dictionary<string, InParamValue>();
    }
}
