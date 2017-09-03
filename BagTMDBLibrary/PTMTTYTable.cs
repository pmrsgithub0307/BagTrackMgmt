using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Data.Entity.Validation;
using BagTMCommon;

namespace BagTMDBLibrary
{
    public class PTMTTYTable : OSUSR_UUK_PAXMSGS, IPTMTTYTable
    {
        /// <summary>
        /// Classes static variable
        /// </summary>
        private String CLASS_Y = "Y";
        private String CLASS_C = "C";
        private String CLASS_F = "F";

        private IDictionary<String, String> map = new Dictionary<String, String>();

        public string IDEST { get; set; }

        public PTMTTYTable ()
        {
            this.TIMESTAMP = DateTime.Now;
            map.Add("F", "F");
            map.Add("C", "C");
            map.Add("Y", "Y");
            map.Add("M", "Y");
        }
        
        /// <summary>
        /// Public method to set attribute values through reflection transforming to attribute types
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="parameter"></param>
        public void SetValue(String methodName, String parameter)
        {
            MethodInfo methodInfo = this.GetType().GetMethod("set"+methodName);
            object[]  parametersArray = new object[1];

            parametersArray[0] = parameter.Trim();

            methodInfo.Invoke(this, parametersArray);
        }

        /// <summary>
        /// Public method to get attribute values formatted through reflection 
        /// </summary>
        /// <param name="fieldInfo"></param>
        public String GetValueFormatted(FieldInfo fieldInfo)
        {
            MethodInfo methodInfo = this.GetType().BaseType.GetMethod("get_" + fieldInfo.Name);
            Type attributeType = methodInfo.ReturnType;
            object[] parametersArray = null;
            
            if (attributeType == typeof(System.String))
            {
                return ((String)methodInfo.Invoke(this, parametersArray)).Trim();
            }
            else if (attributeType == typeof(System.Nullable<int>))
            {
                return ((int)methodInfo.Invoke(this, parametersArray)).ToString();
            }
            else if (attributeType == typeof(System.Nullable<System.DateTime>))
            {
                return ((DateTime)methodInfo.Invoke(this, parametersArray)).ToString("ddMMMyyyy");
            }
            return "";
            
        }

        /// <summary>
        /// Store in database PAXMSGS object
        /// </summary>
        /// <param name="db"></param>
        /// <param name="hub"></param>
        /// <returns></returns>
        public OSUSR_UUK_PAXMSGS Save(BaggageEntities db, String hub)
        {
            OSUSR_UUK_PAXMSGS obj = this.getOSUSR_UUK_PAXMSGS();

            BaggageEntities dbins = new BaggageEntities();
            try
            {

                // Just store and send to Engine queue if it's a hub message
                if (!hub.Equals(this.IDEST))
                {
                    throw new Exception("PTM TTY message not hub, destination: " + this.IDEST);
                }

                dbins.OSUSR_UUK_PAXMSGS.Add(obj);
                dbins.SaveChanges();
                return obj;
            }
            catch (DbEntityValidationException ex)
            {
                dbins.OSUSR_UUK_PAXMSGS.Remove(obj);

                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " PTMTTYTable the validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new TableException(exceptionMessage);
            }
            catch (Exception e)
            {
                dbins.OSUSR_UUK_PAXMSGS.Remove(obj);

                throw e;
            }
        }

        public OSUSR_UUK_PAXMSGS getOSUSR_UUK_PAXMSGS()
        {
            OSUSR_UUK_PAXMSGS obj = new OSUSR_UUK_PAXMSGS();

            // Fill all the information
            obj.ID = this.ID;
            obj.BUSINESS = this.BUSINESS;
            obj.CHD = this.CHD;
            obj.ECONOMY = this.ECONOMY;
            obj.FIRST = this.FIRST;
            obj.INF = this.INF;
            obj.IDATE = this.IDATE;
            obj.IFLTNR = this.IFLTNR;
            obj.IORIGIN = this.IORIGIN;
            obj.ODATE = this.ODATE;
            obj.ODEST = this.ODEST;
            obj.OFLTNR = this.OFLTNR;
            obj.RESERVATIONSTATUS = this.RESERVATIONSTATUS;
            obj.TIMESTAMP = this.TIMESTAMP;
            
            return obj;
        }

        /// <summary>
        /// Logic to insert the flight information in the PAXMSGS object
        /// </summary>
        /// <param name="flightParameter"></param>
        public void setFlight (String flightParameter)
        {
            /// <summary>
            /// TTY message element divider
            /// </summary>
            String matchFlights = @"[A-Z]{2}[0-9]{3,4}/[0-9]{1,2}[A-Z]{3}\s[A-Z]{3}[A-Z]{3}";
            DateTime today = DateTime.Today;

            Match flight = Regex.Match(flightParameter, matchFlights);

            if (flight.Success)
            {
                this.IFLTNR = CommonFunctions.FormatFlightNumber(flight.Value.Substring(0, flight.Value.IndexOf("/")));

                String parameterDate = flight.Value.Substring(flight.Value.IndexOf("/")+1, flight.Value.IndexOf(" ") - flight.Value.IndexOf("/")-1) + today.Year.ToString();
                DateTime date = DateTime.ParseExact(parameterDate, "ddMMMyyyy", CultureInfo.InvariantCulture);

                if (Math.Abs((date - today).TotalDays) > 180)
                {
                    if ((date - today).TotalDays > 0) date.AddYears(-1);
                    else date.AddYears(+1);
                }

                this.IDATE = date;

                this.IORIGIN = flight.Value.Substring(flight.Value.IndexOf(" ") + 1, 3);
                this.IDEST = flight.Value.Substring(flight.Value.IndexOf(" ") + 4, 3);
            }
        }

