using ProjectDataLib;
using System;
using System.Windows;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for AddDevice.xaml
    /// </summary>
    public partial class AddDevice : Window
    {
        private ProjectContainer projectContainer;
        private Device deviceInstance;
        private Guid connectionId = Guid.Empty;
        private Guid projectId = Guid.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddDevice"/> class.
        /// </summary>
        /// <param name="pC">The project container.</param>
        /// <param name="projId">The project ID.</param>
        /// <param name="connId">The connection ID.</param>
        public AddDevice(ProjectContainer pC, Guid projId, Guid connId)
        {
            InitializeComponent();

            try
            {
                projectContainer = pC;
                this.connectionId = connId;
                this.projectId = projId;

                Connection cn = pC.getConnection(projId, connId);
                TbAdress.IsEnabled = cn.Idrv.AuxParam[0];
                deviceInstance = new Device("", 1, projectContainer, projId);
                DataContext = deviceInstance;
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the Click event of the Ok button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                deviceInstance.idrv = projectContainer.getConnection(projectId, connectionId).Idrv;
                projectContainer.addDevice(projectId, connectionId, deviceInstance);
                Close();
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the Click event of the Close button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
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