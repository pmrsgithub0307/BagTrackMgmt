using BagTMCommon;
using BagTMDBLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMEngineProcessing
{
    class FLTINFOProcessing
    {
        /// <summary>
        /// H2H and PTM processing
        /// </summary>
        private H2HProcessing h2hProcessing;
        private PTMH2HProcessing ptmH2HProcessing;

        public FLTINFOProcessing(H2HProcessing h2hProcessing, PTMH2HProcessing ptmH2HProcessing)
        {
            this.h2hProcessing = h2hProcessing;
            this.ptmH2HProcessing = ptmH2HProcessing;
        }

        /// <summary>
        /// Process engine rules 
        /// </summary>
        /// <param name="messageObject"></param>
        /// <param name="n"></param>
        public void ProcessFLTINFOEngine(Object messageObject, int n)
        {
            BagTMLog.LogDebug("BagTM Engine FLT INFO", this);

            // Only process message if is a baggage message
            if (messageObject.GetType() != typeof(OSUSR_UUK_FLT_INFO))
                throw new EngineProcessingException("Not a FLTINFO message.");

            OSUSR_UUK_FLT_INFO info = (OSUSR_UUK_FLT_INFO)messageObject;

            OSUSR_UUK_FLT_INFO flightInfoF = null;
            OSUSR_UUK_FLT_INFO flightInfoI = null;
            bool isHub = false;

            BaggageEntities db = new BaggageEntities();

            var h2hQuery = from s in db.OSUSR_UUK_H2H
                           select s;

            var h2hList = h2hQuery.Where(s => s.IFLTINFOID == info.ID ||
                                    s.OFLTINFOID == info.ID)
                                 .OrderByDescending(x => x.ID)
                                 .ToList<OSUSR_UUK_H2H>();

            foreach (OSUSR_UUK_H2H h2h in h2hList)
            {
                flightInfoF = this.SearchFlightInfoByPK((h2h.OFLTINFOID != null) ? (int)h2h.OFLTINFOID : 0);
                flightInfoI = this.SearchFlightInfoByPK((h2h.IFLTINFOID != null) ? (int)h2h.IFLTINFOID : 0);
                flightInfoI = (flightInfoI != null) ? flightInfoI : new OSUSR_UUK_FLT_INFO();
                isHub = (flightInfoI != null && flightInfoI.FLT_NR != null) ? true : false;


                BagTMLog.LogDebug("BagTM Engine Search FlightInfoI", flightInfoI);
                BagTMLog.LogDebug("BagTM Engine Search FlightInfoF", flightInfoF);

                BagTMLog.LogDebug("BagTM Engine Rules H2H", this);

                if (flightInfoF == null || flightInfoF.FLT_NR == null || flightInfoI == null)
                    BagTMLog.LogDebug("No update to H2H since no flight information, please check flight in FLT_INFO table.", this);
                else
                {
                    this.h2hProcessing.EngineUpdateH2H(flightInfoI, flightInfoF, isHub);
                }
            }

            var ptmH2HQuery = from s in db.OSUSR_UUK_PTM_H2H
                              select s;

            var ptmH2HList = ptmH2HQuery.Where(s => s.IFLTINFOID == info.ID ||
                                    s.OFLTINFOID == info.ID)
                                 .OrderByDescending(x => x.ID)
                                 .ToList<OSUSR_UUK_PTM_H2H>();

            foreach (OSUSR_UUK_PTM_H2H ptmH2H in ptmH2HList)
            {
                flightInfoF = this.SearchFlightInfoByPK((ptmH2H.OFLTINFOID != null) ? (int)ptmH2H.OFLTINFOID : 0);
                flightInfoI = this.SearchFlightInfoByPK((ptmH2H.IFLTINFOID != null) ? (int)ptmH2H.IFLTINFOID : 0);
                flightInfoI = (flightInfoI != null) ? flightInfoI : new OSUSR_UUK_FLT_INFO();
                isHub = (flightInfoI != null && flightInfoI.FLT_NR != null) ? true : false;


                BagTMLog.LogDebug("BagTM Engine Search FlightInfoI", flightInfoI);
                BagTMLog.LogDebug("BagTM Engine Search FlightInfoF", flightInfoF);

                BagTMLog.LogDebug("BagTM Engine Rules H2H", this);

                if (flightInfoF == null || flightInfoF.FLT_NR == null || flightInfoI == null)
                    BagTMLog.LogDebug("No update to H2H since no flight information, please check flight in FLT_INFO table.", this);
                else
                {
                    this.ptmH2HProcessing.EngineUpdatePTMH2H(flightInfoI, (DateTime)this.ptmH2HProcessing.SearchLastPTMForFlight(ptmH2H));
                }
            }
            BagTMLog.LogDebug("BagTM Engine FLT INFO End", this);
        }

        /// <summary>
        /// Search flight information by primary key
        /// </summary>
        /// <param name="bagTag"></param>
        /// <returns>OSUSR_UUK_BSM[]</returns>
        private OSUSR_UUK_FLT_INFO SearchFlightInfoByPK(int flightId)
        {
            BaggageEntities db = new BaggageEntities();

            var bagQuery = from s in db.OSUSR_UUK_FLT_INFO
                           select s;

            var result = bagQuery.Where(s => s.ID == flightId).FirstOrDefault<OSUSR_UUK_FLT_INFO>();

            return result;
        }
    }
}
