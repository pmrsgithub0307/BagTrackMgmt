using BagTMCommon;
using BagTMDBLibrary;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMEngineProcessing
{
    class PTMH2HProcessing
    {
        /// <summary>
        /// Hub code for Baggage Integrity
        /// </summary>
        private String hub;

        /// <summary>
        /// Estimated Time to Close Gate
        /// </summary>
        private Int32 etcg;

        /// <summary>
        /// Default Max Passengers Turnaround
        /// </summary>
        private Int32 maxPaxTurnaround;

        /// <summary>
        /// Airline code for Baggage Integrity
        /// </summary>
        private String airline;

        private const String SEC_Y = "Y";


        public PTMH2HProcessing(String hub, Int32 etcg, Int32 maxPaxTurnaround, String airline)
        {
            this.hub = hub;
            this.etcg = etcg;
            this.maxPaxTurnaround = maxPaxTurnaround;
            this.airline = airline;
        }

        /// <summary>
        /// Process PTM logic 
        /// </summary>
        /// <param name="messageObject"></param>
        /// <param name="n"></param>
        public void ProcessPTMTTYEngine(Object messageObject, int n)
        {
            BagTMLog.LogDebug("BagTM Engine PTM TTY", this);

            // Only process message if is a baggage message
            if (messageObject.GetType() != typeof(OSUSR_UUK_PAXMSGS))
                throw new EngineProcessingException("Not a PTM message.");

            OSUSR_UUK_PAXMSGS ptm = (OSUSR_UUK_PAXMSGS)messageObject;

            BagTMLog.LogDebug("BagTM Engine Search FlightInfoI and FlightInfoF", this);
            OSUSR_UUK_FLT_INFO flightInfoI = null;
            // Obtain list of message from Baggage table
            if (ptm.IFLTNR != null && ptm.IDATE != null)
            {
                flightInfoI = this.SearchFlightInfo(ptm.IFLTNR, ptm.IDATE, ptm.IORIGIN, this.hub);
                if (flightInfoI != null && flightInfoI.FLT_NR != null) this.EngineUpdatePTMH2H(flightInfoI, (DateTime)ptm.TIMESTAMP);
                else throw new EngineProcessingException("PTM TTY messagen with unrecognized flight");
            }
            else
                throw new EngineProcessingException("PTM TTY messagen with unrecognized flight");

            BagTMLog.LogDebug("BagTM Engine Baggage PTM TTY End", this);
        }

        /// <summary>
        /// Update H2H table based on flight information
        /// </summary>
        public void EngineUpdatePTMH2H(OSUSR_UUK_FLT_INFO flightInfoI, DateTime stampPTM)
        {
            BagTMLog.LogDebug("BagTM Engine Update PTM H2H Start", this);
            int i = 0;

            BaggageEntities db = new BaggageEntities();

            try
            {
                BagTMLog.LogDebug("BagTM Engine Update PTM H2H FlightInfoI", flightInfoI);
                BagTMLog.LogDebug("BagTM Engine Update PTM H2H Hub", hub);

                BagTMLog.LogDebug("BagTM Engine Update Search PTM H2H", this);

                String iFLTNR = (flightInfoI != null) ?
                    CommonFunctions.FormatFlightNumber(flightInfoI.OPERATOR + flightInfoI.FLT_NR) : null;
                System.Nullable<DateTime> iDATE = (flightInfoI != null) ? flightInfoI.STD : null;
                String iORIGIN = (flightInfoI != null) ? flightInfoI.FROM_IATA : null;
                String iDEST = (flightInfoI != null) ? flightInfoI.TO_IATA : null;
                System.Nullable<DateTime> stamp = stampPTM;

                List<OSUSR_UUK_PTM_H2H> h2hList = this.SearchPTMH2HInformation(
                    iFLTNR, iDATE, iORIGIN);

                List<OSUSR_UUK_PTMREFS2G> refStandGateTimes = this.SearchRefStandGateTimes();
                List<OSUSR_UUK_PTM_REF_SEC_TIME> secTimes = this.SearchSecTimes();


                BagTMLog.LogDebug("BagTM Engine Update PTM H2H List", h2hList);


                var ptmQuery = from s in db.OSUSR_UUK_PAXMSGS
                               where s.TIMESTAMP == stamp
                               select s;

                var ptmList = ptmQuery.Where(s => s.IFLTNR == iFLTNR &&
                                        s.IDATE == ((DateTime)iDATE).Date &&
                                        s.IORIGIN == iORIGIN &&
                                        s.TIMESTAMP == stamp)
                                     .Select(x => new
                                     {
                                         x.IFLTNR,
                                         x.IDATE,
                                         x.IORIGIN,
                                         x.OFLTNR,
                                         x.ODATE,
                                         x.ODEST,
                                         x.RESERVATIONSTATUS
                                     })
                                     .Distinct()
                                     .ToList();

                BagTMLog.LogDebug("BagTM Engine Update Obtain PTM TTY list", ptmList);

                OSUSR_UUK_FLT_INFO flightInfoF;
                foreach (OSUSR_UUK_PTM_H2H h2h in h2hList)
                {
                    flightInfoF = null;

                    BagTMLog.LogDebug("BagTM Engine Update PTM H2H Flight Process", h2h);
                    i++;
                    var ptmListH2H = ptmList.Where(s => s.IFLTNR == h2h.IFLTNR &&
                                                        s.IDATE == h2h.IDATE &&
                                                        s.IORIGIN == h2h.IORIGIN &&
                                                        s.OFLTNR == h2h.OFLTNR &&
                                                        s.ODATE == h2h.ODATE &&
                                                        s.ODEST == h2h.ODEST &&
                                                        s.RESERVATIONSTATUS == h2h.STATUS)
                                            .ToList();

                    var h2hExists = ptmListH2H.FirstOrDefault();

                    BagTMLog.LogDebug("BagTM Engine Update Calculate PTMH2H PAX Count", ptmListH2H);

                    var PAXCalculate = ptmQuery.Where(s => s.IFLTNR == h2h.IFLTNR &&
                                                        s.IDATE == h2h.IDATE &&
                                                        s.IORIGIN == h2h.IORIGIN &&
                                                        s.OFLTNR == h2h.OFLTNR &&
                                                        s.ODATE == h2h.ODATE &&
                                                        s.ODEST == h2h.ODEST &&
                                                        s.RESERVATIONSTATUS == h2h.STATUS)
                                        .GroupBy(x => new
                                        {
                                            x.IFLTNR,
                                            x.IDATE,
                                            x.IORIGIN,
                                            x.OFLTNR,
                                            x.ODATE,
                                            x.ODEST,
                                            x.RESERVATIONSTATUS
                                        })
                                        .Select(x => new
                                        {
                                            IFLTNR = x.Key.IFLTNR,
                                            IDATE = x.Key.IDATE,
                                            IORIGIN = x.Key.IORIGIN,
                                            OFLTNR = x.Key.OFLTNR,
                                            ODATE = x.Key.ODATE,
                                            ODEST = x.Key.ODEST,
                                            RESERVATIONSTATUS = x.Key.RESERVATIONSTATUS,
                                            SumFIRST = x.Sum(z => z.FIRST),
                                            SumBUSINESS = x.Sum(z => z.BUSINESS),
                                            SumECONOMY = x.Sum(z => z.ECONOMY)
                                        }
                                    ).FirstOrDefault();

                    BagTMLog.LogDebug("BagTM Engine Update H2H Exist", PAXCalculate);

                    if (h2h.OFLTNR != null && h2h.ODATE != null)
                        flightInfoF = this.SearchFlightInfo(h2h.OFLTNR, h2h.ODATE, hub, h2h.ODEST);
                    else
                        flightInfoF = new OSUSR_UUK_FLT_INFO();
                    BagTMLog.LogDebug("BagTM Engine Search FlightInfoF", flightInfoF);

                    if (PAXCalculate != null && PAXCalculate.IFLTNR != null)
                    {
                        // To update inbound NRBAGS count for Hub messages
                        h2h.FIRST = PAXCalculate.SumFIRST;
                        h2h.BUSINESS = PAXCalculate.SumBUSINESS;
                        h2h.ECONOMY = PAXCalculate.SumECONOMY;

                        h2h.ETA = (flightInfoI != null && flightInfoI.DOOROPEN != null && flightInfoI.DOOROPEN.Value.Year != 1900) ? flightInfoI.DOOROPEN :
                                        (flightInfoI != null && flightInfoI.ATA != null && flightInfoI.ATA.Value.Year != 1900) ? flightInfoI.ATA :
                                            (flightInfoI != null && flightInfoI.ETA != null && flightInfoI.ETA.Value.Year != 1900) ? flightInfoI.ETA : null;
                        h2h.GATE = (flightInfoF != null && flightInfoF.FROM_GATE != null) ? flightInfoF.FROM_GATE : null;
                        h2h.ETD = (flightInfoF != null && flightInfoF.DOORCLOSED != null && flightInfoF.DOORCLOSED.Value.Year != 1900) ? flightInfoF.DOORCLOSED :
                                        (flightInfoF != null && flightInfoF.ATD != null && flightInfoF.ATD.Value.Year != 1900) ? flightInfoF.ATD :
                                            (flightInfoF != null && flightInfoF.ETD != null && flightInfoF.ETD.Value.Year != 1900) ? flightInfoF.ETD :
                                                (flightInfoF != null && flightInfoF.STD != null && flightInfoF.STD.Value.Year != 1900) ? flightInfoF.STD : null;
                        h2h.STAND = (flightInfoI != null && flightInfoI.TO_STAND != null) ? flightInfoI.TO_STAND : null;
                        h2h.STATUS = PAXCalculate.RESERVATIONSTATUS;
                        h2h.IFLTINFOID = (flightInfoI != null) ? (System.Nullable<int>)flightInfoI.ID : null;
                        h2h.OFLTINFOID = (flightInfoF != null) ? (System.Nullable<int>)flightInfoF.ID : null;

                        System.Nullable<DateTime> etg = null;
                        if (flightInfoI != null)
                        {
                            if (flightInfoI.DOOROPEN != null) etg = flightInfoI.DOOROPEN;
                            else if (flightInfoI.ATA != null) etg = flightInfoI.ATA;
                            else if (flightInfoI.ETA != null) etg = flightInfoI.ETA;
                        }

                        // Obtain security times from table
                        OSUSR_UUK_PTMREFS2G refStandGateTime = refStandGateTimes.Where(
                                                                       s => s.GATE == h2h.GATE &&
                                                                            s.STAND == h2h.STAND).FirstOrDefault();

                        int standGateMinutes = (refStandGateTime != null && refStandGateTime.MINUTES != null) ? (int)refStandGateTime.MINUTES / 2 : 0;

                        if (refStandGateTime != null && SEC_Y.Equals(refStandGateTime.SEC))
                        {
                            OSUSR_UUK_PTM_REF_SEC_TIME secTime = secTimes.Where(
                                                                       s => s.WEEK == ((DateTime)h2h.IDATE).DayOfWeek.ToString() &&
                                                                            s.FROM_TIME > ((DateTime)etg).AddMinutes(standGateMinutes))
                                                                 .FirstOrDefault();
                            h2h.SEC = (secTime != null && secTime.TIME != null) ? standGateMinutes + (int)secTime.TIME : standGateMinutes;
                        }
                        else
                        {
                            h2h.SEC = standGateMinutes;
                        }
                        if (h2h.ETD != null)
                        {
                            h2h.ETCG = h2h.ETD;
                            h2h.ETCG.Value.AddMinutes(-1 * etcg);
                        }
                        h2h.ETG = h2h.ETA;
                        h2h.ETG = (h2h.ETG != null) ? (System.Nullable<DateTime>)((DateTime)h2h.ETG).AddMinutes(
                            Math.Min((h2h.SEC != null) ? (int)h2h.SEC : 0, this.maxPaxTurnaround)) : null;
                        h2h.HUB = (h2h.ETD != null && h2h.ETG != null) ? (System.Nullable<int>)((TimeSpan)(h2h.ETD - h2h.ETG)).TotalMinutes : null;

                        BagTMLog.LogInfo("BagTM Engine H2H Update: " + h2h.ID, this);
                    }
                    else
                    {
                        // To update inbound NRBAGS count for Hub messages
                        h2h.FIRST = 0;
                        h2h.BUSINESS = 0;
                        h2h.ECONOMY = 0;

                        h2h.ETA = (flightInfoI != null && flightInfoI.ETA != null) ? flightInfoI.ETA : null;
                        h2h.GATE = (flightInfoF != null && flightInfoF.FROM_GATE != null) ? flightInfoF.FROM_GATE : null;
                        h2h.ETD = (flightInfoF != null && flightInfoF.ETD != null) ? flightInfoF.ETD : null;
                        h2h.STAND = (flightInfoI != null && flightInfoI.TO_STAND != null) ? flightInfoI.TO_STAND : null;
                        h2h.STATUS = null;

                        h2h.SEC = 0;
                        h2h.ETG = null;
                        h2h.ETCG = null;
                        h2h.HUB = null;

                    }

                    db.OSUSR_UUK_PTM_H2H.Attach(h2h);
                    db.Entry(h2h).State = EntityState.Modified;
                    BagTMLog.LogInfo("BagTM Engine PTM H2H Update: " + h2h.ID, this);

                    ptmList.Remove(h2hExists);
                }

                OSUSR_UUK_PTM_H2H newH2H = null;
                foreach (var ptmNotIn in ptmList)
                {
                    flightInfoF = null;

                    // Verify if passengers in connection with a FI flight
                    if (ptmNotIn.OFLTNR != null && !ptmNotIn.OFLTNR.StartsWith(this.airline)) continue;

                    newH2H = new OSUSR_UUK_PTM_H2H();

                    var PAXCalculate = ptmQuery.Where(s => s.IFLTNR == ptmNotIn.IFLTNR &&
                                                        s.IDATE == ptmNotIn.IDATE &&
                                                        s.IORIGIN == ptmNotIn.IORIGIN &&
                                                        s.OFLTNR == ptmNotIn.OFLTNR &&
                                                        s.ODATE == ptmNotIn.ODATE &&
                                                        s.ODEST == ptmNotIn.ODEST &&
                                                        s.RESERVATIONSTATUS == ptmNotIn.RESERVATIONSTATUS)
                                        .GroupBy(x => new
                                        {
                                            x.IFLTNR,
                                            x.IDATE,
                                            x.IORIGIN,
                                            x.OFLTNR,
                                            x.ODATE,
                                            x.ODEST,
                                            x.RESERVATIONSTATUS
                                        })
                                        .Select(x => new
                                        {
                                            IFLTNR = x.Key.IFLTNR,
                                            IDATE = x.Key.IDATE,
                                            IORIGIN = x.Key.IORIGIN,
                                            OFLTNR = x.Key.OFLTNR,
                                            ODATE = x.Key.ODATE,
                                            ODEST = x.Key.ODEST,
                                            RESERVATIONSTATUS = x.Key.RESERVATIONSTATUS,
                                            SumFIRST = x.Sum(z => z.FIRST),
                                            SumBUSINESS = x.Sum(z => z.BUSINESS),
                                            SumECONOMY = x.Sum(z => z.ECONOMY)
                                        }
                                    ).FirstOrDefault();

                    BagTMLog.LogDebug("BagTM Engine Update H2H Don't Exist", PAXCalculate);

                    if (PAXCalculate != null && PAXCalculate.IFLTNR != null)
                    {
                        // To update inbound NRBAGS count for Hub messages
                        newH2H.FIRST = PAXCalculate.SumFIRST;
                        newH2H.BUSINESS = PAXCalculate.SumBUSINESS;
                        newH2H.ECONOMY = PAXCalculate.SumECONOMY;
                    }
                    else
                    {
                        newH2H.FIRST = 0;
                        newH2H.BUSINESS = 0;
                        newH2H.ECONOMY = 0;
                    }

                    if (ptmNotIn.OFLTNR != null && ptmNotIn.ODATE != null)
                        flightInfoF = this.SearchFlightInfo(ptmNotIn.OFLTNR, ptmNotIn.ODATE, hub, ptmNotIn.ODEST);
                    else
                        flightInfoF = new OSUSR_UUK_FLT_INFO();
                    BagTMLog.LogDebug("BagTM Engine Search FlightInfoF", flightInfoF);

                    newH2H.IFLTNR = ptmNotIn.IFLTNR;
                    newH2H.IDATE = ptmNotIn.IDATE;
                    newH2H.IORIGIN = ptmNotIn.IORIGIN;
                    newH2H.OFLTNR = ptmNotIn.OFLTNR;
                    newH2H.ODATE = ptmNotIn.ODATE;
                    newH2H.ODEST = ptmNotIn.ODEST;

                    newH2H.ETA = (flightInfoI != null && flightInfoI.DOOROPEN != null && flightInfoI.DOOROPEN.Value.Year != 1900) ? flightInfoI.DOOROPEN :
                                    (flightInfoI != null && flightInfoI.ATA != null && flightInfoI.ATA.Value.Year != 1900) ? flightInfoI.ATA :
                                        (flightInfoI != null && flightInfoI.ETA != null && flightInfoI.ETA.Value.Year != 1900) ? flightInfoI.ETA : null;
                    newH2H.GATE = (flightInfoF != null && flightInfoF.FROM_GATE != null) ? flightInfoF.FROM_GATE : null;
                    newH2H.ETD = (flightInfoF != null && flightInfoF.DOORCLOSED != null && flightInfoF.DOORCLOSED.Value.Year != 1900) ? flightInfoF.DOORCLOSED :
                                    (flightInfoF != null && flightInfoF.ATD != null && flightInfoF.ATD.Value.Year != 1900) ? flightInfoF.ATD :
                                        (flightInfoF != null && flightInfoF.ETD != null && flightInfoF.ETD.Value.Year != 1900) ? flightInfoF.ETD :
                                            (flightInfoF != null && flightInfoF.STD != null && flightInfoF.STD.Value.Year != 1900) ? flightInfoF.STD : null;
                    newH2H.STAND = (flightInfoI != null && flightInfoI.TO_STAND != null) ? flightInfoI.TO_STAND : null;
                    newH2H.STATUS = ptmNotIn.RESERVATIONSTATUS;
                    newH2H.IFLTINFOID = (flightInfoI != null) ? (System.Nullable<int>)flightInfoI.ID : null;
                    newH2H.OFLTINFOID = (flightInfoF != null) ? (System.Nullable<int>)flightInfoF.ID : null;

                    System.Nullable<DateTime> etcg = null;
                    if (flightInfoI != null)
                    {
                        if (flightInfoI.DOOROPEN != null && flightInfoI.DOOROPEN.Value.Year != 1900) etcg = flightInfoI.DOOROPEN;
                        else if (flightInfoI.ATA != null && flightInfoI.ATA.Value.Year != 1900) etcg = flightInfoI.ATA;
                        else if (flightInfoI.ETA != null && flightInfoI.ETA.Value.Year != 1900) etcg = flightInfoI.ETA;
                    }

                    // Obtain security times from table
                    OSUSR_UUK_PTMREFS2G refStandGateTime = refStandGateTimes.Where(
                                                                   s => s.GATE == newH2H.GATE &&
                                                                        s.STAND == newH2H.STAND).FirstOrDefault();

                    int standGateMinutes = (refStandGateTime != null && refStandGateTime.MINUTES != null) ? (int)refStandGateTime.MINUTES / 2 : 0;

                    if (refStandGateTime != null && SEC_Y.Equals(refStandGateTime.SEC))
                    {
                        OSUSR_UUK_PTM_REF_SEC_TIME secTime = secTimes.Where(
                                                                   s => s.WEEK == ((DateTime)newH2H.IDATE).DayOfWeek.ToString() &&
                                                                        s.FROM_TIME > ((DateTime)flightInfoF.ETA).AddMinutes(standGateMinutes)).FirstOrDefault();
                        newH2H.SEC = (secTime != null && secTime.TIME != null) ? standGateMinutes + (int)secTime.TIME : standGateMinutes;
                    }
                    else
                    {
                        newH2H.SEC = standGateMinutes;
                    }
                    newH2H.ETG = newH2H.ETA;
                    newH2H.ETG = (newH2H.ETG != null) ? (System.Nullable<DateTime>)((DateTime)newH2H.ETG).AddMinutes(
                        Math.Min((newH2H.SEC != null) ? (int)newH2H.SEC : 0, this.maxPaxTurnaround)) : null;
                    newH2H.HUB = (newH2H.ETD != null && newH2H.ETG != null) ? (System.Nullable<int>)((TimeSpan)(newH2H.ETD - newH2H.ETG)).TotalMinutes : null;
                    
                    try
                    {
                        db.OSUSR_UUK_PTM_H2H.Add(newH2H);
                        BagTMLog.LogInfo("BagTM Engine PTM H2H Create: " + newH2H.ID, this);
                    }
                    catch (DbEntityValidationException ex)
                    {
                        db.OSUSR_UUK_PTM_H2H.Remove(newH2H);
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
                }

                try
                {
                    db.SaveChanges();

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
            }
            catch (Exception e)
            {
                BagTMLog.LogError("BagTM Engine Error updating PTM H2H Table", this, e);

                throw e;
            }

            BagTMLog.LogDebug("BagTM Engine Update PTM H2H End", this);
        }

        /// <summary>
        /// Search H2H information to be updated
        /// </summary>
        /// <param name="airlineFlightNumber"></param>
        /// <param name="flightDate"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private List<OSUSR_UUK_PTM_H2H> SearchPTMH2HInformation(String iAirlineFlightNumber, Nullable<System.DateTime> iFlightDate, String iOrigin)
        {
            BaggageEntities db = new BaggageEntities();

            var h2hQuery = from s in db.OSUSR_UUK_PTM_H2H
                           select s;

            var result = h2hQuery.Where(s => s.IFLTNR == iAirlineFlightNumber &&
                                    DbFunctions.TruncateTime(s.IDATE) == DbFunctions.TruncateTime(iFlightDate) &&
                                    s.IORIGIN == iOrigin).ToList<OSUSR_UUK_PTM_H2H>();

            return result;
        }

        /// <summary>
        /// Search for last PTM date for the flight
        /// </summary>
        /// <param name="ptm"></param>
        /// <returns>System.Nullable<DateTime></returns>
        public System.Nullable<DateTime> SearchLastPTMForFlight(OSUSR_UUK_PAXMSGS ptm)
        {
            return this.SearchLastPTMForFlight(ptm.IFLTNR, ptm.IDATE, ptm.IORIGIN, ptm.OFLTNR, ptm.ODATE, ptm.ODEST);
        }
        /// <summary>
        /// Search for last PTM date for the flight
        /// </summary>
        /// <param name="ptm"></param>
        /// <returns>System.Nullable<DateTime></returns>
        public System.Nullable<DateTime> SearchLastPTMForFlight(OSUSR_UUK_PTM_H2H ptm)
        {
            return this.SearchLastPTMForFlight(ptm.IFLTNR, ptm.IDATE, ptm.IORIGIN, ptm.OFLTNR, ptm.ODATE, ptm.ODEST);
        }

        /// <summary>
        /// Search for last PTM date for the flight
        /// </summary>
        /// <param name="ptm"></param>
        /// <returns>System.Nullable<DateTime></returns>
        public System.Nullable<DateTime> SearchLastPTMForFlight(String iFLTNR, System.Nullable<DateTime> iDATE, String iORIGIN,
                                                String oFLTNR, System.Nullable<DateTime> oDATE, String oDEST)
        {
            BaggageEntities db = new BaggageEntities();

            var maxPTMDate = (from s in db.OSUSR_UUK_PAXMSGS
                              where s.IFLTNR == iFLTNR &&
                                    s.IDATE == iDATE &&
                                    s.IORIGIN == iORIGIN &&
                                    s.OFLTNR == oFLTNR &&
                                    s.ODATE == oDATE &&
                                    s.ODEST == oDEST
                              select s.TIMESTAMP).Max();

            return maxPTMDate;
        }

        /// <summary>
        /// Search in the baggage TTY messages table the bag tag 
        /// </summary>
        /// <param name="bagTag"></param>
        /// <returns>OSUSR_UUK_BSM[]</returns>
        private OSUSR_UUK_FLT_INFO SearchFlightInfo(String airlineFlightNumber, Nullable<System.DateTime> flightDate, String flightOrigin, String flightDestination)
        {
            String airline = null;
            Int32 flightNumber;

            BaggageEntities db = new BaggageEntities();

            airlineFlightNumber = CommonFunctions.FormatFlightNumber(airlineFlightNumber);

            airline = airlineFlightNumber.Substring(0, 2);
            flightNumber = Convert.ToInt32(airlineFlightNumber.Substring(2, 4));

            if (flightDate == null) throw new EngineProcessingException("Flight date invalid");


            var bagQuery = from s in db.OSUSR_UUK_FLT_INFO
                           select s;

            var result = bagQuery.Where(s => s.OPERATOR == airline &&
                                    s.FLT_NR == flightNumber &&
                                    s.FROM_IATA == flightOrigin &&
                                    s.TO_IATA == flightDestination &&
                                    DbFunctions.TruncateTime(s.STD) == flightDate).FirstOrDefault<OSUSR_UUK_FLT_INFO>();

            return result;
        }

        /// <summary>
        /// Search reference times per stand and gate
        /// </summary>
        /// <returns></returns>
        private List<OSUSR_UUK_PTMREFS2G> SearchRefStandGateTimes()
        {
            BaggageEntities db = new BaggageEntities();

            var refSTandGateTimes = from s in db.OSUSR_UUK_PTMREFS2G
                                    select s;

            var results = refSTandGateTimes.ToList<OSUSR_UUK_PTMREFS2G>();

            return results;
        }

        /// <summary>
        /// Search security times
        /// </summary>
        /// <returns></returns>
        private List<OSUSR_UUK_PTM_REF_SEC_TIME> SearchSecTimes()
        {
            BaggageEntities db = new BaggageEntities();

            var secTimes = from s in db.OSUSR_UUK_PTM_REF_SEC_TIME
                           select s;

            var results = secTimes.ToList<OSUSR_UUK_PTM_REF_SEC_TIME>();

            return results;
        }

        /// <summary>
        /// Calculate the correct message to reinsert so the PTMH2H table is updated
        /// </summary>
        /// <param name="ptm"></param>
        /// <returns></returns>
        public void CalculateErrorReprocessing(OSUSR_UUK_PAXMSGS ptm, int n)
        {
            System.Nullable<DateTime> lastPTMDateTime = this.SearchLastPTMForFlight(ptm);

            if (lastPTMDateTime != null && ptm.TIMESTAMP != null &&
                ((DateTime)ptm.TIMESTAMP).CompareTo((DateTime)lastPTMDateTime) > 0)
            {
                OSUSR_UUK_FLT_INFO flightInfoI = null;
                // Obtain list of message from Baggage table
                if (ptm.IFLTNR != null && ptm.IDATE != null)
                {
                    flightInfoI = this.SearchFlightInfo(ptm.IFLTNR, ptm.IDATE, ptm.IORIGIN, this.hub);
                    if (flightInfoI != null && flightInfoI.FLT_NR != null) this.EngineUpdatePTMH2H(flightInfoI, (DateTime)ptm.TIMESTAMP);
                    else throw new EngineProcessingException("PTM TTY messagen with unrecognized flight");
                }
                else
                    throw new EngineProcessingException("PTM TTY messagen with unrecognized flight");
            }
        }
    }
}
