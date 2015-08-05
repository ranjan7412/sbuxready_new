using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [Serializable]
    [DataContract]
    public class Store
    {
        [DataMember]
        public int storeID { get; set; }

        [DataMember]
        public string storeNumber { get; set; }

        [DataMember]
        public string storeName { get; set; }

        [DataMember]
        public string storeAddress { get; set; }

        [DataMember]
        public string storeCity { get; set; }

        [DataMember]
        public string storeZip { get; set; }

        [DataMember]
        public string storeState { get; set; }

        [DataMember]
        public string storePhone { get; set; }

        [DataMember]
        public string storeManagerName { get; set; }

        [DataMember]
        public string storeEmailAddress { get; set; }

        [DataMember]
        public string storeOwnershipType { get; set; }

        [DataMember]
        public bool PODRequired { get; set; }

    }
}