using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMDBLibrary
{
    public interface IBaggageTTYTable
    {
        void SetValue(String methodName, String parameter);

        void Clean();

        OSUSR_UUK_BAGMSGS Save(BaggageEntities db);
    }
}