        /// <summary>
        /// Set message timestamp
        /// </summary>
        /// <param name="parameter"></param>
        public void setStamp(String parameter)
        {
            this.TIMESTAMP = DateTime.Parse(parameter);
        }
        
        /// <summary>
        /// Logic to insert the flight information in the PAXMSGS object
        /// </summary>
        /// <param name="outboundParameter"></param>
        public void setOutbounds(String outboundParameter)
        {
            /// <summary>
            /// TTY message components divider
            /// </summary>
            String matchOutbound = @"[A-Z]{2}[0-9]{3,4}(/[0-9]{2})?(/S)?(/N)?\s[A-Z]{3}\s[0-9]{1,3}[A-Z]\s[0-9]{1,3}B[0-9]{1,3}K(\s[A-Z/ ]*)?(\.CHD[0-9]{1,2})?(\.INF[0-9]{1,2})?(\.RQ)?(\.SA)?";
            String matchOutboundFlight = @"[A-Z]{2}[0-9]{3,4}";
            String matchOutboundWithChangeDate = @"[A-Z]{2}[0-9]{3,4}/[0-9]{2}";
            String matchOutboundDestination = @"\s[A-Z]{3}\s";
            String matchOutboundPAX = @"\s[0-9]{1,3}[A-Z]\s";
            String matchOutboundChilds = @"\.CHD[0-9]{1,2}";
            String matchOutboundInfants = @"\.INF[0-9]{1,2}";
            String matchOutboundStatus = @"(\.RQ)|(\.SA)";
            DateTime today = DateTime.Today;

            Match outbound = Regex.Match(outboundParameter, matchOutbound);

            if (outbound.Success)
            {
                Match outboundFlight = Regex.Match(outboundParameter, matchOutboundFlight);

                if (outboundFlight.Success)
                {
                    this.OFLTNR = CommonFunctions.FormatFlightNumber(outboundFlight.Value);
                }

                Match outboundWithChangeDate = Regex.Match(outboundParameter, matchOutboundWithChangeDate);

                if (outboundWithChangeDate.Success)
                {
                    String outboundDate = outboundWithChangeDate.Value.Substring(outboundWithChangeDate.Value.IndexOf("/") + 1, 2);

                    if (this.IDATE != null)
                    {
                        this.ODATE = ((DateTime)this.IDATE).AddDays(Int32.Parse(outboundDate) - ((DateTime)this.IDATE).Day);
                    }
                }
                else
                {
                    this.ODATE = this.IDATE;
                }

                Match outboundDestination = Regex.Match(outboundParameter, matchOutboundDestination);

                if (outboundDestination.Success)
                {
                    this.ODEST = outboundDestination.Value.Substring(1, 3);
                }

                Match outboundPAX = Regex.Match(outboundParameter, matchOutboundPAX);

                if (outboundPAX.Success)
                {
                    String outboundPAXClass = outboundPAX.Value.Substring(outboundPAX.Value.Length - 2, 1);
                    // Verify if outbound pax class is mapped otherwise is "Y"
                    if (!map.TryGetValue(outboundPAXClass, out outboundPAXClass)) outboundPAXClass = this.CLASS_Y;

                    if (CLASS_Y.Equals(outboundPAXClass))
                    {
                        this.ECONOMY = Convert.ToInt32(outboundPAX.Value.Substring(1, outboundPAX.Value.Length - 3));
                    }
                    else if (CLASS_C.Equals(outboundPAXClass))
                    {
                        this.BUSINESS = Convert.ToInt32(outboundPAX.Value.Substring(1, outboundPAX.Value.Length - 3));
                    }
                    else if (CLASS_F.Equals(outboundPAXClass))
                    {
                        this.FIRST = Convert.ToInt32(outboundPAX.Value.Substring(1, outboundPAX.Value.Length - 3));
                    }
                }

                Match outboundChild = Regex.Match(outboundParameter, matchOutboundChilds);

                if (outboundChild.Success)
                    this.CHD = Convert.ToInt32(outboundChild.Value.Remove(0, 4));

                Match outboundInfants = Regex.Match(outboundParameter, matchOutboundInfants);

                if (outboundInfants.Success)
                    this.INF = Convert.ToInt32(outboundInfants.Value.Remove(0, 4));

                Match outboundStatus = Regex.Match(outboundParameter, matchOutboundStatus);

                if (outboundStatus.Success && outboundStatus.Length > 0)
                    this.RESERVATIONSTATUS = outboundStatus.Value.Remove(0, 1);

            }
        }
    }
}
