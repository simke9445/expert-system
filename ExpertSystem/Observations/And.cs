using System;
using ExpertSystem.Support;

namespace ExpertSystem.Observations
{
    public class And : Relation
    {
        public And(Observation left, Observation right) : base(left, right, "I")
        {
        }

        public override double Md()
        {
            double result = Math.Max((double) Left.MdCached(), Right.MdCached());

            Core.MyWindow.Dispatcher.Invoke((Action)(() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\nMax(MD(" + Left + "), MD(" + Right + ")) = " + result);
                Core.MyWindow.RichTextBox.ScrollToEnd();
            }));

            Core.MyWindow.mySemaphore.WaitOne();

            return result;
        }

        public override double Mb()
        {
            double result = Math.Min((double) Left.MbCached(), Right.MbCached());
            
            Core.MyWindow.Dispatcher.Invoke((Action)(() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\nMin(MB(" + Left + "), MB(" + Right + ")) = " + result);
                Core.MyWindow.RichTextBox.ScrollToEnd();
            }));

            Core.MyWindow.mySemaphore.WaitOne();
            return result;
        }
    }
}