using System;
using System.Windows;
using ExpertSystem.Observations;

namespace ExpertSystem.Support
{
    /// <summary>
    /// Worker Thread that calculates the probability of the conclusion happening
    /// </summary>
    public class Worker
    {
        private Double _result;
        private readonly ConcreteObservation _resultConclusion;

        public Worker(ConcreteObservation conclusion)
        {
            _resultConclusion = conclusion;
        }

        /// <summary>
        /// Calculates the Cf of the conclusion, and safely displays it on the mainWindow 
        /// in the main thread.
        /// </summary>
        public void StartCalculation()
        {
            Core.MyWindow.mySemaphore.WaitOne();
            Console.WriteLine("UNBLOCKED WORKER");
            _result = _resultConclusion.Cf();
            Core.MyWindow.mySemaphore.Finish();

            Core.MyWindow.Dispatcher.Invoke((Action)(() =>
            {
                Core.MyWindow.RichTextBox.AppendText("\n" + "Cf(" + _resultConclusion + ") = " + _result);
                Core.MyWindow.RichTextBox.AppendText("\n" + "Time Elapsed = " + Core.MyWindow.mySemaphore.TimeMilis);
                Core.MyWindow.RichTextBox.ScrollToEnd();
                Core.MyWindow.ConclusionTextBox.Visibility = Visibility.Visible;
                Core.MyWindow.ConclusionInfo.Visibility=Visibility.Visible;
                Core.MyWindow.StepButton.Visibility = Visibility.Hidden;
                Core.MyWindow.FinishButton.Visibility=Visibility.Hidden;
                Core.MyWindow.RestartButton.Visibility=Visibility.Visible;
            }));

            Console.WriteLine("WORKER DONE");
        }
    }
}