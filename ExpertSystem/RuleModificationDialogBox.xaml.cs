using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ExpertSystem.Observations;
using ExpertSystem.PostFix;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ExpertSystem
{
    /// <summary>
    /// Interaction logic for RuleModificationDialogBox.xaml
    /// </summary>
    public partial class RuleModificationDialogBox : Window
    {
        public RuleModificationDialogBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// adds the text from the callers list item
        /// to the text box
        /// </summary>
        /// <param name="text">
        /// callers list item text
        /// </param>
        public void SetInitialText(string text)
        {
            var para = new Paragraph();
            para.Inlines.Add(new Run(text));
            var flow = new FlowDocument();
            flow.Blocks.Add(para);
            ModificationTextBox.Document = flow;
            UnModifiedRule =
                new TextRange(ModificationTextBox.Document.ContentStart, ModificationTextBox.Document.ContentEnd).Text
                    .Replace("\n", string.Empty).Replace("\r", string.Empty);
            WarningLabel.Content = "Modification dialog: enter your changes and press Ok for submition";
        }

        public string UnModifiedRule { get; set; }

        /// <summary>
        /// Converts the text from the textbox to a rule.
        /// If the conversion isn't possible, display
        /// invalid values colored red in the textbox as a warning,
        /// with an option of autocorrection on CTRL + W.
        /// If created rule is present in the system, do nothing.
        /// Else update the rules collection with the newly created rule.
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = this.Owner as MainWindow;
            string pattern = @"\(\s*\d([,.]\d+)?\s*\)";
            var unmod = UnModifiedRule.Replace(Regex.Match(UnModifiedRule, pattern).ToString(), string.Empty);


            Rule unModRule = MainWindow.GetCachedRule(unmod);

            // this is how you get text from richtextbox
            string expression = new TextRange(ModificationTextBox.Document.ContentStart, ModificationTextBox.Document.ContentEnd).Text;
            expression = expression.Replace("\n", string.Empty).Replace("\r", string.Empty);

            expression = Regex.Replace(expression, @"\s+", " ");

            expression = PostfixConverter.Tokenize(expression);

            // checking if the sentence begins with an IF
            var ifBeginMatch = Regex.Matches(expression, @"^(:?AKO\s)");

            if (ifBeginMatch.Count == 0)
            {
                WarningLabel.Content = "Expression not beginning with IF, please reenter";
                return;
            }

            //Grabbing expression & conclusion 
            var ifMatches = Regex.Matches(expression, @"(?<=(AKO))(.*)(?=(ONDA))|(?<=(ONDA))(.*)");

            if (ifMatches.Count == 0)
            {
                WarningLabel.Content = "Unallowed input, rewrite the rule";
                return;
            }

            // trim the match values
            string test = ifMatches[0].Value.Trim();
            string testFactor = ifMatches[1].Value.Trim();
            Console.WriteLine("First part of the expression: " + test);
            Console.WriteLine("Second part of the expression: " + ifMatches[1].Value.Trim());

            if (!Regex.IsMatch(ifMatches[1].Value, @"\s*\(\s*\d([,.]\d+)?\s*\)\s*((\d|\w)+)$"))
            {
                WarningLabel.Content = "Conclusion parsing failed, check after THEN";
                return;
            }

            // validate the expression
            var tuple = ValidateExpression(test, testFactor);
            ModificationTextBox.Document = tuple.Item1;
            var expressionRegularized = tuple.Item2;

            if (expressionRegularized)
            {
                // clean the expression
                var replacedExpression = expression
                    .Replace(Regex.Match(expression, pattern).ToString(), string.Empty)
                    .Replace("( ", "(")
                    .Replace(" )", ")")
                    .Replace("  ", " ");


                Rule modRule = MainWindow.GetCachedRule(replacedExpression);

                if (modRule == null)
                {
                    MainWindow.ReplaceRule(unModRule, PostfixConverter.RuleConversion(expression));
                }
                else
                {
                    // regex for extracting the factor
                    string match = Regex.Match(testFactor, @"[0-9]([.,][0-9]{1,3})?").ToString();
                    double factor = double.Parse(match);
                    double TOLERANCE = 0.0001;

                    if(Math.Abs(factor - unModRule.ObservedConclusionFactor) > TOLERANCE)
                        unModRule.ObservedConclusionFactor = factor;
                }

                _factor = string.Empty;

                Close();
            }
            else
            {
                WarningLabel.Content = "Potential sytax mistakes. Press Ctrl+W for autocorrect";
            }
        }

        /// <summary>
        /// Try to convert the expression to a valid one.
        /// If its not valid, display the invalid text colored in red
        /// to the user. If valid clears the current textbox
        /// </summary>
        /// <param name="expression">
        /// expression string to be validated
        /// </param>
        /// <param name="factor">
        /// factor part of the expression string
        /// </param>
        /// <returns>
        /// flowDocument that represents the textBox state after validation.
        /// and a Bool that indicates whether or not the expression is valid.
        /// </returns>
        private Tuple<FlowDocument, bool> ValidateExpression(string expression, string factor)
        {
            var expressionRegularized = true;
            _cleanedRule = string.Empty;

            // get the invalid intervals
            var errors = PostfixConverter.CheckInvalidCombinations(expression);

            int[] splitIndexes = new int[errors.Count * 2];

            // split the expression string on the good/bad intervals
            // bad will appear as even splits
            for (int i = 0; i < errors.Count; i++)
            {
                splitIndexes[2 * i] = errors[i].Item1;
                splitIndexes[2 * i + 1] = errors[i].Item2 + 1;
            }

            Console.WriteLine("ALL ERRORS:");
            foreach (var error in errors)
            {
                Console.WriteLine(error + expression.Substring(error.Item1, error.Item2 - error.Item1 + 1));
            }
            
            var split = expression.SplitAt(splitIndexes);
            // wrapping each string to a decoreated Run object
            // and adding it to paragraph
            FlowDocument flow = new FlowDocument();
            Paragraph para = new Paragraph();
            var ifPart = "AKO ";
            var thenPart = " ONDA ";

            para.Inlines.Add(new Run(ifPart));
            _cleanedRule += ifPart;

            // wrap the intervals in a run object
            for (int i = 0; i < split.Length; i++)
            {
                if (i%2 != 0)
                {
                    Brush brush = Brushes.Red;
                    var run = new Run(split[i])
                    {
                        Foreground = brush, FontSize = 20, FontWeight = FontWeights.Bold
                    };
                    para.Inlines.Add(run);
                    expressionRegularized = false;
                }
                else
                {
                    para.Inlines.Add(new Run(split[i]));
                    _cleanedRule += split[i];
                }
            }
            para.Inlines.Add(new Run(thenPart));
            para.Inlines.Add(new Run(factor));

            // clean the necessary multiple spacings
            _cleanedRule += thenPart + " ";
            _cleanedRule = Regex.Replace(_cleanedRule, @"\s+", " ");
            _cleanedRule += factor;
            _cleanedRule = Regex.Replace(_cleanedRule, @"\s+", " ");

            // add the paragraph
            flow.Blocks.Add(para);

            return new Tuple<FlowDocument, bool>(flow, expressionRegularized);

        }

        private static string _factor;
        private static string _cleanedRule;


        /// <summary>
        /// event handler for autocorrecting the rule in the textbox
        /// </summary>
        private void ModificationTextBox_OnCtrlWPressed(object sender, KeyEventArgs keyEventArgs)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && _cleanedRule != string.Empty) // Is Alt key pressed
            {
                if (Keyboard.IsKeyDown(Key.W))
                {
                    bool bestVariableEver = false;
                    FlowDocument flow = ModificationTextBox.Document;

                    while (!bestVariableEver)
                    {
                        var text = _cleanedRule;

                        text = PostfixConverter.Tokenize(text);
                        var ifMatches = Regex.Matches(text, @"(?<=(AKO))(.*)(?=(ONDA))|(?<=(ONDA))(.*)");

                        string test = ifMatches[0].Value.Trim();
                        string testFactor = ifMatches[1].Value.Trim();
                        Console.WriteLine("First part of the expression: " + test);
                        Console.WriteLine("Second part of the expression: " + ifMatches[1].Value.Trim());



                        var tuple = ValidateExpression(test, testFactor);
                        flow = tuple.Item1;
                        bestVariableEver = tuple.Item2;
                    }

                    _cleanedRule = _cleanedRule.Trim();

                    WarningLabel.Content = "Enjoy!!!!";

                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(_cleanedRule);
                    _cleanedRule = string.Empty;

                    ModificationTextBox.Document = flow;

                }
            }
            
        }

        
    }

    public static partial class StringExtensions
    {
        /// <summary>
        ///     Returns a string array that contains the substrings in this instance that are delimited by specified indexes.
        /// </summary>
        /// <param name="source">The original string.</param>
        /// <param name="index">An index that delimits the substrings in this string.</param>
        /// <returns>An array whose elements contain the substrings in this instance that are delimited by one or more indexes.</returns>
        public static string[] SplitAt(this string source, params int[] index)
        {
            index = index.Distinct().OrderBy(x => x).ToArray();
            string[] output = new string[index.Length + 1];
            int pos = 0;

            for (int i = 0; i < index.Length; pos = index[i++])
                output[i] = source.Substring(pos, index[i] - pos);

            output[index.Length] = source.Substring(pos);
            return output;
        }

        /// <summary>
        /// inserts a character around specified indexes
        /// in the source string
        /// </summary>
        /// <param name="source"> source string </param>
        /// <param name="index"> indexes of characters that need to be wrapped </param>
        /// <param name="character"> character to be inserted </param>
        /// <returns> modified string by previous tactic </returns>
        public static string InsertAround(this string source, string character, List<int> index)
        {
            string output = string.Empty;

            if (index.Count == 0)
                return source;

            for (int i = 0; i < index[0]; i++)
            {
                output += source[i];
            }

            output += source[index[0]] + character;

            for (int i = 1; i < index.Count; i++)
            {
                for (int j = index[i-1] + 1; j < index[i]; j++)
                {
                    output += source[j];
                }
                output += source[index[i]] + character;
            }

            for (int i = index[index.Count-1] + 1; i < source.Length; i++)
            {
                output += source[i];
            }

            return output;
        }
    }
}
