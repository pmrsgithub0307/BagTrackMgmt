using System;
using BagTMDBLibrary;
using System.Linq;
using NRules;
using BagTMCommon;
using BagTMEngineProcessing.Rules;
using System.Collections.Generic;
using System.Data.Entity;

namespace BagTMEngineProcessing
{
    class BagIntegrityProcessing
    {
        /// <summary>
        /// Hub code for Baggage Integrity
        /// </summary>
        private String hub;

        /// <summary>
        /// Airline code for Baggage Integrity
        /// </summary>
        private String airline;

        /// <summary>
        /// NRules session factory
        /// </summary>
        ISessionFactory factory;

        /// <summary>
        /// H2H processing
        /// </summary>
        private H2HProcessing h2hProcessing;

        public BagIntegrityProcessing(String hub, String airline, ISessionFactory factory, H2HProcessing h2hProcessing)
        {
            this.hub = hub;
            this.airline = airline;
            this.factory = factory;
            this.h2hProcessing = h2hProcessing;
        }

        /// <summary>
        /// Process engine rules 
        /// </summary>
        /// <param name="messageObject"></param>
        /// <param name="n"></param>
        public void ProcessBaggageTTYEngine(Object messageObject, int n)
        {
            BagTMLog.LogDebug("BagTM Engine Baggage TTY", this);

            // Only process message if is a baggage message
            if (messageObject.GetType() != typeof(OSUSR_UUK_BAGMSGS))
                throw new EngineProcessingException("Not a baggage message.");

            OSUSR_UUK_BAGMSGS bag = (OSUSR_UUK_BAGMSGS)messageObject;

            int nrtag = 0;
            try
            {
                nrtag = Convert.ToInt32(bag.NNRTAGS);
            }
            catch (Exception e)
            {
                BagTMLog.LogError("Bag NNRTAG is not an number.", bag, e);
            }
            if (nrtag > 1)
            {
                for (int i = 0; i < nrtag; i++)
                {
                    var inst = bag.GetType().GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    OSUSR_UUK_BAGMSGS bagClone = (OSUSR_UUK_BAGMSGS)inst.Invoke(bag, null);

                    bagClone.NNRTAGS = "001";
                    bagClone.NBAGTAG = (Convert.ToDecimal(bagClone.NBAGTAG) + i).ToString("0000000000");
                    ProcessBaggageTTYEngine(bagClone, n);
                }
            }

            BagTMLog.LogDebug("BagTM Engine Search BaggageIntegraty", bag);
            // Obtain baggage from Baggage Integraty table 
            OSUSR_UUK_BAGINTEG bagIntegraty = this.SearchBaggageIntegratyByBagIdentifier(bag.NBAGTAG + bag.NNRTAGS);
            if (bagIntegraty == null)
                bagIntegraty = new OSUSR_UUK_BAGINTEG();
            BagTMLog.LogDebug("BagTM Engine Search BaggageIntegraty Found ", bagIntegraty);
            BagTMLog.LogDebug("BagTM Engine Search FlightInfoI and FlightInfoF", this);
            OSUSR_UUK_FLT_INFO flightInfoI = null;
            OSUSR_UUK_FLT_INFO flightInfoF = null;

            bool isHub = false;
            bool isLocalTTYforHub = false;
            bool isLocalEndingInHub = false;

            if ((BagTMRulesActions.hubBagParameter.Equals(bag.VBAGSOURCIND) &&
                this.hub.Equals(bag.VCITY)) ||
                (BagTMRulesActions.localBagParameter.Equals(bag.VBAGSOURCIND) &&
                this.hub.Equals(bag.FDEST))) isHub = true;
            if (isHub && BagTMRulesActions.localBagParameter.Equals(bag.VBAGSOURCIND)) isLocalTTYforHub = true;
            if (isHub && isLocalTTYforHub && (bag.OFLTNR == null || !bag.OFLTNR.StartsWith(this.airline))) isLocalEndingInHub = true;
            if (isHub && !isLocalTTYforHub && bag.IFLTNR != null) isLocalEndingInHub = true;

            // Obtain list of message from Baggage table
            if (isHub)
            {
                if (!isLocalEndingInHub)
                {
                    if (bag.FFLTNR != null && bag.FDATE != null && bag.FFLTNR.StartsWith(this.airline))
                        flightInfoI = this.SearchFlightInfo(bag.FFLTNR, bag.FDATE, bag.VCITY, bag.FDEST);
                    else
                        flightInfoI = new OSUSR_UUK_FLT_INFO();
                    if (bag.OFLTNR != null && bag.ODATE != null && bag.OFLTNR.StartsWith(this.airline))
                        flightInfoF = this.SearchFlightInfo(bag.OFLTNR, bag.ODATE, hub, bag.ODEST);
                    else
                        flightInfoF = new OSUSR_UUK_FLT_INFO();
                }
                else
                {
                    if (bag.IFLTNR != null && bag.IDATE != null && bag.IFLTNR.StartsWith(this.airline))
                        flightInfoI = this.SearchFlightInfo(bag.IFLTNR, bag.IDATE, bag.IORIGIN, bag.VCITY);
                    else
                        flightInfoI = new OSUSR_UUK_FLT_INFO();
                    if (bag.FFLTNR != null && bag.FDATE != null && bag.FFLTNR.StartsWith(this.airline))
                        flightInfoF = this.SearchFlightInfo(bag.FFLTNR, bag.FDATE, bag.VCITY, bag.FDEST);
                    else
                        flightInfoF = new OSUSR_UUK_FLT_INFO();
                }
            }
            else
            {
                if (bag.FFLTNR != null && bag.FDATE != null && bag.FFLTNR.StartsWith(this.airline))
                    flightInfoF = this.SearchFlightInfo(bag.FFLTNR, bag.FDATE, bag.VCITY, bag.FDEST);
                else
                    flightInfoF = new OSUSR_UUK_FLT_INFO();

                flightInfoI = new OSUSR_UUK_FLT_INFO();
            }

            BagTMLog.LogDebug("BagTM Engine Search FlightInfoI", flightInfoI);
            BagTMLog.LogDebug("BagTM Engine Search FlightInfoF", flightInfoF);
            BagTMLog.LogDebug("BagTM Engine Rules Execution", this);
            // Run engine process rules
            this.EngineRulesExecution(bag, bagIntegraty, flightInfoI, flightInfoF, hub, factory);

            // Verify if is a baggage board
            if (BagTMRulesActions.PAX_BOARDED_STATUS.Equals(bag.SPAXCK))
            {
                BagTMLog.LogDebug("BagTM Engine Boaded Pax", this);

                this.ProcessBoardBaggageTTY(bag, bagIntegraty, flightInfoI, flightInfoF, hub, factory);
            }

            if (flightInfoF == null || flightInfoF.FLT_NR == null || flightInfoI == null)
                BagTMLog.LogDebug("No update to H2H since no flight information, please check flight in FLT_INFO table.", this);
            else
            {
                // Reprocess Hull 2 Hull table for selected flight
                if (isHub || (hub.Equals(bag.VCITY) && BagTMRulesActions.localBagParameter.Equals(bag.VBAGSOURCIND)))
                    this.h2hProcessing.EngineUpdateH2H(flightInfoI, flightInfoF, isHub);
            }
            BagTMLog.LogDebug("BagTM Engine Baggage TTY End", this);
        }

