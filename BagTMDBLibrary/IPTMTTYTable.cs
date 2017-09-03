using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMDBLibrary
{
    public interface IPTMTTYTable
    {
        void SetValue(String methodName, String parameter);

        OSUSR_UUK_PAXMSGS Save(BaggageEntities db, String hub);
    }
}
