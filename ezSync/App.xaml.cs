using System.Windows;

namespace ezSync
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        #region Methods

        /// <summary>
        /// Overridden to persist the settings when exiting.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            ezSync.Properties.Settings.Default.Save();
        }

        #endregion
    }
}