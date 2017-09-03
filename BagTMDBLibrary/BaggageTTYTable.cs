using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Data.Entity.Validation;
using BagTMCommon;

namespace BagTMDBLibrary
{
    public class BaggageTTYTable : OSUSR_UUK_BAGMSGS, IBaggageTTYTable
    {
        private DateTime now;

        public BaggageTTYTable ()
        {
            this.now = DateTime.Now;
        }
        
        /// <summary>
        /// Public method to set attribute values through reflection transforming to attribute types
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="parameter"></param>
        public void SetValue(String methodName, String parameter)
        {
            DateTime today = DateTime.Today;
            parameter = parameter.Replace('\n', ' ').Replace('\r', ' ').TrimEnd();
            MethodInfo methodInfo = this.GetType().BaseType.GetMethod("set_"+methodName);
            String typeString = ((ParameterInfo[])methodInfo.GetParameters())[0].ParameterType.ToString();
            Type attributeType = Type.GetType(typeString);
            object[]  parametersArray = new object[1];

            if (attributeType == typeof(System.String))
            {
                parametersArray[0] = parameter.Trim();
            }
            else if (attributeType == typeof(System.Nullable<int>))
            {
                parametersArray[0] = Convert.ToInt32(parameter);
            }
            else if (attributeType == typeof(System.Nullable<System.DateTime>))
            {
                String parameterDate = parameter + today.Year.ToString();
                DateTime date = DateTime.ParseExact(parameterDate, "ddMMMyyyy", CultureInfo.InvariantCulture);

                if (Math.Abs((date - today).TotalDays) > 180)
                {
                    if ((date - today).TotalDays > 0) date.AddYears(-1);
                    else date.AddYears(+1);
                }
                

                parametersArray[0] = date;
            }
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

        public override String ToString()
        {
            StringBuilder result = new StringBuilder();

            foreach (FieldInfo info in this.GetType().GetFields())
            {
                result.AppendLine(String.Format("@|{0};{1}", info.Name, this.GetValueFormatted(info)));
            }

            return result.ToString();

        }

        public OSUSR_UUK_BAGMSGS Save(BaggageEntities db)
        {
            OSUSR_UUK_BAGMSGS obj = this.getOSUSR_UUK_BAGMSGS();
            obj.TIMESTAMP = now;

            BagTMLog.LogDebug("BagTM Queue About to Write to DB message : " + this.NBAGTAG, obj);

            BaggageEntities dbins = new BaggageEntities();
            try
            {

                dbins.OSUSR_UUK_BAGMSGS.Add(obj);
                dbins.SaveChanges();
                return obj;
            }
            catch (DbEntityValidationException ex)
            {
                dbins.OSUSR_UUK_BAGMSGS.Remove(obj);
                // Retrieve the error messages as a list of strings.
                var errorMessages = ex.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.ErrorMessage);

                // Join the list to a single string.
                var fullErrorMessage = string.Join("; ", errorMessages);

                // Combine the original exception message with the new one.
                var exceptionMessage = string.Concat(ex.Message, " BaggageTTYTable the validation errors are: ", fullErrorMessage);

                // Throw a new DbEntityValidationException with the improved exception message.
                throw new TableException(exceptionMessage);
            }
            catch (Exception e)
            {
                dbins.OSUSR_UUK_BAGMSGS.Remove(obj);

                throw e;
            }
        }

