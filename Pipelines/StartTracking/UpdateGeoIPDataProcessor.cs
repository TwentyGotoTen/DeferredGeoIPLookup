using Sitecore.Analytics;
using Sitecore.Analytics.Data.DataAccess.DataSets;
using Sitecore.Analytics.Pipelines.StartTracking;
using Sitecore.Data.Items;
using Sitecore.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeferredGeoIPLookup.Pipelines.StartTracking
{
    public class UpdateGeoIPDataProcessor : StartTrackingProcessor
    {
        public String RulesItemId { get; set; }

        public override void Process(StartTrackingArgs args)
        {
            if (!Tracker.IsActive || String.IsNullOrEmpty(RulesItemId))
                return;

            VisitorDataSet.VisitsRow currentVisit = Tracker.Visitor.GetCurrentVisit();

            if (currentVisit == null || currentVisit.HasGeoIpData)
                return;

            Item rulesItem = Sitecore.Context.Database.GetItem(RulesItemId);

            if (rulesItem == null)
                return;

            RuleList<RuleContext> ruleList = RuleFactory.GetRules<RuleContext>(rulesItem, "Rule");

            if (ruleList == null)
                return;

            RuleContext ruleContext = new RuleContext();

            if (SatisfiesConditions(ruleList, ruleContext))
                currentVisit.UpdateGeoIpData();

            ruleContext.Abort();
        }

        private bool SatisfiesConditions(RuleList<RuleContext> ruleList, RuleContext ruleContext)
        {
            foreach (Rule<RuleContext> rule in ruleList.Rules)
            {
                if (rule.Condition == null)
                    continue;

                RuleStack stack = new RuleStack();
                rule.Condition.Evaluate(ruleContext, stack);

                if (ruleContext.IsAborted)
                    continue;

                if (stack.Count == 0)
                    continue;

                if (!(bool)stack.Pop())
                    continue;

                return true;
            }

            return false;        
        }
    }
}
