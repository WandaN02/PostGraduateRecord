using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class SupervisorSubmissionViewModel
    {
        public int SubmissionID { get; set; }

        public int ProposalID { get; set; }

        public string StudentName { get; set; }

        public string StudentNumber { get; set; }

        public string Qualification { get; set; }

        public string StudyType { get; set; }

        public string ProposalTitle { get; set; }

        public string DocumentName { get; set; }

        public DateTime? SubmittedDate { get; set; }

        public string Status { get; set; }

        public string FilePath { get; set; }

        public string SubmissionType { get; set; }

        public string Feedback { get; set; }

        public int? ReportNumber { get; set; }

    }
}