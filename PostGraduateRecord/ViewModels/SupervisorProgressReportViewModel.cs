using System;
using System.ComponentModel.DataAnnotations;

namespace PostGraduateRecord.ViewModels
{
    public class SupervisorProgressReportViewModel
    {
        // =====================================================
        // STUDENT INFORMATION
        // =====================================================

        public int StudentID { get; set; }

        public int ProposalID { get; set; }

        public string StudentName { get; set; }

        public string StudentNumber { get; set; }

        public string Qualification { get; set; }

        public string StudyType { get; set; }


        // =====================================================
        // RESEARCH INFORMATION
        // =====================================================

        public string ResearchTitle { get; set; }


        // =====================================================
        // REPORT INFORMATION
        // =====================================================

        [Required(ErrorMessage = "Report number is required.")]
        [Display(Name = "Report Number")]
        public int? ReportNumber { get; set; }

        [Required(ErrorMessage = "Report date is required.")]
        [Display(Name = "Report Date")]
        [DataType(DataType.Date)]
        public DateTime ReportDate { get; set; }

        [Required(ErrorMessage = "Reporting period is required.")]
        [Display(Name = "Reporting Period")]
        public string ReportingPeriod { get; set; }


        // =====================================================
        // RESEARCH PROGRESS
        // =====================================================

        [Required(ErrorMessage = "Progress summary is required.")]
        [Display(Name = "Progress Summary")]
        public string Summary { get; set; }

        [Required(ErrorMessage = "Current progress is required.")]
        [Display(Name = "Current Progress")]
        public string CurrentProgress { get; set; }

        [Required(ErrorMessage = "Progress assessment is required.")]
        [Display(Name = "Progress Assessment")]
        public string ProgressStatus { get; set; }

        [Display(Name = "Challenges")]
        public string Challenges { get; set; }


        // =====================================================
        // SUPERVISOR FEEDBACK
        // =====================================================

        [Display(Name = "Supervisor Comments")]
        public string SupervisorComments { get; set; }

        [Display(Name = "Next Steps")]
        public string NextSteps { get; set; }


        // =====================================================
        // REPORT STATUS
        // =====================================================

        public string Status { get; set; }
    }
}