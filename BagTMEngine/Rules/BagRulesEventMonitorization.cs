using BagTMCommon;
using NRules.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMEngineProcessing.Rules
{
    class BagRulesEventMonitorization
    {
        public static void OnFactInsertedEvent(object sender, WorkingMemoryEventArgs e)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Engine Processing Rules Events Fact about to Insert {0}", e.Fact.Value), sender);
            
        }

        public static void OnFactUpdatedEvent(object sender, WorkingMemoryEventArgs e)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Engine Processing Rules Events Fact about to Update {0}", e.Fact.Value), sender);

        }

        public static void OnFactRetractedEvent(object sender, WorkingMemoryEventArgs e)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Engine Processing Rules Events Fact about to be Retracted {0}", e.Fact.Value), sender);
        }

        public static void OnActivationCreatedEvent(object sender, AgendaEventArgs e)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Engine Processing Rules Events Fact about to Activate {0}", e.Rule.Name), sender);
        }

        public static void OnActivationUpdatedEvent(object sender, AgendaEventArgs e)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Engine Processing Rules Events Fact about to Update {0}", e.Rule.Name), sender);
        }

        public static void OnActivationDeletedEvent(object sender, AgendaEventArgs e)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Engine Processing Rules Events Fact about to Delete {0}", e.Rule.Name), sender);
        }

        public static void OnRuleFiringEvent(object sender, AgendaEventArgs e)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Engine Processing Rules about to Fire {0}", e.Rule.Name), sender);
        }

        public static void OnRuleFireEvent(object sender, AgendaEventArgs e)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Engine Processing Rules Fired {0}", e.Rule.Name), sender);
        }

        public static void OnConditionFailedEvent(object sender, ConditionErrorEventArgs e)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Engine Processing Rules Condition Failed {0}", e.Condition), sender);
        }

        public static void OnActionFailedEvent(object sender, ActionErrorEventArgs e)
        {
            BagTMLog.LogDebug(
                String.Format("BagTM Engine Processing Rules Action Failed {0}", e.Action), sender);
        }

    }
}
