using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Workflow;
using System.Activities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System.IO;

namespace System.AFG.Payments.GIACTAccountVerification
{
    public class GIACTAccountVerification : CodeActivity
    {
        protected override void Execute(CodeActivityContext context)
        {
            IWorkflowContext workflowContext = context.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = context.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService orgService = serviceFactory.CreateOrganizationService(workflowContext.UserId);
            ITracingService tracingService = context.GetExtension<ITracingService>();

            try
            {
                Guid paymentId = workflowContext.PrimaryEntityId;
                string entityName = workflowContext.PrimaryEntityName;
                if(entityName == "afg_payment")
                {
                    tracingService.Trace($"Code Activity triggered successfully!");
                    AccountForVerificationDTO _accountDetails = CRMDataRetrievalHandler.GetAccountDetails(tracingService, orgService, paymentId);
                    if(_accountDetails != null)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Error encountered while GIACT Account Verification {ex}");
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}
