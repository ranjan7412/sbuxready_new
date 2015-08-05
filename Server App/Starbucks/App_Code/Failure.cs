using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class Failure
    {
        [DataMember]
        public int failureID { get; set; }

        [DataMember]
        public int stopID { get; set; }

        [DataMember]
        public int parentReasonCode { get; set; }

        [DataMember]
        public int childReasonCode { get; set; }

        [DataMember]
        public bool emailSent { get; set; }

        [DataMember]
        public int valueEntered { get; set; }

        [DataMember]
        public string comment { get; set; }

        [DataMember]
        public bool committed { get; set; }

        [DataMember]
        public string uniqueID { get; set; }

        [DataMember]
        public List<Photo> photos;

        [DataMember]
        public List<Delivery> deliveryCodes;

    }
}