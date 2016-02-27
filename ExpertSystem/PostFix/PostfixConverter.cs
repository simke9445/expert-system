using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ExpertSystem.Observations;

namespace ExpertSystem.PostFix
{
    public class PostfixConverter
    {
        /// <summary>
        /// Converts the ruleString into a rule Object
        /// </summary>
        /// <param name="ruleString"> String representation of a rule </param>
        /// <returns> Rule object </returns>
        public static Rule RuleConversion(string ruleString)
        {
            string[] stringSeparators = { "AKO ", " ONDA " };
            string expression = ruleString;

            // replacing multiple tabs/spaces in the string
            expression = Regex.Replace(expression, @"\s+", " ");
            expression = expression.Replace("\t", string.Empty);

            expression = Tokenize(expression);


            Rule rule = MainWindow.GetCachedRule(expression);
            if (rule != null)
                return rule;

            string[] split = expression.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

            var observationString = Postfix(Tokenize(split[0]));

            Tuple<Observation,List<ConcreteObservation>> observation_list_tuple = ObservationConversion(observationString);

            Observation observation = observation_list_tuple.Item1;



            List<ConcreteObservation> concreteObservations = observation_list_tuple.Item2;

            var factorSplit = split[1].Split(')');
            var factorString = factorSplit[0].Trim('(');

            double factor = double.Parse(factorString);

            var conclusionString = factorSplit[1];
            ConcreteObservation conclusion = ConclusionConversion(conclusionString, factor);

            rule = new Rule(observation, conclusion, concreteObservations);

            return rule;
        }

        /// <summary>
        /// Same as Rule just with Conclusion
        /// </summary>
        public static ConcreteObservation ConclusionConversion(string conclusionString, double factor)
        {
            conclusionString = conclusionString.Trim();
            ConcreteObservation conclusion = MainWindow.GetCachedObservationByName(conclusionString);
            conclusion.ObservedFactor = factor;
            

            return conclusion;
        }

        /// <summary>
        /// Same as Rule just with observation
        /// </summary>
        /// <param name="list"> list of atomic expressions </param>
        /// <returns> 
        /// Packed observation object, and a list containing all the 
        /// ConcreteObservations in the packed object
        ///  </returns>
        public static Tuple<Observation, List<ConcreteObservation>> ObservationConversion(List<AtomicExpression> list)
        {
            var stack = new Stack<object>();
            var concreteObservations = new List<ConcreteObservation>();
            var lastExpressionString = string.Empty;

            try
            {

                foreach (var expression in list)
                {
                    // variable
                    if (!string.IsNullOrEmpty(expression.Value))
                    {
                        ConcreteObservation concreteObservation = MainWindow.GetCachedObservationByName(expression.Value);
                        concreteObservations.Add(concreteObservation);

                        stack.Push(concreteObservation);
                        continue;
                    }
                    // unary operator
                    object result;
                    if (expression.Operator == "-")
                    {
                        Observation tmpEval = stack.Pop() as Observation;
                        if (tmpEval.GetType() != typeof (ConcreteObservation))
                            tmpEval = new Parenthesis(tmpEval);
                        result = expression.Evaluate(tmpEval);
                    }
                    // binary operator
                    else
                    {
                        var p2 = stack.Pop();
                        var p1 = stack.Pop();
                        Observation tmpEval = expression.Evaluate(p1, p2);

                        if (expression.Operator == "ILI" && (lastExpressionString == "I" || lastExpressionString == "-"))
                            tmpEval = new Parenthesis(tmpEval);

                        result = tmpEval;
                    }
                    stack.Push(result);
                    lastExpressionString = expression.Operator;
                }
                var observation = stack.Pop() as Observation;
                return new Tuple<Observation, List<ConcreteObservation>>(observation, concreteObservations);
            }
            catch (InvalidOperationException e)
            {
                throw new ParsingException("parsing failed, reenter your expression!");
            }
        }
  

