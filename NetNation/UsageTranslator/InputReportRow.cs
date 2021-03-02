using System;
using System.Collections.Generic;
using System.Text;

namespace UsageTranslator
{
    public class InputReportRow
    {
        public InputReportRow() { }
        public int PartnerId { get; set; }
        public string PartnerGuid { get; set; }
        public int AccountId { get; set; }
        public string AccountGuid { get; set; }
        public string Username { get; set; }
        public string Domains { get; set; }
        public string Itemname { get; set; }
        public string Plan { get; set; }
        public int ItemType { get; set; }
        public string PartNumber { get; set; }
        public int ItemCount { get; set; }

    }
}
