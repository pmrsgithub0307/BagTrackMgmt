//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BagTMDBLibrary
{
    using System;
    using System.Collections.Generic;
    
    public partial class OSUSR_BMT_H2H
    {
        public int ID { get; set; }
        public string I_FLIGHT { get; set; }
        public string I_ORIGIN { get; set; }
        public string I_STAND { get; set; }
        public Nullable<System.DateTime> I_ETA { get; set; }
        public Nullable<System.DateTime> I_ATA { get; set; }
        public Nullable<System.DateTime> HULL_OPEN { get; set; }
        public Nullable<decimal> I_UNLOAD { get; set; }
        public Nullable<decimal> I_TAXI { get; set; }
        public Nullable<decimal> I_INJECT { get; set; }
        public Nullable<System.DateTime> ETU { get; set; }
        public Nullable<int> NRBAGS { get; set; }
        public string ONTO_FLIGHT { get; set; }
        public Nullable<int> HUB { get; set; }
        public string O_FLIGHT { get; set; }
        public Nullable<decimal> O_INJECT { get; set; }
        public Nullable<decimal> O_TAXI { get; set; }
        public Nullable<decimal> O_LOAD { get; set; }
        public Nullable<System.DateTime> ETL { get; set; }
        public Nullable<System.DateTime> ETD { get; set; }
        public Nullable<System.DateTime> HULL_CLOSE { get; set; }
        public Nullable<System.DateTime> ATD { get; set; }
        public string STAND_D { get; set; }
        public string DEPARTED { get; set; }
    }
}
