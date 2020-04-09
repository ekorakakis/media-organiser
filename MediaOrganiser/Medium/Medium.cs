using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaOrganiser
{
    public class Medium : IMedium
    {
        private FileInfo _fileInfo;

        public Medium(FileInfo file)
        {
            _fileInfo = file;
        }

        public async Task ProcessAsync()
        {
            // do the thing
            await Task.Delay(1); // temp, replace this with processing
        }

        public bool CanProcess()
        {
            return _fileInfo != null;
        }

        public string Name { get { return _fileInfo.Name; } }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
