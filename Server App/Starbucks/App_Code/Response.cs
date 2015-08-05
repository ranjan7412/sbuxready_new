using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class Response
    {
        [DataMember]
        public int statusCode { get; set; }

        [DataMember]
        public string statusDescription { get; set; }

        public Response()
        {
            statusCode = 5;
            statusDescription = "";
        }
    }
}