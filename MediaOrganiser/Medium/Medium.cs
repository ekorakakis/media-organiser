using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaOrganiser
{
    public class Medium : IMedium
    {
        private FileInfo _fileInfo;
        private NameValueCollection _regexPatterns;

        public Medium(FileInfo file, NameValueCollection regexPatterns)
        {
            _fileInfo = file;
            _regexPatterns = regexPatterns;
        }

        public async Task ProcessAsync()
        {
            // do the thing
            await Task.Delay(1); // temp, replace this with processing
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

        public string Name { get { return _fileInfo.Name; } }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
