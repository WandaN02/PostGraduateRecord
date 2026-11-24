using System;
using System.Collections.Generic;

namespace PostGraduateRecord.ViewModels
{
    public class EvaluationDetailsViewModel
    {
        public int EvaluationID { get; set; }

        public int ProposalID { get; set; }

        public string ResearchTitle { get; set; }

        public string StudentName { get; set; }

        public string StudentNumber { get; set; }

        public string EvaluatorName { get; set; }

        public decimal TotalScore { get; set; }

        public decimal TotalPossibleScore { get; set; }

        public decimal FinalMark { get; set; }

        public decimal Percentage { get; set; }

        public string Recommendation { get; set; }

        public string OverallComments { get; set; }

        public DateTime? EvaluationDate { get; set; }

        public bool EvaluationSentToStudent { get; set; }

        public DateTime? EvaluationSentDate { get; set; }

        public List<EvaluationDetailsCriterionViewModel> Criteria { get; set; }
            = new List<EvaluationDetailsCriterionViewModel>();
    }

    public class EvaluationDetailsCriterionViewModel
    {
        public int CriterionID { get; set; }

        public string CriterionName { get; set; }

        public string Description { get; set; }

        public decimal Rating { get; set; }

        public decimal MaxScore { get; set; }

        public string Comments { get; set; }

        public int DisplayOrder { get; set; }
    }
}