        /// <summary>
        /// Load and execute engine rules
        /// </summary>
        public void ProcessBoardBaggageTTY(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegraty, OSUSR_UUK_FLT_INFO flightInfoI, OSUSR_UUK_FLT_INFO flightInfoF, String hub, ISessionFactory factory)
        {
            BagTMLog.LogDebug("BagTM Engine Process Board Baggage Start", this);
            
            try
            {
                List<OSUSR_UUK_BAGMSGS> listOfBagBoarded = this.SearchBaggageMsgsByFlightandSeatNumber(bag);

                foreach (OSUSR_UUK_BAGMSGS boardBag in listOfBagBoarded)
                {
                    OSUSR_UUK_BAGINTEG boardBagInteg = this.SearchBaggageIntegratyByBagIdentifier(boardBag.NBAGTAG + boardBag.NNRTAGS);

                    if (boardBagInteg == null) boardBagInteg = new OSUSR_UUK_BAGINTEG();

                    var inst = bag.GetType().GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    OSUSR_UUK_BAGMSGS bagClone = (OSUSR_UUK_BAGMSGS)inst.Invoke(bag, null);

                    bagClone.NNRTAGS = boardBag.NNRTAGS;
                    bagClone.NBAGTAG = boardBag.NBAGTAG;

                    BagTMLog.LogDebug("BagTM Board Baggage Engine Rules Execution", this);
                    // Run engine process rules
                    this.EngineRulesExecution(bagClone, boardBagInteg, flightInfoI, flightInfoF, hub, factory);
                }
            }
            catch (Exception e)
            {
                BagTMLog.LogError("BagTM Engine Error Process Board Baggage", this, e);

                throw e;
            }

            BagTMLog.LogDebug("BagTM Engine Process Board Baggage Ending", this);
        }

