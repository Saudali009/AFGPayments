using ChoETL.NACHA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//reuire namespaces
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;
using System.Net;

namespace ACH_Console_App
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                /**
                ChoNACHAConfiguration config = new ChoNACHAConfiguration();
                config.BatchNumber = 2022;
                config.DestinationBankRoutingNumber = "0071006486";
                config.OriginatingCompanyId = "1234567892";
                config.DestinationBankName = "CIBC BANK USA";
                config.OriginatingCompanyName = "ALLIANCE FUNDING GROUP";
                config.ReferenceCode = "ALLIANCE FUNDING GROUP LLC.";

                string fileName = $"ACH_{DateTime.Now.Millisecond}.txt";
                MemoryStream stream = new MemoryStream();
                using (ChoNACHAWriter nachaWriter = new ChoNACHAWriter(stream, config))
                {
                    using (ChoNACHABatchWriter batchWriter = nachaWriter.CreateBatch(200))
                    {
                        int transactionCode = 20;
                        string routingNumber = "007029391";
                        string accountNumber = "012107029391";
                        decimal paymentAmount = 120.55M;
                        string individualIDNumber = "CUST# 1224";
                        string individualIDName = "Saud Ali";


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
                var fileReaderJson = Newtonsoft.Json.JsonConvert.SerializeObject(fileReader); 
                byte[] fileByteArray = stream.ToArray(); 

                var base64OfFile = Convert.ToBase64String(fileByteArray); //converted byte array to base64
                **/
                DateTime ss = DateTime.Today.Date;
                string test = GetStringOfLength(8, "0012", "0");
                ulong sss = Convert.ToUInt64("0000001");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occured While Creating Batch: {ex.Message}");
                Console.ReadKey();
            }
        }

        public static string GetStringOfLength(int totallength, string input, string charcterToAppend) {
            return input.PadLeft(totallength, Convert.ToChar(charcterToAppend));
        }
        public static void WriteNumbers(int digits, int number = 0)
        {
            int i = (number % 10) + 1;
            number *= 10;
            for (; i <= 9; i++)
            {
                if (digits == 1)
                    Console.WriteLine(number + i);
                else
                    WriteNumbers(digits - 1, number + i);
            }
        }
        /**
        public static IOrganizationService ConnectD35OnlineUsingOrgSvc()
        {
            IOrganizationService organizationService = null;

            String username = "<username";
            String password = "<your password>";

            String url = "https://<org>.api.crm.dynamics.com/XRMServices/2011/Organization.svc";
            try
            {
                ClientCredentials clientCredentials = new ClientCredentials();
                clientCredentials.UserName.UserName = username;
                clientCredentials.UserName.Password = password;

                // For Dynamics 365 Customer Engagement V9.X, set Security Protocol as TLS12
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                organizationService = (IOrganizationService)new OrganizationServiceProxy(new Uri(url), null, clientCredentials, null);

                if (organizationService != null)
                {
                    Guid gOrgId = ((WhoAmIResponse)organizationService.Execute(new WhoAmIRequest())).OrganizationId;
                    if (gOrgId != Guid.Empty)
                    {
                        Console.WriteLine("Connection Established Successfully...");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to Established Connection!!!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured - " + ex.Message);
            }
            return organizationService;

        }
    **/
    }
}

