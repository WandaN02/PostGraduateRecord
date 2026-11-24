using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class SubmissionHistoryViewModel
    {
        public string SubmissionType { get; set; }

        public string FilePath { get; set; }

        public DateTime? DateSubmitted { get; set; }

        public string Status { get; set; }

        public string Feedback { get; set; }
    }
}