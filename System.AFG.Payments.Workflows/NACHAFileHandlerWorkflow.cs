﻿using System;
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
using ChoETL;

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
                string batchNumber = GetBatchNumber(tracingService, orgService, batchId);
                EntityCollection paymentCollection = GetBatchedPayments(tracingService, orgService, batchId);
                List<NACHAEntry> listOfEntries = GetListOfNachaEntries(paymentCollection);
                if (listOfEntries != null && listOfEntries.Count > 0)
                {
                    tracingService.Trace($"Entries are ready for creation with total: {listOfEntries.Count}");
                    CreateNACHAFile(orgService, tracingService, listOfEntries, batchId, batchNumber);
                }
                else
                {
                    tracingService.Trace($"No Entry Found.");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error encountered while creating NACHA file {ex}");
            }
        }

        public List<NACHAEntry> GetListOfNachaEntries(EntityCollection batchePayments)
        {
            List<NACHAEntry> listOfEntries = new List<NACHAEntry>();
            if (batchePayments != null && batchePayments.Entities.Count > 0)
            {
                foreach (Entity payment in batchePayments.Entities)
                {
                    string paymentName = payment.Contains("afg_reasonofpayment") ? Convert.ToString(payment["afg_reasonofpayment"]) : string.Empty;
                    string accountName = payment.Contains("afg_account") ? ((EntityReference)payment["afg_account"]).Name : string.Empty;
                    int paymentType = payment.Contains("afg_paymenttype") ? ((OptionSetValue)payment["afg_paymenttype"]).Value : 346380000;
                    decimal amount = payment.Contains("afg_amount") ? Convert.ToDecimal(((Money)payment["afg_amount"]).Value) : Convert.ToDecimal(0);
                    string reasonOfPayment = payment.Contains("afg_reasonofpayment") ? Convert.ToString(payment["afg_reasonofpayment"]) : string.Empty;
                    string notes = payment.Contains("afg_note") ? Convert.ToString(payment["afg_note"]) : string.Empty;
                    string bankName = payment.Contains("afg_bankrelation") ? ((EntityReference)payment["afg_bankrelation"]).Name : string.Empty;
                    string routingNumber = payment.Contains("br.afg_routingnumber") ? Convert.ToString(((AliasedValue)payment["br.afg_routingnumber"]).Value) : string.Empty;
                    string accountNumber = payment.Contains("br.tf_accountno") ? Convert.ToString(((AliasedValue)payment["br.tf_accountno"]).Value) : string.Empty;
                    string branch = payment.Contains("br.tf_branchname") ? Convert.ToString(((AliasedValue)payment["br.tf_branchname"]).Value) : string.Empty;
                    string customerId = payment.Contains("acc.afg_lplus_customerid") ? Convert.ToString(((AliasedValue)payment["acc.afg_lplus_customerid"]).Value) : string.Empty;

                    NACHAEntry entry = new NACHAEntry(paymentName, accountName, customerId, paymentType, amount, reasonOfPayment, notes, bankName, routingNumber, accountNumber, branch);
                    if (entry != null)
                    {
                        listOfEntries.Add(entry);
                    }
                }
            }
            return listOfEntries;
        }

        public void CreateNACHAFile(IOrganizationService service, ITracingService tracingService, List<NACHAEntry> listOfEntries, Guid batchId, string batchNumber)
        {
            string batchNumberFormatted = GetStringOfLength(8, batchNumber, "0");
            string fileName = $"ACH_{GenerateRandomAlphanumericString()}.txt";
            MemoryStream stream = new MemoryStream();
            byte[] bytes = null;

            #region Batch File Header Configuration       
            ChoNACHAConfiguration config = new ChoNACHAConfiguration();
            config.EntryDetailTraceSource = ChoEntryDetailTraceSource.OriginatingDFI;
            config.ErrorMode = ChoErrorMode.IgnoreAndContinue;
            config.BatchNumber = 0000001;
            config.DestinationBankRoutingNumber = "0071006486";
            config.OriginatingCompanyId = "2330805823";
            config.DestinationBankName = "CIBC BANK USA";
            config.OriginatingCompanyName = "ALLIANCE FUNDING GROUP";
            config.BlockingFactor = 10;
            config.TurnOffDestinationBankRoutingNumber = true;
            config.TurnOffOriginatingCompanyIdValidation = true;

            ChoActivator.Factory = (t, args) =>
            {
                if (t == typeof(ChoNACHAFileHeaderRecord))
                {
                    tracingService.Trace($"Processing File Header");
                    var header = new ChoNACHAFileHeaderRecord();
                    header.Initialize();
                    header.PriorityCode = "01";
                    header.FileCreationDate = DateTime.Today;
                    header.FileCreationTime = DateTime.Now.ToLocalTime();
                    header.RecordTypeCode = ChoRecordTypeCode.FileHeader;
                    header.ImmediateDestination = "0071006486";
                    header.ImmediateOrigin = "2330805823";
                    header.FileIDModifier = Convert.ToChar("A");
                    header.RecordSize = Convert.ToUInt16(094);
                    header.FormatCode = Convert.ToUInt16(1);
                    header.ImmediateDestinationName = "CIBC BANK USA";
                    header.ImmediateOriginName = "ALLIANCE FUNDING GROUP";
                    header.ReferenceCode = batchNumberFormatted;
                    tracingService.Trace($"File Header Completed");
                    return header;
                }

                if (t == typeof(ChoNACHABatchHeaderRecord))
                {
                    tracingService.Trace($"Processing Batch Header");
                    var header = new ChoNACHABatchHeaderRecord();
                    header.Initialize();
                    header.RecordTypeCode = ChoRecordTypeCode.BatchHeader;
                    header.ServiceClassCode = 200;
                    header.CompanyName = "ALLIANCE FUNDING GROUP";
                    header.CompanyDiscretionaryData = string.Empty;
                    header.CompanyID = "2330805823";
                    header.StandardEntryClassCode = "CCD";
                    header.CompanyEntryDescription = "CNTRCT PMT";
                    header.CompanyDescriptiveDate = $"PMT{DateTime.Today.ToString("yyMMdd")}";
                    header.EffectiveEntryDate = DateTime.Now.AddDays(1);
                    header.OriginatorStatusCode = Convert.ToChar("1");
                    header.OriginatingDFIID = "07100648";
                    header.BatchNumber = 0000001;
                    tracingService.Trace($"Batch Header Completed");
                    return header;
                }
                if (t == typeof(ChoNACHAAddendaRecord))
                {
                    tracingService.Trace($"Processing Entry Detail Record");
                    var header = new ChoNACHAAddendaRecord();
                    header.Initialize();
                    header.AddendaTypeCode = 05;
                    header.RecordTypeCode = ChoRecordTypeCode.Addenda;
                    tracingService.Trace($"Entry Detail Completed");
                    return header;
                }
                return null;
            };

            #endregion

            using (ChoNACHAWriter nachaWriter = new ChoNACHAWriter(stream, config))
            {
                tracingService.Trace($"Processing file starting..");
                int batchServiceClassCode = 200;
                nachaWriter.Configuration.BatchNumber = Convert.ToUInt32(batchNumberFormatted);
                nachaWriter.Configuration.ErrorMode = ChoErrorMode.IgnoreAndContinue;
                using (ChoNACHABatchWriter batchWriter = nachaWriter.CreateBatch(batchServiceClassCode, standardEntryClassCode: "CCD",
                    companyEntryDescription: "CNTRCT PMT",
                    companyDescriptiveDate: DateTime.Today, effectiveEntryDate: DateTime.Now.AddDays(1)))
                {
                    tracingService.Trace($"Batch start writing");

                    for (int iteration = 0; iteration < listOfEntries.Count; iteration++)
                    {
                        NACHAEntry entry = listOfEntries[iteration];
                        string individualIDNumber = !string.IsNullOrEmpty(entry.CustomerId) ? WithMaxLength($"CUST# {entry.CustomerId}", 15) : $"CUST# 0000";
                        string accountName = !string.IsNullOrEmpty(entry.AccountName) ? WithMaxLength(entry.AccountName, 22) : $"NOT FOUND";
                        ChoNACHAEntryDetailWriter debitEntry = batchWriter.CreateDebitEntryDetail(27,
                            entry.RoutingNumber, entry.AccountNumber, entry.Amount, string.Empty,
                            accountName, string.Empty);
                        debitEntry.IsDebit = true;
                        string paymentRelatedInformation = $"CO# 0{iteration+1} CUST #{entry.CustomerId}   CNTRCT# {entry.CustomerId}           TRAN# {GetStringOfLength(9,Convert.ToString(iteration+1), "0")} BATCH# {GetStringOfLength(9, batchNumber, "0")}";
                        debitEntry.CreateAddendaRecord(paymentRelatedInformation, 05);
                        debitEntry.Close();
                        tracingService.Trace($"Entry added & closed");
                    }
                    batchWriter.Close();
                }
                nachaWriter.Close();
                bytes = stream.ToArray();
                tracingService.Trace($"Byte Array length upper {bytes.Length}");
            }

            var base64OfFile = Convert.ToBase64String(bytes); //converted byte array to base64
            if (!string.IsNullOrEmpty(base64OfFile))
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
                Guid noteId = service.Create(note);
                tracingService.Trace("Note created succfully!");
            }
            else
            {
                tracingService.Trace("Unable to create Note in CRM, base64 is null");
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
                    <link-entity name='account' from='accountid' to='afg_account' visible='false' link-type='outer' alias='acc'>
                        <attribute name='afg_lplus_customerid' />
                        <attribute name='name' />
                    </link-entity>
                </entity>
            </fetch>";
            batchedPaymentsXml = string.Format(batchedPaymentsXml, batchId);
            EntityCollection collection = orgService.RetrieveMultiple(new FetchExpression(batchedPaymentsXml));
            return collection;
        }

        public string GetBatchNumber(ITracingService tracingService, IOrganizationService orgService, Guid batchId)
        {
            Entity batch = orgService.Retrieve("afg_batch", batchId, new ColumnSet("afg_batchnumber"));
            if (batch != null)
            {
                string batchNumber = (batch.Contains("afg_batchnumber") && batch["afg_batchnumber"] != null) ? batch["afg_batchnumber"].ToString() : "2022";
                tracingService.Trace($"Retrieved Batch Number {batchNumber}");
                return batchNumber;
            }
            return "2022";
        }

        public string GenerateRandomAlphanumericString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var random = new Random();
            var randomString = new string(Enumerable.Repeat(chars, length)
                                                    .Select(s => s[random.Next(s.Length)]).ToArray());
            return randomString;
        }

        public string WithMaxLength(string value, int maxLength)
        {
            return value?.Substring(0, Math.Min(value.Length, maxLength));
        }
        
        public string GetStringOfLength(int totalLength, string input, string charcterToAppend)
        {
            return input.PadLeft(totalLength, Convert.ToChar(charcterToAppend));
        }
    }
}