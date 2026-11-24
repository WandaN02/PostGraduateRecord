using PostGraduateRecord.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PostGraduateRecord.ViewModels
{
    // =============================================================
    // EVALUATOR REVIEW VIEW MODEL
    // =============================================================

    public class EvaluatorReviewViewModel
    {
        // =========================================================
        // EVALUATION
        // =========================================================

        public int EvaluationID { get; set; }

        public int ProposalID { get; set; }


        // =========================================================
        // PROPOSAL INFORMATION
        // =========================================================

        public Proposal Proposal { get; set; }


        // =========================================================
        // EVALUATION CRITERIA
        // =========================================================

        public List<EvaluatorCriterionViewModel> Criteria { get; set; }
            = new List<EvaluatorCriterionViewModel>();


        // =========================================================
        // SCORE INFORMATION
        // =========================================================

        // Total marks obtained from all criteria.
        //
        // 9 criteria × 5 marks = 45 maximum.
        public decimal? TotalScore { get; set; }


        // Maximum possible score.
        //
        // Normally 9 × 5 = 45.
        // Kept dynamic so it can be calculated from the
        // active EvaluationCriteria records.
        public decimal TotalPossibleScore { get; set; }


        // =========================================================
        // FINAL PERCENTAGE
        // =========================================================

        // Example:
        //
        // 36 / 45 × 100 = 80.00%
        public decimal? OverallScore { get; set; }


        // =========================================================
        // FINAL CONVERTED MARK
        // =========================================================

        // Final mark converted to a mark out of 10.
        //
        // Example:
        //
        // 36 / 45 × 10 = 8.00 / 10
        public decimal? ConvertedScore { get; set; }


        // =========================================================
        // RECOMMENDATION
        // =========================================================

        [Required(ErrorMessage = "Please select a recommendation.")]
        public string Recommendation { get; set; }


        // =========================================================
        // OVERALL EVALUATOR COMMENTS
        // =========================================================

        [Required(ErrorMessage = "Please provide evaluation comments.")]
        public string Comments { get; set; }


        // =========================================================
        // EVALUATION INFORMATION
        // =========================================================

        public DateTime? EvaluationDate { get; set; }

        public DateTime? Deadline { get; set; }


        // =========================================================
        // EVALUATION STATUS
        // =========================================================

        public bool IsCompleted { get; set; }
    }


    // =============================================================
    // INDIVIDUAL EVALUATION CRITERION
    // =============================================================

    public class EvaluatorCriterionViewModel
    {
        // =========================================================
        // CRITERION INFORMATION
        // =========================================================

        public int CriterionID { get; set; }

        public string CriterionName { get; set; }

        public string Description { get; set; }


        // =========================================================
        // MAXIMUM SCORE
        // =========================================================

        // Populated from EvaluationCriteria.MaxScore.
        //
        // Current system:
        // 5 marks per criterion.
        public decimal MaxScore { get; set; }


        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }


        // =========================================================
        // EVALUATOR RATING
        // =========================================================

        // Evaluator must select a whole-number rating:
        //
        // 1 = Poor
        // 2 = Below expectations
        // 3 = Satisfactory
        // 4 = Good
        // 5 = Excellent
        //
        // Ratings 1, 2 and 3 require criterion comments.
        // Ratings 4 and 5 allow comments but do not require them.

        [Required(ErrorMessage = "Please select a rating.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int? Rating { get; set; }


        // =========================================================
        // CRITERION COMMENTS
        // =========================================================

        // Required when Rating is 1, 2 or 3.
        // Optional when Rating is 4 or 5.
        //
        // This conditional validation is enforced in the
        // EvaluatorsController.
        public string Comments { get; set; }


        // =========================================================
        // HELPER PROPERTY
        // =========================================================

        // Used by the view/controller to determine whether
        // a criterion comment should be required.
        public bool IsCommentRequired
        {
            get
            {
                return Rating.HasValue && Rating.Value <= 3;
            }
        }
    }
}