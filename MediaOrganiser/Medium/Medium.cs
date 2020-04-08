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
        private FileInfo fileInfo;

        public Medium(FileInfo file)
        {
            fileInfo = file;
        }

        public void Process()
        {

        }

        public bool CanProcess()
        {
            return false;
        }

        public string Name { get { return fileInfo.Name; } }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
