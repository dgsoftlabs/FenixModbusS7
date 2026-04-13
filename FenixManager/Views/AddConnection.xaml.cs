using ProjectDataLib;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FenixWPF
{
    /// <summary>
    /// Interaction logic for AddConnection.xaml
    /// </summary>
    public partial class AddConnection : Window
    {
        private ProjectContainer projectContainer;
        private GlobalConfiguration globalConfiguration;
        private Connection currentConnection;
        private Guid projectId;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddConnection"/> class.
        /// </summary>
        /// <param name="projectContainerArg">The project container.</param>
        /// <param name="globalConfig">The global configuration.</param>
        /// <param name="id">The project ID.</param>
        public AddConnection(ProjectContainer projectContainerArg, GlobalConfiguration globalConfig, Guid id)
        {
            try
            {
                InitializeComponent();
                projectContainer = projectContainerArg;
                currentConnection = new Connection(globalConfig, projectContainerArg, "", "", null);
                globalConfiguration = globalConfig;
                projectId = id;
                CbDrivers.ItemsSource = globalConfiguration.GetDrivers();
                CbDrivers.SelectedIndex = 0;
                DataContext = currentConnection;
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the selection changed event of the CbDrivers ComboBox.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        private void CbDrivers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                IDriverModel idrv = (IDriverModel)((ComboBox)sender).SelectedItem;

                currentConnection.Parameters = idrv.setDriverParam;
                currentConnection.Idrv = idrv;
                currentConnection.DriverName = idrv.driverName;

                PgDrvProps.SelectedObject = currentConnection.Parameters;
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the click event of the Button_Save button.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                projectContainer.addConnection(projectId, currentConnection);
                Close();
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the click event of the Button_Close button.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }
    }
}