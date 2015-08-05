using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class Stop
    {
        [DataMember]
        public int id { get; set; }

        [DataMember]
        public int tripID { get; set; }

        [DataMember]
        public int mappingID { get; set; }

        [DataMember]
        public bool completed { get; set; }

        [DataMember]
        public int dateAddedEpoch { get; set; }
        public DateTime dateAdded { get; set; }

        [DataMember]
        public int dateUpdatedEpoch { get; set; }
        public DateTime dateUpdated { get; set; }

        [DataMember]
        public string completedDate { get; set; }

        //[DataMember]
        //public string comment { get; set; }

        [DataMember]
        public bool committed { get; set; }

        [DataMember]
        public List<FailureWithReason> failure { get; set; }

        //[DataMember]
        //public List<FailureAndPhoto> failureImages { get; set; }


    }
}