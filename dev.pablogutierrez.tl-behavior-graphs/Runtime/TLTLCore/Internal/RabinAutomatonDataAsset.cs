using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TLTLCore;
using TLTLCore.Framework;
using TLTLUnity.Agents;
using UnityEngine;

namespace TLTLUnity.Data
{
    /// <summary>
    /// clase destinada a almacenar la información necesaria para definir un predicado atómico en un
    /// autómata de Rabin. Aquí se incluyen los parámetros de entrada definidos a partir de variables
    /// de pizarra y el tipo del predicado que representan desde el punto de vista de la ejecución.
    /// Posteriormente estas proposiciones atómicas serán definidas en el objeto de datos del autómata
    /// de Rabin, y referenciadas por índice en las transiciones entre estados.
    /// </summary>
    [Serializable]
    public class RabinAutomatonAtomicPropositionData
    {
        [field: SerializeField] public TLTLInputParamData[] InputParams { get; set; }
        /// <summary>
        /// Nombre de la clase que representa el tipo de predicado codificado por este nodo (la que implementa el usuario).
        /// </summary>
        [field: SerializeField] public string PredicateType { get; set; }

    } // RabinAutomatonAtomicPropositionData

    public enum LabelExpressionType
    {
        BOOLEAN, INT, NEGATION, OR, AND
    } // LabelExpressionType

    /// <summary>
    /// label-expr ::= BOOLEAN | INT | "!" label-expr
    ///         | "(" label-expr ")"
    ///         | label-expr "&" label-expr
    ///         | label-expr "|" label-expr
    /// </summary>
    [Serializable]
    public class LabelExpression
    {
        [field: SerializeField] public LabelExpressionType Type { get; set; }
        [field: SerializeField] public bool BooleanValue { get; set; }
        [field: SerializeField] public int IntValue { get; set; }
        [field: SerializeField] public LabelExpression LeftExpression { get; set; }
        [field: SerializeField] public LabelExpression RightExpression { get; set; }
        [field: SerializeField] public LabelExpression InnerExpression { get; set; }

    } // LabelExpression

    /// <summary>
    /// Definición de una transición en la que se define el índice de su estado final en la 
    /// lista de estados del autómata y los índices de cada una de las proposiciones atómicas
    /// que deben cumplirse para que se pueda tomar la transición. Puesto que las proposiciones
    /// pueden aparecer negadas, hacemos una distinción explícita de aquellas que aparecen de forma
    /// positiva y aquellas que lo hacen de forma negativa.
    /// </summary>
    [Serializable]
    public class RabinAutomatonTransitionData
    {
        /// <summary>
        /// índice del estado final en la lista de estados contemplados en el autómata de Rabin.
        /// </summary>
        [field: SerializeField] public int EndStateIndex { get; set; }

        /// <summary>
        /// Expresión que etiqueta la transición y que deberá ser parseada posteriormente
        /// para obtener las condiciones en tiempo de ejecución.
        /// </summary>
        [field: SerializeField] public string LabelExpression { get; set; }

        /// <summary>
        /// Tokens asociados a la transición. Estos sólo tienen sentido desde el punto
        /// de vista de las condiciones de aceptación de Rabin, que se traducen en que
        /// ciertos tokens deberán ser visitados infinitamente durante la ejecución, mientras
        /// que el resto sólo podrán ser visitados como mucho un número finito de veces.
        /// </summary>
        [field: SerializeField] public string[] TransitionTokens { get; set; }

    } // RabinAutomatonTransitionData

    [Serializable]
    public class RabinAutomatonStateData
    {
        /// <summary>
        /// Etiqueta del estado tal y como viene definida en el output de la librería de transformación a autómatas de Rabin.
        /// </summary>
        [field: SerializeField] public string StateLabel { get; set; }

        /// <summary>
        /// transiciones disponibles desde este estado.
        /// </summary>
        [field: SerializeField] public RabinAutomatonTransitionData[] Transitions { get; set; }

    } // RabinAutomatonStateData

    /// <summary>
    /// Pairs {(E1,F1),…,(Ek,Fk)} where Ei should be visited finitely often, and Fi should be visited infinitely often.
    /// </summary>
    [Serializable]
    public class RabinAcceptancePair
    {
        /// <summary>
        /// List of tokens that should be visited for a finite amount of times for a word to be accepted.
        /// </summary>
        [field: SerializeField] public string[] FinTokens { get; set; }


