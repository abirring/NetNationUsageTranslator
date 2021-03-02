using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Text;

namespace UsageTranslator
{
    public class Program
    {
        // reads input data from a file and outputs to a list with each data row as an item
        public static List<InputReportRow> ReadInputReport(string filePath)
        {
            List<InputReportRow> inputReport = new List<InputReportRow>();

            FileInfo fileInfo = new FileInfo(filePath);
            StreamReader streamReader = fileInfo.OpenText();

            string header = streamReader.ReadLine(); // skip header

            while (!streamReader.EndOfStream)
            {
                string lineContent = streamReader.ReadLine();
                if (lineContent.Trim() == string.Empty)
                    continue;
                string[] words = lineContent.Split(',');
                InputReportRow inputReportRow = new InputReportRow() { PartnerId = Int32.Parse(words[0]), PartnerGuid = words[1], AccountId = Int32.Parse(words[2]), AccountGuid = words[3],
                    Username = words[4], Domains = words[5], Itemname = words[6], Plan = words[7], ItemType = Int32.Parse(words[8]), PartNumber = words[9], ItemCount = Int32.Parse(words[10]) };
                inputReport.Add(inputReportRow);
            }

            streamReader.Close();
            return inputReport;
        }

        // Extracts data fields from sample input report list, as appropriate for the table chargeable
        // Transforms data into suitable form for the table chargeable
        // Does basic string data validation for sql injection prevention
        public static List<ChargeableTableRow> LoadChargeableTable(List<InputReportRow> inputReport, List<int> skipPartnerIds, Dictionary<string, string> typeMap,
            Dictionary<string, int> unitReductionItemCount)
        {
            List<ChargeableTableRow> chargeableTable = new List<ChargeableTableRow>();
            foreach (var inputReportRow in inputReport)
            {
                // no part number, skip
                if (inputReportRow.PartNumber == null)
                {
                    AddToErrorLog("Missing PartNumber: " + inputReportRow.ToString());
                    continue;
                }
                // non positive itemCount, skip
                if (inputReportRow.ItemCount < 0)
                {
                    AddToErrorLog("Non-Positive itemCount: " + inputReportRow.ToString());
                    continue;
                }

                // skip entries in skipPartnerIds
                if (skipPartnerIds.Contains(inputReportRow.PartnerId)) {
                    continue;
                }

                string product = MapPartNumberToproduct(inputReportRow.PartNumber, typeMap);
                string partnerPurchasePlanId = StripNonAlphaNumericChars(inputReportRow.AccountGuid);
                int usage = ApplyUnitReductionRule(inputReportRow.PartNumber, inputReportRow.ItemCount, unitReductionItemCount);

                if (InputValid(new List<string>() { product, partnerPurchasePlanId, inputReportRow.Plan }))
                {
                    ChargeableTableRow chargeableTableRow = new ChargeableTableRow()
                    {
                        PartnerId = inputReportRow.PartnerId,
                        Product = product,
                        PartnerPurchasePlanId = partnerPurchasePlanId,
                        Plan = inputReportRow.Plan,
                        Usage = usage
                    };

                    AddToSuccessLog(product, inputReportRow.ItemCount);

                    chargeableTable.Add(chargeableTableRow);
                }
                else
                {
                    // log invalid data
                    continue;
                }
            }

            return chargeableTable;
        }

        // Extracts data fields from sample input report list, as appropriate for the table domains
        // Transforms data into suitable form for the table domains
        // Does basic string data validation for sql injection prevention
        public static List<DomainsTableRow> LoadDomainsTable(List<InputReportRow> inputReport)
        {
            List<DomainsTableRow> domainsTable = new List<DomainsTableRow>();
            HashSet<string> domainsSet = new HashSet<string>();

            foreach (var inputReportRow in inputReport)
            {
                if (domainsSet.Contains(inputReportRow.Domains)) // for distinct domain names
                    continue;
                else
                {
                    domainsSet.Add(inputReportRow.Domains);

                    string partnerPurchasePlanId = StripNonAlphaNumericChars(inputReportRow.AccountGuid);

                    if (InputValid(new List<string>() { partnerPurchasePlanId, inputReportRow.Domains }))
                    {
                        DomainsTableRow domainsTableRow = new DomainsTableRow()
                        {
                            PartnerPruchasePlanId = partnerPurchasePlanId,
                            Domain = inputReportRow.Domains
                        };
                        domainsTable.Add(domainsTableRow);
                    }
                    else
                    {
                        // log invalid data
                        continue;
                    }
                }
            }
            return domainsTable;
        }

        // Validate input strings
        public static bool InputValid(List<string> dataStrings)
        {
            foreach(string dataString in dataStrings)
            {
                if (!ValidSQLString(dataString))
                    return false;
            }
            return true;
        }

        // Validate a string for absence of certain characters
        public static bool ValidSQLString(string dataString)
        {
            if (dataString == null)
                return true;
            if (dataString.Contains(',') || dataString.Contains(';') || dataString.Contains('"') || dataString.Contains('\''))
                return false;
            else
                return true;
        }

        // simulation of log
        public static void AddToErrorLog(string error)
        {
            // uncomment the following statement if want to see log in the debug window
            // Debug.WriteLine(error);
        }

