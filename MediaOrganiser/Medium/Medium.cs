using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaOrganiser
{
    public class Medium : IMedium
    {
        private FileInfo _fileInfo;
        private NameValueCollection _regexPatterns;
        private string _destination;

        public Medium(FileInfo file, string destination, NameValueCollection regexPatterns)
        {
            _fileInfo = file;
            _destination = destination;
            _regexPatterns = regexPatterns;
        }

        public bool CanProcess()
        {
            // set the default return value to false
            bool returnValue = false;

            // only bother to check if the file has been supplied
            if (_fileInfo != null)
            {
                Regex reg;

                // for every regular expression string supplied in the app.config
                foreach (var regExpression in _regexPatterns)
                {
                    // create a new regex and check if the file's name matches it
                    reg = new Regex(_regexPatterns[regExpression.ToString()]);
                    if (reg.IsMatch(_fileInfo.Name))
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
                string basePart = _fileInfo.Name.Substring(0, _fileInfo.Name.IndexOf(_fileInfo.Extension));

                // Then extract the year; this normally is the first numerical value that 
                // appears in the filename. We have to ignore any other characters before that.
                int yearIndexFrom = basePart.IndexOfAny("0123456789".ToCharArray());
                string year = basePart.Substring(yearIndexFrom, 4);
                string month = basePart.Substring(yearIndexFrom + 4, 2);

                // 2. With this information create the full path and physically create the desitnation directory.
                string yearPath = Path.Combine(_destination, year);
                string fullPath = Path.Combine(yearPath, month);
                Directory.CreateDirectory(fullPath);

                // 3. Move the file to the destination folder
                var destination = Path.Combine(fullPath, _fileInfo.Name);
                File.Move(_fileInfo.FullName, destination);
            });
        }

        public string Name { get { return _fileInfo.Name; } }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
