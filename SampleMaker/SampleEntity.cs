using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace CPD_Coursework_2.Models
{
    public class SampleEntity : TableEntity
    {
        public string Title { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Mp4Blob { get; set; }
        public string SampleMp4Blob { get; set; }
        public string SampleMp4URL { get; set; }
        public DateTime? SampleDate { get; set; }

        public SampleEntity(string partitionKey, string sampleID)
        {
            PartitionKey = partitionKey;
            RowKey = sampleID;
        }

        public SampleEntity() { }
    }
}