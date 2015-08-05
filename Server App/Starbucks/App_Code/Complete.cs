using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Starbucks
{
    [DataContract]
    public class Commit
    {
        [DataMember]
        public string commit { get; set; }

        [DataMember]
        public int stopID { get; set; }
    }
}