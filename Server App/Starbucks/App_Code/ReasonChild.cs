using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class ReasonChild
    {
        [DataMember]
        public int childReasonCode { get; set; }

        [DataMember]
        public string childReasonName { get; set; }

        [DataMember]
        public string childReasonExplanation { get; set; }

        [DataMember]
        public bool escalation { get; set; }

        [DataMember]
        public bool photoRequired { get; set; }

        [DataMember]
        public bool valueRequired { get; set; }

        [DataMember]
        public string valueUnit { get; set; }

        [DataMember]
        public float valueUnitPrice { get; set; }

        [DataMember]
        public int reasonCode { get; set; }

        [DataMember]
        public bool PODRequired { get; set; }
        
    }
}