using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class Delivery
    {
        [DataMember]
        public int deliveryID { get; set; }

        [DataMember]
        public int stopID { get; set; }

        [DataMember]
        public int failureID { get; set; }

        [DataMember]
        public long deliveryCode { get; set; }

        //[DataMember]
        //public string dateAdded { get; set; }

    }
}