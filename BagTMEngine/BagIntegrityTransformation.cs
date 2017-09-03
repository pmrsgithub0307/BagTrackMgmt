using BagTMCommon;
using BagTMDBLibrary;
using BagTMEngineProcessing.Rules;
using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;

namespace BagTMEngineProcessing
{
    class BagIntegrityTransformation : IBagIntegrityTransformation
    {
        public void CreateBagIntegrity(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub, bool isHub)
        {
            BagTMLog.LogDebug("BagTM Engine Create Bag Integrity Start", this);

            BagTMLog.LogDebug("BagTM Engine Create Bag Integrity ", bag);
            BagTMLog.LogDebug("BagTM Engine Create Bag Integrity ", bagIntegrity);
            BagTMLog.LogDebug("BagTM Engine Create Bag Integrity ", hub);
            BagTMLog.LogDebug("BagTM Engine Create Bag Integrity ", isHub);

            bool isLocalTTYforHub = false;
            bool isLocalEndingInHub = false;

            if (isHub && BagTMRulesActions.localBagParameter.Equals(bag.VBAGSOURCIND)) isLocalTTYforHub = true;
            if (isHub && isLocalTTYforHub && bag.OFLTNR == null) isLocalEndingInHub = true;
            if (isHub && !isLocalTTYforHub && bag.IFLTNR != null) isLocalEndingInHub = true;

            bagIntegrity.NUMBER = bag.NBAGTAG + bag.NNRTAGS;
            bagIntegrity.CLOSE = null;
            bagIntegrity.TIMESTAMP = DateTime.Now;

            // Create different BagIntegrity record creation for Hub and Local TTY messages
            if (isHub)
            {
                // The message is a local message for Hub transit
                if (isLocalTTYforHub)
                {
                    bagIntegrity.IFLTNR = CommonFunctions.FormatFlightNumber(bag.FFLTNR);
                    bagIntegrity.IDATE = bag.FDATE;
                    bagIntegrity.IORIGIN = bag.VCITY;
                    bagIntegrity.ISEAT = bag.SSEAT;
                    bagIntegrity.IAUT = bag.SAUTL;
                    // Fill the FFP information for the flight
                    if (bag.YFQTV != null)
                    {
                        if (bag.YFQTV.Contains(BagTMRulesActions.FFP_GOLD))
                            bagIntegrity.IFFP = BagTMRulesActions.FFP_GOLD_VALUE;
                        else if (bag.YFQTV.Contains(BagTMRulesActions.FFP_SILVER))
                            bagIntegrity.IFFP = BagTMRulesActions.FFP_SILVER_VALUE;
                        else bagIntegrity.IFFP = BagTMRulesActions.FFP_BLUE_VALUE;
                    }
                    // If TTY implicates a onward flight out of the Hub
                    if (!isLocalEndingInHub)
                    {
                        bagIntegrity.FFLTNR = CommonFunctions.FormatFlightNumber(bag.OFLTNR);
                        bagIntegrity.FDATE = bag.ODATE;
                        bagIntegrity.FDEST = bag.ODEST;
                    }
                    if (BagTMRulesActions.SMI_BPM.Equals(bag.SMI))
                        bagIntegrity.BPMIN = BagTMRulesActions.YES;
                    if (BagTMRulesActions.PAX_BOARDED_STATUS.Equals(bag.SPAXCK))
                        bagIntegrity.BSMBOARDI = BagTMRulesActions.PAX_BOARDED_STATUS;
                    if (BagTMRulesActions.BAGGAGE_RUSH.Equals(bag.EEXCEP) || this.IsRush(bagIntegrity))
                        bagIntegrity.ISRUSH = BagTMRulesActions.BAGGAGE_RUSH_VALUE;
                    if (BagTMRulesActions.BAGGAGE_PRIO.Equals(bag.EEXCEP))
                        bagIntegrity.ISRUSH = BagTMRulesActions.BAGGAGE_PRIO_VALUE;
                    if (bag.PPAXNAME.Equals(BagTMRulesActions.BAGGAGE_CREW))
                    {
                        bagIntegrity.IAUT = BagTMRulesActions.BAG_CREW_VALUE;
                        bagIntegrity.BPMIN = BagTMRulesActions.BAG_CREW_VALUE;
                        bagIntegrity.BSMBOARDI = BagTMRulesActions.BAG_CREW_VALUE;
                    }
                }
                else
                {
                    bagIntegrity.IFLTNR = CommonFunctions.FormatFlightNumber(bag.IFLTNR);
                    bagIntegrity.IDATE = bag.IDATE;
                    bagIntegrity.IORIGIN = bag.IORIGIN;
                    bagIntegrity.FFLTNR = CommonFunctions.FormatFlightNumber(bag.FFLTNR);
                    bagIntegrity.FDATE = bag.FDATE;
                    bagIntegrity.FDEST = bag.FDEST;
                    bagIntegrity.FAUT = bag.SAUTL;
                    bagIntegrity.FSEAT = bag.SSEAT;
                    // Fill the FFP information for the flight
                    if (bag.YFQTV != null)
                    {
                        if (bag.YFQTV.Contains(BagTMRulesActions.FFP_GOLD))
                            bagIntegrity.OFFP = BagTMRulesActions.FFP_GOLD_VALUE;
                        else if (bag.YFQTV.Contains(BagTMRulesActions.FFP_SILVER))
                            bagIntegrity.OFFP = BagTMRulesActions.FFP_SILVER_VALUE;
                        else bagIntegrity.OFFP = BagTMRulesActions.FFP_BLUE_VALUE;
                    }
                    if (BagTMRulesActions.SMI_BPM.Equals(bag.SMI))
                        bagIntegrity.BPMHUB = BagTMRulesActions.YES;
                    if (BagTMRulesActions.PAX_BOARDED_STATUS.Equals(bag.SPAXCK))
                        bagIntegrity.BSMBOARD = BagTMRulesActions.PAX_BOARDED_STATUS;
                    if (BagTMRulesActions.BAGGAGE_RUSH.Equals(bag.EEXCEP) || this.IsRush(bagIntegrity))
                        bagIntegrity.ISRUSH = BagTMRulesActions.BAGGAGE_RUSH_VALUE;
                    if (BagTMRulesActions.BAGGAGE_PRIO.Equals(bag.EEXCEP))
                        bagIntegrity.ISRUSH = BagTMRulesActions.BAGGAGE_PRIO_VALUE;
                    if (bag.PPAXNAME.Equals(BagTMRulesActions.BAGGAGE_CREW))
                    {
                        bagIntegrity.FAUT = BagTMRulesActions.BAG_CREW_VALUE;
                        bagIntegrity.BPMHUB = BagTMRulesActions.BAG_CREW_VALUE;
                        bagIntegrity.BSMBOARD = BagTMRulesActions.BAG_CREW_VALUE;
                    }
                }
            }
            else
            {
                bagIntegrity.FFLTNR = CommonFunctions.FormatFlightNumber(bag.FFLTNR);
                bagIntegrity.FDATE = bag.FDATE;
                bagIntegrity.FDEST = bag.FDEST;
                bagIntegrity.FAUT = bag.SAUTL;
                bagIntegrity.FSEAT = bag.SSEAT;
                // Fill the FFP information for the flight
                if (bag.YFQTV != null)
                {
                    if (bag.YFQTV.Contains(BagTMRulesActions.FFP_GOLD))
                        bagIntegrity.OFFP = BagTMRulesActions.FFP_GOLD_VALUE;
                    else if (bag.YFQTV.Contains(BagTMRulesActions.FFP_SILVER))
                        bagIntegrity.OFFP = BagTMRulesActions.FFP_SILVER_VALUE;
                    else bagIntegrity.OFFP = BagTMRulesActions.FFP_BLUE_VALUE;
                }
                if (BagTMRulesActions.SMI_BPM.Equals(bag.SMI))
                    bagIntegrity.BPMHUB = BagTMRulesActions.YES;
                if (BagTMRulesActions.PAX_BOARDED_STATUS.Equals(bag.SPAXCK))
                    bagIntegrity.BSMBOARD = BagTMRulesActions.PAX_BOARDED_STATUS;
                if (BagTMRulesActions.BAGGAGE_RUSH.Equals(bag.EEXCEP) || this.IsRush(bagIntegrity))
                    bagIntegrity.ISRUSH = BagTMRulesActions.BAGGAGE_RUSH_VALUE;
                if (BagTMRulesActions.BAGGAGE_PRIO.Equals(bag.EEXCEP))
                    bagIntegrity.ISRUSH = BagTMRulesActions.BAGGAGE_PRIO_VALUE;
                if (bag.PPAXNAME.Equals(BagTMRulesActions.BAGGAGE_CREW))
                {
                    bagIntegrity.FAUT = BagTMRulesActions.BAG_CREW_VALUE;
                    bagIntegrity.BPMHUB = BagTMRulesActions.BAG_CREW_VALUE;
                    bagIntegrity.BSMBOARD = BagTMRulesActions.BAG_CREW_VALUE;
                }
            }

            // Change register for baggage groups
            if (!bagIntegrity.NUMBER.EndsWith(BagTMRulesActions.BAGGAGE_NRTAG_001))
            {
                bagIntegrity.FAUT = (bagIntegrity.FAUT != null) ? BagTMRulesActions.BAG_GROUP_AUT_CODE : null;
                bagIntegrity.IAUT = (bagIntegrity.IAUT != null) ? BagTMRulesActions.BAG_GROUP_AUT_CODE : null;
                bagIntegrity.BPMIN = (bagIntegrity.BPMIN != null) ? BagTMRulesActions.BAG_GROUP_BPM_CODE : null;
                bagIntegrity.IAUT = (bagIntegrity.BPMHUB != null) ? BagTMRulesActions.BAG_GROUP_BPM_CODE : null;
            }

            BaggageEntities dbin = new BaggageEntities();
            try
            {
                BagTMLog.LogDebug("BagTM Engine Create Bag Integrity Saving", bagIntegrity);

                dbin.OSUSR_UUK_BAGINTEG.Add(bagIntegrity);
                dbin.SaveChanges();
                BagTMLog.LogInfo("BagTM Engine Create Bag Integrity: " + bagIntegrity.NUMBER, this);
            }
            catch (DbEntityValidationException ex)
            {
                dbin.OSUSR_UUK_BAGINTEG.Remove(bagIntegrity);

                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors;

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new EngineProcessingException(exceptionMessage);
            }
            catch (Exception e)
            {
                dbin.OSUSR_UUK_BAGINTEG.Remove(bagIntegrity);

                throw e;
            }
            BagTMLog.LogDebug("BagTM Engine Create Bag Integrity End", this);


        }