        /// <summary>
        /// List of tokens that should be visited for an infinite amount of times for a word to be accepted.
        /// </summary>
        [field: SerializeField] public string[] InfTokens { get; set; }
    } // RabinAcceptancePair

    public class RabinAutomatonDataAsset : ScriptableObject
    {
        /// <summary>
        /// Proposiciones atómicas disponibles en el predicado de partida, a referenciar por las transiciones
        /// entre estados que se definan en este objeto de datos.
        /// </summary>
        [field: SerializeField] public RabinAutomatonAtomicPropositionData[] AtomicPropositions { get; set; }

        /// <summary>
        /// Estados disponibles en el atómata.
        /// </summary>
        [field: SerializeField] public RabinAutomatonStateData[] States { get; set; }

        /// <summary>
        /// Condiciones de aceptación posibles para el autómata.
        /// </summary>
        [field: SerializeField] public RabinAcceptancePair[] AcceptanceConditions { get; set; }

        /// <summary>
        /// Datos de la pizarra empleada para especificar la lista de parámetros de entrada
        /// aceptables por las proposiciones atómicas y los tipos esperados por cada uno de ellos.
        /// </summary>
        [field: SerializeField] public TLTLBlackboardData BlackboardData { get; set; }

        /// <summary>
        /// Índice del estado inicial del autómata.
        /// </summary>
        [field: SerializeField] public int InitialState { get; set; }

