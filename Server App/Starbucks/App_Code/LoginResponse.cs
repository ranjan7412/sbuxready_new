using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class LoginResponse : Response
    {
        [DataMember]
        public StarbucksUser user { get; set; }

        [DataMember]
        public string sessionID { get; set; }

        public LoginResponse()
        {
            user = null;
            sessionID = "";
            statusCode = 5;
            statusDescription = "";
        }
    }
}