using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class AIResponseViewModel
    {
        public bool Success { get; set; }

        public string Response { get; set; }

        public string Error { get; set; }
    }
}