        /// <summary>
        /// Postfix Conversion of a Infix expression
        /// </summary>
        /// <param name="expression"> Infix expression </param>
        /// <returns> List of Atomic Expressions </returns>
        public static List<AtomicExpression> Postfix(string expression)
        {
            var expressions = new List<AtomicExpression>();
            var variable = string.Empty;
            var stack = new Stack<string>();
            // remove spaces
            //expression = expression.Replace(" ", string.Empty);
            string opr = string.Empty;
            var lastVariable = string.Empty;
            for (var i = 0; i < expression.Length; i++)
            {
                var c = expression[i];

                if (c == ' ')
                {
                    lastVariable = variable;
                    variable = string.Empty;
                    continue;
                }
                // variable or number
                if (char.IsLetterOrDigit(c) && (variable+c != "I") && (variable+c != "ILI") && (variable+c != "-"))
                {
                    variable += c;
                }
                // left parthesis
                else if (c == '(')
                {
                    stack.Push(c.ToString());
                }
                // right parenthesis
                else if (c == ')')
                {
                    // add variable
                    if (!string.IsNullOrEmpty(lastVariable))
                    {
                        expressions.Add(new AtomicExpression { Value = lastVariable });
                    }
                    variable = string.Empty;
                    while (!stack.Peek().Equals("("))
                    {
                        expressions.Add(new AtomicExpression { Operator = stack.Pop() });
                    }
                    stack.Pop();
                }
                // operator
                else
                {
                    var sc = variable + c;

                    // add variable
                    if (!string.IsNullOrEmpty(lastVariable))
                    {
                        expressions.Add(new AtomicExpression { Value = lastVariable });
                    }
                    lastVariable = string.Empty;
                    variable = string.Empty;
                    
                    
                    if (expression[i+1] == 'L' && expression[i+2] == 'I' && expression[i+3] ==' ' && i + 3 < expression.Length)
                    {
                        sc += expression[i + 1];
                        i++;
                        sc += expression[i + 1];
                        i++;
                    }

                    // add all expressions with higher or same priority
                    while (stack.Count > 0 && Priority(stack.Peek()) >= Priority(sc))
                    {
                        expressions.Add(new AtomicExpression { Operator = stack.Pop() });
                    }
                    stack.Push(sc);
                }
            }

            // add last variable
            if (!string.IsNullOrEmpty(variable))
            {
                expressions.Add(new AtomicExpression { Value = variable });
            }

            while (stack.Count > 0)
            {
                expressions.Add(new AtomicExpression { Operator = stack.Pop() });
            }

            for (int i = 0; i < expressions.Count; i++)
            {
                Console.WriteLine(expressions[i] + " index: " + i);
            }

            return expressions;
        }

        /// <summary>
        /// Searches for the invalid combination of Parenthesis
        /// in a expressions.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns> start/end index pairs of invalid intervals </returns>
        public static List<Tuple<int,int>> CheckParenthesis(string expression)
        {
            var indexList = new List<Tuple<int,int>>();
            var leftBracketList = new Stack<int>();

            for (int i = 0; i < expression.Length; i++)
            {
                switch (expression[i])
                {
                    case '(':
                        leftBracketList.Push(i);
                        break;
                    case ')':
                        if (leftBracketList.Count == 0)
                        {
                            indexList.Add(new Tuple<int, int>(i,i));
                        }
                        else
                        {
                            leftBracketList.Pop();
                        }

                        break;
                }
            }
            while (leftBracketList.Count > 0)
            {
                var item = leftBracketList.Pop();
                indexList.Add(new Tuple<int,int>(item,item));

            }

            return indexList;
        }

        public static List<Tuple<int, int>> IllegalOperators(string expression)
        {
            var matches = Regex.Matches(expression, @"(?<=(\s+((:?I)|(:?ILI)|(\-)|(\())))(\s+((:?I)|(:?ILI))\s+)");
            List<Tuple<int, int>> returnTupleList = new List<Tuple<int, int>>();

            foreach (Match match in matches)
            {
                returnTupleList.Add(new Tuple<int, int>(match.Index, match.Index + match.Length - 1));
            }
            return returnTupleList;
        }

        public static Tuple<int, int> StartingWithOperator(string expression)
        {
            var matches = Regex.Match(expression, @"^((\s)*(:?I|:?ILI)(\s)+)");

            Tuple<int, int> returnTuple = null;

            if (matches.Length > 0)
                returnTuple = new Tuple<int, int>(matches.Index, matches.Index + matches.Length - 1);

            return returnTuple;
        }

        public static Tuple<int,int> EndingWithOperator(string expression)
        {
            var matches = Regex.Match(expression, @"(\s+(:?I|:?ILI)(\s)*)$");

            Tuple<int,int> returnTuple = null;

            if (matches.Length > 0)
                returnTuple = new Tuple<int, int>(matches.Index, matches.Index + matches.Length - 1);

            return returnTuple;
        }

