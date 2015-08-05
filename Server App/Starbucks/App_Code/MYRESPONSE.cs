using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class MyResponse
    {
        
        public int statusCode { get; set; }

        [DataMember]
        public string statusDescription { get; set; }

        public MyResponse()
        {
            statusCode= 0;
            statusDescription = "";
        }
    }
}