        public void InitializeFromSpecs(string hoaSpec, List<RabinAutomatonAtomicPropositionData> atomicPropositions)
        {
            AtomicPropositions = atomicPropositions.ToArray();
            BlackboardData = new TLTLBlackboardData();

            var lines = hoaSpec.Split(new[] { "\r\n" }, StringSplitOptions.None);

            // búsqueda del número de estados
            bool numberOfStatesFound = false;
            int i = 0;
            while (i < lines.Length && !numberOfStatesFound)
            {
                // estamos esperando el número de estados
                Match match = Regex.Match(lines[i], @"States: (\d+)");

                if (match.Success)
                {
                    numberOfStatesFound = true;
                    string numberStr = match.Groups[1].Value;

                    if (int.TryParse(numberStr, out int number))
                    {
                        // tenemos el número de estados, inicializamos la lista de estados
                        States = new RabinAutomatonStateData[number];
                    }
                    else
                    {
                        Debug.LogError("Error al parsear el número de estados del fichero HOA.");
                        return;
                    }
                }
                ++i;
            }

            // búsqueda de las condiciones de aceptación
            bool acceptanceConditionsFound = false;
            i = 0;
            while (i < lines.Length && !acceptanceConditionsFound)
            {
                // estamos esperando la lista de condiciones de aceptación
                Match matchAcceptanceLine = Regex.Match(lines[i], @"Acceptance: (\d+)");

                if (matchAcceptanceLine.Success && int.TryParse(matchAcceptanceLine.Groups[1].Value, out int acceptanceCount))
                {
                    acceptanceConditionsFound = true;

                    // hay tantas condiciones de aceptación como pares
                    // el número que se proporciona aquí se corresponde con el número de elementos 
                    // fin o inf, por lo que basta con dividir entre 2 para obtener las parejas
                    AcceptanceConditions = new RabinAcceptancePair[acceptanceCount/2];

                    // expresión regular para encontrar parejas de fin/inf en la línea
                    Regex regex = new Regex(@"Fin\((\d+)\)&Inf\((\d+)\)");

                    // Find all matches in the input string
                    MatchCollection matches = regex.Matches(lines[i]);

                    if(matches.Count != AcceptanceConditions.Length)
                    {
                        Debug.LogError("Error al parsear el número de condiciones de aceptación del fichero HOA.");
                        return;
                    }

                    // Loop through the matches and extract the numbers
                    for (int m = 0; m < matches.Count; m++)
                    {
                        Match match = matches[m];
                        string finNumber = match.Groups[1].Value;
                        string infNumber = match.Groups[2].Value;

                        AcceptanceConditions[m] = new RabinAcceptancePair()
                        {
                            InfTokens = new string[] { infNumber },
                            FinTokens = new string[] { finNumber }
                        };
                    }
                }
                ++i;
            }

            // avanzamos hasta llegar al cuerpo de la definición
            while (i < lines.Length && !(lines[i] == "--BODY--")) { ++i; }
            ++i; // avanzamos una más para empezar con los estados en sí

            // creación de los estados uno a uno
            int remainingStates = States.Length;
            while (i < lines.Length && remainingStates > 0)
            {
                // estamos esperando la declaración de un estado
                Match match = Regex.Match(lines[i], @"State: (\d+) ""(.+?)""");

                if (match.Success)
                {
                    // hemos encontrado la declaración de un estado
                    string stateIndex = match.Groups[1].Value;
                    string stateName = match.Groups[2].Value;

                    if (int.TryParse(stateIndex, out int number) && number >= 0 && number < States.Length)
                    {
                        remainingStates--;

                        // tenemos el índice del estado y es válido, podemos inicializarlo y empezar a añadir sus transiciones
                        States[number] = new RabinAutomatonStateData();
                        States[number].StateLabel = stateName;

                        List<RabinAutomatonTransitionData> transitions = new List<RabinAutomatonTransitionData>();
                        ++i; // avanzamos a la siguiente línea que debería tener la primera transición o un estado
                        bool transitionFound = true;
                        while (i < lines.Length && transitionFound)
                        {
                            // estamos esperando la declaración de una transición
                            Match transitionMatch = Regex.Match(lines[i], @"\[(.+)] (\d+) {(.+)}");

                            if (transitionMatch.Success)
                            {
                                // hemos encontrado la declaración de una transición
                                string conditions = transitionMatch.Groups[1].Value;
                                int endStateIndex = int.Parse(transitionMatch.Groups[2].Value);
                                string transitionTokens = transitionMatch.Groups[3].Value;
                                if(transitionTokens.EndsWith(" ")) transitionTokens = transitionTokens.Remove(transitionTokens.Length - 1);

                                RabinAutomatonTransitionData transitionData = new RabinAutomatonTransitionData();
                                transitionData.EndStateIndex = endStateIndex;
                                transitionData.LabelExpression = conditions;
                                transitionData.TransitionTokens = transitionTokens.Split(' ');
                                transitions.Add(transitionData);

                                ++i;
                            }
                            // nos hemos pasado de lo válido, avanzamos con el siguiente estado
                            else transitionFound = false;
                        }
                        States[number].Transitions = transitions.ToArray();
                    }
                    else
                    {
                        Debug.LogError("Error al parsear uno de los estados del fichero HOA.");
                        return;
                    }
                }
            }

            if (i >= lines.Length)
            {
                Debug.LogError("Fin de fichero alcanzado antes de línea de fin en archivo HOA.");
                return;
            }

            if (remainingStates > 0)
            {
                Debug.LogError("No se encontraron todos los estados especificados en la cabecera del fichero HOA.");
                return;
            }

            // si queda otra línea, sólo puede ser la de END
            if (lines[i] != "--END--")
            {
                Debug.LogError("Se esperaba encontrar un --END-- al final de las definiciones de estados.");
                return;
            }
        } // InitializeFromHOASpecification

    } // RabinAutomatonDataAsset

    public static class HOAParsingTools
    {
        // to check if the input character
        // is an operator or a '('
        private static int IsOperator(char input)
        {
            switch (input)
            {
                case '&':
                    return 1;
                case '|':
                    return 1;
                case '!':
                    return 1;
                case '(':
                    return 1;
            }
            return 0;
        } // IsOperator

        // to check if the input character is an operand (int)
        private static int IsNumericOperand(char input)
        {
            if (input >= 48 && input <= 57)
            {
                return 1;
            }
            return 0;
        } // IsNumericOperand

        // to check if the input character is an operand (bool)
        private static int IsBooleanOperand(char input)
        {
            if (input == 't' || input == 'f')
            {
                return 1;
            }
            return 0;
        } // IsBooleanOperand

        // function to return precedence value
        // if operator is present in stack
        private static int InPrec(char input)
        {
            switch (input)
            {
                case '|':
                    return 2;
                case '&':
                    return 4;
                case '!':
                    return 6;
                case '(':
                    return 0;
            }
            return 0;
        } // InPrec

        // function to return precedence value
        // if operator is present outside stack.
        private static int OutPrec(char input)
        {
            switch (input)
            {
                case '|':
                    return 1;
                case '&':
                    return 3;
                case '!':
                    return 5;
                case '(':
                    return 100;
            }
            return 0;
        } // OutPrec

