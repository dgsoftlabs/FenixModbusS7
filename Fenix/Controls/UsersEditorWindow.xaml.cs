using ProjectDataLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Fenix
{
    public partial class UsersEditorWindow : Window
    {
        public ObservableCollection<UserClass> Users { get; }

        public UsersEditorWindow(IEnumerable<UserClass> users)
        {
            InitializeComponent();
            Users = new ObservableCollection<UserClass>(users.Select(x => new UserClass(x.Name, x.Pass)));
            DataContext = this;
        }

        public void ApplyTo(IList target)
        {
            var sourceUsers = Users.Where(x => x != null).ToList();
            var targetUsers = target.Cast<UserClass>().ToList();
            var updateCount = Math.Min(targetUsers.Count, sourceUsers.Count);

            for (var i = 0; i < updateCount; i++)
            {
                targetUsers[i].Name = sourceUsers[i].Name;
                targetUsers[i].Pass = sourceUsers[i].Pass;
            }

            for (var i = updateCount; i < sourceUsers.Count; i++)
                target.Add(new UserClass(sourceUsers[i].Name, sourceUsers[i].Pass));
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            UsersGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            UsersGrid.CommitEdit(DataGridEditingUnit.Row, true);
            DialogResult = true;
        }
    }
}