using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class Photo
    {
        [DataMember]
        public int photoID { get; set; }

        [DataMember]
        public int stopID { get; set; }

        [DataMember]
        public int failureID { get; set; }

        [DataMember]
        public string imageData { get; set; }
    }
}