using PostGraduateRecord.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class EvaluatorDashboardViewModel
    {
        public int AssignedProposals { get; set; }

        public int PendingReviews { get; set; }

        public int CompletedReviews { get; set; }

        public int UpcomingDeadlines { get; set; }

        public List<Evaluation> Evaluations { get; set; }
    }
}