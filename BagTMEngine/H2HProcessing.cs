using BagTMCommon;
using BagTMDBLibrary;
using BagTMEngineProcessing.Rules;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMEngineProcessing
{
    class H2HProcessing
    {

        /// <summary>
        /// Hub code for Baggage Integrity
        /// </summary>
        private String hub;

        /// <summary>
        /// Reference tables
        /// </summary>
        List<OSUSR_UUK_BAGTIMESREFERENCE> refList;
        List<OSUSR_UUK_EQUIPTYPE> equipementTypeList;
        List<OSUSR_UUK_TAXITIMES> taxiTimesList;
        List<OSUSR_UUK_REGISTRATIONS> registrationList;

        /// <summary>
        /// Default Equipment Type for Reference Tables
        /// </summary>
        private String defaultEquipment;

        /// <summary>
        /// Default From for Reference Tables
        /// </summary>
        private String defaultStandFrom;

        /// <summary>
        /// Default To for Reference Tables
        /// </summary>
        private String defaultGateTo;

        /// <summary>
        /// Baggage Terminal Code
        /// </summary>
        private String baggageTerminalCode;

        /// <summary>
        /// Min Load / Unload Time
        /// </summary>
        private Int32 minLoadUnloadTime;

        /// <summary>
        /// Max Load / Unload Time
        /// </summary>
        private Int32 maxLoadUnloadTime;

        /// <summary>
        /// Default Max Baggage Turnaround
        /// </summary>
        private Int32 maxBaggageTurnaround;

        /// <summary>
        /// Sorter Max ThroughPut Baggages / Minute
        /// </summary>
        private Int32 maxSorterThroughPut;

        /// <summary>
        /// Map to store sorter working volumes
        /// </summary>
        private SorterProcessingVolumeMap sorterVolumeMap;

        /// <summary>
        /// Sorter Timer in minutes
        /// </summary>
        private Int32 sorterTime;

        public H2HProcessing(String hub, String defaultEquipment, 
                String defaultStandFrom, String defaultGateTo, String baggageTerminalCode, Int32 minLoadUnloadTime,
                Int32 maxLoadUnloadTime, Int32 maxBaggageTurnaround, SorterProcessingVolumeMap sorterVolumeMap,
                Int32 maxSorterThroughPut, Int32 sorterTime, List<OSUSR_UUK_BAGTIMESREFERENCE> refList, 
                List<OSUSR_UUK_EQUIPTYPE> equipementTypeList, List<OSUSR_UUK_TAXITIMES> taxiTimesList,
                List<OSUSR_UUK_REGISTRATIONS> registrationList)
        {
            this.hub = hub;
            this.registrationList = registrationList;
            this.defaultEquipment = defaultEquipment;
            this.defaultStandFrom = defaultStandFrom;
            this.defaultGateTo = defaultGateTo;
            this.baggageTerminalCode = baggageTerminalCode;
            this.minLoadUnloadTime = minLoadUnloadTime;
            this.maxLoadUnloadTime = maxLoadUnloadTime;
            this.maxBaggageTurnaround = maxBaggageTurnaround;
            this.sorterVolumeMap = sorterVolumeMap;
            this.maxSorterThroughPut = maxSorterThroughPut;
            this.sorterTime = sorterTime;
            this.refList = refList;
            this.equipementTypeList = equipementTypeList;
            this.taxiTimesList = taxiTimesList;
            this.registrationList = registrationList;
        }

        /// <summary>
        /// Update H2H table based on flight information
        /// </summary>
        public void EngineUpdateH2H(OSUSR_UUK_FLT_INFO flightInfoI, OSUSR_UUK_FLT_INFO flightInfoF, bool isHub)
        {
            BagTMLog.LogDebug("BagTM Engine Update H2H Start", this);

            try
            {
                BagTMLog.LogDebug("BagTM Engine Update H2H FlightInfoI", flightInfoI);
                BagTMLog.LogDebug("BagTM Engine Update H2H FlightInfoF", flightInfoF);
                BagTMLog.LogDebug("BagTM Engine Update H2H Hub", this.hub);

                BagTMLog.LogDebug("BagTM Engine Update Search H2H", this);

                String iFLTNR = null;
                System.Nullable<DateTime> iDATE = null;
                String iORIGIN = null;
                String iDEST = null;
                String fFLTNR = null;
                System.Nullable<DateTime> fDATE = null;
                String fORIGIN = null;
                String fDEST = null;

                iFLTNR = (flightInfoI != null) ?
                    CommonFunctions.FormatFlightNumber(flightInfoI.OPERATOR + flightInfoI.FLT_NR) : null;
                iDATE = (flightInfoI != null) ? flightInfoI.STD : null;
                iORIGIN = (flightInfoI != null) ? flightInfoI.FROM_IATA : null;
                iDEST = (flightInfoI != null) ? flightInfoI.TO_IATA : null;
                fFLTNR = (flightInfoF != null) ?
                    CommonFunctions.FormatFlightNumber(flightInfoF.OPERATOR + flightInfoF.FLT_NR) : null;
                fDATE = (flightInfoF != null) ? flightInfoF.STD : null;
                fORIGIN = (flightInfoF != null) ? flightInfoF.FROM_IATA : null;
                fDEST = (flightInfoF != null) ? flightInfoF.TO_IATA : null;

                OSUSR_UUK_H2H h2h = this.SearchH2HyByFlightPair(
                    iFLTNR, iDATE, iORIGIN, fFLTNR, fDATE, fORIGIN);

                BagTMLog.LogDebug("BagTM Engine Update H2H List", h2h);

                BaggageEntities dbinup = new BaggageEntities();

                var bagQuery = from s in dbinup.OSUSR_UUK_BAGINTEG
                               select s;

                var bagList = bagQuery.Where(s => s.IFLTNR == iFLTNR &&
                                        DbFunctions.TruncateTime(s.IDATE) == DbFunctions.TruncateTime(iDATE) &&
                                        s.IORIGIN == iORIGIN &&
                                        (s.IAUT != BagTMRulesActions.PAX_AUT_STATUS_CODE ||
                                         s.IAUT != BagTMRulesActions.BAG_GROUP_AUT_CODE))
                                     .ToList();

                BagTMLog.LogDebug("BagTM Engine Update Calculate BagIntegrity Bag Count", bagList);

                var bagCalculate = (from bi in bagList
                                        // here I choose each field I want to group by
                                    group bi by new { bi.IFLTNR, bi.IDATE, bi.IORIGIN, bi.FFLTNR, bi.FDATE, bi.FDEST } into g
                                    select new
                                    {
                                        IFLTNR = g.Key.IFLTNR,
                                        IDATE = g.Key.IDATE,
                                        IORIGIN = g.Key.IORIGIN,
                                        OFLTNR = g.Key.FFLTNR,
                                        ODATE = g.Key.FDATE,
                                        ODEST = g.Key.FDEST,
                                        Count = g.Count()
                                    }
                                ).ToList();

                BagTMLog.LogDebug("BagTM Engine Update H2H Bag Calculate", bagCalculate);

                if (h2h != null)
                {
                    BagTMLog.LogDebug("BagTM Engine Update H2H Flight Process", h2h);

                    h2h.IDATE = (flightInfoI != null) ? flightInfoI.STD : null;
                    h2h.IETA = (flightInfoI != null) ? flightInfoI.ETA : null;
                    h2h.IATA = (flightInfoI != null) ? flightInfoI.ATA : null;
                    h2h.ISTAND = (flightInfoI != null) ? flightInfoI.TO_STAND : null;
                    h2h.HULLOPEN = (flightInfoI != null) ? flightInfoI.DOOROPEN : null;
                    h2h.IFLTINFOID = (flightInfoI != null && flightInfoI.ID != 0) ? (System.Nullable<int>)flightInfoI.ID : null;
                    h2h.ODATE = (flightInfoF != null) ? flightInfoF.STD : null;
                    h2h.ETD = (flightInfoF != null) ? flightInfoF.ETD : null;
                    h2h.ATD = (flightInfoF != null) ? flightInfoF.ATD : null;
                    h2h.STANDD = (flightInfoF != null) ? flightInfoF.FROM_STAND : null;
                    h2h.HULLCLOSE = (flightInfoF != null) ? flightInfoF.DOORCLOSED : null;
                    h2h.OFLTINFOID = (flightInfoF != null && flightInfoF.ID != 0) ? (System.Nullable<int>)flightInfoF.ID : null;

                    var calcBaggageFlightIF = bagCalculate.Where(s => s.IFLTNR == h2h.IFLTNR &&
                                                        s.IDATE == ((h2h.IDATE != null) ? ((DateTime)h2h.IDATE).Date : h2h.IDATE) &&
                                                        s.IORIGIN == h2h.IORIGIN &&
                                                        s.OFLTNR == h2h.OFLTNR &&
                                                        s.ODATE == ((h2h.ODATE != null) ? ((DateTime)h2h.ODATE).Date : h2h.ODATE))
                                                .FirstOrDefault();

                    var calcBaggageFlightI = bagCalculate.Where(s => s.IFLTNR == h2h.IFLTNR &&
                                                                    s.IDATE == ((h2h.IDATE != null) ? ((DateTime)h2h.IDATE).Date : h2h.IDATE) &&
                                                                    s.IFLTNR != null)
                                                         .GroupBy(s => new { s.IFLTNR, s.IDATE })
                                                         .Select(r => new
                                                         {
                                                             FLIGHT = r.Key.IFLTNR,
                                                             DATE = r.Key.IDATE,
                                                             SUMBAGGAGES = r.Sum(x => x.Count)
                                                         }).FirstOrDefault();

                    BagTMLog.LogDebug("BagTM Engine Update H2H Exist", calcBaggageFlightIF);
                    if (calcBaggageFlightIF != null && calcBaggageFlightIF.Count > 0)
                    {
                        // To update inbound NRBAGS count for Hub messages
                        if (h2h.IFLTNR != null)
                        {
                            h2h.INRBAGS = calcBaggageFlightIF.Count;
                            h2h.OLNRBAGS = 0;
                        }
                        else
                        {
                            h2h.INRBAGS = 0;
                            h2h.OLNRBAGS = calcBaggageFlightIF.Count;
                        }
                        if (h2h.OFLTNR == null) h2h.OLNRBAGS = 0;


                        BagTMLog.LogInfo("BagTM Engine H2H Update: " + h2h.ID, this);
                    }
                    else
                    {
                        // To update inbound NRBAGS count for Hub messages
                        if (isHub)
                        {
                            h2h.INRBAGS = 0;
                        }
                        else
                        {
                            h2h.OLNRBAGS = 0;
                        }
                    }

                    System.Nullable<int> inboundFlightEquip = null;

                    if (flightInfoI != null && flightInfoI.AC_REGISTRATION != null)
                    {
                        OSUSR_UUK_REGISTRATIONS reg = registrationList.Where(x => x.ACREGISTRATION == flightInfoI.AC_REGISTRATION)
                                                                    .FirstOrDefault<OSUSR_UUK_REGISTRATIONS>();

                        inboundFlightEquip = (reg != null) ? reg.EQUIPTYPEID : (System.Nullable<int>)Convert.ToInt32(this.defaultEquipment);
                    }

                    System.Nullable<int> outboundFlightEquip = null;

                    if (flightInfoF != null && flightInfoF.AC_REGISTRATION != null)
                    {
                        OSUSR_UUK_REGISTRATIONS reg = registrationList.Where(x => x.ACREGISTRATION == flightInfoF.AC_REGISTRATION)
                                                                .FirstOrDefault<OSUSR_UUK_REGISTRATIONS>();

                        outboundFlightEquip = (reg != null) ? reg.EQUIPTYPEID : (System.Nullable<int>)Convert.ToInt32(this.defaultEquipment);
                    }

                    Decimal[] timesInbound = this.CalculateTimes(
                            (inboundFlightEquip != null) ? inboundFlightEquip : (System.Nullable<int>)Convert.ToInt32(this.defaultEquipment),
                            (flightInfoI != null) ? flightInfoI.TO_STAND : this.defaultStandFrom,
                            (flightInfoI != null) ? this.baggageTerminalCode : this.defaultGateTo);
                    Decimal[] timesOutbound = this.CalculateTimes(
                            (outboundFlightEquip != null) ? outboundFlightEquip : (System.Nullable<int>)Convert.ToInt32(this.defaultEquipment),
                            (flightInfoF != null) ? this.baggageTerminalCode : this.defaultStandFrom,
                            (flightInfoF != null) ? flightInfoF.FROM_GATE : this.defaultGateTo);

                    Double calcETU = 0;
                    Double calcETL = 0;

                    // Calculate times and insert if flight exists
                    if (h2h.IFLTNR != null)
                    {
                        h2h.IUNLOAD = (calcBaggageFlightI != null) ? timesInbound[0] * (Decimal)calcBaggageFlightI.SUMBAGGAGES
                                                                       : 0;
                        h2h.IINJECT = (calcBaggageFlightI != null) ? timesInbound[1] * (Decimal)calcBaggageFlightI.SUMBAGGAGES
                                                                       : 0;
                        h2h.ITAXI = (calcBaggageFlightI != null) ? timesInbound[3] : 0;

                        // Calculate ETU
                        calcETU = Convert.ToDouble(h2h.IUNLOAD + h2h.IINJECT + h2h.ITAXI);

                        calcETU = Math.Max(this.minLoadUnloadTime * 60 + Convert.ToDouble(h2h.IINJECT) + Convert.ToDouble(h2h.ITAXI), calcETU);
                        calcETU = Math.Min(this.maxLoadUnloadTime * 60 + Convert.ToDouble(h2h.IINJECT) + Convert.ToDouble(h2h.ITAXI), calcETU);

                        h2h.ETU = (h2h.HULLOPEN != null && h2h.HULLOPEN.Value.Year != 1900) ? h2h.HULLOPEN :
                                        (h2h.IATA != null && h2h.IATA.Value.Year != 1900) ? h2h.IATA :
                                            (h2h.IETA != null && h2h.IETA.Value.Year != 1900) ? h2h.IETA : null;

                        if (h2h.ETU != null) h2h.ETU = ((DateTime)h2h.ETU).AddSeconds(Math.Min(calcETU, (double)this.maxBaggageTurnaround * 60));

                    }

                    if (h2h.OFLTNR != null)
                    {

                        h2h.OLOAD = (calcBaggageFlightIF != null) ? timesOutbound[2] * (Decimal)calcBaggageFlightIF.Count
                                                                     : 0;
                        h2h.OINJECT = (calcBaggageFlightIF != null) ? timesOutbound[1] * (Decimal)calcBaggageFlightIF.Count
                                                                    : 0;
                        h2h.OTAXI = (calcBaggageFlightIF != null) ? timesOutbound[3] : 0;

                        // Calculate ETL
                        calcETL = Convert.ToDouble(h2h.OLOAD + h2h.OINJECT + h2h.OTAXI);

                        calcETL = Math.Max(this.minLoadUnloadTime * 60 + Convert.ToDouble(h2h.OINJECT) + Convert.ToDouble(h2h.OTAXI), calcETL);
                        calcETL = Math.Min(this.maxLoadUnloadTime * 60 + Convert.ToDouble(h2h.OINJECT) + Convert.ToDouble(h2h.OTAXI), calcETL);

                        h2h.ETL = (h2h.HULLOPEN != null && h2h.HULLOPEN.Value.Year != 1900) ? h2h.HULLOPEN :
                                        (h2h.IATA != null && h2h.IATA.Value.Year != 1900) ? h2h.IATA :
                                            (h2h.IETA != null && h2h.IETA.Value.Year != 1900) ? h2h.IETA : null;

                        if (h2h.ETL != null)
                        {
                            // Calc final ETL time
                            calcETL = calcETL + this.processSorterProcessingTimes((DateTime)h2h.ETL) * 60;
                            h2h.ETL = ((DateTime)h2h.ETL).AddSeconds(Math.Min(calcETU + calcETL, 2 * (double)this.maxBaggageTurnaround * 60));
                            h2h.ETLU = ((DateTime)h2h.ETL).AddSeconds(calcETU + calcETL);
                        }
                    }

                    try
                    {
                        // Calculate Hub time
                        h2h.HUB = (h2h.ETL != null && h2h.ETD != null &&
                                    ((DateTime)h2h.ETL).Year > 1900 && ((DateTime)h2h.ETD).Year > 1900) ?
                                        (System.Nullable<int>)((TimeSpan)(h2h.ETD - h2h.ETL)).TotalMinutes : null;

                        BagTMLog.LogDebug("BagTM Engine Update to ", h2h);

                        dbinup.OSUSR_UUK_H2H.Attach(h2h);
                        dbinup.Entry(h2h).State = EntityState.Modified;

                        BagTMLog.LogInfo("BagTM Engine H2H Update: " + h2h.ID, this);
                    }
                    catch (DbEntityValidationException ex)
                    {
                        dbinup.OSUSR_UUK_H2H.Remove(h2h);
                        // Retrieve the error messages as a list of strings.
                        var errorMessages = ex.EntityValidationErrors
                                .SelectMany(x => x.ValidationErrors)
                                .Select(x => x.ErrorMessage);

                        // Join the list to a single string.
                        var fullErrorMessage = string.Join("; ", errorMessages);

                        // Combine the original exception message with the new one.
                        var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                        // Throw a new DbEntityValidationException with the improved exception message.
                        throw new EngineProcessingException(exceptionMessage);
                    }
                    catch (Exception e)
                    {
                        dbinup.OSUSR_UUK_H2H.Remove(h2h);

                        throw e;
                    }
                }
                else
                {
                    OSUSR_UUK_H2H newH2H = new OSUSR_UUK_H2H();
                    newH2H.IFLTNR = iFLTNR;
                    newH2H.IORIGIN = iORIGIN;
                    newH2H.IDATE = iDATE;
                    newH2H.IETA = (flightInfoI != null) ? flightInfoI.ETA : null;
                    newH2H.IATA = (flightInfoI != null) ? flightInfoI.ATA : null;
                    newH2H.ISTAND = (flightInfoI != null) ? flightInfoI.TO_STAND : null;
                    newH2H.HULLOPEN = (flightInfoI != null) ? flightInfoI.DOOROPEN : null;
                    newH2H.IFLTINFOID = (flightInfoI != null && flightInfoI.ID != 0) ? (System.Nullable<int>)flightInfoI.ID : null;
                    newH2H.ONTOFLIGHT = fFLTNR;
                    newH2H.OFLTNR = fFLTNR;
                    newH2H.ODATE = fDATE;
                    newH2H.ETD = (flightInfoF != null) ? flightInfoF.ETD : null;
                    newH2H.ATD = (flightInfoF != null) ? flightInfoF.ATD : null;
                    newH2H.STANDD = (flightInfoF != null) ? flightInfoF.FROM_STAND : null;
                    newH2H.HULLCLOSE = (flightInfoF != null) ? flightInfoF.DOORCLOSED : null;
                    newH2H.OFLTINFOID = (flightInfoF != null && flightInfoF.ID != 0) ? (System.Nullable<int>)flightInfoF.ID : null;
                    // To update inbound NRBAGS count for Hub messages
                    if (newH2H.IFLTNR != null)
                    {
                        newH2H.INRBAGS = 1;
                        newH2H.OLNRBAGS = 0;
                    }
                    else
                    {
                        newH2H.INRBAGS = 0;
                        newH2H.OLNRBAGS = 1;
                    }
                    if (newH2H.OFLTNR == null) newH2H.OLNRBAGS = 0;

                    System.Nullable<int> inboundFlightEquip = null;

                    if (flightInfoI != null && flightInfoI.AC_REGISTRATION != null)
                    {
                        OSUSR_UUK_REGISTRATIONS reg = registrationList.Where(x => x.ACREGISTRATION == flightInfoI.AC_REGISTRATION)
                                                                    .FirstOrDefault<OSUSR_UUK_REGISTRATIONS>();

                        inboundFlightEquip = (reg != null) ? reg.EQUIPTYPEID : (System.Nullable<int>)Convert.ToInt32(this.defaultEquipment);
                    }

                    System.Nullable<int> outboundFlightEquip = null;

                    if (flightInfoF != null && flightInfoF.AC_REGISTRATION != null)
                    {
                        OSUSR_UUK_REGISTRATIONS reg = registrationList.Where(x => x.ACREGISTRATION == flightInfoI.AC_REGISTRATION)
                                                                    .FirstOrDefault<OSUSR_UUK_REGISTRATIONS>();

                        outboundFlightEquip = (reg != null) ? reg.EQUIPTYPEID : (System.Nullable<int>)Convert.ToInt32(this.defaultEquipment);
                    }
                    Decimal[] timesInbound = this.CalculateTimes(
                            (inboundFlightEquip != null) ? inboundFlightEquip : (System.Nullable<int>)Convert.ToInt32(this.defaultEquipment),
                            (flightInfoI != null) ? flightInfoI.TO_STAND : this.defaultStandFrom,
                            (flightInfoI != null) ? this.baggageTerminalCode : this.defaultGateTo);
                    Decimal[] timesOutbound = this.CalculateTimes(
                            (outboundFlightEquip != null) ? outboundFlightEquip : (System.Nullable<int>)Convert.ToInt32(this.defaultEquipment),
                            (flightInfoF != null) ? this.baggageTerminalCode : this.defaultStandFrom,
                            (flightInfoF != null) ? flightInfoF.FROM_GATE : this.defaultGateTo);

                    Double calcETU = 0;
                    Double calcETL = 0;

                    // Calculate times and insert if flight exists
                    if (newH2H.IFLTNR != null)
                    {
                        newH2H.IUNLOAD = timesInbound[0];
                        newH2H.IINJECT = timesInbound[1];
                        newH2H.ITAXI = timesInbound[3];

                        // Calculate ETU
                        calcETU = Convert.ToDouble(newH2H.IUNLOAD + newH2H.IINJECT + newH2H.ITAXI);

                        calcETU = Math.Max(this.minLoadUnloadTime * 60 + Convert.ToDouble(newH2H.IINJECT) + Convert.ToDouble(newH2H.ITAXI), calcETU);
                        calcETU = Math.Min(this.maxLoadUnloadTime * 60 + Convert.ToDouble(newH2H.IINJECT) + Convert.ToDouble(newH2H.ITAXI), calcETU);

                        newH2H.ETU = (newH2H.HULLOPEN != null && newH2H.HULLOPEN.Value.Year != 1900) ? newH2H.HULLOPEN :
                                        (newH2H.IATA != null && newH2H.IATA.Value.Year != 1900) ? newH2H.IATA :
                                            (newH2H.IETA != null && newH2H.IETA.Value.Year != 1900) ? newH2H.IETA : null;

                        if (newH2H.ETU != null) newH2H.ETU = ((DateTime)newH2H.ETU).AddSeconds(Math.Min(calcETU, (double)this.maxBaggageTurnaround * 60));

                    }

                    if (newH2H.OFLTNR != null)
                    {

                        newH2H.OLOAD = timesOutbound[2];
                        newH2H.OINJECT = timesOutbound[1];
                        newH2H.OTAXI = timesOutbound[3];

                        // Calculate ETL
                        calcETL = Convert.ToDouble(newH2H.OLOAD + newH2H.OINJECT + newH2H.OTAXI);


                        calcETL = Math.Max(this.minLoadUnloadTime * 60 + Convert.ToDouble(newH2H.OINJECT) + Convert.ToDouble(newH2H.OTAXI), calcETL);
                        calcETL = Math.Min(this.maxLoadUnloadTime * 60 + Convert.ToDouble(newH2H.OINJECT) + Convert.ToDouble(newH2H.OTAXI), calcETL);


                        newH2H.ETL = (newH2H.HULLOPEN != null && newH2H.HULLOPEN.Value.Year != 1900) ? newH2H.HULLOPEN :
                                        (newH2H.IATA != null && newH2H.IATA.Value.Year != 1900) ? newH2H.IATA :
                                            (newH2H.IETA != null && newH2H.IETA.Value.Year != 1900) ? newH2H.IETA : null;

                        if (newH2H.ETL != null)
                        {
                            // Calc final ETL time
                            calcETL = calcETL + this.processSorterProcessingTimes((DateTime)newH2H.ETL) * 60;
                            newH2H.ETL = ((DateTime)newH2H.ETL).AddSeconds(Math.Min(calcETU + calcETL, 2 * (double)this.maxBaggageTurnaround * 60));
                            newH2H.ETLU = ((DateTime)newH2H.ETL).AddSeconds(calcETU + calcETL);
                        }
                    }

                    try
                    {
                        // Calculate Hub time
                        newH2H.HUB = (newH2H.ETL != null && newH2H.ETD != null &&
                                        ((DateTime)newH2H.ETL).Year > 1900 && ((DateTime)newH2H.ETD).Year > 1900) ?
                                            (System.Nullable<int>)((TimeSpan)(newH2H.ETD - newH2H.ETL)).TotalMinutes : null;

                        OSUSR_UUK_H2H h2hExist = this.SearchH2HInformation(
                            newH2H.IFLTNR, newH2H.IDATE, newH2H.IORIGIN, newH2H.OFLTNR, newH2H.ODATE, "");

                        if (h2hExist != null && h2hExist.IFLTNR != null)
                        {
                            EngineUpdateH2H(flightInfoI, flightInfoF, isHub);
                            BagTMLog.LogDebug("BagTM Engine Update H2H Already Exist", h2hExist);
                            return;
                        }
                        dbinup.OSUSR_UUK_H2H.Add(newH2H);

                        BagTMLog.LogInfo("BagTM Engine H2H Create: " + newH2H.ID, this);
                        BagTMLog.LogDebug("BagTM Engine Update H2H Create", newH2H);
                    }
                    catch (DbEntityValidationException ex)
                    {
                        dbinup.OSUSR_UUK_H2H.Remove(newH2H);
                        // Retrieve the error messages as a list of strings.
                        var errorMessages = ex.EntityValidationErrors
                                .SelectMany(x => x.ValidationErrors)
                                .Select(x => x.ErrorMessage);

                        // Join the list to a single string.
                        var fullErrorMessage = string.Join("; ", errorMessages);

                        // Combine the original exception message with the new one.
                        var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                        // Throw a new DbEntityValidationException with the improved exception message.
                        throw new EngineProcessingException(exceptionMessage);
                    }
                    catch (Exception e)
                    {
                        dbinup.OSUSR_UUK_H2H.Remove(newH2H);

                        throw e;
                    }
                }


                dbinup.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new EngineProcessingException(exceptionMessage);
            }

            BagTMLog.LogDebug("BagTM Engine Update H2H End", this);
        }

        /// <summary>
        /// Calculate sorter time for bagagem based on current sorter load
        /// </summary>
        /// <param name="flightInfo"></param>
        /// <returns></returns>
        private double processSorterProcessingTimes(DateTime time)
        {
            return this.sorterVolumeMap.getSorterTime(this.sorterTime, this.maxSorterThroughPut, time);
        }

        /// <summary>
        /// Search H2H information to be updated
        /// </summary>
        /// <param name="airlineFlightNumber"></param>
        /// <param name="flightDate"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private OSUSR_UUK_H2H SearchH2HInformation(String iAirlineFlightNumber, Nullable<System.DateTime> iFlightDate, String iOrigin,
                                                         String fAirlineFlightNumber, Nullable<System.DateTime> fFlightDate, String fOrigin)
        {
            if (fAirlineFlightNumber == null && iAirlineFlightNumber == null) throw new EngineProcessingException("Flight number invalid search");

            if (fAirlineFlightNumber != null && fAirlineFlightNumber.Length != 6
                || iAirlineFlightNumber != null && iAirlineFlightNumber.Length != 6)
            {
                throw new EngineProcessingException("Flight number invalid");
            }

            if (fFlightDate == null && iFlightDate == null) throw new EngineProcessingException("Flight date invalid search");

            BaggageEntities db = new BaggageEntities();

            var h2hQuery = from s in db.OSUSR_UUK_H2H
                           select s;

            var result = h2hQuery.Where(s => s.IFLTNR == iAirlineFlightNumber &&
                                    DbFunctions.TruncateTime(s.IDATE) == DbFunctions.TruncateTime(iFlightDate) &&
                                    s.IORIGIN == iOrigin &&
                                    s.OFLTNR == fAirlineFlightNumber &&
                                    DbFunctions.TruncateTime(s.ODATE) == DbFunctions.TruncateTime(fFlightDate))
                                 .OrderByDescending(x => x.ID)
                                 .FirstOrDefault<OSUSR_UUK_H2H>();

            return result;
        }

        /// <summary>
        /// Search in the baggage integraty table the bag tag 
        /// </summary>
        /// <param name="bagTag"></param>
        /// <returns>OSUSR_UUK_BAGINTEG</returns>
        private OSUSR_UUK_H2H SearchH2HyByFlightPair(String iAirlineFlightNumber, Nullable<System.DateTime> iFlightDate, String iOrigin,
                                                         String fAirlineFlightNumber, Nullable<System.DateTime> fFlightDate, String fOrigin)
        {
            if (fAirlineFlightNumber == null && iAirlineFlightNumber == null) throw new EngineProcessingException("Flight number invalid search");

            if (fAirlineFlightNumber != null && fAirlineFlightNumber.Length != 6
                || iAirlineFlightNumber != null && iAirlineFlightNumber.Length != 6)
            {
                throw new EngineProcessingException("Flight number invalid");
            }

            if (fFlightDate == null && iFlightDate == null) throw new EngineProcessingException("Flight date invalid search");

            BaggageEntities db = new BaggageEntities();

            var h2hQuery = from s in db.OSUSR_UUK_H2H
                           select s;

            var results = h2hQuery.Where(s => s.IFLTNR == iAirlineFlightNumber &&
                                    DbFunctions.TruncateTime(s.IDATE) == DbFunctions.TruncateTime(iFlightDate) &&
                                    s.IORIGIN == iOrigin &&
                                    s.OFLTNR == fAirlineFlightNumber &&
                                    DbFunctions.TruncateTime(s.ODATE) == DbFunctions.TruncateTime(fFlightDate))
                                 .OrderByDescending(x => x.ID)
                                 .ToList<OSUSR_UUK_H2H>();

            OSUSR_UUK_H2H result = results.FirstOrDefault();

            if (results != null && results.Count > 1)
            {
                foreach (OSUSR_UUK_H2H h2h in results)
                {
                    if (result != h2h) db.OSUSR_UUK_H2H.Remove(h2h);
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    throw e;
                }

            }

            return result;
        }

        /// <summary>
        /// Calculate times for flight 
        /// </summary>
        /// <param name="equip"></param>
        /// <param name="taxiFrom"></param>
        /// <param name="taxiTo"></param>
        /// <returns></returns>
        private Decimal[] CalculateTimes(System.Nullable<int> equip, String taxiFrom, String taxiTo)
        {
            Decimal[] results = new Decimal[4];
            //Obtain reference times for flight or in limit to default -1
            OSUSR_UUK_BAGTIMESREFERENCE bagTimesRef =
                this.refList.Where<OSUSR_UUK_BAGTIMESREFERENCE>
                    (s => s.EQUIPTYPEID == equip).FirstOrDefault();

            if (bagTimesRef == null || bagTimesRef.EQUIPTYPEID == null)
                bagTimesRef = this.refList.Where<OSUSR_UUK_BAGTIMESREFERENCE>
                    (s => s.EQUIPTYPEID == Convert.ToInt32(this.defaultEquipment)).FirstOrDefault();

            //Obtain reference times for flight or in limit to default -1
            OSUSR_UUK_TAXITIMES taxiTimes =
                this.taxiTimesList.Where<OSUSR_UUK_TAXITIMES>
                    (s => s.FROM == taxiFrom &&
                          s.TO == taxiTo).FirstOrDefault();
            if (taxiTimes == null || taxiTimes.FROM == null)
                taxiTimes = this.taxiTimesList.Where<OSUSR_UUK_TAXITIMES>
                    (s => s.FROM == this.defaultStandFrom &&
                          s.TO == this.defaultGateTo).FirstOrDefault();

            bagTimesRef.UNLTIME = (bagTimesRef != null && bagTimesRef.UNLTIME != null) ? bagTimesRef.UNLTIME : 0;
            bagTimesRef.HULL_L = (bagTimesRef != null && bagTimesRef.HULL_L != null) ? bagTimesRef.HULL_L : 0;
            bagTimesRef.DOLLIE_L = (bagTimesRef != null && bagTimesRef.DOLLIE_L != null) ? bagTimesRef.DOLLIE_L : 0;
            bagTimesRef.INJTIME = (bagTimesRef != null && bagTimesRef.INJTIME != null) ? bagTimesRef.INJTIME : 0;
            taxiTimes.TAXITIME = (taxiTimes != null && taxiTimes.TAXITIME != null) ? taxiTimes.TAXITIME : 0;
            results[0] = (Decimal)(bagTimesRef.UNLTIME + bagTimesRef.DOLLIE_L);
            results[1] = (Decimal)(bagTimesRef.INJTIME);
            results[2] = (Decimal)(bagTimesRef.HULL_L + bagTimesRef.DOLLIE_L);
            results[3] = (Decimal)taxiTimes.TAXITIME;

            return results;
        }
    }
}