        public OSUSR_UUK_BAGMSGS getOSUSR_UUK_BAGMSGS()
        {
            OSUSR_UUK_BAGMSGS obj = new OSUSR_UUK_BAGMSGS();

            // Fill all the information
            obj.ID = this.ID;
            obj.SMI = this.SMI;
            obj.SLMI = this.SLMI;
            obj.STATUSINDICATOR = this.STATUSINDICATOR;
            obj.BIRREG = this.BIRREG;
            obj.CCORP = this.CCORP;
            obj.DREMOTELOC = this.DREMOTELOC;
            obj.EEXCEP = this.EEXCEP;
            obj.FFLTNR = this.FFLTNR;
            obj.FDATE = this.FDATE;
            obj.FDEST = this.FDEST;
            obj.FCLASS = this.FCLASS;
            obj.HHANDTERM = this.HHANDTERM;
            obj.HHANDBAY = this.HHANDBAY;
            obj.HHANDSTDGTE = this.HHANDSTDGTE;
            obj.IFLTNR = this.IFLTNR;
            obj.IDATE = this.IDATE;
            obj.IORIGIN = this.IORIGIN;
            obj.ICLASS = this.ICLASS;
            obj.JRECO = this.JRECO;
            obj.JAGENTID = this.JAGENTID;
            obj.JSCANID = this.JSCANID;
            obj.JREADLOC = this.JREADLOC;
            obj.JSENTLOC = this.JSENTLOC;
            obj.KDMP = this.KDMP;
            obj.LPNR = this.LPNR;
            obj.NBAGTAG = this.NBAGTAG;
            obj.NNRTAGS = this.NNRTAGS;
            obj.OFLTNR = this.OFLTNR;
            obj.ODATE = this.ODATE;
            obj.ODEST = this.ODEST;
            obj.OCLASS = this.OCLASS;
            obj.PPAXNAME = this.PPAXNAME;
            obj.RINTERNAL = this.RINTERNAL;
            obj.SAUTL = this.SAUTL;
            obj.SSEAT = this.SSEAT;
            obj.SPAXCK = this.SPAXCK;
            obj.SPAXPROF = this.SPAXPROF;
            obj.SAUTTRANS = this.SAUTTRANS;
            obj.SBAGTAGSTATUS = this.SBAGTAGSTATUS;
            obj.TTAGPRINTERID = this.TTAGPRINTERID;
            obj.UULD = this.UULD;
            obj.UCOMPT = this.UCOMPT;
            obj.UTYPEOFBAG = this.UTYPEOFBAG;
            obj.UDESTCONTAINER = this.UDESTCONTAINER;
            obj.UCONTTYPE = this.UCONTTYPE;
            obj.VBAGSOURCIND = this.VBAGSOURCIND;
            obj.VCITY = this.VCITY;
            obj.WPWINDICATOR = this.WPWINDICATOR;
            obj.WNRCKBAGS = this.WNRCKBAGS;
            obj.WTOTK = this.WTOTK;
            obj.XSECURITY = this.XSECURITY;
            obj.YFQTV = this.YFQTV;
            obj.TIMESTAMP = this.TIMESTAMP;

            return obj;
        }

        public void Clean()
        {
            this.SMI = null;
            this.SLMI = null;
            this.STATUSINDICATOR = null;
            this.BIRREG = null;
            this.CCORP = null;
            this.DREMOTELOC = null;
            this.EEXCEP = null;
            this.FFLTNR = null;
            this.FDATE = null;
            this.FDEST = null;
            this.FCLASS = null;
            this.HHANDTERM = null;
            this.HHANDBAY = null;
            this.HHANDSTDGTE = null;
            this.IFLTNR = null;
            this.IDATE = null;
            this.IORIGIN = null;
            this.ICLASS = null;
            this.JRECO = null;
            this.JAGENTID = null;
            this.JSCANID = null;
            this.JREADLOC = null;
            this.JSENTLOC = null;
            this.KDMP = null;
            this.LPNR = null;
            this.NBAGTAG = null;
            this.NNRTAGS = null;
            this.OFLTNR = null;
            this.ODATE = null;
            this.ODEST = null;
            this.OCLASS = null;
            this.PPAXNAME = null;
            this.RINTERNAL = null;
            this.SAUTL = null;
            this.SSEAT = null;
            this.SPAXCK = null;
            this.SPAXPROF = null;
            this.SAUTTRANS = null;
            this.SBAGTAGSTATUS = null;
            this.TTAGPRINTERID = null;
            this.UULD = null;
            this.UCOMPT = null;
            this.UTYPEOFBAG = null;
            this.UDESTCONTAINER = null;
            this.UCONTTYPE = null;
            this.VBAGSOURCIND = null;
            this.VCITY = null;
            this.WPWINDICATOR = null;
            this.WNRCKBAGS = null;
            this.WTOTK = null;
            this.XSECURITY = null;
            this.YFQTV = null;
            this.TIMESTAMP = null;
        }
    }
}
