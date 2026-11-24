using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

//namespace PostGraduateRecord.ViewModels
namespace PostGraduateRecord.ViewModels
{
    public class PendingReviewViewModel
    {
        public int ProposalID { get; set; }

        public string StudentName { get; set; }

        public string ProposalTitle { get; set; }

        public DateTime? SubmissionDate { get; set; }

        public string Status { get; set; }
    }
}