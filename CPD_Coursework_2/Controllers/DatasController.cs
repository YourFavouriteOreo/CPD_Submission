using CPD_Coursework_2.Models;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace CPD_Coursework_2.Controllers
{

    public class DatasController : ApiController
    {
        private const String partitionName = "Samples_Partition_1";
        CloudStorageAccount storageAccount;
        CloudBlobClient blobClient;
        private CloudTableClient tableClient;
        private CloudTable table;
        CloudQueueClient queueClient;
        CloudQueue queue;
        CloudBlobContainer videoContainer;

        public DatasController()
        {
            // Setup all storage related values
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("Samples");
            queueClient = storageAccount.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference("samples");
            queue.CreateIfNotExists();
            blobClient = storageAccount.CreateCloudBlobClient();
            videoContainer = blobClient.GetContainerReference("original");
            videoContainer.CreateIfNotExists();


        }

        // GET: api/Datas/5
        [HttpGet]
        [Route("api/datas/{id}")]
        public HttpResponseMessage Get(string id)
        {            
            //Get the sample video file from sample ID passed by user
            TableOperation retriveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id); ;

            TableResult getOperationResult = table.Execute(retriveOperation);

            if (getOperationResult.Result == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            SampleEntity sample = (SampleEntity)getOperationResult.Result;
            CloudBlockBlob blob = videoContainer.GetBlockBlobReference(sample.SampleMp4Blob);
            Stream blobStream = blob.OpenRead(); 
            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK); 
            message.Content = new StreamContent(blobStream);
            message.Content.Headers.ContentLength = blob.Properties.Length; 
            message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4"); 
            message.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = blob.Name, Size = blob.Properties.Length }; 
            return message; 
        }

        // PUT: api/Datas/5
        [HttpPut]
        [Route("api/datas/{id}")]
        public async Task<string> UploadBlob(string id)
        {
            // Get Sample entry 
            blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer videoContainer = blobClient.GetContainerReference("original");
            TableOperation retriveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id); ;

            TableResult getOperationResult = table.Execute(retriveOperation);

            if (getOperationResult.Result == null)
            {
                return NotFound().ToString();
            }

            SampleEntity sample = (SampleEntity)getOperationResult.Result;
            // Delete current existings blobs if they exist
            deleteOldBlobs(sample);


            //Stream in new MP4 file
            var provider = new AzureStorageMultipartFormDataStreamProvider(videoContainer);

            try
            {
                await Request.Content.ReadAsMultipartAsync(provider);
            }

            catch (Exception e)
            {
                return ($"An error has occured. Details: {e.Message.ToString()}");
            }

            var filename = provider.FileData.FirstOrDefault()?.LocalFileName;
            if (string.IsNullOrEmpty(filename))
            {
                return BadRequest("An error has occured while uploading your file. Please try again.").ToString();
            }

            // Add URI of new sample 
            var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
            String sampleURL = baseUrl.ToString() + "/api/data/" + id;
            sample.Mp4Blob = provider.getFileName();
            sample.SampleMp4URL = sampleURL;

            TableOperation tableOp = TableOperation.Replace(sample);
            table.Execute(tableOp);

            // Put new message in queue
            var queueMessage = new SampleEntity(partitionName, id);
            queue.AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(queueMessage)));

            return Ok($"File: {filename} has successfully uploaded").ToString();
        }

        private void deleteOldBlobs(SampleEntity sample)
        {
            // Delete previous original and sample blobs if they exist
            CloudBlobContainer videoContainer = blobClient.GetContainerReference("original");
            if (sample.Mp4Blob != null)
            {
                CloudBlockBlob blob = videoContainer.GetBlockBlobReference(sample.Mp4Blob);
                blob.DeleteIfExists();
                sample.Mp4Blob = null;
            }
            if (sample.SampleMp4Blob != null)
            {
                CloudBlockBlob blob = videoContainer.GetBlockBlobReference(sample.SampleMp4Blob);
                blob.DeleteIfExists();
                sample.SampleMp4URL = null;
            }
        }
    }
}
