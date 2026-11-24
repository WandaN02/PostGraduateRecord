using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class NotificationViewModel
    {
        public int NotificationID { get; set; }

        public string Message { get; set; }

        public string Link { get; set; }

        public bool IsRead { get; set; }

        public DateTime? SentDate { get; set; }
    }
}