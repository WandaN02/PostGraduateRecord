using PostGraduateRecord.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class ProposalReviewViewModel
    {
        public Proposal Proposal { get; set; }

        [Required]
        public string Comments { get; set; }

        [Required]
        public string Decision { get; set; }

        public List<Feedback> PreviousFeedback { get; set; }

    }
}