using Caliburn.Micro;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Configuration;
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
                if (_isSelectedPathValid)
                {
                    // Save last known good path
                    SaveAppSetting("environment", "source", _selectedPath);
                }

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
                if (_isDestinationValid)
                {
                    // Save last known good path
                    SaveAppSetting("environment", "destination", _destination);
                }

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
            if (CanProceed)
            {
                await DoLoadingAsync();
            }
            else
            {
                // TODO Prompt user to browse for valid paths                
            }
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
                DialogResult result = folderDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    SelectedPath = folderDialog.SelectedPath;
                    Reset();
                }
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
                folderDialog.ShowNewFolderButton = true;
                DialogResult result = folderDialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    Destination = folderDialog.SelectedPath;
                    Reset();
                }
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

            // Assume that the user hasn't made a mistake and that they want that destination folder created
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            return destination;
        }

        private void SaveAppSetting(string section, string key, string value)
        {
            // Persist setting value in app config
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                var settings = ((AppSettingsSection)config.GetSection(section)).Settings;
                                
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }

                config.Save(ConfigurationSaveMode.Full);

                ConfigurationManager.RefreshSection(section);
            }
            catch (ConfigurationErrorsException)
            {
                // what do we want to do here?
            }
        }

        private async Task DoLoadingAsync()
        {
            // todo - this is not an async process anymore?
            _media.Clear();

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
                    if (medium.CanProcess)
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
                    
                // do we need to update this here too?
                // UpdateSummary(currentFileIndex);
                throw e;
            }
        }

        private async Task DoProcessingAsync()
        {
            try
            {
                // todo: question - MessageBox.Show() are you sure?
                // also another todo - add another progress bar (or reuse the same but place elsewhere?) to show progress as you move files about
                // also another todo - how do we enable/disable statuses as we get into the various methods (so buttons disabled when loading the files or when processing them)
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
