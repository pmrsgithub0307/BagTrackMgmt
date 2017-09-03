using BagTMDBLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMEngineProcessing
{
    interface IBagIntegrityTransformation
    {
        void CreateBagIntegrity(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub, bool isHub);
        void UpdateBagIntegrity(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub, bool isHub, Int32 TTYChange);
    }
}
