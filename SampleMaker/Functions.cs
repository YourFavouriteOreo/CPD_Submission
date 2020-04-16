using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure.WebJobs;
using CPD_Coursework_2.Models;
using System.Configuration;
using NReco.VideoConverter;

namespace SampleMaker
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        // This class contains the application-specific WebJob code consisting of event-driven
        // methods executed when messages appear in queues with any supporting code.

        // Trigger method  - run when new message detected in queue. "thumbnailmaker" is name of queue.
        // "photogallery" is name of storage container; "images" and "thumbanils" are folder names. 
        // "{queueTrigger}" is an inbuilt variable taking on value of contents of message automatically;
        // the other variables are valued automatically.
        public static void GenerateSample(
        [QueueTrigger("samples")] SampleEntity sampleEntity , TextWriter logger)
        {
            //use log.WriteLine() rather than Console.WriteLine() for trace output
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            logger.WriteLine("GenerateThumbnail() started...");
            logger.WriteLine(sampleEntity.RowKey);
            CloudTable table = storageAccount.CreateCloudTableClient().GetTableReference("Samples");
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer videoContainer = blobClient.GetContainerReference("original");
            videoContainer.CreateIfNotExists();
            TableOperation retriveOperation = TableOperation.Retrieve<SampleEntity>("Samples_Partition_1", sampleEntity.RowKey); ;

            TableResult getOperationResult = table.Execute(retriveOperation);

            if (getOperationResult.Result == null)
            {
                return;
            }

            SampleEntity sample = (SampleEntity)getOperationResult.Result;
            CloudBlockBlob blob = videoContainer.GetBlockBlobReference(sample.Mp4Blob);

            logger.WriteLine(sample.SampleMp4URL);
            string fileName = Guid.NewGuid().ToString();

            CloudBlockBlob outputBlob = videoContainer.GetBlockBlobReference(fileName);

            using (Stream input = blob.OpenRead())
            using (Stream output = outputBlob.OpenWrite())
            {
                CropVideo(input, output);
                outputBlob.Properties.ContentType = "video/mp4";
            }
            
            
            sample.SampleDate = DateTime.Now;
            sample.SampleMp4Blob = fileName;
            TableOperation tableOp = TableOperation.Replace(sample);
            table.Execute(tableOp);
            logger.WriteLine("GenerateVideo() completed");
        }

        // Create thumbnail - the detail is unimportant but notice formal parameter types.
        public static void CropVideo(Stream input, Stream output)
        {
            BinaryWriter Writer = null;
            try
            {
                // Create a new stream to write to the file
                Writer = new BinaryWriter(File.Open("temp.mp4", FileMode.Create));
                BinaryReader Reader = new BinaryReader(input);
                byte[] imageBytes = null;
                imageBytes = Reader.ReadBytes((int)input.Length);
                // Writer raw data                
                Writer.Write(imageBytes);
                Writer.Flush();
                Writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("*** FileWrite exception: " + e.Message);

            }

            var vid_duration = new ConvertSettings();
            vid_duration.MaxDuration = 5;

            var ffMpeg = new FFMpegConverter();
            ffMpeg.ConvertMedia("temp.mp4", "mp4", output, "mp4", vid_duration);
        }
    }
}
