using PostGraduateRecord.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class SupervisorDashboardViewModel
    {
        public int AssignedStudents { get; set; }

        public int ActiveProposals { get; set; }

        public int PendingReviews { get; set; }

        public int UpcomingDeadlines { get; set; }


        public List<SupervisorAssignment> Assignments { get; set; }
        public List<PendingReviewViewModel> PendingReviewList { get; set; }
    }
}