using System;
using ExpertSystem.Support;

namespace ExpertSystem.Observations
{
    public class Not : Observation
    {
        private readonly Observation _observation;

        public Not(Observation observation):base("-")
        {
            _observation = observation;
        }

        public override string DisplayName()
        {
            return Name + " " + _observation.DisplayName();
        }

        public override double MbCached()
        {
            return Mb();
        }

        public override double MdCached()
        {
            return Md();
        }

        public override double Md()
        {
            double result = _observation.MbCached();
            Core.MyWindow.Dispatcher.Invoke((Action)(() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\nMD(" + DisplayName() + ") = " + result);
                Core.MyWindow.RichTextBox.ScrollToEnd();
            }));

            Core.MyWindow.mySemaphore.WaitOne();
            return result;
        }

        public override double Mb()
        {
            double result = _observation.MdCached();
            Core.MyWindow.Dispatcher.Invoke((Action) (() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\nMD(" + DisplayName() + ") = " + result);
                Core.MyWindow.RichTextBox.ScrollToEnd();
            }));

            return result;
        }
    }
}