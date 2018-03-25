using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.UI.Xaml;

namespace DSA.Shell.Logging
{
    public static class GlobalCrashHandler
    {
        public static void Configure()
        {
            Application.Current.UnhandledException += App_UnhandledException;
        }

        static void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // unbind we're going to re-enter and don't want to loop...
            Application.Current.UnhandledException -= App_UnhandledException;

            // say we've handled this one. this allows our FATAL write to complete.
            e.Handled = true;

            // Log critical issue
            var logger = LoggingServices.DefaultLogManager.GetLogger();
            logger.Log(LoggingLevel.Critical, "The application crashed: " + e.Message, e);

            // abort the app here...
            Application.Current.Exit();
        }
    }
}
