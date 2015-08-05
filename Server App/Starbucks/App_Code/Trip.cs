using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class Trip
    {
        [DataMember]
        public int id { get; set; }

        [DataMember]
        public string routeName { get; set; }

        [DataMember]
        public bool closed { get; set; }

        [DataMember]
        public string username { get; set; }

        [DataMember]
        public int dateStartedEpoch { get; set; }
        public DateTime dateStarted { get; set; }

        [DataMember]
        public string dateStartedString { get; set; }

        [DataMember]
        public int dateClosedEpoch { get; set; }
        public DateTime dateClosed { get;  set; }

        [DataMember]
        public float latitude { get; set; }

        [DataMember]
        public float longitude { get; set; }

        [DataMember]
        public float GMTOffset { get; set; }

        [DataMember]
        public string tripDetails { get; set; }
    }
}