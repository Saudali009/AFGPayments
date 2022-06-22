using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace System.AFG.Payments.GIACTAccountVerification
{
    public class WebRequestHandler
    {
        public void VerifyAccount(AccountForVerificationDTO request)
        {
            var url = "https://sandbox.api.giact.com/verificationservices/web_api/inquiries_v5_9";

            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Method = "POST";
            string username = "myusername";
            string pass = "myusername";
            httpRequest.Accept = "application/json";
            httpRequest.ContentType = "application/json";
            string credidentials = username + ":" + pass;
            string token = Convert.ToBase64String(Encoding.Default.GetBytes(credidentials));
            httpRequest.Headers.Add("Authorization", "Bearer " + token);

            var data = @"{
                          ""UniqueID"": ""12346"",
                          ""ServiceFlags"": [
                            ""authenticate""
                          ],
                          ""BankAccountEntity"": {
                            ""RoutingNumber"": ""122105278"",
                            ""AccountNumber"": ""0000000025"",
                            ""AccountType"": ""checking""
   
                          },
  
                          ""BusinessEntity"": {
                            ""BusinessName"": ""GIACT SYSTEMS, LLC"",
                            ""AddressEntity"": {
                              ""AddressLine1"": ""700 Central Expy S"",
                              ""AddressLine2"": ""Suite 300"",
                              ""City"": ""Allen"",
                              ""State"": ""TX"",
                              ""ZipCode"": ""75013"",
                              ""Country"": ""US""
                            },
                            ""PhoneNumber"": ""2146440450""    
                          }
                        }";

            using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
            {
                streamWriter.Write(data);
            }

            var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
           
        }
    }
}
