using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class ApprovedStudentViewModel
    {

        public int ApplicationID { get; set; }

        public string StudentName { get; set; }

        public string ResearchTitle { get; set; }

        public string DegreeType { get; set; }

        public string ResearchField { get; set; }

        public DateTime SubmittedDate { get; set; }

        public string SupervisorName { get; set; }

        public DateTime? AssignedDate { get; set; }

        public string AssignmentStatus { get; set; }
    }
}