        public void UpdateBagIntegrity(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub, bool isHub, Int32 TTYChange)
        {
            BagTMLog.LogDebug("BagTM Engine Upodate Bag Integrity Start", this);

            BagTMLog.LogDebug("BagTM Engine Upodate Bag Integrity", bag);
            BagTMLog.LogDebug("BagTM Engine Upodate Bag Integrity", bagIntegrity);
            BagTMLog.LogDebug("BagTM Engine Upodate Bag Integrity", hub);
            BagTMLog.LogDebug("BagTM Engine Upodate Bag Integrity", isHub);
            BagTMLog.LogDebug("BagTM Engine Upodate Bag Integrity", TTYChange);
            bool isLocalTTYforHub = false;

            if (isHub && BagTMRulesActions.localBagParameter.Equals(bag.VBAGSOURCIND)) isLocalTTYforHub = true;

            if (bag.NBAGTAG == null || bagIntegrity.NUMBER != bag.NBAGTAG + bag.NNRTAGS)
                throw new EngineProcessingException(
                        String.Format("Message TTY {0} and BAGTAG {1} not processed due to incosistences.",
                                bag.SMI, bag.NBAGTAG));

            // Have to be defined what to do
            if (null != bagIntegrity.CLOSE) bagIntegrity.CLOSE = null;
            
            // Create different BagIntegrity record creation for Hub and Local TTY messages
            if (isHub)
            {
                // The message is a local message for Hub transit
                if (isLocalTTYforHub)
                {
                    if (bag.FFLTNR != null && !CommonFunctions.FormatFlightNumber(bag.FFLTNR).Equals(bagIntegrity.IFLTNR))
                        bagIntegrity.IFLTNR = CommonFunctions.FormatFlightNumber(bag.FFLTNR);
                    if (bag.FDATE != null && !bag.FDATE.Equals(bagIntegrity.IDATE))
                        bagIntegrity.IDATE = bag.FDATE;
                    if (bag.VCITY != null && !bag.VCITY.Equals(bagIntegrity.IORIGIN))
                        bagIntegrity.IORIGIN = bag.VCITY;
                    if (bag.SSEAT != null && !bag.SSEAT.Equals(bagIntegrity.ISEAT))
                        bagIntegrity.ISEAT = bag.SSEAT;
                    if (bag.OFLTNR != null && !CommonFunctions.FormatFlightNumber(bag.OFLTNR).Equals(bagIntegrity.FFLTNR))
                        bagIntegrity.FFLTNR = CommonFunctions.FormatFlightNumber(bag.OFLTNR);
                    if (bag.ODATE != null && !bag.ODATE.Equals(bagIntegrity.FDATE))
                        bagIntegrity.FDATE = bag.ODATE;
                    if (bag.ODEST != null && !bag.ODEST.Equals(bagIntegrity.FDEST))
                        bagIntegrity.FDEST = bag.ODEST;
                    if (BagTMRulesActions.SMI_BPM.Equals(bag.SMI))
                        bagIntegrity.BPMIN = BagTMRulesActions.YES;
                    if (BagTMRulesActions.PAX_BOARDED_STATUS.Equals(bag.SPAXCK))
                        bagIntegrity.BSMBOARDI = BagTMRulesActions.PAX_BOARDED_STATUS;

                    switch (TTYChange)
                    {
                        case BagTMRulesActions.BAG_TTY_CHANGE_AUTH:
                            bagIntegrity.IAUT = bag.SAUTL;
                            break;

                        case BagTMRulesActions.BAG_TTY_CHANGE_PAX_BOARDED:
                            bagIntegrity.BSMBOARDI = BagTMRulesActions.PAX_BOARDED_STATUS;
                            break;

                        case BagTMRulesActions.BAG_TTY_CHANGE_LOAD_FAIL:
                            bagIntegrity.BPMIN = BagTMRulesActions.PAX_BOARDED_FAIL;
                            break;

                        case BagTMRulesActions.BAG_TTY_CHANGE_BAGGAGE_OFFLOAD:
                            if (bagIntegrity.IFLTNR != null)
                            {
                                bagIntegrity.IAUT = BagTMRulesActions.BAG_OFF;
                                bagIntegrity.BPMIN = BagTMRulesActions.BAG_OFF;
                            }
                            if (bagIntegrity.FFLTNR != null)
                            {
                                bagIntegrity.FAUT = BagTMRulesActions.BAG_OFF;
                                bagIntegrity.BPMHUB = BagTMRulesActions.BAG_OFF;
                            }
                            break;

                        case BagTMRulesActions.BAG_TTY_CHANGE_PAX_CANCEL:
                            if (bagIntegrity.IFLTNR != null)
                            {
                                bagIntegrity.IAUT = BagTMRulesActions.PAX_AUT_STATUS_CODE;
                                bagIntegrity.BPMIN = BagTMRulesActions.PAX_AUT_STATUS_CODE;
                            }
                            if (bagIntegrity.FFLTNR != null)
                            {
                                bagIntegrity.FAUT = BagTMRulesActions.PAX_AUT_STATUS_CODE;
                                bagIntegrity.BPMHUB = BagTMRulesActions.PAX_AUT_STATUS_CODE;
                            }
                            break;

                    }
                    if (bag.PPAXNAME.Equals(BagTMRulesActions.BAGGAGE_CREW))
                    {
                        bagIntegrity.FAUT = BagTMRulesActions.BAG_CREW_VALUE;
                        bagIntegrity.BPMIN = BagTMRulesActions.BAG_CREW_VALUE;
                        bagIntegrity.BSMBOARDI = BagTMRulesActions.BAG_CREW_VALUE;
                    }
                }
                else
                {
                    if (bag.IFLTNR != null && !CommonFunctions.FormatFlightNumber(bag.IFLTNR).Equals(bagIntegrity.IFLTNR))
                        bagIntegrity.IFLTNR = CommonFunctions.FormatFlightNumber(bag.IFLTNR);
                    if (bag.IDATE != null && !bag.IDATE.Equals(bagIntegrity.IDATE))
                        bagIntegrity.IDATE = bag.IDATE;
                    if (bag.IORIGIN != null && !bag.IORIGIN.Equals(bagIntegrity.IORIGIN))
                        bagIntegrity.IORIGIN = bag.IORIGIN;
                    if (bag.SSEAT != null && !bag.SSEAT.Equals(bagIntegrity.FSEAT))
                        bagIntegrity.FSEAT = bag.SSEAT;
                    if (bag.FFLTNR != null && !CommonFunctions.FormatFlightNumber(bag.FFLTNR).Equals(bagIntegrity.FFLTNR))
                        bagIntegrity.FFLTNR = CommonFunctions.FormatFlightNumber(bag.FFLTNR);
                    if (bag.FDATE != null && !bag.FDATE.Equals(bagIntegrity.FDATE))
                        bagIntegrity.FDATE = bag.FDATE;
                    if (bag.FDEST != null && !bag.FDEST.Equals(bagIntegrity.FDEST))
                        bagIntegrity.FDEST = bag.FDEST;

                    if (BagTMRulesActions.SMI_BPM.Equals(bag.SMI))
                        bagIntegrity.BPMHUB = BagTMRulesActions.YES;
                    if (BagTMRulesActions.BAGGAGE_RUSH.Equals(bag.EEXCEP) || this.IsRush(bagIntegrity))
                        bagIntegrity.ISRUSH = BagTMRulesActions.BAGGAGE_RUSH_VALUE;
                    if (BagTMRulesActions.BAGGAGE_PRIO.Equals(bag.EEXCEP))
                        bagIntegrity.ISRUSH = BagTMRulesActions.BAGGAGE_PRIO_VALUE;
                    if (BagTMRulesActions.PAX_BOARDED_STATUS.Equals(bag.SPAXCK))
                        bagIntegrity.BSMBOARD = BagTMRulesActions.PAX_BOARDED_STATUS;

                    switch (TTYChange)
                    {
                        case BagTMRulesActions.BAG_TTY_CHANGE_AUTH:
                            bagIntegrity.FAUT = bag.SAUTL;
                            break;

                        case BagTMRulesActions.BAG_TTY_CHANGE_PAX_BOARDED:
                            bagIntegrity.BSMBOARD = BagTMRulesActions.PAX_BOARDED_STATUS;
                            break;

                        case BagTMRulesActions.BAG_TTY_CHANGE_LOAD_FAIL:
                            bagIntegrity.BPMHUB = BagTMRulesActions.PAX_BOARDED_FAIL;
                            break;

                        case BagTMRulesActions.BAG_TTY_CHANGE_BAGGAGE_OFFLOAD:
                            bagIntegrity.FAUT = BagTMRulesActions.BAG_OFF;
                            bagIntegrity.BPMHUB = BagTMRulesActions.BAG_OFF;
                            break;

                        case BagTMRulesActions.BAG_TTY_CHANGE_PAX_CANCEL:
                            bagIntegrity.FAUT = BagTMRulesActions.PAX_AUT_STATUS_CODE;
                            bagIntegrity.BPMHUB = BagTMRulesActions.PAX_AUT_STATUS_CODE;
                            break;

                    }
                    if (bag.PPAXNAME.Equals(BagTMRulesActions.BAGGAGE_CREW))
                    {
                        bagIntegrity.FAUT = BagTMRulesActions.BAG_CREW_VALUE;
                        bagIntegrity.BPMHUB = BagTMRulesActions.BAG_CREW_VALUE;
                        bagIntegrity.BSMBOARD = BagTMRulesActions.BAG_CREW_VALUE;
                    }
                }
            }
            else
            {
                if (bag.FFLTNR != null && !CommonFunctions.FormatFlightNumber(bag.FFLTNR).Equals(bagIntegrity.FFLTNR))
                    bagIntegrity.FFLTNR = CommonFunctions.FormatFlightNumber(bag.FFLTNR);
                if (bag.FDATE != null && !bag.FDATE.Equals(bagIntegrity.FDATE))
                    bagIntegrity.FDATE = bag.FDATE;
                if (bag.FDEST != null && !bag.FDEST.Equals(bagIntegrity.FDEST))
                    bagIntegrity.FDEST = bag.FDEST;
                if (bag.SSEAT != null && !bag.SSEAT.Equals(bagIntegrity.FSEAT))
                    bagIntegrity.FSEAT = bag.SSEAT;

                if (BagTMRulesActions.SMI_BPM.Equals(bag.SMI))
                    bagIntegrity.BPMHUB = BagTMRulesActions.YES;
                if (BagTMRulesActions.BAGGAGE_RUSH.Equals(bag.EEXCEP) || this.IsRush(bagIntegrity))
                    bagIntegrity.ISRUSH = BagTMRulesActions.BAGGAGE_RUSH_VALUE;
                if (BagTMRulesActions.BAGGAGE_PRIO.Equals(bag.EEXCEP))
                    bagIntegrity.ISRUSH = BagTMRulesActions.BAGGAGE_PRIO_VALUE;
                if (BagTMRulesActions.PAX_BOARDED_STATUS.Equals(bag.SPAXCK))
                    bagIntegrity.BSMBOARD = BagTMRulesActions.PAX_BOARDED_STATUS;

                switch (TTYChange)
                {
                    case BagTMRulesActions.BAG_TTY_CHANGE_AUTH:
                        bagIntegrity.FAUT = bag.SAUTL;
                        break;

                    case BagTMRulesActions.BAG_TTY_CHANGE_PAX_BOARDED:
                        bagIntegrity.BSMBOARD = BagTMRulesActions.PAX_BOARDED_STATUS;
                        break;

                    case BagTMRulesActions.BAG_TTY_CHANGE_LOAD_FAIL:
                        bagIntegrity.BPMHUB = BagTMRulesActions.PAX_BOARDED_FAIL;
                        break;

                    case BagTMRulesActions.BAG_TTY_CHANGE_BAGGAGE_OFFLOAD:
                        bagIntegrity.FAUT = BagTMRulesActions.BAG_OFF;
                        bagIntegrity.BPMHUB = BagTMRulesActions.BAG_OFF;
                        break;

                    case BagTMRulesActions.BAG_TTY_CHANGE_PAX_CANCEL:
                        bagIntegrity.FAUT = BagTMRulesActions.PAX_AUT_STATUS_CODE;
                        bagIntegrity.BPMHUB = BagTMRulesActions.PAX_AUT_STATUS_CODE;
                        break;
                }
                if (bag.PPAXNAME.Equals(BagTMRulesActions.BAGGAGE_CREW))
                {
                    bagIntegrity.FAUT = BagTMRulesActions.BAG_CREW_VALUE;
                    bagIntegrity.BPMHUB = BagTMRulesActions.BAG_CREW_VALUE;
                    bagIntegrity.BSMBOARD = BagTMRulesActions.BAG_CREW_VALUE;
                }
            }

            // Change register for baggage groups
            if (!bagIntegrity.NUMBER.EndsWith(BagTMRulesActions.BAGGAGE_NRTAG_001))
            {
                bagIntegrity.FAUT = (bagIntegrity.FAUT != null) ? BagTMRulesActions.BAG_GROUP_AUT_CODE : null;
                bagIntegrity.IAUT = (bagIntegrity.IAUT != null) ? BagTMRulesActions.BAG_GROUP_AUT_CODE : null;
                bagIntegrity.BPMIN = (bagIntegrity.BPMIN != null) ? BagTMRulesActions.BAG_GROUP_BPM_CODE : null;
                bagIntegrity.IAUT = (bagIntegrity.BPMHUB != null) ? BagTMRulesActions.BAG_GROUP_BPM_CODE : null;
            }

            BaggageEntities dbup = new BaggageEntities();
            try
            {
                BagTMLog.LogDebug("BagTM Engine Upodate Bag Integrity Saving", bagIntegrity);

                // Verifies if updating last baggage integrity record, if more then one remove all
                OSUSR_UUK_BAGINTEG lastBaggageIntegraty = this.LastBaggageIntegraty(bagIntegrity);
                if (lastBaggageIntegraty != null && lastBaggageIntegraty.TIMESTAMP != null &&
                    lastBaggageIntegraty.TIMESTAMP > bagIntegrity.TIMESTAMP)
                {
                    this.UpdateBagIntegrity(bag, lastBaggageIntegraty, hub, isHub, TTYChange);
                }
                else
                {
                    if (lastBaggageIntegraty != null) bagIntegrity = MergeBagIntegrity(bagIntegrity, lastBaggageIntegraty);

                    bagIntegrity.TIMESTAMP = DateTime.Now;

                    dbup.OSUSR_UUK_BAGINTEG.Attach(bagIntegrity);
                    dbup.Entry(bagIntegrity).State = EntityState.Modified;
                    dbup.SaveChanges();
                }
                
                BagTMLog.LogInfo("BagTM Engine Update Bag Integrity: " + bagIntegrity.NUMBER, this);
            }
            catch (DbEntityValidationException ex)
            {
                dbup.OSUSR_UUK_BAGINTEG.Remove(bagIntegrity);

                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors;

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " The validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new EngineProcessingException(exceptionMessage);
            }
            catch (Exception e)
            {
                dbup.OSUSR_UUK_BAGINTEG.Remove(bagIntegrity);

                throw e;
            }

            BagTMLog.LogDebug("BagTM Engine Upodate Bag Integrity End", this);

        }

