using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;


namespace PostGraduateRecord.ViewModels
{
    public class ProposalViewModel
    {
        public int ProposalID { get; set; }
        [Required]
        [Display(Name = "Research Title")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Research Field")]
        public string ResearchField { get; set; }

        [Required]
        [Display(Name = "Abstract")]
        public string Abstract { get; set; }

        [Display(Name = "Keywords")]
        public string Keywords { get; set; }

        [Display(Name = "Proposal Document")]
        public HttpPostedFileBase ProposalFile { get; set; }
    }
}