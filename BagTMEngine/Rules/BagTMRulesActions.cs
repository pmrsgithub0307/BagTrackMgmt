using System;

namespace BagTMEngineProcessing.Rules
{
    static class BagTMRulesActions
    {
        public static String YES = "Y";
        public static String localBagParameter = "L";
        public static String hubBagParameter = "T";
        public static String SMI_BSM = "BSM";
        public static String SMI_BPM = "BPM";
        public static String[] standardMessageIdentifiers = new String[2] { SMI_BSM, SMI_BPM };
        public static String PAX_BOARDED_STATUS = "B";
        public static String PAX_BOARDED_FAIL = "X";
        public static String PAX_AUT_STATUS_CODE = "X";
        public static String BAG_GROUP_AUT_CODE = "X";
        public static String BAG_GROUP_BPM_CODE = "X";
        public static String BAG_OFF = "X";
        public static String BAGGAGE_OFFLOAD = "OFF";
        public static String PAX_CANCEL = "DEL";
        public static String BAGGAGE_RUSH = "RUSH";
        public static String BAGGAGE_PRIO = "PRIO";
        public static String BAGGAGE_CREW = "CREW";
        public static String BAGGAGE_NRTAG_001 = "001";
        public static String BAG_CREW_VALUE = "X";
        public static String BAGGAGE_RUSH_VALUE = "R";
        public static String BAGGAGE_PRIO_VALUE = "P";
        public static String FFP_GOLD = "GOLD";
        public static String FFP_SILVER = "SILVER";
        public static String FFP_GOLD_VALUE = "G";
        public static String FFP_SILVER_VALUE = "S";
        public static String FFP_BLUE_VALUE = "B";

        public const Int32 BAG_TTY_CHANGE_AUTH = 1;
        public const Int32 BAG_TTY_CHANGE_PAX_BOARDED = 2;
        public const Int32 BAG_TTY_CHANGE_LOAD_FAIL = 3;
        public const Int32 BAG_TTY_CHANGE_BAGGAGE_OFFLOAD = 4;
        public const Int32 BAG_TTY_CHANGE_PAX_CANCEL = 5;
    }
}
