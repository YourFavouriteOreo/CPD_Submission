using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;
using CPD_Coursework_2.Models;
using System.Diagnostics;

namespace CPD_Coursework_2.Migrations
{
    public class InitializeSamples
    {
        public static void go()
        {
            const String partitionName = "Samples_Partition_1";

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("Samples");

            // If table doesn't already exist in storage then create and populate it with some initial values, otherwise do nothing
            if (!table.Exists())
            {
                // Create table if it doesn't exist already
                table.CreateIfNotExists();

                // Create the batch operation.
                TableBatchOperation batchOperation = new TableBatchOperation();

                // Create a sample entity and add it to the table.
                SampleEntity sample1 = new SampleEntity(partitionName, "1");
                sample1.Title = "randomTitle";
                sample1.CreatedDate = null;
                sample1.SampleDate = null;

                SampleEntity sample2 = new SampleEntity(partitionName, "2");
                sample2.Title = "randomTitle2";
                sample2.CreatedDate = null;
                sample2.SampleDate = null;



                // Add sample entities to the batch insert operation.
                batchOperation.Insert(sample1);
                batchOperation.Insert(sample2);


                // Execute the batch operation.
                try
                {
                    table.ExecuteBatch(batchOperation);
                }
                catch(StorageException e)
                {
                    Debug.WriteLine(e.RequestInformation.ExtendedErrorInformation.ErrorMessage);
                    table.Delete();
                }
            }

        }
    }
}