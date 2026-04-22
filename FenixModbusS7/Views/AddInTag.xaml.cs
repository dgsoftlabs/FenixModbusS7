using ProjectDataLib;
using System;
using System.Windows;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for AddInTag.xaml
    /// </summary>
    public partial class AddInTag : Window
    {
        private Project currentProject { get; set; }
        private ProjectContainer projectContainer { get; set; }
        private InTag currentInTag { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddInTag"/> class.
        /// </summary>
        /// <param name="projId">The project ID.</param>
        /// <param name="prCon">The project container.</param>
        public AddInTag(Guid projId, ProjectContainer prCon)
        {
            try
            {
                InitializeComponent();

                projectContainer = prCon;
                currentProject = projectContainer.getProject(projId);

                string nm = "InTag";
                for (int x = 0; ; x++)
                {
                    if (projectContainer.GetAllITags().Exists(k => k.Name == nm))
                        nm = $"{nm}{x}";
                    else
                        break;
                }

                currentInTag = new InTag(projectContainer, currentProject, nm, "", TypeData.DOUBLE, "0");
                DataContext = currentInTag;
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the click event of the Ok button.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                projectContainer.AddIntTag(currentProject.objId, currentInTag);
                Close();
            }
            catch (Exception Ex)
            {
                projectContainer.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        /// <summary>
        /// Handles the click event of the Cancel button.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
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