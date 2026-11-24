using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class SupervisorDeadlineViewModel
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string StudentName { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string ActionText { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public int Id { get; set; }
    }
}