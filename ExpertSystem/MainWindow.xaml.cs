using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ExpertSystem.Observations;
using ExpertSystem.PostFix;
using ExpertSystem.Support;

namespace ExpertSystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            Core.SetWindow(this);
            RuleListBox.ItemsSource = Rules;
            ConcreteObservationListBox.ItemsSource = ConcreteObservations;
        }

        
        private static readonly List<ConcreteObservation> Observations = new List<ConcreteObservation>(); 

        public static readonly TrulyObservableCollection<Rule> Rules = new TrulyObservableCollection<Rule>();

        private static readonly TrulyObservableCollection<ConcreteObservation> ConcreteObservations =
            new TrulyObservableCollection<ConcreteObservation>();


        public static void AddRule(Rule rule)
        {
            if (!Rules.Contains(rule))
            {
                Rules.Add(rule);
                rule.Conclusion.ClearCache();
                ConcreteObservations.Remove(rule.Conclusion);
                foreach (var concreteObservation in rule.ConcreteObservations)
                {
                    if (!ConcreteObservations.Contains(concreteObservation) && !concreteObservation.IsConclusion())
                    {
                        ConcreteObservations.Add(concreteObservation);

                    }
                }
            }
        }

        public static void ReplaceRule(Rule originalRule, Rule replacementRule)
        {
            var index = Rules.IndexOf(originalRule);

            RemoveRule(originalRule);
            Rules.Insert(index, replacementRule);

            replacementRule.Conclusion.ClearCache();
            ConcreteObservations.Remove(replacementRule.Conclusion);
            foreach (var concreteObservation in replacementRule.ConcreteObservations)
            {
                if(!ConcreteObservations.Contains(concreteObservation) && !concreteObservation.IsConclusion())
                    ConcreteObservations.Add(concreteObservation);
            }
        }

        public static void RemoveRule(Rule rule)
        {
            rule.Conclusion.ConclusionRuleList.Remove(rule);
            rule.Conclusion.ClearCache();

            foreach (var ruleIter in rule.Conclusion.ObservationRuleList)
            {
                ruleIter.Conclusion.ClearCache();
            }
            
            if (!rule.Conclusion.IsConclusion() && rule.Conclusion.IsObservation())
            {
                rule.Conclusion.ObservedFactor = 0;
                rule.Conclusion.NotObservedFactor = 0;
                ConcreteObservations.Add(rule.Conclusion);
            }
                

            Rules.Remove(rule);
            

            foreach (var concreteObservation in rule.ConcreteObservations)
            {
                concreteObservation.ObservationRuleList.Remove(rule);

                if (!concreteObservation.IsObservation())
                {
                    Observations.Remove(concreteObservation);
                    ConcreteObservations.Remove(concreteObservation);
                }      
            }
        }

        /// <summary>
        /// checks if the rule with the same name is in the cache,
        /// if it is, it returns the original object, if its not
        /// it creates a new one, and caches it
        /// </summary>
        /// <param name="ruleName">
        /// the name of the rule which we're looking for
        /// </param>
        /// <returns>
        /// cached rule
        /// </returns>
        public static Rule GetCachedRule(string ruleName)
        {
            return Rules.SingleOrDefault(x => x.NameCleaned() == ruleName); ;
        }

        /// <summary>
        /// same as the the rule, just with Observations
        /// </summary>
        /// <param name="observationName">
        /// observation that we're looking for
        /// </param>
        /// <returns>
        /// cached observation
        /// </returns>
        public static ConcreteObservation GetCachedObservationByName(string observationName)
        {
            if (!Observations.Any(x => x.ToString() == observationName))
                Observations.Add(new ConcreteObservation(observationName));

            return Observations.Single(x => x.ToString() == observationName);
        }

        /// <summary>
        /// hides the step button, and resets the stepByStep mode
        /// </summary>
        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            StepButton.Visibility = Visibility.Hidden;

            mySemaphore.StepByStepMode = false;
            
        }

        internal MySemaphore mySemaphore;

        /// <summary>
        /// wrapper for the Semaphore class so the stepByStep mode 
        /// is enabled
        /// </summary>
        public class MySemaphore
        {
            private readonly Semaphore _semaphore;

            private bool _stepByStepMode;

            private readonly Stopwatch _stopwatch = new Stopwatch();

            public double TimeMilis { get; set; }

            public bool StepByStepMode
            {
                get { return _stepByStepMode; }
                set
                {
                    _stepByStepMode = value;
                    if (!value)
                    {
                        _stopwatch.Reset();
                        _stopwatch.Start();
                        _semaphore.Release();
                    }
                }
            }


            public MySemaphore(int currentValue, int cap)
            {
                _semaphore = new Semaphore(currentValue, cap);
                TimeMilis = 0;
                StepByStepMode = true;
            }

            public void WaitOne()
            {
                if (StepByStepMode)
                {
                    _stopwatch.Stop();
                    TimeMilis += _stopwatch.ElapsedMilliseconds;
                    _semaphore.WaitOne();
                }
            }

            public void Release()
            {
                if (StepByStepMode)
                {
                    _stopwatch.Reset();
                    _stopwatch.Start();
                    _semaphore.Release();
                    
                }
            }

            public void Finish()
            {
                _stopwatch.Stop();
                TimeMilis += _stopwatch.ElapsedMilliseconds;
            }

        }


        /// <summary>
        /// resets current state of the program
        /// </summary>
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {

            FinishButton.Visibility = Visibility.Hidden;
            ConclusionTextBox.Visibility = Visibility.Visible;
            ConclusionInfo.Visibility = Visibility.Visible;

            mySemaphore = new MySemaphore(0,1);
            ConcreteObservations.Clear();
            Rules.Clear();
            Observations.Clear();

            /*
            string[] testExamples =
            {
                "AKO e1 ONDA (0.3)z1",
                "AKO z1 ILI - z2 ONDA (0.6)z",
                "AKO e2 ONDA (0.6)z2",
                "AKO e3 ONDA (0.7)z1",
                "AKO e4 ONDA (0.5)z1"
            };

            foreach (var testExample in testExamples)
            {
                Rule rule = PostfixConverter.RuleConversion(testExample);
                AddRule(rule);
            }
            */
            RichTextBox.Document.Blocks.Clear();
        }

        private static readonly Random Rnd = new Random();

        /// <summary>
        /// get a random color brush
        /// </summary>
        public static Brush PickBrush()
        {
            Type brushesType = typeof(Brushes);

            PropertyInfo[] properties = brushesType.GetProperties();

            int random = Rnd.Next(properties.Length);
            Brush result = (Brush)properties[random].GetValue(null, null);

            return result;
        }

        /// <summary>
        /// generate a random font style
        /// </summary>
        /// <returns>
        /// generated fontstyle
        /// </returns>
        public static FontStyle PickFontStyle()
        {
            Type fontType = typeof(FontStyles);

            PropertyInfo[] properties = fontType.GetProperties();

            int random = Rnd.Next(properties.Length);
            FontStyle result = (FontStyle)properties[random].GetValue(null, null);

            return result;
        }

        /// <summary>
        /// wraps the string in a colored run object
        /// </summary>
        /// <param name="strInformation">
        /// the string info
        /// </param>
        /// <param name="color">
        /// color of the run
        /// </param>
        /// <returns>
        /// run object
        /// </returns>
        public Run RunWrapper(string strInformation, Brush color)
        {
            Run wrapper = new Run(strInformation);
            wrapper.Foreground = color;
            wrapper.FontStyle = PickFontStyle();
            double randFontSize = 10 * Rnd.NextDouble() + 10;
            wrapper.FontSize = randFontSize;
            return wrapper;
        }

        /// <summary>
        /// opens a dialog box for rule modification
        /// </summary>
        private void RuleListBox_OnMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;

            while (obj != null && !Equals(obj, RuleListBox))
            {
                if (obj.GetType() == typeof(ListBoxItem))
                {
                    RuleModificationDialogBox dlg = new RuleModificationDialogBox { Owner = this };

                    // Configure the dialog box

                    // pass the original rule text
                    Rule rule = RuleListBox.SelectedItem as Rule;

                    if (rule != null)
                        dlg.SetInitialText(rule.DisplayName());

                    // Open the dialog box modally 
                    dlg.ShowDialog();
                
                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }


        }

        /// <summary>
        /// clears the focused rule and updates the main collections
        /// </summary>
        private void RuleListBox_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;

            while (obj != null && obj != RuleListBox)
            {
                if (obj.GetType() == typeof(ListBoxItem))
                {
                    Rule rule = RuleListBox.SelectedItem as Rule;

                    RemoveRule(rule);

                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }

        }

        /// <summary>
        /// starts a worker thread for estimating the entered 
        /// conclusion probability of happening
        /// </summary>
        private void ConclusionTextBox_EnterClicked(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ConcreteObservation conclusion = GetCachedObservationByName(ConclusionTextBox.Text);

                FinishButton.Visibility = Visibility.Visible;
                mySemaphore = new MySemaphore(0, 1);

                Worker myWorker = new Worker(conclusion);
                Thread myThread = new Thread(myWorker.StartCalculation);
                myThread.Start();

                StepButton.Visibility = Visibility.Visible;
                ConclusionTextBox.Visibility = Visibility.Hidden;
                ConclusionInfo.Visibility = Visibility.Hidden;
                ConclusionTextBox.Clear();

                e.Handled = true;
            }   
        }

        /// <summary>
        /// signals the worker that's waiting on semaphore
        /// </summary>
        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            mySemaphore.Release();
        }

        /// <summary>
        /// parses the entered rule. if the rule is valid
        /// adds it to rules, if not displays the invalid text
        /// in the rule textbox as red, which can be autocorrected
        /// with CTRL + W command
        /// </summary>
        private void RuleRichTextBox_EnterClicked(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Return)
            {
                string pattern = @"\(\d([,.]\d+)?\)";
                // this is how you get text from richtextbox
                string initialExpression =
                    new TextRange(RuleRichTextBox.Document.ContentStart, RuleRichTextBox.Document.ContentEnd).Text;
                string expression = initialExpression.Replace("\n", string.Empty).Replace("\r", string.Empty);

                expression = Regex.Replace(expression, @"\s+", " ");

                // tokenize the expression
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

                // trim the factor/expression part
                string test = ifMatches[0].Value.Trim();
                string testFactor = ifMatches[1].Value.Trim();

                Console.WriteLine("First part of the expression: " + test);
                Console.WriteLine("Second part of the expression: " + ifMatches[1].Value.Trim());

                // Checks whether the conclusion is valid
                if (!Regex.IsMatch(ifMatches[1].Value, @"\s*\(\s*\d([,.]\d+)?\s*\)\s*"))
                {
                    WarningLabel.Content = "Conclusion parsing failed, check after THEN";
                    return;
                }

                // Validate the expression
                var tuple = ValidateExpression(test, testFactor);

                // set the TextBox Document and regularized flag
                RuleRichTextBox.Document = tuple.Item1;
                var expressionRegularized = tuple.Item2;

                if (expressionRegularized)
                {
                    // prepare the expression for the caching part
                    var replacedExpression = expression
                        .Replace(Regex.Match(expression, pattern).ToString(), string.Empty)
                        .Replace("( ", "(")
                        .Replace(" )", ")")
                        .Replace("  ", " ");

                    Rule modRule = MainWindow.GetCachedRule(replacedExpression);

                    if (modRule == null)
                    {
                        try
                        {
                            AddRule(PostfixConverter.RuleConversion(expression));
                        }
                        catch (ParsingException ex)
                        {
                            WarningLabel.Content = ex.Message;
                            RuleRichTextBox.Document = new FlowDocument();
                        }
                    }
                    else
                    {
                        // regex for extracting the factor
                        string match = Regex.Match(expression, @"[0-9]([.,][0-9]{1,3})?").ToString();
                        double factor = double.Parse(match);
                        modRule.ObservedConclusionFactor = factor;
                    }

                    _factor = string.Empty;

                    RuleRichTextBox.Document = new FlowDocument();
                }
                else
                {
                    WarningLabel.Content = "Potential sytax mistakes. Press Ctrl+W for autocorrect";
                }



                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && _cleanedRule != string.Empty)
            {
                if (Keyboard.IsKeyDown(Key.W))
                {
                    bool bestVariableEver = false;
                    FlowDocument flow = RuleRichTextBox.Document;

                    // loop until all the mistakes are corrected
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


                    RuleRichTextBox.Document = flow;
                }
            }
        }

        /// <summary>
        /// Converts the expression and it's factor to their respective objects.
        /// If the conversion isn't successful, the invalid parts of the expression
        /// are colored red in the textbox as a warning to the user.
        /// </summary>
        /// <param name="expression">
        /// expression that needs to be validated
        /// </param>
        /// <param name="factor">
        /// factor part of the initial rule
        /// </param>
        /// <returns>
        /// flowDocument which contains the processed text,
        /// and a indicator whether the expression is valid
        /// </returns>
        private Tuple<FlowDocument, bool> ValidateExpression(string expression, string factor)
        {
            var expressionRegularized = true;
            _cleanedRule = string.Empty;

            // get all invalid character intervals in the expressions
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

            // wrap the intervals in a run object and
            // add it to the paragraph
            for (int i = 0; i < split.Length; i++)
            {
                if (i % 2 != 0)
                {
                    Brush brush = Brushes.Red;
                    var run = new Run(split[i])
                    {
                        Foreground = brush,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold
                    };
                    para.Inlines.Add(run);
                    expressionRegularized = false;
                }
                else
                {
                    para.Inlines.Add(new Run(split[i]));
                    _cleanedRule += " " + split[i]+ " ";
                }
            }
            para.Inlines.Add(new Run(thenPart));
            para.Inlines.Add(new Run(factor));

            // clean necessary multiple spacings
            _cleanedRule += " " + thenPart + " ";
            _cleanedRule = Regex.Replace(_cleanedRule, @"\s+", " ");
            _cleanedRule += factor;
            _cleanedRule = Regex.Replace(_cleanedRule, @"\s+", " ");

            // add the paragraph
            flow.Blocks.Add(para);
            Console.WriteLine(
                new TextRange(RuleRichTextBox.Document.ContentStart, RuleRichTextBox.Document.ContentEnd).Text);

            return new Tuple<FlowDocument, bool>(flow, expressionRegularized);

        }

        private static string _factor;
        private static string _cleanedRule;

        

    }
}
