using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class StarbucksUser
    {
        [DataMember]
        public string username { get; set; }

        [DataMember]
        public string firstName { get; set; }

        [DataMember]
        public string lastName { get; set; }

        [DataMember]
        public string phoneNumber { get; set; }

        [DataMember]
        public string emailAddress { get; set; }

        [DataMember]
        public bool state { get; set; }

        [DataMember]
        public int userType { get; set; }

        [DataMember]
        public string userTypeName { get; set; }

        [DataMember]
        public string password { get; set; }

        [DataMember]
        public string associatedID { get; set; }

        [DataMember]
        public string associatedFieldName { get; set; }

        [DataMember]
        public string associatedFieldValue { get; set; }
    }
}