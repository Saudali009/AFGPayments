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

                EntityCollection batchePayments = GetBatchedPayments(orgService, batchId);
                List<NACHAEntry> listOfEntries = null;

                if (batchePayments != null && batchePayments.Entities.Count > 0)
                {
                    listOfEntries = new List<NACHAEntry>();
                    foreach (Entity payment in batchePayments.Entities)
                    {
                        string paymentName = Convert.ToString(payment[PaymentAttributes.PaymentName]);
                        string accountName = ((EntityReference)payment[PaymentAttributes.AccountName]).Name;
                        int paymentType = Convert.ToInt32(payment[PaymentAttributes.PaymentType]);
                        decimal amount = ((Money)payment[PaymentAttributes.Amount]).Value;
                        string reasonOfPayment = Convert.ToString(payment[PaymentAttributes.ReasonOfPayment]);
                        string notes = Convert.ToString(payment[PaymentAttributes.PaymentNotes]);
                        string bankName = ((EntityReference)payment[PaymentAttributes.BankName]).Name;
                        string routingNumber = Convert.ToString((AliasedValue)payment[$"br.{PaymentAttributes.RoutingNumber}"]);
                        string accountNumber = Convert.ToString((AliasedValue)payment[$"br.{PaymentAttributes.AccountNumber}"]);
                        string branch = Convert.ToString((AliasedValue)payment[$"br.{PaymentAttributes.BranchName}"]);

                        tracingService.Trace($"Error encountered while creating NACHA file {paymentName}{accountName}{paymentType}{amount}{reasonOfPayment}{notes}{bankName}{routingNumber}{accountNumber}{branch}");
                        listOfEntries.Add(new NACHAEntry(paymentName, accountName, paymentType, amount, reasonOfPayment, notes, bankName, routingNumber, accountNumber, branch));
                    }
                }

                if (listOfEntries != null && listOfEntries.Count > 0)
                {
                    tracingService.Trace($"Entries are ready for creation with total: {listOfEntries.Count}");
                    CreateNACHAFile(tracingService, listOfEntries);
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error encountered while creating NACHA file {ex}");
            }
        }

        public EntityCollection GetBatchedPayments(IOrganizationService orgService, Guid batchId)
        {
            string batchedPaymentsXml = $@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='afg_payment'>
                <attribute name='afg_paymentid' />
                <attribute name='afg_reasonofpayment' />
                <attribute name='afg_paymenttype' />
                <attribute name='afg_note' />
                <attribute name='afg_bankrelation' />
                <attribute name='afg_amount' />
                <attribute name='afg_account' />
                < filter type = 'and' >
                    < condition attribute = 'afg_batch' operator= 'eq' value = '{batchId}' />
                </ filter >
                <link-entity name='tf_bankrelationship' from='tf_bankrelationshipid' to='afg_bankrelation' visible='false' link-type='outer' alias='br'>
                  <attribute name='afg_routingnumber' />
                  <attribute name='tf_branchname' />
                  <attribute name='tf_bankbranchid' />
                  <attribute name='tf_accountno' />
                  <attribute name='tf_accountname' />
                </link-entity>
              </entity>
            </fetch>";

            EntityCollection collection = orgService.RetrieveMultiple(new FetchExpression(batchedPaymentsXml));
            return collection;
        }

        public void CreateNACHAFile(ITracingService tracingService, List<NACHAEntry> listOfEntries)
        {
            tracingService.Trace("Trying to create NACHA file with entries.");
            ChoNACHAConfiguration config = new ChoNACHAConfiguration();
            config.BatchNumber = Convert.ToUInt32("1001");
            config.DestinationBankRoutingNumber = "123456789";
            config.OriginatingCompanyId = "123456789";
            config.DestinationBankName = "BANK USA";
            config.OriginatingCompanyName = "ALLIANCE FUNDING GROUP";
            config.ReferenceCode = "ALLIANCE FUNDING GROUP LLC.";

            string fileName = $"ACH_{DateTime.Now.Millisecond}.txt";
            ChoNACHAWriter nachaWriter = new ChoNACHAWriter(fileName, config);
            ChoNACHABatchWriter batchWriter = nachaWriter.CreateBatch(200);

            foreach (NACHAEntry entry in listOfEntries)
            {
               batchWriter.CreateDebitEntryDetail(20, entry.RoutingNumber, entry.AccountNumber,
               entry.Amount, entry.Bank, entry.AccountName, entry.PaymentName)
               .CreateAddendaRecord(entry.ReasonOfPayment);
            }

            batchWriter.Close();
            batchWriter.Dispose();
            nachaWriter.Close();
            nachaWriter.Dispose();
        }
    }
}
