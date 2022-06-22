using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.AFG.Payments.Workflows
{
    public class CRMDataRetrievalHandler
    {
        public static EntityCollection GetNACHAConfiguration(ITracingService tracingService, IOrganizationService orgService, int bankCode, int paymentType)
        {
            string nachaConfigXml = @"<fetch top='1'>
              <entity name='afg_nachafile'>
                <attribute name='afg_name' />
                <attribute name='afg_blockingfactor' />
                <attribute name='afg_bankname' />
                <attribute name='afg_companyname' />
                <attribute name='afg_destinationbankroutingnumber' />
                <attribute name='afg_destinationbankname' />
                <attribute name='afg_originatingcompanyid' />
                <filter>
                  <condition attribute='statecode' operator='eq' value='0' />
                  <condition attribute='afg_bankname' operator='eq' value='{0}' />
                  <condition attribute='afg_paymenttype' operator='eq' value='{1}' />
                </filter>
                <order attribute='createdon' descending='true' />
              </entity>
            </fetch>";
            nachaConfigXml = string.Format(nachaConfigXml, bankCode, paymentType);
            EntityCollection collection = orgService.RetrieveMultiple(new FetchExpression(nachaConfigXml));
            return collection;
        }
        public static Entity GetBatchDetails(ITracingService tracingService, IOrganizationService orgService, Guid batchId)
        {
            Entity batch = orgService.Retrieve("afg_batch", batchId, new ColumnSet(new string[] { "afg_batchnumber", "afg_bank", "afg_paymenttype" }));
            return batch;
        }
        public static EntityCollection GetBatchedPayments(ITracingService tracingService, IOrganizationService orgService, Guid batchId)
        {
            string batchedPaymentsXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' >
                <entity name='afg_payment' >
                    <attribute name='afg_paymentid' />
                    <attribute name='afg_reasonofpayment' />
                    <attribute name='afg_paymenttype' />
                    <attribute name='afg_note' />
                    <attribute name='afg_bankrelation' />
                    <attribute name='afg_amount' />
                    <attribute name='afg_account' />
                    <order attribute='afg_name' descending='false' />
                    <filter type='and' >
                        <condition attribute='afg_batch' operator='eq' value='{0}' />
                    </filter>
                    <link-entity name='tf_bankrelationship' from='tf_bankrelationshipid' to='afg_bankrelation' visible='false' link-type='outer' alias='br' >
                        <attribute name='afg_routingnumber' />
                        <attribute name='tf_branchname' />
                        <attribute name='tf_bankbranchid' />
                        <attribute name='tf_accountno' />
                        <attribute name='tf_accountname' />
                    </link-entity>
                    <link-entity name='account' from='accountid' to='afg_account' visible='false' link-type='outer' alias='acc'>
                        <attribute name='afg_lplus_customerid' />
                        <attribute name='name' />
                    </link-entity>
                   <link-entity name='opportunity' from='opportunityid' to='afg_opportunity' visible='false' link-type='outer' alias='opp'>
                      <attribute name='afg_lplusid' />
                    </link-entity>
                </entity>
            </fetch>";
            batchedPaymentsXml = string.Format(batchedPaymentsXml, batchId);
            tracingService.Trace($"XML is {batchedPaymentsXml}");
            EntityCollection collection = orgService.RetrieveMultiple(new FetchExpression(batchedPaymentsXml));
            return collection;
        }
        public static EntityCollection GetEffectiveHolidays(ITracingService tracingService, IOrganizationService orgService)
        {
            string getEffectiveHolidaysXml = @"<fetch>
              <entity name='afg_bankholiday'>
                <attribute name='afg_date' />
                <attribute name='afg_name' />
                <filter>
                  <condition attribute='afg_istoobserve' operator='eq' value='1' />
                </filter>
              </entity>
            </fetch>";
            getEffectiveHolidaysXml = string.Format(getEffectiveHolidaysXml);
            EntityCollection collection = orgService.RetrieveMultiple(new FetchExpression(getEffectiveHolidaysXml));
            return collection;
        }
        public static List<NACHAEntry> GetListOfNachaEntries(ITracingService tracingService, EntityCollection batchePayments)
        {
            List<NACHAEntry> listOfEntries = new List<NACHAEntry>();
            if (batchePayments != null && batchePayments.Entities.Count > 0)
            {
                foreach (Entity payment in batchePayments.Entities)
                {
                    string paymentName = payment.Contains("afg_reasonofpayment") && payment["afg_reasonofpayment"] != null ? Convert.ToString(payment["afg_reasonofpayment"]) : string.Empty;
                    string accountName = payment.Contains("afg_account") && payment["afg_account"] != null ? ((EntityReference)payment["afg_account"]).Name : string.Empty;
                    int paymentType = payment.Contains("afg_paymenttype") && payment["afg_paymenttype"] != null ? ((OptionSetValue)payment["afg_paymenttype"]).Value : 346380000;
                    decimal amount = payment.Contains("afg_amount") && payment["afg_amount"] != null ? Convert.ToDecimal(((Money)payment["afg_amount"]).Value) : Convert.ToDecimal(0);
                    string reasonOfPayment = payment.Contains("afg_reasonofpayment") && payment["afg_reasonofpayment"] != null ? Convert.ToString(payment["afg_reasonofpayment"]) : string.Empty;
                    string notes = payment.Contains("afg_note") && payment["afg_note"] != null ? Convert.ToString(payment["afg_note"]) : string.Empty;
                    string bankName = payment.Contains("afg_bankrelation") && payment["afg_bankrelation"] != null ? ((EntityReference)payment["afg_bankrelation"]).Name : string.Empty;
                    string routingNumber = payment.Contains("br.afg_routingnumber") && payment["br.afg_routingnumber"] != null ? Convert.ToString(((AliasedValue)payment["br.afg_routingnumber"]).Value) : string.Empty;
                    string accountNumber = payment.Contains("br.tf_accountno") && payment["br.tf_accountno"] != null ? Convert.ToString(((AliasedValue)payment["br.tf_accountno"]).Value) : string.Empty;
                    string branch = payment.Contains("br.tf_branchname") && payment["br.tf_branchname"] != null ? Convert.ToString(((AliasedValue)payment["br.tf_branchname"]).Value) : string.Empty;
                    string customerId = payment.Contains("acc.afg_lplus_customerid") && payment["acc.afg_lplus_customerid"] != null ? Convert.ToString(((AliasedValue)payment["acc.afg_lplus_customerid"]).Value) : string.Empty;
                    string contractNumber = payment.Contains("opp.afg_lplusid") && payment["opp.afg_lplusid"] != null ? Convert.ToString(((AliasedValue)payment["opp.afg_lplusid"]).Value) : string.Empty;

                    NACHAEntry entry = new NACHAEntry(paymentName, accountName, customerId, paymentType, amount, reasonOfPayment, notes, bankName, routingNumber, accountNumber, branch, contractNumber);
                    if (entry != null)
                    {
                        listOfEntries.Add(entry);
                    }
                }
            }
            return listOfEntries;
        }
        public static Guid CreateNote(ITracingService tracingService, IOrganizationService service, Guid batchId, string fileName, string base64OfFile)
        {
            string entityName = "afg_batch";
            Entity note = new Entity("annotation");
            note["objectid"] = new EntityReference(entityName, batchId);
            note["objecttypecode"] = entityName;
            note["documentbody"] = base64OfFile;
            note["mimetype"] = "text/plain";
            note["filename"] = fileName;
            note["subject"] = $"NACHA-{fileName}";
            note["notetext"] = $"NACHA created at {DateTime.Now}";
            tracingService.Trace("Note created succfully!");
            return service.Create(note);
        }
    }
}
