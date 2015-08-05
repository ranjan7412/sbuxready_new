using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class Op
    {
        [DataMember]
        public int storeID { get; set; }

        [DataMember]
        public string storeNumber { get; set; }

        [DataMember]
        public string division { get; set; }

        [DataMember]
        public string region { get; set; }

        [DataMember]
        public string area { get; set; }

        [DataMember]
        public string district { get; set; }

        [DataMember]
        public string divisionName { get; set; }

        [DataMember]
        public string dvpOutlookname { get; set; }

        [DataMember]
        public string dvpEmailAddress { get; set; }

        [DataMember]
        public string regionName { get; set; }

        [DataMember]
        public string rvpOutlookName { get; set; }

        [DataMember]
        public string rvpEmailAddress { get; set; }

        [DataMember]
        public string areaName { get; set; }

        [DataMember]
        public string rdOutlookName { get; set; }

        [DataMember]
        public string rdEmailAddress { get; set; }

        [DataMember]
        public string districtName { get; set; }

        [DataMember]
        public string dmOutlookName { get; set; }

        [DataMember]
        public string dmEmailAddress { get; set; }
    }
} 