        /// <summary>
        /// Load and execute engine rules
        /// </summary>
        public void EngineRulesExecution(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegraty, OSUSR_UUK_FLT_INFO flightInfoI, OSUSR_UUK_FLT_INFO flightInfoF, String hub, ISessionFactory factory)
        {
            BagTMLog.LogDebug("BagTM Engine Rules Execution Start", this);

            ISession session = factory.CreateSession();

            try
            {
                bagIntegraty.IATD = (flightInfoI != null && flightInfoI.ATD != null) ? flightInfoI.ATD :
                        (flightInfoI != null && flightInfoI.ETD != null) ? flightInfoI.ETD :
                        (flightInfoI != null && flightInfoI.STD != null) ? flightInfoI.STD : null;
                bagIntegraty.FATD = (flightInfoF != null && flightInfoF.ATD != null) ? flightInfoF.ATD :
                        (flightInfoF != null && flightInfoF.ETD != null) ? flightInfoF.ETD :
                        (flightInfoF != null && flightInfoF.STD != null) ? flightInfoF.STD : null;

                session.Insert(hub);

                //Insert facts into rules engine's memory
                session.Insert(bag);
                session.Insert(bagIntegraty);

                //Start match/resolve/act cycle
                session.Fire();

            }
            catch (Exception e)
            {
                BagTMLog.LogError("BagTM Engine Error Processing Rules", this, e);

                throw e;
            }

            BagTMLog.LogDebug("BagTM Engine Rules Execution Ending", this);
        }

        /// <summary>
        /// Search in the baggage integraty table the bag tag 
        /// </summary>
        /// <param name="bagTag"></param>
        /// <returns>OSUSR_UUK_BAGINTEG</returns>
        public OSUSR_UUK_BAGINTEG SearchBaggageIntegratyByBagIdentifier(String bagTag)
        {
            BaggageEntities db = new BaggageEntities();

            var bagQuery = from s in db.OSUSR_UUK_BAGINTEG
                           where s.NUMBER == bagTag
                           select s;

            var results = bagQuery.OrderByDescending(s => s.TIMESTAMP)
                .ToList<OSUSR_UUK_BAGINTEG>();

            OSUSR_UUK_BAGINTEG result = results.FirstOrDefault();

            return result;
        }

        /// <summary>
        /// Search in the baggage integraty table the bag tag with timestamp higher that specified 
        /// </summary>
        /// <param name="bagTag"></param>
        /// <returns>List<OSUSR_UUK_BAGMSGS></returns>
        private List<OSUSR_UUK_BAGMSGS> SearchBaggageMsgsByBagTagandTimestamp(OSUSR_UUK_BAGMSGS bagTag)
        {
            BaggageEntities db = new BaggageEntities();

            var bagQuery = from s in db.OSUSR_UUK_BAGMSGS
                           select s;

            var results = bagQuery.Where(s => s.NBAGTAG == bagTag.NBAGTAG &&
                                    s.TIMESTAMP > bagTag.TIMESTAMP)
                .OrderBy(s => s.TIMESTAMP)
                .ToList<OSUSR_UUK_BAGMSGS>();

            return results;
        }

        /// <summary>
        /// Search in the baggage integraty table the bag tag with timestamp higher that specified 
        /// </summary>
        /// <param name="bagTag"></param>
        /// <returns>List<OSUSR_UUK_BAGMSGS></returns>
        private List<OSUSR_UUK_BAGMSGS> SearchBaggageMsgsByFlightandSeatNumber(OSUSR_UUK_BAGMSGS bagTag)
        {
            BaggageEntities db = new BaggageEntities();

            var bagQuery = from s in db.OSUSR_UUK_BAGMSGS
                           select s;

            var results = bagQuery.Where(s => s.FFLTNR == bagTag.FFLTNR &&
                                    s.FDATE == bagTag.FDATE &&
                                    s.FDEST == bagTag.FDEST &&
                                    s.IFLTNR == bagTag.IFLTNR &&
                                    s.IDATE == bagTag.IDATE &&
                                    s.IORIGIN == bagTag.IORIGIN &&
                                    s.SSEAT == bagTag.SSEAT &&
                                    s.NBAGTAG != bagTag.NBAGTAG &&
                                    s.TIMESTAMP == bagQuery.Where(k => k.NBAGTAG == s.NBAGTAG &&
                                                                k.SMI == BagTMRulesActions.SMI_BSM)
                                                    .Max(x => x.TIMESTAMP))
                .OrderBy(s => s.TIMESTAMP)
                .ToList<OSUSR_UUK_BAGMSGS>();

            return results;
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
        /// Calculate the correct message to reinsert so the BAGINTEG table is updated
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        public void CalculateErrorReprocessing(OSUSR_UUK_BAGMSGS bag, int n)
        {
            List<OSUSR_UUK_BAGMSGS> listOfBags = this.SearchBaggageMsgsByBagTagandTimestamp(bag);

            if (listOfBags != null && listOfBags.Count > 0)
            {
                foreach (OSUSR_UUK_BAGMSGS  bagAux in listOfBags)
                {
                    this.ProcessBaggageTTYEngine(bagAux, n);
                }
                return;
            } else
            {
                return;
            }
        }
    }
}
