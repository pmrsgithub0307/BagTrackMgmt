using BagTMDBLibrary;
using NRules.Fluent.Dsl;
using NRules.RuleModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BagTMEngineProcessing.Rules
{

    [Name("BagTMRule"), Description("First Local BSM/BPM Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("First Message"), Tag("Local")]
    [Priority(12)]
    public class FirstLocalBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                .Match<OSUSR_UUK_BAGMSGS>(b => b.SMI != null && BagTMRulesActions.standardMessageIdentifiers.Contains<String>(b.SMI))
                .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER == null)
                .Match<String>(() => hub, h => h != null && bag.VCITY != null && h.Equals(bag.VCITY));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.CreateBagIntegrity(bag, bagIntegrity, hub, false);
        }
    }

    [Name("BagTMRule"), Description("First Hub BSM/BPM Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("First Message"), Tag("Hub")]
    [Priority(11)]
    public class FirstHubBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Or(x => x
                    .And(y1 => y1
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.hubBagParameter))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.SMI != null && BagTMRulesActions.standardMessageIdentifiers.Contains<String>(b.SMI))
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER == null)
                        .Match<String>(() => hub, h => h != null && bag.VCITY != null && h.Equals(bag.VCITY))
                    )
                    .And(y2 => y2
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.SMI != null && BagTMRulesActions.standardMessageIdentifiers.Contains<String>(b.SMI))
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER == null)
                        .Match<String>(() => hub, h => h != null && bag.FDEST != null && h.Equals(bag.FDEST))));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.CreateBagIntegrity(bag, bagIntegrity, hub, true);
        }
    }

    [Name("BagTMRule"), Description("Local Autorized to Load Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("Authorized Message"), Tag("Local")]
    [Priority(10)]
    public class LocalAuthorizedBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                .Match<OSUSR_UUK_BAGMSGS>(b => b.SMI != null && BagTMRulesActions.standardMessageIdentifiers.Contains<String>(b.SMI))
                .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                .Match<String>(() => hub, h => h != null && bag.VCITY != null && h.Equals(bag.VCITY));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.UpdateBagIntegrity(bag, bagIntegrity, hub, false, BagTMRulesActions.BAG_TTY_CHANGE_AUTH);
        }
    }

    [Name("BagTMRule"), Description("Hub Autorized to Load Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("Authorized Message"), Tag("Hub")]
    [Priority(9)]
    public class HubAuthorizedBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Or(x => x
                    .And(y1 => y1
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => BagTMRulesActions.standardMessageIdentifiers.Contains<String>(b.SMI))
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.hubBagParameter))
                        .Match<String>(() => hub, h => h != null && bag.VCITY != null && bag.VCITY.Equals(h))
                    )
                    .And(y2 => y2
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => BagTMRulesActions.standardMessageIdentifiers.Contains<String>(b.SMI))
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                        .Match<String>(() => hub, h => h != null && bag.FDEST != null && bag.FDEST.Equals(h))));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.UpdateBagIntegrity(bag, bagIntegrity, hub, true, BagTMRulesActions.BAG_TTY_CHANGE_AUTH);
        }
    }

    [Name("BagTMRule"), Description("Local PAX Boarded Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("PAX Boarded Message"), Tag("Local")]
    [Priority(8)]
    public class LocalPAXBoardedBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI != null && b.SMI.Equals(BagTMRulesActions.SMI_BSM))
                .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                .Match<String>(() => hub, h => h != null && bag.VCITY != null && h.Equals(bag.VCITY))
                .Match<OSUSR_UUK_BAGMSGS>(b => b.SPAXCK != null && b.SPAXCK.Equals(BagTMRulesActions.PAX_BOARDED_STATUS));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.UpdateBagIntegrity(bag, bagIntegrity, hub, false, BagTMRulesActions.BAG_TTY_CHANGE_PAX_BOARDED);
        }
    }

    [Name("BagTMRule"), Description("Hub PAX Boarded Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("PAX Boarded Message"), Tag("Hub")]
    [Priority(7)]
    public class HubPAXBoardedBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Or(x => x
                    .And(y1 => y1
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI != null && b.SMI.Equals(BagTMRulesActions.SMI_BSM))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.SPAXCK != null && b.SPAXCK.Equals(BagTMRulesActions.PAX_BOARDED_STATUS))
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.hubBagParameter))
                        .Match<String>(() => hub, h => h != null && bag.VCITY != null && bag.VCITY.Equals(h))
                    )
                    .And(y2 => y2
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI != null && b.SMI.Equals(BagTMRulesActions.SMI_BSM))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.SPAXCK != null && b.SPAXCK.Equals(BagTMRulesActions.PAX_BOARDED_STATUS))
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                        .Match<String>(() => hub, h => h != null && bag.FDEST != null && bag.FDEST.Equals(h))));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.UpdateBagIntegrity(bag, bagIntegrity, hub, true, BagTMRulesActions.BAG_TTY_CHANGE_PAX_BOARDED);
        }
    }

    [Name("BagTMRule"), Description("Local Load Fail Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("Load Fail Message"), Tag("Local")]
    [Priority(6)]
    public class LocalLoadFailBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI.Equals(BagTMRulesActions.SMI_BSM))
                .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                .Match<String>(() => hub, h => h != null && h.Equals(bag.VCITY))
                .Match<OSUSR_UUK_BAGMSGS>(b => b.XSECURITY != null);
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.UpdateBagIntegrity(bag, bagIntegrity, hub, false, BagTMRulesActions.BAG_TTY_CHANGE_LOAD_FAIL);
        }
    }

    [Name("BagTMRule"), Description("Hub Load Fail Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("Load Fail Message"), Tag("Hub")]
    [Priority(5)]
    public class HubLoadFailBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Or(x => x
                    .And(y1 => y1
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI.Equals(BagTMRulesActions.SMI_BSM))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.XSECURITY != null)
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND.Equals(BagTMRulesActions.hubBagParameter))
                        .Match<String>(() => hub, h => bag.VCITY.Equals(h))
                    )
                    .And(y2 => y2
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI.Equals(BagTMRulesActions.SMI_BSM))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.XSECURITY != null)
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                        .Match<String>(() => hub, h => bag.FDEST.Equals(h))));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.UpdateBagIntegrity(bag, bagIntegrity, hub, true, BagTMRulesActions.BAG_TTY_CHANGE_LOAD_FAIL);
        }
    }

    [Name("BagTMRule"), Description("Local Off Load Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("Off Load Message"), Tag("Local")]
    [Priority(4)]
    public class LocalOffLoadBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI.Equals(BagTMRulesActions.SMI_BPM))
                .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                .Match<String>(() => hub, h => h != null && h.Equals(bag.VCITY))
                .Match<OSUSR_UUK_BAGMSGS>(b => b.BIRREG != null && b.BIRREG.Equals(BagTMRulesActions.BAGGAGE_OFFLOAD));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.UpdateBagIntegrity(bag, bagIntegrity, hub, false, BagTMRulesActions.BAG_TTY_CHANGE_BAGGAGE_OFFLOAD);
        }
    }

    [Name("BagTMRule"), Description("Hub OffLoad Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("OffLoad Message"), Tag("Hub")]
    [Priority(3)]
    public class HubOffLoadBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Or(x => x
                    .And(y1 => y1
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI.Equals(BagTMRulesActions.SMI_BPM))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.BIRREG != null && b.BIRREG.Equals(BagTMRulesActions.BAGGAGE_OFFLOAD))
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND.Equals(BagTMRulesActions.hubBagParameter))
                        .Match<String>(() => hub, h => bag.VCITY.Equals(h))
                    )
                    .And(y2 => y2
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI.Equals(BagTMRulesActions.SMI_BPM))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.BIRREG != null && b.BIRREG.Equals(BagTMRulesActions.BAGGAGE_OFFLOAD))
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                        .Match<String>(() => hub, h => bag.FDEST.Equals(h))));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.UpdateBagIntegrity(bag, bagIntegrity, hub, true, BagTMRulesActions.BAG_TTY_CHANGE_BAGGAGE_OFFLOAD);
        }
    }

    [Name("BagTMRule"), Description("Local PAX Cancel Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("PAX Cancel Message"), Tag("Local")]
    [Priority(2)]
    public class LocalPAXCancelBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI.Equals(BagTMRulesActions.SMI_BSM))
                .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND != null && b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                .Match<String>(() => hub, h => h != null && h.Equals(bag.VCITY))
                .Match<OSUSR_UUK_BAGMSGS>(b => b.STATUSINDICATOR != null && b.STATUSINDICATOR.Equals(BagTMRulesActions.PAX_CANCEL));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.UpdateBagIntegrity(bag, bagIntegrity, hub, false, BagTMRulesActions.BAG_TTY_CHANGE_PAX_CANCEL);
        }
    }

    [Name("BagTMRule"), Description("Hub PAX Cancel Message")]
    [Tag("BagTM"), Tag("BagIntegrity"), Tag("PAX Cancel Message"), Tag("Hub")]
    [Priority(1)]
    public class HubPAXCancelBagRule : Rule
    {
        IBagIntegrityTransformation transformation;

        public override void Define()
        {
            OSUSR_UUK_BAGMSGS bag = null;
            OSUSR_UUK_BAGINTEG bagIntegrity = null;
            String hub = null;

            When()
                .Or(x => x
                    .And(y1 => y1
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI.Equals(BagTMRulesActions.SMI_BSM))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.STATUSINDICATOR != null && b.STATUSINDICATOR.Equals(BagTMRulesActions.PAX_CANCEL))
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND.Equals(BagTMRulesActions.hubBagParameter))
                        .Match<String>(() => hub, h => bag.VCITY.Equals(h))
                    )
                    .And(y2 => y2
                        .Match<OSUSR_UUK_BAGMSGS>(() => bag, b => b.SMI.Equals(BagTMRulesActions.SMI_BSM))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.STATUSINDICATOR != null && b.STATUSINDICATOR.Equals(BagTMRulesActions.PAX_CANCEL))
                        .Match<OSUSR_UUK_BAGINTEG>(() => bagIntegrity, bi => bi.NUMBER != null && bag.NBAGTAG != null && bi.NUMBER.Equals(bag.NBAGTAG + bag.NNRTAGS))
                        .Match<OSUSR_UUK_BAGMSGS>(b => b.VBAGSOURCIND.Equals(BagTMRulesActions.localBagParameter))
                        .Match<String>(() => hub, h => bag.FDEST.Equals(h))));
            Then()
                .Do(ctx => this.Action(bag, bagIntegrity, hub));
        }

        public void Action(OSUSR_UUK_BAGMSGS bag, OSUSR_UUK_BAGINTEG bagIntegrity, String hub)
        {
            transformation = new BagIntegrityTransformation();
            transformation.UpdateBagIntegrity(bag, bagIntegrity, hub, true, BagTMRulesActions.BAG_TTY_CHANGE_PAX_CANCEL);
        }
    }
}
