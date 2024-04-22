using System;
using System.Collections.Generic;
using System.Linq;

namespace TLTLPredicateBuilder.Utils
{
    /// <summary>
    /// See the procedure from https://en.wikipedia.org/wiki/Linear_temporal_logic_to_B%C3%BCchi_automaton.
    /// </summary>
    public class LabelingFunction
    {
        public Dictionary<uint, LTLSet> Neg;
        public Dictionary<uint, LTLSet> Pos;

        public LabelingFunction()
        {
            Neg = new Dictionary<uint, LTLSet>();
            Pos = new Dictionary<uint, LTLSet>();
        }

        public LabelingFunction(Dictionary<uint, LTLSet> Neg_, Dictionary<uint, LTLSet> Pos_)
        {
            Neg = Neg_;
            Pos = Pos_;
        }
    } // LabelingFunction

    public class AtomicPropositions
    {
        public HashSet<LTLFormula> AP_List;

        public AtomicPropositions()
        {
            AP_List = new HashSet<LTLFormula>();
        }

        public AtomicPropositions(HashSet<LTLFormula> List_)
        {
            AP_List = List_;
        }

    } //AtomicPropositions
      // ---------------------------------------------------------
      //                    LTL: LTL formulas
      // ---------------------------------------------------------
    #region LTL: LTL formulas

    public abstract class LTLFormula : IEquatable<LTLFormula>
    {
        public bool Equals(LTLFormula other)
        {
            if (other is null) return false;

            // two formulas are the same if their string representation matches.
            return ToString().Equals(other.ToString());
        } // Equals

        // We force the implementing class to override the ToString method of LTL Formulas.
        public abstract override string ToString();

        public abstract LTLFormula ToNNF();

        // Generate the subsets
        private static void GenerateSubsets(List<LTLFormula> originalList, LTLSet currentSubset, int index, List<LTLSet> allSubsets)
        {
            allSubsets.Add(new LTLSet(currentSubset));

            for (int i = index; i < originalList.Count; i++)
            {
                currentSubset.Add(originalList[i]);
                GenerateSubsets(originalList, currentSubset, i + 1, allSubsets);
                currentSubset.Remove(originalList[i]);
            }
        } // GenerateSubsets

        public static List<LTLSet> GetAllSubsets(AtomicPropositions propositions)
        {
            List<LTLFormula> originalList = new List<LTLFormula>(propositions.AP_List);
            List<LTLSet> allSubsets = new List<LTLSet>();
            GenerateSubsets(originalList, new LTLSet(), 0, allSubsets);
            return allSubsets;
        } // GetAllSubsets
    } // LTLFormula

    /// <summary>
    /// An atomic proposition in an LTL Formula.
    /// Since this is merely a syntactic model, there is no need to define any sort of numerical or truthy
    /// value for a proposition of this type (we are only concerned about identifying predicates).
    /// </summary>
    public class LTLAtomicProposition : LTLFormula
    {
        public string Formula { get; }

        public LTLAtomicProposition(string formula)
        {
            Formula = formula;
        } // LTLAtomicProposition

        public override string ToString()
        {
            return Formula;
        } // ToString

        public override LTLFormula ToNNF()
        {
            // Nothing to do, AtomicProposition is in NNF
            return this;
        } // ToNNF

    } // LTLAtomicProposition

    /// <summary>
    /// A simple negation of an LTL predicate. Takes in an LTLFormula as a parameter in its
    /// constructor, which is negated, and stores a public reference to the negated formula.
    /// </summary>
    public class LTLNegation : LTLFormula
    {
        public LTLFormula Formula { get; }

        public LTLNegation(LTLFormula formula)
        {
            Formula = formula;
        } // LTLNegation

        public LTLNegation(LTLSet formula)
        {
            if (formula.Count == 1)
            {
                Formula = formula.First();
            }// LTLNegation
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            LTLNegation other = (LTLNegation)obj;
            return other.Equals(this);
        } //Equals

        public override int GetHashCode()
        {
            return base.GetHashCode();
        } //GetHashCode
        public override string ToString()
        {
            return $"!({Formula})";
        }

