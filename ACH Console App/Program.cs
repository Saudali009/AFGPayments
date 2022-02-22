using ChoETL.NACHA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACH_Console_App
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ChoNACHAConfiguration config = new ChoNACHAConfiguration();
                config.BatchNumber = Convert.ToUInt32("1001");
                config.DestinationBankRoutingNumber = "123456789";
                config.OriginatingCompanyId = "123456789";
                config.DestinationBankName = "BANK USA";
                config.OriginatingCompanyName = "ALLIANCE FUNDING GROUP";
                config.ReferenceCode = "ALLIANCE FUNDING GROUP LLC.";

                string fileName = $"ACH_{DateTime.Now.Millisecond}.txt";
                using (ChoNACHAWriter nachaWriter = new ChoNACHAWriter(fileName, config))
                {
                    using (ChoNACHABatchWriter batchWriter = nachaWriter.CreateBatch(200))
                    {
                        int transactionCode = 20;
                        string routingNumber = "007029391";
                        string accountNumber = "012107029391";
                        decimal paymentAmount = 120.55M;
                        string individualIDNumber = "CUST# 1224";
                        string individualIDName = "Saud Ali";
                        ChoNACHAEntryDetailWriter creditEntry = batchWriter.CreateDebitEntryDetail(transactionCode, routingNumber, accountNumber,
                            paymentAmount, individualIDNumber, individualIDName, "");
                        creditEntry.CreateAddendaRecord("HOME BUILDING MATERIAL");
                        creditEntry.Close();

                        ChoNACHAEntryDetailWriter creditEntry2 = batchWriter.CreateDebitEntryDetail(transactionCode, routingNumber, accountNumber,
                           paymentAmount, individualIDNumber, individualIDName, "");
                        creditEntry2.CreateAddendaRecord("MAJOR CLEANUP INC. ");
                        creditEntry2.Close();

                        batchWriter.Close();
                        batchWriter.Dispose();
                    }
                    nachaWriter.Close();
                    nachaWriter.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occured While Creating Batch: {ex.Message}");
                Console.ReadKey();
            }
        }
    }
}

