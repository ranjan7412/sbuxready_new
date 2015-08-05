using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Starbucks
{
    public class ReportSummary
    {
        public Dictionary<string, List<ReportSummaryRow>> data { get; set; }

        public List<string> allDates { get; set; }

        public ReportSummary()
        {
            data = new Dictionary<string, List<ReportSummaryRow>>();
            allDates = new List<string>();
        }

        public void addRow(string name, string date, int issues, int nonIssues)
        {
            if (data.ContainsKey(name))
            {
                List<ReportSummaryRow> thatRowList = data[name];

                ReportSummaryRow newItem = new ReportSummaryRow();
                newItem.date = date;
                newItem.issues = issues;
                newItem.nonIssues = nonIssues;

                thatRowList.Add(newItem);
            }
            else
            {
                ReportSummaryRow newItem = new ReportSummaryRow();
                newItem.date = date;
                newItem.issues = issues;
                newItem.nonIssues = nonIssues;

                List<ReportSummaryRow> newItemList = new List<ReportSummaryRow>();

                newItemList.Add(newItem);
                
                data.Add(name, newItemList);
            }

            addDate(date);
        }

        public void addDate(string date)
        {
            bool dateExists = false;
            foreach (string aDate in allDates)
            {
                if (aDate.Equals(date))
                {
                    dateExists = true;
                }
            }
            if (!dateExists)
            {
                allDates.Add(date);
            }

            allDates.Sort();
        }

        public List<ReportSummaryRow> getRowsForName(string name)
        {
            if (data.ContainsKey(name))
            {
                return data[name];
            }
            else
            {
                return null;
            }
        }
    }
}