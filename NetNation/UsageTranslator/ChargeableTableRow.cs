using System;
using System.Collections.Generic;
using System.Text;

namespace UsageTranslator
{
    public class ChargeableTableRow
    {
        public ChargeableTableRow() { }

        public int PartnerId { get; set; }
        public string Product { get; set; }
        public string PartnerPurchasePlanId { get; set; }
        public string Plan { get; set; }
        public int Usage { get; set; }

    }
}