        /// <summary>
        /// Create adhoc rules for verifying is baggage is rush
        /// </summary>
        /// <param name="bagInteg"></param>
        /// <returns></returns>
        private bool IsRush(OSUSR_UUK_BAGINTEG bagInteg)
        {
            return bagInteg.NUMBER.StartsWith("2108");
        }

        /// <summary>
        /// Last baggage integraty table 
        /// </summary>
        /// <param name="bagInteg"></param>
        /// <returns>OSUSR_UUK_BAGINTEG</returns>
        private OSUSR_UUK_BAGINTEG LastBaggageIntegraty(OSUSR_UUK_BAGINTEG bagInteg)
        {
            BaggageEntities db = new BaggageEntities();

            var bagQuery = from s in db.OSUSR_UUK_BAGINTEG
                           where s.NUMBER == bagInteg.NUMBER
                           select s;

            var results = bagQuery.OrderByDescending(s => s.TIMESTAMP)
                .ToList<OSUSR_UUK_BAGINTEG>();

            OSUSR_UUK_BAGINTEG lastBagInteg = results.FirstOrDefault();

            if (results != null && results.Count > 1)
            {
                foreach (OSUSR_UUK_BAGINTEG aux in results)
                {
                    if (aux != lastBagInteg)
                    {
                        lastBagInteg = MergeBagIntegrity(lastBagInteg, aux);
                        db.OSUSR_UUK_BAGINTEG.Remove(aux);
                    }
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

            return lastBagInteg;
        }

        private OSUSR_UUK_BAGINTEG MergeBagIntegrity(OSUSR_UUK_BAGINTEG merge, OSUSR_UUK_BAGINTEG aux)
        {
            if (aux.ISEAT != null && merge.ISEAT == null)
                merge.ISEAT = aux.ISEAT;
            if (aux.IAUT != null && merge.IAUT == null)
                merge.IAUT = aux.IAUT;
            if (aux.BPMIN != null && merge.BPMIN == null)
                merge.BPMIN = aux.BPMIN;
            if (aux.BSMBOARDI != null && merge.BSMBOARDI == null)
                merge.BSMBOARDI = aux.BSMBOARDI;
            if (aux.IFFP != null && merge.IFFP == null)
                merge.IFFP = aux.IFFP;
            if (aux.FSEAT != null && merge.FSEAT == null)
                merge.FSEAT = aux.FSEAT;
            if (aux.FAUT != null && merge.FAUT == null)
                merge.FAUT = aux.FAUT;
            if (aux.BPMHUB != null && merge.BPMHUB == null)
                merge.BPMHUB = aux.BPMHUB;
            if (aux.BSMBOARD != null && merge.BSMBOARD == null)
                merge.BSMBOARD = aux.BSMBOARD;
            if (aux.OFFP != null && merge.OFFP == null)
                merge.OFFP = aux.OFFP;
            
            return merge;
        }
    }
}
