using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaOrganiser.ViewModels
{
    public class ShellViewModel : PropertyChangedBase
    {
        private String _summary;
        private String _selectedPath;
        private Int32 _currentProgress;
        private NameValueCollection _regexPatterns;
        private ObservableCollection<Medium> _media;

        public ShellViewModel()
        {
            try
            {
                _media = new ObservableCollection<Medium>();
                
                var section = ConfigurationManager.GetSection("environment") as NameValueCollection;
                SelectedPath = section["path"];

                _regexPatterns = ConfigurationManager.GetSection("regexpatterns") as NameValueCollection;
                // _regexPatterns = section["regex[atterms"]
            }
            catch (Exception e)
            {
                // todo
                // what we should be doing here is to show them a screen where they can input this information and we store it
                // second best choice is to tell them what the problem is and so they can stop the app, fill in the config and restart
                // third best choice is show them a message
            }
        }

        public ObservableCollection<Medium> Media
        {
            get { return _media; }
            set
            {
                _media = value;
                NotifyOfPropertyChange(() => Media);
            }
        }

        public String SelectedPath
        {
            get { return _selectedPath; }
            set
            {
                _selectedPath = value;
                NotifyOfPropertyChange(() => SelectedPath);
            }
        }

        public Int32 CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                _currentProgress = value;
                NotifyOfPropertyChange(() => CurrentProgress);
            }
        }

        public String Summary
        {
            get { return _summary; }
            set
            {
                _summary = value;
                NotifyOfPropertyChange(() => Summary);
            }
        }

        private async Task DoLoadingAsync()
        {
            try
            {
                _media.Clear();

                DirectoryInfo directory = new DirectoryInfo(SelectedPath);
                FileInfo[] files = directory.GetFiles("*.*", System.IO.SearchOption.AllDirectories);
                var filtered = files.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden));

                int progressValue = 0;
                int filteredIndex = 0;
                double iMax = filtered.Count();
                foreach (var file in filtered)
                {
                    progressValue++;
                    Int32 workerValue = Convert.ToInt32((double)(progressValue / iMax) * 100);
                    CurrentProgress = workerValue;
                    
                    var medium = new Medium(file, _regexPatterns);
                    if (medium.CanProcess())
                    {
                        filteredIndex++;
                        await medium.ProcessAsync();
                        _media.Add(medium);
                        UpdateSummary(filteredIndex);
                    }
                }
            }

            catch (Exception e)
            {
                // ApplicationException() - what do we want to do here?
                // do not swallow the original exception
                //    System.Windows.MessageBox.Show("An error occured.", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw e;
            }
        }

        private void UpdateSummary(Int32 numberOfFiles)
        {
            if (numberOfFiles == 0)
                Summary = String.Format("No files found");
            else if (numberOfFiles == 1)
                Summary = String.Format("{0} file found", numberOfFiles);
            else
                Summary = String.Format("{0} files found", numberOfFiles);
        }

        public async void LoadFiles()
        {
            await DoLoadingAsync();
        }

        public void Browse()
        {
            try
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                folderDialog.SelectedPath = SelectedPath;
                folderDialog.ShowDialog();
                SelectedPath = folderDialog.SelectedPath;
                Reset();
                
                // System.Windows.MessageBox.Show(SelectedPath);

                /* A few things that need to happen here:
                 1. Have a service registered and available to call (a filedialogservice);
                 2. Use the service to open the dialog box and record the path;
                 3. This ViewModel should have a property for the path somewhere. There is a lot of logic now on the ViewModel; is that a problem?
                 */

                //var fileDialogService = container.Resolve<IFileDialogService>();

                //https://stackoverflow.com/questions/1619505/wpf-openfiledialog-with-the-mvvm-pattern

                //string path = fileDialogService.OpenFileDialog();

                //if (!string.IsNullOrEmpty(path))
                //{
                //Do stuff
                //}
            }
            catch (Exception e)
            {
                // what do we want to do here?
            }
        }

        private void Reset()
        {
            CurrentProgress = 0;
            // Summary = ""
        }
    }
}