        private static void ReduceExpressionStack(Stack<LabelExpression> expStack, char operand)
        {
            if (expStack.Count == 0) return;
            if (operand == '!')
            {
                LabelExpression exp = expStack.Pop();
                expStack.Push(new LabelExpression()
                {
                    Type = LabelExpressionType.NEGATION,
                    InnerExpression = exp
                });
            }
            else if (operand == '|' || operand == '&')
            {
                LabelExpression expRight = expStack.Pop();
                LabelExpression expLeft = expStack.Pop();
                expStack.Push(new LabelExpression()
                {
                    Type = operand == '&' ? LabelExpressionType.AND : LabelExpressionType.OR,
                    LeftExpression = expLeft,
                    RightExpression = expRight
                });
            }
        } // ReduceExpressionStack

        /// <summary>
        /// Construye una expresión <see cref="LabelExpression"/> a partir de un string
        /// con la especificación dada en un formato label-expr (http://adl.github.io/hoaf/)
        /// en HOA. Esto se usará mayoritariamente para parsear las condiciones de las transiciones.
        /// </summary>
        public static LabelExpression BuildLabelExpression(string input)
        {
            Stack<LabelExpression> expStack = new Stack<LabelExpression>();
            Stack<char> s = new Stack<char>();

            // while input is not NULL iterate
            int i = 0;
            while (input.Length != i)
            {

                // if character is an integer: ¿cómo de largo es?
                if (IsNumericOperand(input[i]) == 1)
                {
                    string op = $"{input[i]}";
                    int k = 1; // offset
                    // buscamos las fronteras del número actual
                    while (i + k < input.Length && IsNumericOperand(input[i + k]) == 1)
                    {
                        op += input[i + k]; // añadimos el siguiente dígito a la cadena
                        k++;
                    }
                    i = i + k - 1; // modificamos el valor de i para incluir todos los caracteres adicionales procesados

                    int opInt = int.Parse(op); // obtenemos el valor numérico del operando
                    LabelExpression operandExp = new LabelExpression
                    {
                        Type = LabelExpressionType.INT,
                        IntValue = opInt
                    };
                    expStack.Push(operandExp); // colocamos el operando numérico en la cima de la pila
                    // expStack.Push();
                }

                // character is boolean
                else if (IsBooleanOperand(input[i]) == 1)
                {
                    LabelExpression operandExp = new LabelExpression
                    {
                        Type = LabelExpressionType.BOOLEAN,
                        BooleanValue = input[i] == 't'
                    };
                    expStack.Push(operandExp); // colocamos el operando booleano en la cima de la pila
                }

                // if input is an operator, push
                else if (IsOperator(input[i]) == 1)
                {
                    if (s.Count == 0
                        || OutPrec(input[i]) > InPrec(s.Peek()))
                    {
                        s.Push(input[i]);
                    }
                    else
                    {
                        while (s.Count != 0
                               && OutPrec(input[i]) < InPrec(s.Peek()))
                        {
                            ReduceExpressionStack(expStack, s.Peek());
                            s.Pop();
                        }
                        s.Push(input[i]);
                    }
                } // condition for opening bracket
                else if (input[i] == ')')
                {
                    while (s.Peek() != '(')
                    {
                        ReduceExpressionStack(expStack, s.Peek());
                        s.Pop();

                        // if opening bracket not present
                        if (s.Count == 0)
                        {
                            Console.Write("Wrong input\n");
                            return null;
                        }
                    }

                    // pop the opening bracket.
                    s.Pop();
                }
                i++;
            }

            // pop the remaining operators
            while (s.Count != 0)
            {
                if (s.Peek() == '(')
                {
                    Console.Write("\n Wrong input\n");
                    return null;
                }
                ReduceExpressionStack(expStack, s.Peek());
                s.Pop();
            }

            if (expStack.Count == 1)
            {
                return expStack.Pop();
            }
            return null;
        }
    }

