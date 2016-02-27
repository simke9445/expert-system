using System;
using ExpertSystem.Observations;

namespace ExpertSystem.PostFix
{
    /// <summary>
    /// Objects of this class contain atomic information
    /// like operators or values of the expression.
    /// </summary>
    public class AtomicExpression
    {
        private string _value;

        public object ParsedValue { get; set; }

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                double outd;
                if (double.TryParse(value, out outd))
                {
                    ParsedValue = outd;
                }
                bool outb;
                if (bool.TryParse(value, out outb))
                {
                    ParsedValue = outb;
                }
                _value = value;
            }
        }

        public string Operator { get; set; }   

        public override string ToString()
        {
            return string.Format("{0}{1}", Value, Operator);
        }

        /// <summary>
        /// Evaluates if the specified base is really the base class
        /// of the specified descendant
        /// </summary>
        public static bool IsSameOrSubclass(Type potentialBase, Type potentialDescendant)
        {
            return potentialDescendant.IsSubclassOf(potentialBase)
                   || potentialDescendant == potentialBase;
        }

        public Observation Evaluate(object value)
        {
            if (Operator == "-" && IsSameOrSubclass(typeof (Observation), value.GetType()))
            {
                return new Not(value as Observation);
            }
            throw new NotImplementedException(Operator + " operator not implemented");
        }

        public Observation Evaluate(object val1, object val2)
        {
            /*
            if (val1.GetType() == typeof (Or))
                val1 = new Parenthesis(val1 as Or);
            if(val2.GetType() == typeof(Or))
                val2 = new Parenthesis(val2 as Or);
            */

            if(IsSameOrSubclass(typeof(Observation), val1.GetType()) && IsSameOrSubclass(typeof(Observation), val2.GetType()))
            switch (Operator)
            {
                case "I":
                    return new And(val1 as Observation, val2 as Observation);
                case "ILI":
                    return new Or(val1 as Observation, val2 as Observation);

            }
            throw new NotImplementedException(Operator + " operator not implemented");
        }
    }
}