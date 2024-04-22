using System;
using System.Collections.Generic;

namespace TLTLCore
{

    /// <summary>
    /// Pizarra utilizada como intercambio de informaci�n
    /// entre comportamientos. Cada entrada est� tipada y
    /// no pueden hacerse asignaciones incompatibles con esos
    /// tipos.
    /// La propia pizarra permite gestionar una pila de pizarras,
    /// de forma que en las b�squedas/lecturas, si no se encuentra
    /// el s�mbolo buscado se delega en las pizarras inferiores.
    /// </summary>
    public class Blackboard
    {

        public Blackboard() { this.nextInStack = null; }

        public Blackboard(Blackboard nextInStack)
        {
            this.nextInStack = nextInStack;
        }

        /// <summary>
        /// Devuelve la siguiente pizarra en la cadena de
        /// delegaci�n.
        /// </summary>
        /// <returns></returns>
        public Blackboard next()
        {
            return nextInStack;
        }

        /// <summary>
        /// Devuelve null si no est� o si, estando, no
        /// es del tipo correcto.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public object Get(string name, Type t)
        {
            if (String.IsNullOrEmpty(name))
                // TODO: alg�n mensaje o algo para avisar al usuario... de momento,
                // simplemente devuelve null como si no estuviera
                return null;

            Entry e = default(Entry);
            if (!_blackboard.TryGetValue(name, out e))
            {
                if (nextInStack != null)
                    return nextInStack.Get(name, t);
                else
                    return null;
            }

            if (e.type != t)
            {
                // TODO: alg�n mensaje o algo...
                return null;
            }
            return e.value;
        }

        /// <summary>
        /// Devuelve false si hay alg�n error en la escritura
        /// (t�picamente, error de tipos, pues ya hab�a una entrada
        /// en ese nombre con otro tipo). Tambi�n devuelve error
        /// si el nombre pasado es vac�o.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="t"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Set(string name, Type t, object value)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            Entry e = default(Entry);
            if (!_blackboard.TryGetValue(name, out e))
            {
                // Nueva entrada
                e.type = t;
                e.value = value;
                _blackboard.Add(name, e);
                return true;
            }

            // Entrada ya existente.
            // Comprobamos tipos
            if (e.type != t)
                return false;
            e.value = value;
            _blackboard[name] = e;
            return true;
        }

        private struct Entry
        {
            public Type type;
            public object value;
        }

        /// <summary>
        /// Cada entrada guarda el tipo (BBType) y el valor.
        /// </summary>
        Dictionary<string, Entry> _blackboard = new Dictionary<string, Entry>();

        /// <summary>
        /// Siguiente pizarra en la jerarqu�a. Utilizada
        /// en las lecturas.
        /// </summary>
        Blackboard nextInStack;
    }
}