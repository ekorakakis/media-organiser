using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaOrganiser
{
    /// <summary>
    /// Implements the <see cref="IMedium" /> interface but there is no exception handling here. 
    /// The caller is responsible for handling any errors that may happen here.
    /// </summary>
    public class Medium : IMedium
    {
        /***********************
         ** Private Members
         ***********************/

        // Passed in the constructor
        private FileInfo _file;
        private string _destination;
        private DateTime _dateAfter;
        private NameValueCollection _regexPatterns;

        // Derived by the objects passed in the constructor (for ease of use)
        private string _fullPath;
        private string _name;
        private string _extension;
        private bool _hidden;
        private long _length;

        // Calculated
        private DateTime _dateTaken;
        private bool _isProcessed;
        private bool _regexMatching;

        /***********************
         ** Constructor
         ***********************/
        public Medium(FileInfo file, string destination, DateTime dateAfter, NameValueCollection regexPatterns)
        {
            // typically false by default; should become true if regex matches or if there is no regex matching to be applied (filename doesn't have the date)
            _regexMatching = false;

            // file properties
            _file = file;
            _fullPath = file.FullName;
            _name = file.Name;
            _extension = file.Extension;
            _hidden = file.Attributes.HasFlag(FileAttributes.Hidden);
            _length = file.Length;
            _dateTaken = CalculateDateAfter(_name);
            _dateAfter = dateAfter;

            // other useful properties
            _destination = destination;
            _regexPatterns = regexPatterns;
        }

        /***********************
         ** Private Interface
         ***********************/
        private DateTime CalculateDateAfter(string name)
        {
            DateTime returnDateTime = DateTime.MinValue;

            // 1. Extract the year and the month. 
            // To do so, first find the filename without the extension
            string basePart = name.Substring(0, name.IndexOf(_extension));

            // Then extract the year; this normally is the first numerical value that 
            // appears in the filename. We have to ignore any other characters before that.
            int yearIndexFrom = basePart.IndexOfAny("0123456789".ToCharArray());
            var leftOverLength = basePart.Length - yearIndexFrom;

            // does the filename have any numbers in it and are they enough to potentially form a date?
            if (leftOverLength >= 8 && yearIndexFrom >= 0)
            {
                string year = basePart.Substring(yearIndexFrom, 4);
                string month = basePart.Substring(yearIndexFrom + 4, 2);
                string day = basePart.Substring(yearIndexFrom + 6, 2);

                string datePattern = "ddMMyyyy";
                DateTime.TryParseExact(day + month + year, datePattern, null, DateTimeStyles.None, out returnDateTime);
            }
            else
            {
                // it doesn't look like we have the date in the filename - try the date created instead
                returnDateTime = _file.CreationTime;
                _regexMatching = true;
            }

            return returnDateTime;
        }

        private Boolean RegexPatternsMatch()
        {
            bool returnValue = _regexMatching;
            
            if (!returnValue)
            {
                // set the default return value to false
                Regex reg;

                // for every regular expression string supplied in the app.config
                foreach (var regExpression in _regexPatterns)
                {
                    // create a new regex and check if the file's name matches it
                    reg = new Regex(_regexPatterns[regExpression.ToString()]);
                    if (reg.IsMatch(_name))
                    {
                        // if this matches set the return value to true and return. It
                        // means that we can extract the year and month out of the name.
                        returnValue = true;
                        break;
                    }
                }
            }

            return returnValue;
        }

        /***********************
         ** Public Interface
         ***********************/

        /// <summary>
        /// "Filtering" criteria:
        /// 1. A file has actually been supplied
        /// 2. It's not hidden
        /// 3. No files before the pre-configured date
        /// 4. It matches the regex
        /// </summary>
        public bool CanProcess
        {
            get
            {
                return (_fullPath != null && !_hidden && _dateTaken >= _dateAfter && RegexPatternsMatch());
            }
        }

        public async Task ProcessAsync()
        {
            await Task.Run(() =>
            {
                // Create the full path and physically create the desitnation directory.
                string yearPath = Path.Combine(_destination, _dateTaken.ToString("yyyy"));
                string fullPath = Path.Combine(yearPath, _dateTaken.ToString("MM"));
                Directory.CreateDirectory(fullPath);

                // 3. Move the file to the destination folder
                var destination = Path.Combine(fullPath, _name);
                File.Move(_fullPath, destination);

                // mark this medium as processed
                _isProcessed = true;
            });
        }

        public string Name { get { return _name; } }

        public string FullPath {  get { return _fullPath; } }

        public string Extension { get { return _extension; } }

        public DateTime DateTaken { get { return _dateTaken; } }

        public double Length { get { return _length/1024/1024; } }

        public bool IsProcessed {  get { return _isProcessed; } }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
