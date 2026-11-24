using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class EthicsCertificateViewModel
    {
        [Required(ErrorMessage = "Please enter your ethics reference number.")]
        [Display(Name = "Ethics Reference Number")]
        public string ReferenceNumber { get; set; }
    }
}