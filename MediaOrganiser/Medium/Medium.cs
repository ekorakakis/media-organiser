using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaOrganiser
{
    public class Medium : IMedium
    {
        /// <summary>
        /// Private members
        /// </summary>

        private FileInfo _file;
        private string _fullPath;
        private string _name;
        private string _extension;
        private bool _hidden;
        private DateTime _dateTaken;

        private NameValueCollection _regexPatterns;
        private string _destination;

        /// <summary>
        /// Construction
        /// </summary>

        public Medium(FileInfo file, string destination, NameValueCollection regexPatterns)
        {
            // file properties
            _file = file;
            _fullPath = file.FullName;
            _name = file.Name;
            _extension = file.Extension;
            _hidden = file.Attributes.HasFlag(FileAttributes.Hidden);

            _dateTaken = CalculateDateAfter(_name);

            // other useful properties
            _destination = destination;
            _regexPatterns = regexPatterns;
        }

        /// <summary>
        /// Private interface
        /// </summary>

        private DateTime CalculateDateAfter(string name)
        {
            DateTime returnDateTime = DateTime.Now;

            // 1. Extract the year and the month. 
            // To do so, first find the filename without the extension
            string basePart = name.Substring(0, name.IndexOf(_extension));

            // Then extract the year; this normally is the first numerical value that 
            // appears in the filename. We have to ignore any other characters before that.
            int yearIndexFrom = basePart.IndexOfAny("0123456789".ToCharArray());
            string year = basePart.Substring(yearIndexFrom, 4);
            string month = basePart.Substring(yearIndexFrom + 4, 2);
            string day = basePart.Substring(yearIndexFrom + 6, 2);

            string datePattern = "ddMMyyyy";
            DateTime.TryParseExact(day + month + year , datePattern, null, DateTimeStyles.None, out returnDateTime);
            
            return returnDateTime;
        }

        /// <summary>
        /// Public Interface
        /// </summary>

        public bool CanProcess()
        {
            // set the default return value to false
            bool returnValue = false;

            // only bother to check if the file has been supplied
            if (_fullPath != null)
            {
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

        public async Task ProcessAsync()
        {
            await Task.Run(() =>
            {
                // await Task.Delay(1); // temp, replace this with processing
                
                // 1. Extract the year and the month. 
                // To do so, first find the filename without the extension
                // string basePart = _name.Substring(0, _name.IndexOf(_extension));

                // Then extract the year; this normally is the first numerical value that 
                // appears in the filename. We have to ignore any other characters before that.
                // int yearIndexFrom = basePart.IndexOfAny("0123456789".ToCharArray());
                // string year = basePart.Substring(yearIndexFrom, 4);
                // string month = basePart.Substring(yearIndexFrom + 4, 2);

                // 2. With this information create the full path and physically create the desitnation directory.
                string yearPath = Path.Combine(_destination, _dateTaken.ToString("yyyy"));
                string fullPath = Path.Combine(yearPath, _dateTaken.ToString("MM"));
                Directory.CreateDirectory(fullPath);

                // 3. Move the file to the destination folder
                var destination = Path.Combine(fullPath, _name);
                File.Move(_fullPath, destination);
            });
        }

        public string Name { get { return _name; } }

        public string FullPath {  get { return _fullPath; } }

        public string Extension { get { return _extension; } }

        private DateTime DateTaken { get { return _dateTaken; } }
        
        // private bool _hidden;

        public override string ToString()
        {
            return this.Name;
        }
    }
}