        public static List<Tuple<int, int>> OperatorWithBrackets(string expression)
        {
            var matches = Regex.Matches(expression, @"((?<=(\())(\s*((:?I)|(:?ILI)))\s+)|((\s*((:?I)|(:?ILI))\s+)(?=(\))))");

            List<Tuple<int, int>> returnTupleList = new List<Tuple<int, int>>();

            foreach (Match match in matches)
            {
                returnTupleList.Add(new Tuple<int, int>(match.Index, match.Index + match.Length - 1));
            }
            return returnTupleList;
        }

        public static List<Tuple<int, int>> EmptyBrackets(string expression)
        {
            var matches = Regex.Matches(expression, @"\(\s*\)");

            List<Tuple<int, int>> returnTupleList = new List<Tuple<int, int>>();

            foreach (Match match in matches)
            {
                returnTupleList.Add(new Tuple<int, int>(match.Index, match.Index + match.Length - 1));
            }
            return returnTupleList;
        }

        public static List<Tuple<int, int>> WordsAfterBrackets(string expression)
        {
            var matches = Regex.Matches(expression, @"(?<=\s+\))(\s+\w+\s+)(?<!(\s+((:?ILI)|(:?I))\s+))");

            List<Tuple<int, int>> returnTupleList = new List<Tuple<int, int>>();

            foreach (Match match in matches)
            {
                returnTupleList.Add(new Tuple<int, int>(match.Index, match.Index + match.Length - 1));
            }
            return returnTupleList;
        } 

        public static List<Tuple<int, int>> WordsWithoutOperators(string expression)
        {
            var matches = Regex.Matches(expression, @"(?<!(\s+((:?I)|(:?ILI)|\-|\()))(\s+\w+\s+)(?<!(\s+((:?I)|(:?ILI))\s+))(?!((\)|(:?I)|(:?ILI))\s+))");

            List<Tuple<int, int>> returnTupleList = new List<Tuple<int, int>>();

            foreach (Match match in matches)
            {
                returnTupleList.Add(new Tuple<int, int>(match.Index, match.Index + match.Length - 1));
            }
            return returnTupleList;
        } 

        public static Tuple<int, int> EndingWithWord(string expression)
        {
            var matches = Regex.Matches(expression, @"((?<=(\s+((\)\s+)|(\(\s+)|(\w+\s+))))(?<!(\s+((:?ILI)|(:?I))\s+))(\w+\s*))$");

            Tuple<int, int> returnTuple = null;

            if (matches.Count > 0)
                returnTuple = new Tuple<int, int>(matches[0].Index, matches[0].Index + matches[0].Length - 1);

            return returnTuple;
        }


        public static List<Tuple<int, int>> InvalidWordCombinations(string expression)
        {
            var matches = Regex.Matches(expression, @"(?<=(\s+\w+))(?<!(\s+((:?ILI)|(:?I))))(\s+\w+\s+)(?<!(\s+((:?ILI)|(:?I))\s+))");

            List<Tuple<int, int>> returnTupleList = new List<Tuple<int, int>>();

            foreach (Match match in matches)
            {
                returnTupleList.Add(new Tuple<int, int>(match.Index, match.Index + match.Length - 1));
            }
            return returnTupleList;
        } 

