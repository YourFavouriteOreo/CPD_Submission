using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using CPD_Coursework_2.Models;
//using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CPD_Coursework_2.Controllers
{
    public class SamplesController : ApiController
    {
        private const String partitionName = "Samples_Partition_1";

        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;
        CloudBlobClient blobClient;

        public SamplesController()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            blobClient = storageAccount.CreateCloudBlobClient();
            table = tableClient.GetTableReference("Samples");
        }

        /// <summary>
        /// Get all Samples
        /// </summary>
        /// <returns></returns>
        // GET: api/Samples
        public IEnumerable<Sample> Get()
        {
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));
            List<SampleEntity> entityList = new List<SampleEntity>(table.ExecuteQuery(query));

            // Basically create a list of Sample from the list of SampleEntity with a 1:1 object relationship, filtering data as needed
            IEnumerable<Sample> SampleList = from e in entityList
                                             select new Sample()
                                             {
                                                 SampleID = e.RowKey,
                                                 Title = e.Title,
                                                 CreatedDate = e.CreatedDate,
                                                 Mp4Blob = e.Mp4Blob,
                                                 SampleMp4Blob = e.SampleMp4Blob,
                                                 SampleMp4URL = e.SampleMp4URL,
                                                 SampleDate = e.SampleDate,
                                             };
            return SampleList;
        }

        // GET: api/Samples/5
        /// <summary>
        /// Get a Sample
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult GetSample(string id)
        {
            // Create a retrieve operation that takes a Sample entity.
            TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult getOperationResult = table.Execute(getOperation);

            // Construct response including a new DTO as apprporiatte
            if (getOperationResult.Result == null) return NotFound();
            else
            {
                SampleEntity sampleEntity = (SampleEntity)getOperationResult.Result;
                Sample p = new Sample()
                {
                    SampleID = sampleEntity.RowKey,
                    Title = sampleEntity.Title,
                    CreatedDate = sampleEntity.CreatedDate,
                    Mp4Blob = sampleEntity.Mp4Blob,
                    SampleMp4Blob = sampleEntity.SampleMp4Blob,
                    SampleMp4URL = sampleEntity.SampleMp4URL,
                    SampleDate = sampleEntity.SampleDate,
                };
                return Ok(p);
            }
        }

        // POST: api/Samples
        /// <summary>
        /// Create a new Sample
        /// </summary>
        /// <param name="Sample"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.Created)]
        [ResponseType(typeof(Sample))]
        public IHttpActionResult PostSample(Sample Sample)
        {

            SampleEntity SampleEntity = new SampleEntity()
            {
                RowKey = getNewMaxRowKeyValue(),
                PartitionKey = partitionName,
                Title = Sample.Title,
                CreatedDate = DateTime.Now,
                Mp4Blob = Sample.Mp4Blob,
                SampleMp4Blob = null,
                SampleMp4URL = null,
                SampleDate = null,
            };
            //{
            //    RowKey = getNewMaxRowKeyValue(),
            //    PartitionKey = partitionName,
            //    Name = Sample.Name,
            //    Price = Sample.Price,
            //    Category = Sample.Category
            //};

            // Create the TableOperation that inserts the Sample entity.
            var insertOperation = TableOperation.Insert(SampleEntity);

            // Execute the insert operation.
            try
            {
                table.Execute(insertOperation);
            }

            catch (StorageException e)
            {
                Debug.WriteLine(e.RequestInformation.ExtendedErrorInformation.ErrorMessage);
            }

            return CreatedAtRoute("DefaultApi", new { id = SampleEntity.RowKey }, SampleEntity);
        }

        // PUT: api/Samples/5
        /// <summary>
        /// Update a Sample
        /// </summary>
        /// <param name="id"></param>
        /// <param name="Sample"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.NoContent)]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutSample(string id, Sample Sample)
        {
            if (id != Sample.SampleID)
            {
                return BadRequest();
            }

            // Create a retrieve operation that takes a Sample entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a SampleEntity object.
            SampleEntity updateEntity = (SampleEntity)retrievedResult.Result;

            updateEntity.Title = Sample.Title;
            updateEntity.Mp4Blob = Sample.Mp4Blob;
            updateEntity.SampleMp4Blob = Sample.SampleMp4Blob;
            updateEntity.SampleDate = DateTime.Now;

            // Create the TableOperation that inserts the Sample entity.
            // Note semantics of InsertOrReplace() which are consistent with PUT
            // See: https://stackoverflow.com/questions/14685907/difference-between-insert-or-merge-entity-and-insert-or-replace-entity
            var updateOperation = TableOperation.InsertOrReplace(updateEntity);

            // Execute the insert operation.
            table.Execute(updateOperation);

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/Samples/5
        /// <summary>
        /// Delete a Sample
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult DeleteSample(string id)
        {
            // Create a retrieve operation that takes a Sample entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result == null) return NotFound();
            else
            {
                SampleEntity deleteEntity = (SampleEntity)retrievedResult.Result;
                deleteBlobs(deleteEntity);
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                table.Execute(deleteOperation);

                return Ok(retrievedResult.Result);
            }
        }

        private void deleteBlobs(SampleEntity sample)
        {
            CloudBlobContainer videoContainer = blobClient.GetContainerReference("original");
            if (sample.Mp4Blob != null)
            {
                CloudBlockBlob blob = videoContainer.GetBlockBlobReference(sample.Mp4Blob);
                blob.DeleteIfExists();
            }
            if (sample.SampleMp4Blob != null)
            {
                CloudBlockBlob blob = videoContainer.GetBlockBlobReference(sample.SampleMp4Blob);
                blob.DeleteIfExists();
                sample.SampleMp4URL = null;
            }
        }

        private String getNewMaxRowKeyValue()
        {
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));

            int maxRowKeyValue = 0;
            foreach (SampleEntity entity in table.ExecuteQuery(query))
            {
                int entityRowKeyValue = Int32.Parse(entity.RowKey);
                if (entityRowKeyValue > maxRowKeyValue) maxRowKeyValue = entityRowKeyValue;
            }
            maxRowKeyValue++;
            return maxRowKeyValue.ToString();
        }


    }
}
