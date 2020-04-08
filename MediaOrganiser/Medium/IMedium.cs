using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaOrganiser
{
    public interface IMedium
    {
        void Process();

        bool CanProcess();
    }
}
