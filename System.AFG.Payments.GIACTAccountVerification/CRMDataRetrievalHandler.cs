using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.AFG.Payments.GIACTAccountVerification
{
    public class CRMDataRetrievalHandler
    {
        public static AccountForVerificationDTO GetAccountDetails(ITracingService tracingService, IOrganizationService orgService, Guid paymentId)
        {
            AccountForVerificationDTO accountForVerification = new AccountForVerificationDTO();
            string accountDetailsXml = @"<fetch>
                                        <entity name='afg_payment' >
                                            <filter>
                                                <condition attribute='afg_paymentid' operator='eq' value='{0}' />
                                            </filter>
                                            <link-entity name='tf_bankrelationship' from='tf_bankrelationshipid' to='afg_bankrelation' alias='bnk' >
                                                <attribute name='afg_routingnumber' />
                                                <attribute name='tf_accountno' />
                                                <attribute name='tf_accounttypewithoptions' />
                                            </link-entity>
                                        </entity>
                                    </fetch>>";
            accountDetailsXml = string.Format(accountDetailsXml, paymentId);
            EntityCollection collection = orgService.RetrieveMultiple(new FetchExpression(accountDetailsXml));

            //Check If collection is not null and details are available
            if (collection != null && collection.Entities.Count > 0)
            {
                int accountType = 0;
                Entity account = collection.Entities[0];
                accountForVerification.GVerifyEnabled = "true";
                accountForVerification.UniqueId = CommonUtilities.GetUniqueNumber();
                accountForVerification.RoutingNumber = account.Contains("bnk.afg_routingnumber") && account["bnk.afg_routingnumber"] != null ? Convert.ToString(((AliasedValue)account["bnk.afg_routingnumber"]).Value) : string.Empty;
                accountForVerification.AccountNumber = account.Contains("bnk.tf_accountno") && account["bnk.tf_accountno"] != null ? Convert.ToString(((AliasedValue)account["bnk.tf_accountno"]).Value) : string.Empty;
                AliasedValue accountTypeAlias = account.GetAttributeValue<AliasedValue>("bnk.tf_accounttypewithoptions");
                if (accountTypeAlias != null)
                {
                    accountType = ((accountTypeAlias.Value) as OptionSetValue).Value;
                }
                accountForVerification.AccountType = GetAccountType(accountType);
                return accountForVerification;
            }
            return null;
        }

        private static string GetAccountType(int value)
        {
            if (value == 0)
            {
                return "";
            }
            else if (value == 100000001)
            {
                return "Savings";
            }
            else
            {
                return "Checking";
            }
        }
    }
}
