using System;
using BagTMDBLibrary;
using BagTMCommon;
using System.Linq;
using System.Collections.Generic;

namespace BagTMEngineProcessing
{
    public class SorterProcessingVolumeMap
    {
        /// <summary>
        /// Sorter processing volume refresh limit date 
        /// </summary>
        private DateTime refreshLimit;

        /// <summary>
        /// Sorter processing volume
        /// </summary>
        private List<SorterVolume> sorterVolumeCalculate;

        public SorterProcessingVolumeMap()
        {
            this.refreshList();
        }

        /// <summary>
        /// Calculate sorter time for processing volumes for flight
        /// </summary>
        /// <param name="sorterTime"></param>
        /// <param name="flightInfo"></param>
        /// <returns></returns>
        public double getSorterTime(double sorterTime, int maxSorterThroughPut, DateTime time)
        {
            if (DateTime.Now > refreshLimit) this.refreshList();

            SorterVolume sorterVolume = (sorterVolumeCalculate != null) ?
                sorterVolumeCalculate.Where(s => time > s.DATEINIT && time <= s.DATEEND).FirstOrDefault() : null;

            double sorterProcessingTime = (sorterVolume != null) ? sorterVolume.COUNT : 0;

            sorterProcessingTime = (sorterProcessingTime > maxSorterThroughPut) ? sorterProcessingTime / maxSorterThroughPut : 0;

            return sorterTime + sorterProcessingTime;
        }


        private void refreshList()
        {
            refreshLimit = DateTime.Now.AddMinutes(5);
            
            BaggageEntities dbSV = new BaggageEntities();

            var sorterVolumeQuery = (from bi in dbSV.OSUSR_UUK_H2H
                                    where bi.IETA != null && bi.IETA >= DateTime.Now
                                         // here I choose each field I want to group by
                                    group bi by new { bi.IETA, bi.IATA } into g
                                    select new
                                    {
                                        DATEINIT = (g.Key.IATA != null && ((DateTime)g.Key.IATA).Year > 1900) ? 
                                                        (DateTime)g.Key.IATA : (DateTime)g.Key.IETA,
                                        Count = g.Sum(x => x.INRBAGS)
                                    }
                                ).ToList();

            var sorterVolumePerTime = sorterVolumeQuery
                .Select(s => new
                {
                    DATEINIT = s.DATEINIT.AddMinutes(-1 * s.DATEINIT.Minute % 5),
                    Count = s.Count
                });

            sorterVolumeCalculate = sorterVolumePerTime
                .GroupBy(s => new { s.DATEINIT })
                .Select(r => new SorterVolume
                {
                    DATEINIT = r.Key.DATEINIT,
                    DATEEND = r.Key.DATEINIT.AddMinutes(5),
                    COUNT = r.Sum(x => (double) x.Count)
                }).ToList<SorterVolume>();

            BagTMLog.LogDebug("BagTM Engine Update H2H Bag Calculate", sorterVolumeCalculate);

        }
    }

    class SorterVolume
    {
        public DateTime DATEINIT { get; set; }
        public DateTime DATEEND { get; set; }
        public double COUNT { get; set; }
    }
}

