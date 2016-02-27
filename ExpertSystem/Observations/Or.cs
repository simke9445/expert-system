using System;
using ExpertSystem.Support;

namespace ExpertSystem.Observations
{
    public class Or : Relation
    {
        public Or(Observation left, Observation right) : base(left, right, "ILI")
        {
        }

        public override double Md()
        {
            double result = Math.Min(Left.MdCached(), Right.MdCached());
            
            Core.MyWindow.Dispatcher.Invoke((Action)(() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\nMin(MD(" + Left + "), MD(" + Right + ")) = " + result);
                Core.MyWindow.RichTextBox.ScrollToEnd();
            }));

            Core.MyWindow.mySemaphore.WaitOne();
            return result;
        }

        public override double Mb()
        {
            double result = Math.Max(Left.MbCached(), Right.MbCached());
            Core.MyWindow.Dispatcher.Invoke((Action)(() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\nMax(MB(" + Left + "), MB(" + Right + ")) = " + result);
                Core.MyWindow.RichTextBox.ScrollToEnd();
            }));

            Core.MyWindow.mySemaphore.WaitOne();
            return result;
        }



        

    }

}