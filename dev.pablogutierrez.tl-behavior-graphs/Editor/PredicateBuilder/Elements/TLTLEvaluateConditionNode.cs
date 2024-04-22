using UnityEngine;

namespace TLTLPredicateBuilder.Elements
{
    using System;
    using TLTLCore.Framework;
    using TLTLPredicateBuilder.Utils;
    using TLTLPredicateBuilder.Windows;
    using UnityEditor.Experimental.GraphView;

    public class TLTLEvaluateConditionNode : TLTLPredicateNode
    {
        private Type _conditionType = typeof(LessThanPredicate);

        public void Initialize(TLTLPredicateGraphView tltlGraphView, Vector2 position)
        {
            base.Initialize(tltlGraphView, typeof(LessThanPredicate), position);
            PredicateType = _conditionType;
        } // Initialize

        public override void Draw()
        {
            base.Draw();

            /* INPUT CONTAINER */
            var inParams = TLTLPredicate.GetInProperties(_conditionType);
            foreach (var param in inParams)
            {
                Port inputPort = this.CreatePort(param.PropertyType, param.Name, Orientation.Horizontal, Direction.Input, Port.Capacity.Single);
                inputContainer.Add(inputPort);
            }

            RefreshExpandedState();
        } // Draw

        protected override string GetOutputPortName()
        {
            return TLTLPredicate.GetOutputExpression(_conditionType);
        }

        public override string GetDisplayName()
        {
            return TLTLPredicate.GetPredicateDisplayName(_conditionType);
        }

    } // TLTLEvaluateConditionNode

} // namespace TLTLPredicateBuilder.Elements
