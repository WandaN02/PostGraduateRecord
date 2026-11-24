using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class SupervisorTimelineViewModel
    {
        // ==========================================
        // EXISTING MILESTONE PROPERTIES
        // Keep these because the current Timeline.cshtml
        // still uses them.
        // ==========================================

        public int StudentMilestoneID { get; set; }

        public string StudentName { get; set; }

        public string MilestoneName { get; set; }

        public string Status { get; set; }

        public DateTime? CompletionDate { get; set; }

        public string Comments { get; set; }


        // ==========================================
        // NEW TIMELINE INFORMATION
        // ==========================================

        public int StudentID { get; set; }

        public string ResearchTitle { get; set; }

        public List<TimelineStageViewModel> Stages { get; set; }


        // ==========================================
        // PROGRESS
        // ==========================================

        public int CompletedStages { get; set; }

        public int TotalStages { get; set; }

        public int ProgressPercentage { get; set; }


        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        public SupervisorTimelineViewModel()
        {
            Stages = new List<TimelineStageViewModel>();
        }
    }


    // ==========================================
    // TIMELINE STAGE
    // ==========================================

    public class TimelineStageViewModel
    {
        public string StageName { get; set; }

        public string Description { get; set; }

        public string Status { get; set; }

        public DateTime? Date { get; set; }

        public string Comments { get; set; }
    }
}