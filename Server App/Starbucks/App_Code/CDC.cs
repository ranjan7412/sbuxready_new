using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [Serializable]
    [DataContract]
    public class CDC
    {
        [DataMember]
        public int id { get; set; }

        [DataMember]
        public string name { get; set; }

        [DataMember]
        public string address { get; set; }

        [DataMember]
        public string city { get; set; }

        [DataMember]
        public string state { get; set; }

        [DataMember]
        public string zip { get; set; }

        [DataMember]
        public string phone { get; set; }

        [DataMember]
        public string email { get; set; }

        [DataMember]
        public int providerID { get; set; }

        public CDC()
        {
            name = "";
            id = 0;
        }

        public CDC(string _name, int _id)
        {
            name = _name;
            id = _id;
        }
    }
}