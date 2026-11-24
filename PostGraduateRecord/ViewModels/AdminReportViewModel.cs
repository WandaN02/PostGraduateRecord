using System;
using System.Collections.Generic;

namespace PostGraduateRecord.ViewModels
{
    public class AdminReportViewModel
    {
        // ==========================================
        // SUMMARY
        // ==========================================

        public int TotalStudents { get; set; }

        public int ActiveStudents { get; set; }

        public int CompletedStudents { get; set; }

        public int TotalProposals { get; set; }

        public int TotalEthicsApplications { get; set; }

        public int TotalEvaluations { get; set; }


        // ==========================================
        // PROPOSAL SUMMARY
        // ==========================================

        public int PendingProposals { get; set; }

        public int ApprovedProposals { get; set; }

        public int UnderEvaluationProposals { get; set; }

        public int RejectedProposals { get; set; }

        public int RevisionRequiredProposals { get; set; }


        // ==========================================
        // ETHICS SUMMARY
        // ==========================================

        public int PendingEthics { get; set; }

        public int SubmittedEthics { get; set; }

        public int ApprovedEthics { get; set; }

        public int RejectedEthics { get; set; }

        public int RevisionRequiredEthics { get; set; }


        // ==========================================
        // EVALUATION SUMMARY
        // ==========================================

        public int PendingEvaluations { get; set; }

        public int InProgressEvaluations { get; set; }

        public int CompletedEvaluations { get; set; }


        // ==========================================
        // STUDENT PROGRESS
        // ==========================================

        public List<AdminStudentProgressViewModel> StudentProgress { get; set; }


        // ==========================================
        // SUPERVISOR WORKLOAD
        // ==========================================

        public List<AdminSupervisorReportViewModel> SupervisorWorkload { get; set; }


        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        public AdminReportViewModel()
        {
            StudentProgress = new List<AdminStudentProgressViewModel>();

            SupervisorWorkload = new List<AdminSupervisorReportViewModel>();
        }
    }


    // ==========================================
    // STUDENT PROGRESS
    // ==========================================

    public class AdminStudentProgressViewModel
    {
        public int StudentID { get; set; }

        public string StudentName { get; set; }

        public string StudentNumber { get; set; }

        public string ResearchTitle { get; set; }

        public int ProgressPercentage { get; set; }

        public string Status { get; set; }
    }


    // ==========================================
    // SUPERVISOR REPORT
    // ==========================================

    public class AdminSupervisorReportViewModel
    {
        public int SupervisorID { get; set; }

        public string SupervisorName { get; set; }

        public int AssignedStudents { get; set; }

        public int PendingReviews { get; set; }
    }
}