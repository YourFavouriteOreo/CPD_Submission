using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace CPD_Coursework_2.Controllers
{
    // Code adapted from https://ppolyzos.com/2016/02/07/upload-a-file-to-azure-blob-storage-using-web-api/
    public class AzureStorageMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        // Initialize variables
        private readonly CloudBlobContainer _blobContainer;
        private string fileName;
        private readonly string[] _supportedMimeTypes = {"video/mp4"};

        public AzureStorageMultipartFormDataStreamProvider(CloudBlobContainer blobContainer) : base("azure")
        {
            _blobContainer = blobContainer;
        }

        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            // Ensure Parent and Headers are not nul
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (headers == null) throw new ArgumentNullException(nameof(headers));

            // Ensure that the file sent is an MP4
            if (!_supportedMimeTypes.Contains(headers.ContentType.ToString().ToLower()))
            {
                throw new NotSupportedException("Only mp4 supported");
            }

            // Generate a new filename for every new blob
            fileName = Guid.NewGuid().ToString();

            CloudBlockBlob blob = _blobContainer.GetBlockBlobReference(fileName);

            if (headers.ContentType != null)
            {
                // Set appropriate content type for your uploaded file
                blob.Properties.ContentType = headers.ContentType.MediaType;
            }

            this.FileData.Add(new MultipartFileData(headers, blob.Name));

            return blob.OpenWrite();
        }
        public string getFileName()
        {
            // Get Filename of Last transferred file
            return this.fileName;
        }
    }
}