        public override LTLFormula ToNNF()
        {
            var nnfFormula = Formula.ToNNF();

            if (nnfFormula is LTLTrue true_)
            {
                return new LTLFalse();
            }
            if (nnfFormula is LTLFalse false_)
            {
                return new LTLTrue();
            }
            if (nnfFormula is LTLNegation negation)
            {
                return negation.Formula.ToNNF();
            }
            // ¬G(A) = ¬ (false R A) = (true U ¬ A.ToNNF() )
            if (Formula is LTLGlobally globally)
            {   // ¬ (false R A)
                LTLNegation negation_ = new LTLNegation(globally.ToNNF());
                return negation_.ToNNF();
            }
            // ¬F(A) = ¬(true U A) = (false R ¬ A.ToNNF() )
            if (Formula is LTLEventually eventually)
            {
                LTLNegation negation_ = new LTLNegation(eventually.ToNNF());
                return negation_.ToNNF();
            }
            // ¬(A ∧ B) = (¬A) V (¬B)
            if (Formula is LTLConjunction conjunction)
            {
                return new LTLDisjunction(new LTLNegation(conjunction.LeftFormula.ToNNF()), new LTLNegation(conjunction.RightFormula.ToNNF()));
            }
            //¬(A V B) = (¬A) ∧ (¬B)
            if (Formula is LTLDisjunction disjunction)
            {
                return new LTLConjunction(new LTLNegation(disjunction.LeftFormula.ToNNF()), new LTLNegation(disjunction.RightFormula.ToNNF()));
            }
            // ¬(φ U ψ) ≡ ((¬ φ) R (¬ ψ))
            if (Formula is LTLUntil until)
            {
                return new LTLRelease(new LTLNegation(until.LeftFormula), new LTLNegation(until.RightFormula)).ToNNF();
            }
            // ¬(φ R ψ) ≡ ((¬ φ) U (¬ ψ))
            if (Formula is LTLRelease release)
            {
                return new LTLUntil(new LTLNegation(release.LeftFormula), new LTLNegation(release.RightFormula)).ToNNF();
            }
            if (nnfFormula is LTLAtomicProposition atomic)
            {
                return this; // ¬(f) is NNF
            }

            // ¬(X(p)) -> ¬(X(p))  OR  // ¬(X(A)) -> ¬(X(A.ToNNF()))
            else
            {
                return new LTLNegation(nnfFormula);
            }

        } // ToNNF

    } // LTLNegation

    /// <summary>
    /// Simple conjunction supporting two predicates (TODO: arbitrary number).
    /// </summary>
    public class LTLConjunction : LTLFormula
    {
        public LTLFormula LeftFormula { get; }
        public LTLFormula RightFormula { get; }

        public LTLConjunction(LTLFormula leftFormula, LTLFormula rightFormula)
        {
            LeftFormula = leftFormula;
            RightFormula = rightFormula;
        } // LTLConjunction

        public override string ToString()
        {
            return $"({LeftFormula}) & ({RightFormula})";
        } // ToString

        public override LTLFormula ToNNF()
        {
            // Nothing to do, AtomicProposition is in NNF
            var nnfLeft = LeftFormula.ToNNF();
            var nnfRight = RightFormula.ToNNF();
            return new LTLConjunction(nnfLeft, nnfRight);
        } // ToNNF

    } // LTLConjunction

    public class LTLDisjunction : LTLFormula
    {
        public LTLFormula LeftFormula { get; }
        public LTLFormula RightFormula { get; }

        public LTLDisjunction(LTLFormula leftFormula, LTLFormula rightFormula)
        {
            LeftFormula = leftFormula;
            RightFormula = rightFormula;
        } // LTLConjunction

        public override string ToString()
        {
            return $"({LeftFormula}) | ({RightFormula})";
        } // ToString

        public override LTLFormula ToNNF()
        {
            // Nothing to do, AtomicProposition is in NNF
            var nnfLeft = LeftFormula.ToNNF();
            var nnfRight = RightFormula.ToNNF();
            return new LTLDisjunction(nnfLeft, nnfRight);
        } // ToNNF

    } // LTLConjunction

    public class LTLNext : LTLFormula
    {
        public LTLFormula Formula { get; }

        public LTLNext(LTLFormula formula)
        {
            Formula = formula;
        } // LTLNext

        public override string ToString()
        {
            return $"X({Formula})";
        } // ToString

        public override LTLFormula ToNNF()
        {
            // Nothing to do, AtomicProposition is in NNF
            var nnfFormula = Formula.ToNNF();
            return new LTLNext(nnfFormula);
        } // ToNNF

    } // LTLNext

    public class LTLRelease : LTLFormula
    {
        public LTLFormula LeftFormula { get; }
        public LTLFormula RightFormula { get; }

        public LTLRelease(LTLFormula leftFormula, LTLFormula rightFormula)
        {
            LeftFormula = leftFormula;
            RightFormula = rightFormula;
        } // LTLRelease

        public override string ToString()
        {
            return $"({LeftFormula}) R ({RightFormula})";
        } // ToString

        public override LTLFormula ToNNF()
        {
            // Nothing to do, AtomicProposition is in NNF
            var nnfLeft = LeftFormula.ToNNF();
            var nnfRight = RightFormula.ToNNF();
            return new LTLRelease(nnfLeft, nnfRight);
        } // ToNNF

    } // LTLRelease

    public class LTLUntil : LTLFormula
    {
        public LTLFormula LeftFormula { get; }
        public LTLFormula RightFormula { get; }

