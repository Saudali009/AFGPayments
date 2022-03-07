using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using ChoETL.NACHA;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System.IO;

namespace System.AFG.Payments.Workflows
{
    public class NACHAFileHandlerWorkflow : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService orgService = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService tracingService = context.GetExtension<ITracingService>();

            try
            {
                Guid batchId = workflowContext.PrimaryEntityId;
                string entityName = workflowContext.PrimaryEntityName;
                tracingService.Trace($"Workflow trigger for entity {entityName} & {batchId}");
                EntityCollection batchePayments = GetBatchedPayments(tracingService, orgService, batchId);
                tracingService.Trace($"Payments included in this batch {batchePayments.Entities.Count}");
                List<NACHAEntry> listOfEntries = null;

                if (batchePayments != null && batchePayments.Entities.Count > 0)
                {
                    listOfEntries = new List<NACHAEntry>();
                    foreach (Entity payment in batchePayments.Entities)
                    {
                        string paymentName = payment.Contains("afg_reasonofpayment") ? Convert.ToString(payment["afg_reasonofpayment"]) : string.Empty;
                        tracingService.Trace($"Payment Name {paymentName}");
                        string accountName = payment.Contains("afg_account") ? ((EntityReference)payment["afg_account"]).Name : string.Empty;
                        tracingService.Trace($"Account Name {accountName}");
                        int paymentType = payment.Contains("afg_paymenttype") ? ((OptionSetValue)payment["afg_paymenttype"]).Value : 346380000;
                        tracingService.Trace($"Payment Type {paymentType}");
                        decimal amount = ((Money)payment["afg_amount"]).Value;
                        tracingService.Trace($"Amount {amount}");
                        string reasonOfPayment = payment.Contains("afg_reasonofpayment") ? Convert.ToString(payment["afg_reasonofpayment"]) : string.Empty;
                        tracingService.Trace($"Reason of Payment {reasonOfPayment}");
                        string notes = payment.Contains("afg_note") ? Convert.ToString(payment["afg_note"]) : string.Empty;
                        tracingService.Trace($"Notes {notes}");
                        string bankName = payment.Contains("afg_bankrelation") ? ((EntityReference)payment["afg_bankrelation"]).Name : string.Empty;
                        tracingService.Trace($"Bank Name {bankName}");
                        tracingService.Trace($"Going for realted entities.");
                        string routingNumber = payment.Contains("br.afg_routingnumber") ? Convert.ToString(((AliasedValue)payment["br.afg_routingnumber"]).Value) : string.Empty;
                        tracingService.Trace($"Regarding : Rounting Number {routingNumber}");
                        string accountNumber = payment.Contains("br.tf_accountno") ? Convert.ToString(((AliasedValue)payment["br.tf_accountno"]).Value) : string.Empty;
                        tracingService.Trace($"Regarding : Account Number {accountNumber}");
                        string branch = payment.Contains("br.tf_branchname") ? Convert.ToString(((AliasedValue)payment["br.tf_branchname"]).Value) : string.Empty;
                        tracingService.Trace($"Regarding : branch Name {branch}");

                        NACHAEntry entry = new NACHAEntry(paymentName, accountName, paymentType, amount, reasonOfPayment, notes, bankName, routingNumber, accountNumber, branch);
                        if (entry != null)
                        {
                            listOfEntries.Add(entry);
                        }
                        else
                        {
                            tracingService.Trace($"Null Entry.");
                        }
                    }
                }
                else
                {
                    tracingService.Trace($"No payment is batched. Stopping execution.");
                    return;
                }

                if (listOfEntries != null && listOfEntries.Count > 0)
                {
                    tracingService.Trace($"Entries are ready for creation with total: {listOfEntries.Count}");
                    CreateNACHAFile(orgService, tracingService, listOfEntries, batchId);
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error encountered while creating NACHA file {ex}");
            }
        }

        public EntityCollection GetBatchedPayments(ITracingService tracingService, IOrganizationService orgService, Guid batchId)
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
                </entity>
            </fetch>";
            batchedPaymentsXml = string.Format(batchedPaymentsXml, batchId);
            EntityCollection collection = orgService.RetrieveMultiple(new FetchExpression(batchedPaymentsXml));
            return collection;
        }

        public void CreateNACHAFile(IOrganizationService service, ITracingService tracingService, List<NACHAEntry> listOfEntries, Guid batchId)
        {
            tracingService.Trace($"Trying to create NACHA file with entries : {listOfEntries.Count}");
            ChoNACHAConfiguration config = new ChoNACHAConfiguration();
            config.BatchNumber = 2022;
            config.DestinationBankRoutingNumber = "123456789";
            config.OriginatingCompanyId = "123456789";
            config.DestinationBankName = "BANK USA";
            config.OriginatingCompanyName = "ALLIANCE FUNDING GROUP";
            config.ReferenceCode = "ALLIANCE FUNDING GROUP LLC.";

            string randomString = GenerateRandomAlphanumericString();
            string fileName = $"ACH_{GenerateRandomAlphanumericString()}.txt";
            tracingService.Trace($"Filename is generated as {fileName}");
            MemoryStream stream = new MemoryStream();
            using (ChoNACHAWriter nachaWriter = new ChoNACHAWriter(stream, config))
            {
                using (ChoNACHABatchWriter batchWriter = nachaWriter.CreateBatch(200))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        ChoNACHAEntryDetailWriter creditEntry = batchWriter.CreateDebitEntryDetail(20, "123456789", "1313131313", 22.505M, "ID Number", "ID Name", "Desc Data");
                        creditEntry.CreateAddendaRecord("HOME BUILDING MATERIAL");
                        creditEntry.Close();
                    }

                    batchWriter.Close();
                    batchWriter.Dispose();
                }
                nachaWriter.Close();
                nachaWriter.Dispose();
            }

            var fileReader = new ChoNACHAReader(stream); //to read file
            tracingService.Trace($"File Reader {fileReader.ToString()}");
            byte[] fileByteArray = stream.ToArray(); //converted Json object to byte array
            tracingService.Trace($"Byte Array length {fileByteArray.Length}");
            var base64OfFile = Convert.ToBase64String(fileByteArray); //converted byte array to base64
            if (!string.IsNullOrEmpty(base64OfFile))
            {
                tracingService.Trace($"Trying to create Note in CRM");
                string entityName = "afg_batch";
                Entity note = new Entity("annotation");
                note["objectid"] = new EntityReference(entityName, batchId);
                note["objecttypecode"] = entityName;
                note["documentbody"] = base64OfFile;
                note["mimetype"] = "text/plain";
                note["filename"] = fileName;
                note["subject"] = $"NACHA-{fileName}";
                note["notetext"] = $"NACHA created at {DateTime.Now}";
                Guid noteId = service.Create(note);
                tracingService.Trace("Note created succfully!");
            }
            else
            {
                tracingService.Trace("Unable to create Note in CRM, base64 is null");
            }
        }

        public string GenerateRandomAlphanumericString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var random = new Random();
            var randomString = new string(Enumerable.Repeat(chars, length)
                                                    .Select(s => s[random.Next(s.Length)]).ToArray());
            return randomString;
        }
    }
}