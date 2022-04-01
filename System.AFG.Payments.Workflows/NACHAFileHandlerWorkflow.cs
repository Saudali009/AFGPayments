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
                Entity batchDetails = CRMDataRetrievalHandler.GetBatchDetails(tracingService, orgService, batchId);
                string batchNumber = string.Empty;
                int bankCode = (int)BanksEnum.CIBC;

                if (batchDetails != null)
                {
                    batchNumber = batchDetails.Contains("afg_batchnumber") && batchDetails["afg_batchnumber"] != null ? batchDetails["afg_batchnumber"].ToString() : "2022";
                    bankCode = batchDetails.Contains("afg_bank") && batchDetails["afg_bank"] != null ? ((OptionSetValue)batchDetails["afg_bank"]).Value : (int)BanksEnum.CIBC;

                    if (!bankCode.IsNull())
                    {
                        EntityCollection configuration = CRMDataRetrievalHandler.GetNACHAConfiguration(tracingService, orgService, bankCode);
                        if (configuration != null && configuration.Entities.Count > 0)
                        {
                            Entity NACHAConfig = configuration.Entities[0];
                            EntityCollection paymentCollection = CRMDataRetrievalHandler.GetBatchedPayments(tracingService, orgService, batchId);
                            List<NACHAEntry> listOfEntries = CRMDataRetrievalHandler.GetListOfNachaEntries(tracingService, paymentCollection);
                            if (listOfEntries != null && listOfEntries.Count > 0)
                            {
                                CreateNACHAFile(orgService, tracingService, listOfEntries, bankCode, batchId, batchNumber, NACHAConfig);
                            }
                            else
                            {
                                throw new InvalidPluginExecutionException($"No associated payment found with batch.");
                            }
                        }
                        else
                        {
                            throw new InvalidPluginExecutionException($"NACHA file configuration is missing. Please provide NACHA configuration for file creation.");
                        }
                    }
                    else
                    {
                        throw new InvalidPluginExecutionException($"Bank is not associated with Batch for NACHA file creation.");
                    }
                }
                else
                {
                    throw new InvalidPluginExecutionException($"Batch Details not found. Please provide associated batch.");
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error encountered while creating NACHA file {ex}");
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }

        public void CreateNACHAFile(IOrganizationService service, ITracingService tracingService, List<NACHAEntry> listOfEntries, int bankCode, Guid batchId, string batchNumber, Entity NACHAConfig)
        {
            string batchNumberFormatted = GeneralUtilities.GetStringOfLength(8, batchNumber, "0");
            string fileName = $"ACH_{ GeneralUtilities.GenerateRandomAlphanumericString()}.ACH";
            DateTime calculatedEffectiveDate = GeneralUtilities.GetNextWorkingDay(tracingService, service);

            byte[] bytes = null;
            using (MemoryStream stream = new MemoryStream())
            {
                #region Batch File Header Configuration       
                ChoNACHAConfiguration config = new ChoNACHAConfiguration();
                config.EntryDetailTraceSource = ChoEntryDetailTraceSource.OriginatingDFI;
                config.ErrorMode = ChoErrorMode.IgnoreAndContinue;
                config.BatchNumber = 0000001;
                string routingNumber = NACHAConfig.Contains("afg_destinationbankroutingnumber") && NACHAConfig["afg_destinationbankroutingnumber"] != null ? Convert.ToString(NACHAConfig["afg_destinationbankroutingnumber"]) : string.Empty;
                config.DestinationBankRoutingNumber = bankCode == (int)BanksEnum.CIBC ? routingNumber : routingNumber.PadLeft(routingNumber.Length + 1, Convert.ToChar(" "));
                config.OriginatingCompanyId = NACHAConfig.Contains("afg_originatingcompanyid") && NACHAConfig["afg_originatingcompanyid"] != null ? Convert.ToString(NACHAConfig["afg_originatingcompanyid"]) : string.Empty;
                config.DestinationBankName = NACHAConfig.Contains("afg_destinationbankname") && NACHAConfig["afg_destinationbankname"] != null ? Convert.ToString(NACHAConfig["afg_destinationbankname"]) : string.Empty;
                config.OriginatingCompanyName = NACHAConfig.Contains("afg_companyname") && NACHAConfig["afg_companyname"] != null ? Convert.ToString(NACHAConfig["afg_companyname"]) : string.Empty;
                config.BlockingFactor = NACHAConfig.Contains("afg_blockingfactor") && NACHAConfig["afg_blockingfactor"] != null ? Convert.ToUInt32(NACHAConfig["afg_blockingfactor"]) : 10;
                config.TurnOffDestinationBankRoutingNumber = true;
                config.TurnOffOriginatingCompanyIdValidation = true;
                config.ReferenceCode = batchNumberFormatted;

                ChoActivator.Factory = (t, args) =>
                {
                    if (t == typeof(ChoNACHAFileHeaderRecord))
                    {
                        var header = new ChoNACHAFileHeaderRecord();
                        header.Initialize();
                        header.PriorityCode = "01";
                        header.FileCreationDate = DateTime.Today;
                        header.FileCreationTime = DateTime.Now.ToLocalTime();
                        header.RecordTypeCode = ChoRecordTypeCode.FileHeader;
                        header.ImmediateDestination = NACHAConfig.Contains("afg_destinationbankroutingnumber") && NACHAConfig["afg_destinationbankroutingnumber"] != null ? Convert.ToString(NACHAConfig["afg_destinationbankroutingnumber"]) : string.Empty;
                        header.ImmediateOrigin = NACHAConfig.Contains("afg_originatingcompanyid") && NACHAConfig["afg_originatingcompanyid"] != null ? Convert.ToString(NACHAConfig["afg_originatingcompanyid"]) : string.Empty;
                        header.FileIDModifier = Convert.ToChar("A");
                        header.RecordSize = Convert.ToUInt16(094);
                        header.FormatCode = Convert.ToUInt16(1);
                        header.ImmediateDestinationName = NACHAConfig.Contains("afg_destinationbankname") && NACHAConfig["afg_destinationbankname"] != null ? Convert.ToString(NACHAConfig["afg_destinationbankname"]) : string.Empty;
                        header.ImmediateOriginName = NACHAConfig.Contains("afg_companyname") && NACHAConfig["afg_companyname"] != null ? Convert.ToString(NACHAConfig["afg_companyname"]) : string.Empty;
                        header.ReferenceCode = batchNumberFormatted;
                        return header;
                    }

                    if (t == typeof(ChoNACHABatchHeaderRecord))
                    {
                        var header = new ChoNACHABatchHeaderRecord();
                        header.Initialize();
                        header.RecordTypeCode = ChoRecordTypeCode.BatchHeader;
                        header.ServiceClassCode = 200;
                        header.CompanyName = NACHAConfig.Contains("afg_companyname") && NACHAConfig["afg_companyname"] != null ? Convert.ToString(NACHAConfig["afg_companyname"]) : string.Empty;
                        header.CompanyDiscretionaryData = string.Empty;
                        header.CompanyID = NACHAConfig.Contains("afg_originatingcompanyid") && NACHAConfig["afg_originatingcompanyid"] != null ? Convert.ToString(NACHAConfig["afg_originatingcompanyid"]) : string.Empty;
                        header.StandardEntryClassCode = "CCD";
                        header.CompanyEntryDescription = "CNTRCT PMT";
                        header.CompanyDescriptiveDate = $"PMT{DateTime.Today.ToString("yyMMdd")}";
                        header.EffectiveEntryDate = calculatedEffectiveDate; // DateTime.Now.AddDays(1);
                        header.OriginatorStatusCode = Convert.ToChar("1");
                        header.OriginatingDFIID = NACHAConfig.Contains("afg_destinationbankroutingnumber") && NACHAConfig["afg_destinationbankroutingnumber"] != null ? Convert.ToString(NACHAConfig["afg_destinationbankroutingnumber"]) : string.Empty;
                        header.BatchNumber = 0000001;
                        return header;
                    }
                    if (t == typeof(ChoNACHAAddendaRecord))
                    {
                        var header = new ChoNACHAAddendaRecord();
                        header.Initialize();
                        header.AddendaTypeCode = 05;
                        header.RecordTypeCode = ChoRecordTypeCode.Addenda;
                        return header;
                    }
                    return null;
                };

                #endregion

                using (ChoNACHAWriter nachaWriter = new ChoNACHAWriter(stream, config))
                {
                    int batchServiceClassCode = 200;
                    nachaWriter.Configuration.BatchNumber = Convert.ToUInt32(batchNumberFormatted);
                    nachaWriter.Configuration.ErrorMode = ChoErrorMode.IgnoreAndContinue;
                    using (ChoNACHABatchWriter batchWriter = nachaWriter.CreateBatch(batchServiceClassCode, standardEntryClassCode: "CCD",
                        companyEntryDescription: "CNTRCT PMT",
                        companyDescriptiveDate: DateTime.Today, effectiveEntryDate: calculatedEffectiveDate))
                    {
                        for (int iteration = 0; iteration < listOfEntries.Count; iteration++)
                        {
                            NACHAEntry entry = listOfEntries[iteration];
                            string accountName = !string.IsNullOrEmpty(entry.AccountName) ? GeneralUtilities.WithMaxLength(entry.AccountName, 22) : $"NOT FOUND";
                            string contractId = string.IsNullOrEmpty(entry.ContractNumber) ? entry.CustomerId : entry.ContractNumber;
                            string paymentRelatedInformation = $"CO# 0{iteration + 1} CUST #{entry.CustomerId}  CNTRCT# {contractId} TRAN# { GeneralUtilities.GetStringOfLength(9, Convert.ToString(iteration + 1), "0")} BATCH# { GeneralUtilities.GetStringOfLength(9, batchNumber, "0")}";

                            #region Add Debit Entry in NACHA file
                            ChoNACHAEntryDetailWriter debitEntry = null;
                            if (bankCode == (int)BanksEnum.CIBC)
                            {
                                debitEntry = batchWriter.CreateDebitEntryDetail(27,
                                entry.RoutingNumber, entry.AccountNumber, entry.Amount, string.Empty,
                                accountName, string.Empty);
                            }
                            else if (bankCode == (int)BanksEnum.FithThird)
                            {
                                debitEntry = batchWriter.CreateDebitEntryDetail(27,
                                entry.RoutingNumber, entry.AccountNumber, entry.Amount, $"-{ contractId}",
                                accountName, string.Empty);
                            }
                            debitEntry.IsDebit = true;
                            debitEntry.CreateAddendaRecord(paymentRelatedInformation, 05);
                            debitEntry.Close();
                            #endregion
                        }
                        batchWriter.Close();
                    }
                    nachaWriter.Close();
                    bytes = stream.ToArray();
                }
            }
            var base64OfFile = Convert.ToBase64String(bytes);
            if (!string.IsNullOrEmpty(base64OfFile))
            {
                CRMDataRetrievalHandler.CreateNote(tracingService, service, batchId, fileName, base64OfFile);
            }
            else
            {
                tracingService.Trace("Unable to create Note in CRM, base64 is null");
            }
        }
    }
}