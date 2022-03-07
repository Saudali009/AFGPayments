using ChoETL.NACHA;
using System;
using System.Collections.Generic;
using System.IO;
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
                //var fileReaderJson = Newtonsoft.Json.JsonConvert.SerializeObject(fileReader); // added Newtonsoft for converting object to Json 
                byte[] fileByteArray = stream.ToArray(); //converted Json object to byte array

                var base64OfFile = Convert.ToBase64String(fileByteArray); //converted byte array to base64

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Occured While Creating Batch: {ex.Message}");
                Console.ReadKey();
            }
        }
    }
}

