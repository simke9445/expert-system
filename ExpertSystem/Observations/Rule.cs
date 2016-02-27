using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ExpertSystem.Observations
{
    public partial class Rule : INotifyPropertyChanged
    {
        /// <summary>
        /// Observation Object, containing nested Observations
        /// </summary>
        public Observation Observation { get; set; }


        public ConcreteObservation Conclusion { get;set;}

        /// <summary>
        /// List of nested objects in Observation
        /// </summary>
        public List<ConcreteObservation> ConcreteObservations;

        private double _observedConclusionFactor;

        public double ObservedConclusionFactor
        {
            get { return _observedConclusionFactor; }
            set
            {
                _observedConclusionFactor = (value > 1) ? 1 : (value < 0) ? 0 : value;
                UpdateName();
                Conclusion.ClearCache();
                foreach (var rule in Conclusion.ObservationRuleList)
                {
                    rule.Conclusion.ClearCache();
                }
                OnPropertyChanged(nameof(ObservedConclusionFactor));
            }
        }

        private double _notObservedConclusionFactor;

        public double NotObservedConclusionFactor
        {
            get { return _notObservedConclusionFactor; }
            set
            {
                _notObservedConclusionFactor = (value > 1) ? 1 : (value < 0) ? 0 : value;
                UpdateName();
                Conclusion.ClearCache();
                foreach (var rule in Conclusion.ObservationRuleList)
                {
                    rule.Conclusion.ClearCache();
                }
                OnPropertyChanged(nameof(NotObservedConclusionFactor));
            }
        }

        private string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
        
        public Rule(Observation observation, ConcreteObservation conclusion, List<ConcreteObservation> concreteObservations)
        {
            Observation = observation;
            Conclusion = conclusion;

            ConcreteObservations = concreteObservations;
            foreach (var concreteObservation in ConcreteObservations)
            {
                concreteObservation.ObservationRuleList.Add(this);
            }

            ObservedConclusionFactor = conclusion.ObservedFactor;
            NotObservedConclusionFactor = conclusion.NotObservedFactor;

            UpdateName();

            conclusion.ConclusionRuleList.Add(this);
        }

        private void UpdateName()
        {
            Name = "AKO " + Observation + " \n\tONDA " + "(" + ObservedConclusionFactor + ")" + Conclusion;
        }

        public string DisplayName()
        {
            return Name.Replace("\t", string.Empty).Replace("\n", string.Empty);
        }

        public string NameCleaned()
        {
            var match = Regex.Match(Name, @"\(\d([,.]\d{1,3})?\)").ToString();

            return DisplayName().Replace(match,string.Empty);
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            bool result = base.Equals(obj);
            if (obj.GetType() == typeof(Rule))
            {
                result = ((Rule) obj).NameCleaned() == NameCleaned();
            }

            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}