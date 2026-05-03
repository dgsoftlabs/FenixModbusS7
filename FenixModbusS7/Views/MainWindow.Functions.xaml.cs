using AvalonDock.Layout.Serialization;
using ProjectDataLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Fenix
{
    public partial class MainWindow : Window
    {
        private void CheckAccessForNodes()
        {
            try
            {
                propManag.Enabled = _viewModel.CheckAccessForNodes(
                    tvMain.View.SelectedItem,
                    SelObj,
                    PrCon.SrcType,
                    PrCon.anyCommunication(),
                    propManag.Enabled);
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private async Task VerifySoftwareUpdate(object sender)
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                Dispatcher.Invoke(() => lbInfo.Content = "No Internet connection.");
                return;
            }

            Dispatcher.Invoke(() => lbInfo.Content = "Checking update for software...");

            try
            {
                string result = await ProjectContainer.GetVersionFromGitHub();
                if (result != null)
                {
                    var newVer = ProjectContainer.ParseVersionFromContent(result);
                    var url = ProjectContainer.ParseUrlFromContent(result);

                    CheckVersion(newVer, url, (bool)sender);
                    Dispatcher.Invoke(() => lbInfo.Content = "Completed");
                }
            }
            catch (Exception)
            {
                // Handle exceptions if necessary
            }
        }

        private void CheckVersion(Version newVersion, string url, bool automatic)
        {
            // Get the running version
            Version curVersion = Assembly.GetExecutingAssembly().GetName().Version;

            // Compare the versions
            if (curVersion < newVersion)
            {
                // Ask the user if they would like to download the new version
                string title = "New version detected.";
                string question = $"Download the new version Fenix {newVersion}?";

                if (MessageBox.Show(question, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    // Navigate the default web browser to the app homepage (the URL comes from the XML content)
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
            }
            else if (automatic)
            {
                MessageBox.Show(this, "Your version is up to date.");
            }
        }

        private void DeleteElementMethod(Guid projId, Guid id, ElementKind elKind)
        {
            try
            {
                //Brak projektów w Buforze
                if (PrCon.projectList.Count == 0)
                    return;

                //Chcemy usunąc projekt

                if (MessageBox.Show("Do you really want delate this element", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    PrCon.deleteElement(Pr.objId, id, elKind);
                    return;
                }
            }
            catch (Exception Ex)
            {
                if (PrCon.ApplicationError != null)
                    PrCon.ApplicationError(this, new ProjectEventArgs(Ex));
            }
        }

        private void SaveLayout()
        {
            try
            {
                if (Pr == null) return;
                string path = Path.GetDirectoryName(PrCon.projectList.First().path) + "\\" + PrCon.LayoutFile;
                XmlLayoutSerializer serializer = new XmlLayoutSerializer(dockManager);
                serializer.Serialize(path);
                System.Diagnostics.Debug.WriteLine($"[SaveLayout] OK: {path}");
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveLayout] ERROR: {Ex.Message}");
                PrCon.ApplicationError?.Invoke(this, new ProjectEventArgs(Ex));
            }
        }

        private TimersFolder FindParentTimersFolder(CustomTimer timer)
        {
            if (Pr == null) return null;

            var scriptChildren = ((ITreeViewModel)Pr.ScriptEng).Children;
            if (scriptChildren != null)
                foreach (var child in scriptChildren)
                    if (child is TimersFolder tf && ((ITreeViewModel)tf).Children.Contains(timer))
                        return tf;

            var intTagChildren = ((ITreeViewModel)Pr.InternalTagsDrv).Children;
            if (intTagChildren != null)
                foreach (var child in intTagChildren)
                    if (child is TimersFolder tf && ((ITreeViewModel)tf).Children.Contains(timer))
                        return tf;

            return null;
        }
    }
}