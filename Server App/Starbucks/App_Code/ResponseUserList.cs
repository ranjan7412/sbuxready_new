using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace Starbucks
{
    [DataContract]
    public class ResponseUserList : Response
    {
        [DataMember]
        public List<StarbucksUser> users { get; set; }
    }
}