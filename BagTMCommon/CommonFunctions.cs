using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMCommon
{
    public class CommonFunctions
    {

        public static String FormatFlightNumber(String flightNumber)
        {
            return (flightNumber == null) ? 
                        flightNumber : 
                        (flightNumber.Length < 3) ? null : 
                                                    flightNumber.Substring(0, 2) + Int32.Parse(flightNumber.Remove(0, 2)).ToString("0000");
        }
    }
}