        public LTLUntil(LTLFormula leftFormula, LTLFormula rightFormula)
        {
            LeftFormula = leftFormula;
            RightFormula = rightFormula;
        } // LTLUntil

        public override string ToString()
        {
            return $"({LeftFormula}) U ({RightFormula})";
        } // ToString

        public override LTLFormula ToNNF()
        {
            // Nothing to do, AtomicProposition is in NNF
            var nnfLeft = LeftFormula.ToNNF();
            var nnfRight = RightFormula.ToNNF();
            return new LTLUntil(nnfLeft, nnfRight);
        } // ToNNF

    } // LTLUntil

    public class LTLGlobally : LTLFormula
    {
        public LTLFormula Formula { get; }

        public LTLGlobally(LTLFormula formula_)
        {
            Formula = formula_;
        } //LTLGlobally

        public override string ToString()
        {
            return $"G({Formula})";

        } //ToString

        public override LTLFormula ToNNF()
        {
            // Nothing to do, AtomicProposition is in NNF

            return new LTLRelease(new LTLFalse(), Formula.ToNNF());
        } // ToNNF
    } //Globally

    public class LTLEventually : LTLFormula
    {
        public LTLFormula Formula { get; }

        public LTLEventually(LTLFormula formula_)
        {
            Formula = formula_;
        } //LTLEventually

        public override string ToString()
        {
            return $"F({Formula})";

        } //ToString

        public override LTLFormula ToNNF()
        {
            // Nothing to do, AtomicProposition is in NNF

            return new LTLUntil(new LTLTrue(), Formula.ToNNF());
        } // ToNNF
    } //Globally

    public class LTLTrue : LTLFormula
    {
        //empty class
        public override string ToString()
        {
            return "T";
        } // ToString

        public static LTLTrue Instance { get; } = new LTLTrue();

        public override LTLFormula ToNNF()
        {
            // Nothing to do, True is in NNF
            return this;
        } // ToNNF

    } //LTLTrue

    public class LTLFalse : LTLFormula
    {
        public override string ToString()
        {
            return "F";
        } //ToString

        public static LTLFalse Instance { get; } = new LTLFalse();
        public override LTLFormula ToNNF()
        {
            // Nothing to do, True is in NNF
            return this;
        } // ToNNF
    }
    #endregion

    // ---------------------------------------------------------
    //                      typedefs
    // ---------------------------------------------------------
    #region typedefs

    /// <summary>
    /// Set to store LTLFormulas. Note that since LTLFormulas implement the IEquatable
    /// interface based on string representation equality, uniqueness is determined by their "textual"
    /// representation.
    /// </summary>
    public class LTLSet : HashSet<LTLFormula>
    {
        public override string ToString()
        {
            return string.Join(",", this);
        } // ToString

        public LTLSet(LTLSet other) : base(other, null) { }
        public LTLSet() : base() { }

        public bool IsEqualTo(LTLSet other)
        {
            if (this.Count != other.Count)
            {
                return false;
            }

            foreach (var formula in this)
            {
                if (!other.Contains_Formula(formula))
                {
                    return false;
                }
            }

            return true;
        } //IsEqualTo

        public bool Contains_Formula(LTLFormula formula)
        {
            foreach (var a_formula in this)
            {
                if (formula.ToString() == a_formula.ToString())
                {
                    return true;
                }
            }
            return false;
        }
    } // LTLSet

    /// <summary>
    /// Sets of graph nodes ∪ {init}. This is just represented as a HashSet of integer values
    /// that has an init node by default (0-index).
    /// </summary>
    public class NodeSet
    {
        public HashSet<uint> Nodes { get; }

        public NodeSet()
        {
            Nodes = new HashSet<uint>();
            Nodes.Add(0);
        } // NodeSet

        public NodeSet(NodeSet other)
        {
            Nodes = other.Nodes;
        }

        public NodeSet(HashSet<uint> nodes)
        {
            Nodes = nodes;
        } // NodeSet

        public bool ContainsSubset(NodeSet subset)
        {
            return subset.Nodes.IsSubsetOf(subset.Nodes);
        } //ContainsSubset

        public void AddNode()
        {
            // always add nodes based on increasing count (first added node will have index 1, next one 2 and so on)
            Nodes.Add((uint)Nodes.Count);
        } // AddNode

        public bool ContainsNode(uint node)
        {
            return Nodes.Contains(node);
        } // ContainsNode

        public NodeSet UnionWith(NodeSet other)
        {
            var unionNodes = new HashSet<uint>(Nodes);
            unionNodes.UnionWith(other.Nodes);
            return new NodeSet(unionNodes);
        } // UnionWith

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            NodeSet other = (NodeSet)obj;

