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
        /***********************
         ** Private Members
         ***********************/
        private String _summary;
        private String _selectedPath;
        private String _destination;
        private bool _isSelectedPathValid;
        private bool _isDestinationValid;
        private Int32 _currentProgress;
        private DateTime _dateAfter;
        private NameValueCollection _regexPatterns;
        private ObservableCollection<Medium> _media;

        /***********************
         ** Constructor 
         ***********************/
        public ShellViewModel()
        {
            try
            {
                _media = new ObservableCollection<Medium>();

                InitShell();

                LoadFiles();
            }
            catch (Exception e)
            {
                // todo
                // what we should be doing here is to show them a screen where they can input this information and we store it
                // second best choice is to tell them what the problem is and so they can stop the app, fill in the config and restart
                // third best choice is show them a message
            }
        }


        /***********************
         ** Public Interface
         ***********************/
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
                _selectedPath = GetSelectedPath(value);                
                _isSelectedPathValid = !string.IsNullOrEmpty(_selectedPath);
                NotifyOfPropertyChange(() => SelectedPath);
            }
        }

        public String Destination
        {
            get { return _destination; }
            set
            {
                _destination = GetDestination(value);
                _isDestinationValid = !string.IsNullOrEmpty(_destination);
                NotifyOfPropertyChange(() => Destination);
            }
        }

        public bool CanProceed
        {
            get 
            { 
                // Processing possible only if both source and destinations paths are valid
                return _isSelectedPathValid && _isDestinationValid;  
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

        public async void ProcessFiles()
        {

            await DoProcessingAsync();
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
            get { return _dateAfter; }
            set
            {
                _dateAfter = value;
                NotifyOfPropertyChange(() => DateAfter);
            }
        }

        /***********************
         ** Private Helpers
         ***********************/
        private void InitShell()
        {
            // Load app configuration
            var section = ConfigurationManager.GetSection("environment") as NameValueCollection;

            SelectedPath = section["source"];       
            Destination = section["destination"];   

            string datePattern = section["datePattern"];
            DateTime.TryParseExact(section["dateAfter"], datePattern, null, DateTimeStyles.None, out _dateAfter);

            _regexPatterns = ConfigurationManager.GetSection("regexpatterns") as NameValueCollection;
        }

        private string GetSelectedPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return string.Empty;    // or can be Last Known Good Path
            }

            return path;
        }

        private string GetDestination(string destination)
        {
            if (string.IsNullOrEmpty(destination))
            {
                // OS location of the user picture folder by default
                // (or can be Last Known Good Path)
                destination = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }

            if (!Directory.Exists(destination))
                return string.Empty;

            return destination;
        }


        private async Task DoLoadingAsync()
        {
            _media.Clear();

            if (CanProceed)
            {
                try
                {
                    DirectoryInfo directory = new DirectoryInfo(SelectedPath);

                    // No filtering to be applied here; the filtering will happen once we have a populated collection of media
                    FileInfo[] files = directory.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

                    int progressValue = 0;
                    int currentFileIndex = 0;
                    double iMax = files.Count();


                    foreach (var file in files)
                    {
                        progressValue++;
                        Int32 workerValue = Convert.ToInt32((double)(progressValue / iMax) * 100);
                        CurrentProgress = workerValue;

                        var medium = new Medium(file, _destination, _dateAfter, _regexPatterns);

                        // The CanProcess method can "filter out" any media that are not 
                        if (medium.CanProcess())
                        {
                            var duplicateMedium = _media.FirstOrDefault(x => (x.Name == medium.Name) && (x.Length <= medium.Length));
                            if (duplicateMedium != null)
                            {
                                _media.Remove(duplicateMedium);
                                currentFileIndex--;
                            }

                            _media.Add(medium);
                            currentFileIndex++;
                            UpdateSummary(currentFileIndex);
                        }
                    }

                    // this needs run outside the loop just in case there was none found ...
                    UpdateSummary(currentFileIndex);
                }
                catch (Exception e)
                {
                    // ApplicationException() - what do we want to do here?
                    // do not swallow the original exception
                    //    System.Windows.MessageBox.Show("An error occured.", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw e;
                }
            }
            else
            {
                // TODO Prompt user to browse for valid paths                
            }
        }

        private async Task DoProcessingAsync()
        {
            try
            {
                // todo: question - MessageBox.Show() are you sure?
                foreach (var medium in _media)
                    await medium.ProcessAsync();
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
