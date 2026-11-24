using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class SupervisorPendingReviewViewModel
    {
        public int ReviewID { get; set; }

        public string ReviewType { get; set; }

        public string Title { get; set; }

        public string StudentName { get; set; }

        public DateTime? SubmittedDate { get; set; }

        public string Status { get; set; }

        public string Action { get; set; }

        public string Controller { get; set; }
    }
}