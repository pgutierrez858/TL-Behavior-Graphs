using UnityEngine;

namespace TLTLUnity
{
    using System.Collections.Generic;
    using TLTLUnity.Data;

    /// <summary>
    /// Representa un predicado en TLTL en el proyecto de Unity, que
    /// luego podr� o no estar asignado a GameObjects.
    /// </summary>
    /// Es el responsable de la integraci�n de los distintos modos
    /// que BB tiene de almacenar internamente los comportamientos
    /// con la forma de trabajar de Unity, en concreto es el pegamento
    /// que consigue:
    ///    - Que el comportamiento se haga expl�cito en la vista del
    ///    proyecto y pueda verse su contenido f�cilmente.
    ///    - Que Unity se encargue de la serializaci�n de los
    ///    comportamientos, utilizando nuestro formato.
    /// 
    /// Aunque esta clase se encuentre dentro del ensamblado, es luego derivada
    /// en una clase externa (PredicateAsset) que es la que Unity utiliza para serializar.
    /// Es necesario sacar el PredicateAsset del ensamblado para que no se pierdan las
    /// referencias de guid entre las distintas build.
    /// Si dejaramos el PredicateAsset dentro de las dll, cada vez que realizaramos
    /// una nueva build, las instancias de los Scriptable Object cambiar�an de guid,
    /// haciendo incompatibles los BT de uan versi�n con la siguiente.
    /// De esta forma podemos controlar el guid que se asignar� a los Scriptable Objects,
    /// que ser� el guid del BrickAsset.meta
    /// Adem�s, la clase p�blica/hija tiene m�todos relacionados con la serializaci�n
    /// que son los invocados por Unity. Gracias a #if en ellas, podemos averiguar
    /// si la compilaci�n es para el editor o no.
    public class InternalPredicateAsset : ScriptableObject
    {
        /// <summary>
        /// Nombre del predicado. Puede incluir '/' para separar
        /// a modo de jerarqu�a.
        /// </summary>
        [field: SerializeField] public string predicateName;

        /// <summary>
        /// Predicado al que representa, en el formato
        /// utilizado por el motor de ejecuci�n.
        /// </summary>
        /// Durante la edici�n, se guarda el predicado
        /// extendido con la informaci�n del editor.
        [System.NonSerialized]
        public TLTLCore.Framework.TLTLPredicate predicate;


        /// <summary>
        /// Referencia al asset que incluye la informaci�n pura del predicado
        /// </summary>
        [field: SerializeField] public TLTLPredicateDataAsset predicateFile;

        [SerializeField]
        private string _guid = null;


        public void Initialize()
        {
            // TODO: Mejor con On Before/After Serialize
            
        } // Initialize 

        public string GUID
        {
            get
            {
                return _guid;
            }
        } // GUID

        /// <summary>
        /// M�todo llamado en el momento de construir el brickasset con
        /// su identificador �nico.
        /// </summary>
        /// 
        /// El m�todo es p�blico porque ser� llamado desde el c�digo del editor en la
        /// opci�n de creaci�n del BrickAsset. El BrickAsset no puede ponerse un
        /// GUID �nico autom�ticamente porque necesitar�a c�digo del editor
        /// (AssetDatabase.AssetPathToGUID) del que no queremos depender aqu�.
        /// Necesitamos que sea p�blico para que pueda invocarse desde el c�digo
        /// del editor mencionado, pero NO deber�a llamarlo nadie m�s.
        /// 
        /// <param name="guid"></param>
        public void _SetGUID(string guid)
        {
            _guid = guid;
        } // _SetGUID

    } // TLTLPredicateSO

} // namespace TLTL.ScriptableObjects

