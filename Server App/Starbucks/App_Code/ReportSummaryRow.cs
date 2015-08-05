using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Starbucks
{
    public class ReportSummaryRow
    {
        public string date { get; set; }

        public int issues { get; set; }

        public int nonIssues { get; set; }
    }
}