using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ExpertSystem.Support;

namespace ExpertSystem.Observations
{
    public class ConcreteObservation : Observation, INotifyPropertyChanged
    {
        private double _observedFactor;
        private double _notObservedFactor;


        public double ObservedFactor
        {
            get { return _observedFactor;}
            set
            {
                _observedFactor = (value>1)?1:(value<0)?0:value;
                if(ObservationRuleList != null)
                    foreach (var rule in ObservationRuleList)
                    {
                        if (!rule.Conclusion.IsCached(rule.Conclusion.MbCache))
                        {
                            rule.Conclusion.ClearCache();
                            foreach (var rule1 in rule.Conclusion.ObservationRuleList)
                            {
                                rule1.Conclusion.ClearCache();
                            }
                        }
                    }
                ClearCache();
                OnPropertyChanged(nameof(ObservedFactor));
            } 
        }

        public double NotObservedFactor
        {
            get { return _notObservedFactor; }
            set
            {
                _notObservedFactor = (value > 1) ? 1 : (value < 0) ? 0 : value;
                if (ObservationRuleList != null)
                    foreach (var rule in ObservationRuleList)
                    {
                        if (!rule.Conclusion.IsCached(rule.Conclusion.MdCache))
                        {
                            rule.Conclusion.ClearCache();
                            foreach (var rule1 in rule.Conclusion.ObservationRuleList)
                            {
                                rule1.Conclusion.ClearCache();
                            }
                        }
                    }
                ClearCache();
                OnPropertyChanged(nameof(NotObservedFactor));
            } 
        }

        public List<Rule> ConclusionRuleList { get; set; }

        public List<Rule> ObservationRuleList { get; set; }

        public ConcreteObservation(string name): base(name)
        {
            ObservedFactor = 0;
            NotObservedFactor = 0;
            ConclusionRuleList = new List<Rule>();
            ObservationRuleList = new List<Rule>();
        }

        public bool IsObservation()
        {
            return ObservationRuleList.Count != 0;
        }

        public bool IsConclusion()
        {
            return ConclusionRuleList.Count != 0;
        }


        public override bool Equals(object obj)
        {
            bool result = base.Equals(obj);
            if (obj.GetType() == typeof(ConcreteObservation))
            {
                result = ((ConcreteObservation)obj).DisplayName() == DisplayName();
            }

            return result;
        }

        public override double Mb()
        {
            double tmpResult;
            if (!IsConclusion())
                tmpResult = ObservedFactor;
            else if (ConclusionRuleList.Count == 1)
                tmpResult = MbStrength(ConclusionRuleList[0]);
            else
            {
                tmpResult = CummulativeM(MbStrength(ConclusionRuleList[0]), MbStrength(ConclusionRuleList[1]));
                for (int i = 2; i < ConclusionRuleList.Count; i++)
                {
                    tmpResult = CummulativeM(tmpResult, MbStrength(ConclusionRuleList[i]));
                }
            }
           

            Core.MyWindow.Dispatcher.Invoke((Action)(() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\nMB(" + DisplayName() + ") = " + tmpResult);
                Core.MyWindow.RichTextBox.ScrollToEnd();
            }));

            Core.MyWindow.mySemaphore.WaitOne();
            return tmpResult;
        }

        public double CummulativeM(double res1, double res2)
        {
            return res1 + res2 - res1 * res2;
        }

        public double MbStrength(Rule rule)
        {
            double result = rule.ObservedConclusionFactor*Math.Max(0, rule.Observation.Cf());
            
            Core.MyWindow.Dispatcher.Invoke((Action)(() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\nMB(" + DisplayName() + ") = " + result);
                Core.MyWindow.RichTextBox.ScrollToEnd();
            }));

            Core.MyWindow.mySemaphore.WaitOne();
            return result;
        }

        public override double Md()
        {
            double tmpResult;
            if (ConclusionRuleList.Count == 0)
                tmpResult = NotObservedFactor;
            else if (ConclusionRuleList.Count == 1)
                tmpResult = MdStrength(ConclusionRuleList[0]);
            else
            {
                tmpResult = CummulativeM(MdStrength(ConclusionRuleList[0]), MdStrength(ConclusionRuleList[1]));
                for (int i = 2; i < ConclusionRuleList.Count; i++)
                {
                    tmpResult = CummulativeM(tmpResult, MdStrength(ConclusionRuleList[i]));
                }
            }

            Core.MyWindow.Dispatcher.Invoke((Action)(() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\nMD(" + DisplayName() + ") = " + tmpResult);
                Core.MyWindow.RichTextBox.ScrollToEnd();
            }));

            Core.MyWindow.mySemaphore.WaitOne();

            return tmpResult;
        }

        public double MdStrength(Rule rule)
        {
            double result = rule.NotObservedConclusionFactor* Math.Max(0, rule.Observation.Cf());
            

            Core.MyWindow.Dispatcher.Invoke((Action)(() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\nMD(" + DisplayName() + ") = " + result);
                Core.MyWindow.RichTextBox.ScrollToEnd();
            }));

            Core.MyWindow.mySemaphore.WaitOne();
            return result;
        }

        public override string DisplayName()
        {
            return Name;
        }



        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}