    public static class RabinAutomatonDataAssetTools
    {
        private static TLTLPredicate BuildRuntimeAtomicProposition(RabinAutomatonAtomicPropositionData atomicProposition, Blackboard blackboard)
        {
            // cada proposición atómica contiene información sobre su tipo y parámetros de entrada
            Type nodeType = Type.GetType(atomicProposition.PredicateType);
            // creación de la instancia del tipo correcto, a falta de rellenar con parámetros
            TLTLPredicate predicateInstance = Activator.CreateInstance(nodeType) as TLTLPredicate;

            // obtención de las propiedades de entrada del predicado actual
            var inProperties = TLTLPredicate.GetInProperties(nodeType);

            foreach (var property in inProperties)
            {
                // NOTA: Todas las propiedades de entrada de este predicado van a ser necesariamente
                // objetos distintos a otros TLTLPredicate por construcción. Por tanto,
                // estarán registradas en la pizarra, y aquí lo único que hay que hacer es vincularlas.
                TLTLInputParamData inputParamData = atomicProposition.InputParams.First(p => p.InParamName == property.Name);
                property.SetValue(predicateInstance, blackboard.Get(inputParamData.BlackboardParamName, property.PropertyType));
            }
            return predicateInstance;
        } // BuildRuntimeAtomicProposition

        private static TLTLPredicate BuildRuntimeEdgeCondition(LabelExpression condition, TLTLPredicate[] runtimePropositions)
        {
            // caso base 1: T or F
            if (condition.Type == LabelExpressionType.BOOLEAN)
            {
                // 't'
                if (condition.BooleanValue == true) { return new TruePredicate(); }
                // 'f'
                else { return new NotPredicate() { Predicate = new TruePredicate() }; }
            }

            // caso base 2: referencia a un predicado atómico
            if (condition.Type == LabelExpressionType.INT)
            {
                // devolvemos la proposición de la lista de predicados en ejecución a la que apunta el índice
                return runtimePropositions[condition.IntValue];
            }

            // caso recursivo 1: negación
            if (condition.Type == LabelExpressionType.NEGATION)
            {
                return new NotPredicate() { Predicate = BuildRuntimeEdgeCondition(condition.InnerExpression, runtimePropositions) };
            }

            // caso recursivo 2: conjunción
            if (condition.Type == LabelExpressionType.AND)
            {
                return new AndPredicate()
                {
                    A = BuildRuntimeEdgeCondition(condition.LeftExpression, runtimePropositions),
                    B = BuildRuntimeEdgeCondition(condition.RightExpression, runtimePropositions)
                };
            }

            // caso recursivo 3: disyunción
            if (condition.Type == LabelExpressionType.OR)
            {
                return new OrPredicate()
                {
                    A = BuildRuntimeEdgeCondition(condition.LeftExpression, runtimePropositions),
                    B = BuildRuntimeEdgeCondition(condition.RightExpression, runtimePropositions)
                };
            }

            return null;
        } // BuildRuntimeEdgeCondition

        public static TLTLRewardAutomaton BuildRuntimeAutomaton(RabinAutomatonDataAsset rabinAutomaton, Blackboard blackboard)
        {
            RabinAutomatonAtomicPropositionData[] atomicPropositions = rabinAutomaton.AtomicPropositions;
            RabinAutomatonStateData[] states = rabinAutomaton.States;

            // Ya tenemos la estructura disponible, lo único que nos falta es convertirla en un objeto 
            // ejecutable, hidratando cada uno de los predicados del fichero con clases de runtime.

            // creación de los predicados en runtime
            TLTLPredicate[] runtimePropositions = atomicPropositions.Select(ap => BuildRuntimeAtomicProposition(ap, blackboard)).ToArray();

            // una vez tenemos las proposiciones de runtime, basta con realizar una traducción directa
            // entre tipos de assets y tipos en ejecución.
            TLTLRewardAutomatonState[] rewardAutomatonStates = states.Select(s =>
            {
                TLTLRewardAutomatonState state = new TLTLRewardAutomatonState(s.StateLabel);
                foreach (var transition in s.Transitions)
                {
                    LabelExpression exp = HOAParsingTools.BuildLabelExpression(transition.LabelExpression);
                    TLTLPredicate pred = BuildRuntimeEdgeCondition(exp, runtimePropositions);
                    state.AddTransition(pred, transition.EndStateIndex, transition.TransitionTokens);
                }
                return state;
            }).ToArray();

            TLTLRewardAutomaton rewardAutomaton = new TLTLRewardAutomaton(rewardAutomatonStates, rabinAutomaton.InitialState, rabinAutomaton.AcceptanceConditions);
            return rewardAutomaton;
        } // BuildRuntimeAutomaton
    } // RabinAutomatonDataAssetTools

} // namespace TLTLUnity.Data
