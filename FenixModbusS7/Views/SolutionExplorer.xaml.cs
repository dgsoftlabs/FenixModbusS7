using System.Windows.Controls;

namespace Fenix
{
    /// <summary>
    /// Interaction logic for TreeViewManager.xaml
    /// </summary>
    public partial class SolutionExplorer : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionExplorer"/> class.
        /// </summary>
        public SolutionExplorer()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}