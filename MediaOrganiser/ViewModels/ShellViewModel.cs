using Caliburn.Micro;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaOrganiser.ViewModels
{
    public class ShellViewModel : PropertyChangedBase
    {
        /* Private Members */
        private String _summary;
        private String _selectedPath;
        private String _destination;
        private Int32 _currentProgress;
        private DateTime _dateAfter;
        private NameValueCollection _regexPatterns;
        private ObservableCollection<Medium> _media;

        /* Constructor */
        public ShellViewModel()
        {
            try
            {
                _media = new ObservableCollection<Medium>();
                
                var section = ConfigurationManager.GetSection("environment") as NameValueCollection;
                SelectedPath = section["source"];
                Destination = section["destination"];

                string datePattern = "dd/MM/yyyy";
                DateTime.TryParseExact(section["dateAfter"], datePattern, null, DateTimeStyles.None, out _dateAfter);

                _regexPatterns = ConfigurationManager.GetSection("regexpatterns") as NameValueCollection;
            }
            catch (Exception e)
            {
                // todo
                // what we should be doing here is to show them a screen where they can input this information and we store it
                // second best choice is to tell them what the problem is and so they can stop the app, fill in the config and restart
                // third best choice is show them a message
            }
        }

        /* Public interface */
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

        public String Destination
        {
            get { return _destination; }
            set
            {
                _destination = value;
                NotifyOfPropertyChange(() => Destination);
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

        public async void LoadFiles()
        {
            await DoLoadingAsync();
        }

        public void BrowseSource()
        {
            try
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                folderDialog.SelectedPath = SelectedPath;
                folderDialog.ShowDialog();
                SelectedPath = folderDialog.SelectedPath;
                Reset();
            }
            catch (Exception e)
            {
                // what do we want to do here?
            }
        }

        public void BrowseDestination()
        {
            try
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                folderDialog.SelectedPath = Destination;
                folderDialog.ShowDialog();
                Destination = folderDialog.SelectedPath;
                Reset();
            }
            catch (Exception e)
            {
                // what do we want to do here?
            }
        }

        public DateTime DateAfter
        {
            get            {                return _dateAfter;             }
            
            set
            {
                _dateAfter = value;
                NotifyOfPropertyChange(() => DateAfter);
            }
        }

        /* Private helpers */
        private async Task DoLoadingAsync()
        {
            try
            {
                _media.Clear();

                DirectoryInfo directory = new DirectoryInfo(SelectedPath);
                FileInfo[] files = directory.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

                // Filter criteria:
                // 1. No hidden files
                // 2. No files before the pre-configured date
                var filtered = files.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden));
                
                int progressValue = 0;
                int currentFileIndex = 0;
                double iMax = filtered.Count();
                // double iMax = files.Count();

                foreach (var file in filtered)
                {
                    progressValue++;
                    Int32 workerValue = Convert.ToInt32((double)(progressValue / iMax) * 100);
                    CurrentProgress = workerValue;
                    
                    var medium = new Medium(file, _destination, _regexPatterns);
                    if (medium.CanProcess())
                    {
                        currentFileIndex++;

                        // to do - probably don't want to process the files here; this should be for loading only
                        // await medium.ProcessAsync();
                        _media.Add(medium);
                        UpdateSummary(currentFileIndex);
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

        private void Reset()
        {
            CurrentProgress = 0;
            // Summary = ""
        }
    }
}
