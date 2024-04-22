using UnityEngine;

namespace TLTLUnity
{
    using System.Collections.Generic;
    using TLTLUnity.Data;

    /// <summary>
    /// Representa un predicado en TLTL en el proyecto de Unity, que
    /// luego podrá o no estar asignado a GameObjects.
    /// </summary>
    /// Es el responsable de la integración de los distintos modos
    /// que BB tiene de almacenar internamente los comportamientos
    /// con la forma de trabajar de Unity, en concreto es el pegamento
    /// que consigue:
    ///    - Que el comportamiento se haga explícito en la vista del
    ///    proyecto y pueda verse su contenido fácilmente.
    ///    - Que Unity se encargue de la serialización de los
    ///    comportamientos, utilizando nuestro formato.
    /// 
    /// Aunque esta clase se encuentre dentro del ensamblado, es luego derivada
    /// en una clase externa (PredicateAsset) que es la que Unity utiliza para serializar.
    /// Es necesario sacar el PredicateAsset del ensamblado para que no se pierdan las
    /// referencias de guid entre las distintas build.
    /// Si dejaramos el PredicateAsset dentro de las dll, cada vez que realizaramos
    /// una nueva build, las instancias de los Scriptable Object cambiarían de guid,
    /// haciendo incompatibles los BT de uan versión con la siguiente.
    /// De esta forma podemos controlar el guid que se asignará a los Scriptable Objects,
    /// que será el guid del BrickAsset.meta
    /// Además, la clase pública/hija tiene métodos relacionados con la serialización
    /// que son los invocados por Unity. Gracias a #if en ellas, podemos averiguar
    /// si la compilación es para el editor o no.
    public class InternalPredicateAsset : ScriptableObject
    {
        /// <summary>
        /// Nombre del predicado. Puede incluir '/' para separar
        /// a modo de jerarquía.
        /// </summary>
        [field: SerializeField] public string predicateName;

        /// <summary>
        /// Predicado al que representa, en el formato
        /// utilizado por el motor de ejecución.
        /// </summary>
        /// Durante la edición, se guarda el predicado
        /// extendido con la información del editor.
        [System.NonSerialized]
        public TLTLCore.Framework.TLTLPredicate predicate;


        /// <summary>
        /// Referencia al asset que incluye la información pura del predicado
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
        /// Método llamado en el momento de construir el brickasset con
        /// su identificador único.
        /// </summary>
        /// 
        /// El método es público porque será llamado desde el código del editor en la
        /// opción de creación del BrickAsset. El BrickAsset no puede ponerse un
        /// GUID único automáticamente porque necesitaría código del editor
        /// (AssetDatabase.AssetPathToGUID) del que no queremos depender aquí.
        /// Necesitamos que sea público para que pueda invocarse desde el código
        /// del editor mencionado, pero NO debería llamarlo nadie más.
        /// 
        /// <param name="guid"></param>
        public void _SetGUID(string guid)
        {
            _guid = guid;
        } // _SetGUID

    } // TLTLPredicateSO

} // namespace TLTL.ScriptableObjects

