using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class AIRequestViewModel
    {
        [Required]
        public string Message { get; set; }

        public string Role { get; set; }
    }
}