        // simulation of log
        public static void AddToSuccessLog(string product, int itemCount)
        {
            // uncomment the following statement if want to see log in the debug window
            // Debug.WriteLine("Product: {0}, Item Count: {1}", product, itemCount);
        }

        public static int ApplyUnitReductionRule(string partNumber, int itemCount, Dictionary<string, int> unitReductionItemCount)
        {
            // no check is made if unitReductionItemCountValue > itemCount
            int unitReductionItemCountValue;
            if (unitReductionItemCount.TryGetValue(partNumber, out unitReductionItemCountValue))
                return itemCount / unitReductionItemCountValue;
            else
                return itemCount;
        }

        public static string StripNonAlphaNumericChars(string str)
        {
            // method does not count if returned string has exactly 32 characters
            var result = str.Where(ch => (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'));
            string resultString = new string(result.ToArray());
            return resultString;
        }

        public static string MapPartNumberToproduct(string partNumber, Dictionary<string, string> typeMap) 
        {
            // if partNumber is not in json, empty string will be returned
            string product;
            typeMap.TryGetValue(partNumber, out product);
            return product;
        }

        public static List<int> LoadSkipPartnerIds()
        {
            // move this function to a separate file and/or read the data from a config file
            List<int> skipPartnerIds = new List<int>();
            skipPartnerIds.Add(26392);
            return skipPartnerIds;
        }

        // Loads json file into a dictionary to be used by the other methods
        public static Dictionary<string, string> LoadJson(string path)
        {
            Dictionary<string, string> typeMap = new Dictionary<string, string>();
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                typeMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }
            return typeMap;
        }

        // An insert query string is created and returned for the table chargeable
        // Query data comes from list of table rows
        public static string CreateChargeableInsertQuery(List<ChargeableTableRow> chargeableTable)
        {
            StringBuilder chargeableInsertQuery = new StringBuilder();
            chargeableInsertQuery.Append("INSERT INTO chargeable (partnerID, product, partnerPurchasePlanID, plan, usage)");
            chargeableInsertQuery.AppendLine();
            chargeableInsertQuery.Append("VALUES");
            chargeableInsertQuery.AppendLine();
            foreach (var row in chargeableTable)
            {
                chargeableInsertQuery.Append("( " + row.PartnerId + ", '" + row.Product + "', '" + row.PartnerPurchasePlanId + "', '" + row.Plan + "', " + row.Usage + " ),");
                chargeableInsertQuery.AppendLine();
            }
            chargeableInsertQuery.Length--; chargeableInsertQuery.Length--; chargeableInsertQuery.Length--;
            chargeableInsertQuery.Append(";");
            return chargeableInsertQuery.ToString();
        }

        // An insert query string is created and returned for the table domains
        // Query data comes from list of table rows
        public static string CreateDomainsInsertQuery(List<DomainsTableRow> domainsTable)
        {
            StringBuilder domainsInsertQuery = new StringBuilder();
            domainsInsertQuery.Append("INSERT INTO domains (partnerPurchasePlanID, domain)");
            domainsInsertQuery.AppendLine();
            domainsInsertQuery.Append("VALUES");
            domainsInsertQuery.AppendLine();
            foreach (var row in domainsTable)
            {
                domainsInsertQuery.Append("( '" + row.PartnerPruchasePlanId + "', '" + row.Domain + "' ),");
                domainsInsertQuery.AppendLine();
            }
            domainsInsertQuery.Length--; domainsInsertQuery.Length--; domainsInsertQuery.Length--;
            domainsInsertQuery.Append(";");
            return domainsInsertQuery.ToString();
        }

        // main driver program
        static void Main(string[] args)
        {
            Debug.WriteLine("Hello World!");

            Dictionary<string, string> typeMap;
            typeMap = LoadJson(@"C:\temp\NetNationsFiles\typemap.json");

            Dictionary<string, int> unitReductionItemCount = new Dictionary<string, int>() {
                { "EA000001GB0O", 1000 },
                { "PMQ00005GB0R", 5000 },
                { "SSX006NR",     1000 },
                { "SPQ00001MB0R", 2000 }
            };

            List<int> skipPartnerIds;
            skipPartnerIds = LoadSkipPartnerIds();

            List<InputReportRow> inputReport;
            string filePath = @"C:\temp\NetNationsFiles\Sample_Report.csv";
            try
            {
                inputReport = ReadInputReport(filePath);

                List<ChargeableTableRow> chargeableTable;
                chargeableTable = LoadChargeableTable(inputReport, skipPartnerIds, typeMap, unitReductionItemCount);

                string chargeableInsertQuery = CreateChargeableInsertQuery(chargeableTable);
                // write insert query to a file
                using (StreamWriter writer = new StreamWriter(@"C:\temp\NetNationsFiles\chargeable_insert_query.txt"))
                {
                    writer.WriteLine(chargeableInsertQuery);
                }

                List<DomainsTableRow> domainsTable;
                domainsTable = LoadDomainsTable(inputReport);

                string domainsInsertQuery = CreateDomainsInsertQuery(domainsTable);
                // write insert query to a file
                using (StreamWriter writer = new StreamWriter(@"C:\temp\NetNationsFiles\domains_insert_query.txt"))
                {
                    writer.WriteLine(domainsInsertQuery);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception thrown: " + e.ToString());
            }
        }
    }
}
