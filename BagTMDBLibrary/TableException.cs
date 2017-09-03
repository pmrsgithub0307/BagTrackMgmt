using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMDBLibrary
{
    public class TableException : Exception
    {
        public TableException (String exceptionMessage) : base(exceptionMessage)
        {
        }
    }
}
