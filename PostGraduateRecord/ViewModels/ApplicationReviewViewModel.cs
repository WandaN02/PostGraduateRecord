using PostGraduateRecord.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class ApplicationReviewViewModel
    {
        public Application Application { get; set; }

        public string Comments { get; set; }

        public string Decision { get; set; }

        public List<Feedback> PreviousFeedback { get; set; }
    }
}