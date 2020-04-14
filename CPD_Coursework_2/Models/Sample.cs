using System;
using System.ComponentModel.DataAnnotations;

namespace CPD_Coursework_2.Models
{
    public class Sample
    {
        /// <summary>
        /// Product ID
        /// </summary>
        [Key]
        public string SampleID { get; set; }

        /// <summary>
        /// Name of the product
        /// </summary>
        public string Title { get; set; }

        ///// <summary>
        ///// Date the MP4 request was created
        ///// </summary>
        public DateTime? CreatedDate { get; set; }

        ///// <summary>
        ///// MP4 to be samplified
        ///// </summary>
        public string Mp4Blob { get; set; }

        ///// <summary>
        ///// Created Sample of MP4
        ///// </summary>
        public string SampleMp4Blob { get; set; }

        ///// <summary>
        ///// Location to access the MP4 blob
        ///// </summary>
        public string SampleMp4URL { get; set; }

        ///// <summary>
        ///// Date Sample was created
        ///// </summary>
        public DateTime? SampleDate { get; set; }
    }
}