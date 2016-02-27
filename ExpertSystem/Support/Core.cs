namespace ExpertSystem.Support
{
    /// <summary>
    /// Class that serves as a global mainWindow refernce
    /// </summary>
    public class Core
    {
        private static MainWindow _mainWindow = null;

        // call this when your non-static form is created
        //
        public static void SetWindow(MainWindow wnd)
        {
            _mainWindow = wnd;
        }

        public static MainWindow MyWindow 
        {
            get
            {
                return _mainWindow;
            }
        }

    }
}