            return Nodes.SetEquals(other.Nodes);
        } // Equals

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (uint node in Nodes)
                {
                    hash = hash * 23 + node.GetHashCode();
                }
                return hash;
            }
        } // GetHashCode


        public override string ToString()
        {
            string nodesStr = string.Join(", ", Nodes);
            return $"{{{nodesStr}}}";
        } // ToString
    } // NodeSet
    #endregion

    #region LTL utils
    public class LTLFormulaUtils
    {
        #region FindSubformulasUntil
        public static List<LTLUntil> FindSubformulasUntil(LTLFormula formula)
        {
            var subformulas = new List<LTLUntil>();

            // check if f = "g1 U g2"
            if (formula is LTLUntil untilFormula)
            {
                subformulas.Add(untilFormula);
            }

            // Goes into the sub-formulas
            if (formula is LTLNegation negationFormula)
            {
                subformulas.AddRange(FindSubformulasUntil(negationFormula.Formula));
            }
            else if (formula is LTLConjunction conjunctionFormula)
            {
                subformulas.AddRange(FindSubformulasUntil(conjunctionFormula.LeftFormula));
                subformulas.AddRange(FindSubformulasUntil(conjunctionFormula.RightFormula));
            }
            else if (formula is LTLDisjunction disjunctionFormula)
            {
                subformulas.AddRange(FindSubformulasUntil(disjunctionFormula.LeftFormula));
                subformulas.AddRange(FindSubformulasUntil(disjunctionFormula.RightFormula));
            }
            else if (formula is LTLNext nextFormula)
            {
                subformulas.AddRange(FindSubformulasUntil(nextFormula.Formula));
            }
            else if (formula is LTLRelease releaseFormula)
            {
                subformulas.AddRange(FindSubformulasUntil(releaseFormula.LeftFormula));
                subformulas.AddRange(FindSubformulasUntil(releaseFormula.RightFormula));
            }

            return subformulas;
        } // FindSubformulasUntil
        #endregion

        #region cl(f)
        public static List<LTLFormula> cl(LTLFormula formula)
        {
            List<LTLFormula> CL = new List<LTLFormula>();

            // f is in CL(f)
            CL.Add(formula);

            // formula is Negation : not(formula) is in cl
            if (formula is LTLNegation negation)
            {
                CL.AddRange(cl(negation.Formula));
            }
            // if X f1 ∈ cl(f) then f1 ∈ cl(f)
            if (formula is LTLNext nextFormula)
            {
                CL.AddRange(cl(nextFormula.Formula));
            }

            else if (formula is LTLConjunction conjunctionFormula)
            {
                // Condition: if f1 ∧ f2 ∈ cl(f) then f1,f2 ∈ cl(f)
                CL.AddRange(cl(conjunctionFormula.LeftFormula));
                CL.AddRange(cl(conjunctionFormula.RightFormula));
            }
            else if (formula is LTLDisjunction disjunctionFormula)
            {
                // Condition: if f1 ∨ f2 ∈ cl(f) then f1,f2 ∈ cl(f)
                CL.AddRange(cl(disjunctionFormula.LeftFormula));
                CL.AddRange(cl(disjunctionFormula.RightFormula));
            }
            else if (formula is LTLUntil untilFormula)
            {
                // Condition: if f1 U f2 ∈ cl(f) then f1,f2 ∈ cl(f)
                CL.AddRange(cl(untilFormula.LeftFormula));
                CL.AddRange(cl(untilFormula.RightFormula));
            }
            else if (formula is LTLRelease releaseFormula)
            {
                // Condition: if f1 R f2 ∈ cl(f) then f1,f2 ∈ cl(f)
                CL.AddRange(cl(releaseFormula.LeftFormula));
                CL.AddRange(cl(releaseFormula.RightFormula));
            }

            return CL;

        }
        #endregion

        #region is_final
        //Check if a node q is "final" such as Pos(q= = Neg(q) = {} AND no edge is going out of q
        public static bool is_final(uint q, LabelingFunction L, Dictionary<uint, NodeSet> Incoming)
        {
            // Pos and Neg are empty for the node q
            bool final = (L.Pos.TryGetValue(q, out LTLSet set) && set.Count == 0
                    && L.Neg.TryGetValue(q, out LTLSet set1) && set1.Count == 0);

            // Incoming = {key (receptor) : {incoming nodes}, ...}
            foreach (var elem in Incoming)
            {
                var key_ = elem.Key;
                var val_ = elem.Value;

                // node q has an outgoing edge to another node
                if (final && key_ != q && val_.ContainsNode(q))
                {
                    // if the other node is final, no problem
                    // if not :
                    if (!is_final(key_, L, Incoming))
                    {
                        final = false;
                    }
                }
            }
            return final;
        }
        #endregion

    }
    #endregion

} // namespace TLTLPredicateBuilder.Utils