        /// <summary>
        /// Checks all the previous Regular Expressions
        /// and concatenates all of the irregular intervals
        /// int a single one
        /// </summary>
        public static List<Tuple<int,int>> CheckInvalidCombinations(string expression)
        {
            var errorIndexes = new List<Tuple<int,int>>();

            // checking if ends with a bracket + word
            var bracketAndWordEnding = EndingWithWord(expression);
            if (bracketAndWordEnding != null)
                errorIndexes.Add(bracketAndWordEnding);

            // checking if ends with a operator
            var operatorEnding = EndingWithOperator(expression);
            if (operatorEnding != null)
                errorIndexes.Add(operatorEnding);


            // checking for invalid brackets + operators
            var bracketOperatorList = OperatorWithBrackets(expression);
            errorIndexes.AddRange(bracketOperatorList);

            // words without operators

            var wordsWithoutOperators = WordsWithoutOperators(expression);
            errorIndexes.AddRange(wordsWithoutOperators);

            // words after brackets
            var wordsAfterBrackets = WordsAfterBrackets(expression);
            errorIndexes.AddRange(wordsAfterBrackets);

            // invalid words
            var invalidWords = InvalidWordCombinations(expression);
            errorIndexes.AddRange(invalidWords);

            // illegal operators
            var illegaloperators = IllegalOperators(expression);
            errorIndexes.AddRange(illegaloperators);

            // empty brackets
            var emptyBrackets = EmptyBrackets(expression);
            errorIndexes.AddRange(emptyBrackets);

            //starting with operator
            var startWithOperator = StartingWithOperator(expression);
            if(startWithOperator != null)
                errorIndexes.Add(startWithOperator);

            // checking for unallowed OR combinations
            var resultsOR = Regex.Matches(expression, @"(?<=(ILI))((\s*(:?ILI|(:?I))\s*)+)");

            // checking for unallowed AND combinations
            var resultsAND = Regex.Matches(expression, @"(?<=(I))((\s*(:?ILI|(:?I))\s*)+)");

            foreach (Match result in resultsOR)
            {
                errorIndexes.Add(new Tuple<int, int>(result.Index, result.Index + result.Length - 1));
            }

            foreach (Match result in resultsAND)
            {
                errorIndexes.Add(new Tuple<int, int>(result.Index, result.Index + result.Length - 1));
            }

            // check for invalid parenthesis
            var invalidIndexes = PostfixConverter.CheckParenthesis(expression);

            errorIndexes.AddRange(invalidIndexes);

            // sort the intervals
            errorIndexes.Sort(delegate(Tuple<int,int> a, Tuple<int,int> b)
            {
                if (a.Item1 > b.Item1)
                    return 1;
                else if (a.Item1 == b.Item1)
                {
                    if (a.Item2 > b.Item2)
                        return 1;
                    else
                    {
                        return -1;
                    }
                }
                else
                    return -1;
            });

            foreach (var errorIndex in errorIndexes)
            {
                Console.WriteLine(errorIndex);
            }

            List<Tuple<int,int>> errors = new List<Tuple<int, int>>();


            if (errorIndexes.Count == 0)
                return errors;
            if (errorIndexes.Count == 1)
            {
                errors.Add(errorIndexes[0]);
            }
            else
            {
                var start = errorIndexes[0].Item1;

                // Fuse overlapping intervals
                for (int i = 0; i < errorIndexes.Count - 1; i++)
                {
                    if (errorIndexes[i].Item1 != errorIndexes[i + 1].Item1)
                    {
                        if (errorIndexes[i].Item2 < errorIndexes[i + 1].Item1 - 1)
                        {
                            errors.Add(new Tuple<int, int>(start, errorIndexes[i].Item2));
                            start = errorIndexes[i + 1].Item1;
                        }
                    }
                }
                errors.Add(new Tuple<int, int>(start, errorIndexes.Last().Item2));
            }


            return errors;
        }

        /// <summary>
        /// Tokenizes the sentence so that it contains at most one spacing between
        /// the substrings.
        /// </summary>
        /// <param name="sentence"> initial sentence </param>
        /// <returns> tokenized string </returns>
        public static string Tokenize(string sentence)
        {
            string pattern = "(:?ILI)|(:?I)|\\-|[a-z]+|(?!((:?ILI)|(:?I)))([A-Z]+)|\\(|\\)";

            var matches = Regex.Matches(sentence, pattern);
            var wrappWithWhiteSpace = new List<int>();


            for (int i = 0; i < matches.Count - 1; i++)
            {
                if (matches[i].Index + matches[i].Value.Length == matches[i + 1].Index)
                    wrappWithWhiteSpace.Add(matches[i].Index + matches[i].Value.Length - 1);
            }

            sentence = sentence.InsertAround(" ", wrappWithWhiteSpace);

            return sentence;
        }

        /// <summary>
        /// Returns the Postfix priority of a operator 
        /// </summary>
        /// <param name="x"> operator </param>
        /// <returns> priority of the operator </returns>
        private static int Priority(string x)
        {
            if (x.Equals("ILI"))
                return 0;
            if (x.Equals("I"))
                return 1;
            if (x.Equals("-"))
                return 2;
            if (x.Equals("(") || x.Equals(")"))
                return -1;
            throw new NotImplementedException(x + " is not implemented");
        }
    }

    public static partial class StringExtensions
    {
        public static IEnumerable<int> AllIndexesOf(this string str, string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index, StringComparison.Ordinal);
                if (index == -1)
                    break;
                yield return index;
            }
    }
    }
    
}