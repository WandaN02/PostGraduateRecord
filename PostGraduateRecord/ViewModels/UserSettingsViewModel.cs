using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PostGraduateRecord.ViewModels
{
    public class UserSettingsViewModel
    {
        // =========================================================
        // APPEARANCE
        // =========================================================

        [Display(Name = "Theme")]
        public string Theme { get; set; }

        // =========================================================
        // LANGUAGE & SPELL CHECK
        // =========================================================

        [Display(Name = "Preferred Language")]
        public string PreferredLanguage { get; set; }

        [Display(Name = "Interface Language")]
        public string InterfaceLanguage { get; set; }

        [Display(Name = "Enable Spell Check")]
        public bool SpellCheckEnabled { get; set; }

        [Display(Name = "Spell Check Language")]
        public string SpellCheckLanguage { get; set; }

        // =========================================================
        // DOWNLOADS
        // =========================================================

        [Display(Name = "Open PDF in Browser")]
        public bool OpenPdfInBrowser { get; set; }

        [Display(Name = "Download Confirmation")]
        public bool DownloadConfirmation { get; set; }

        // =========================================================
        // NOTIFICATIONS
        // =========================================================

        [Display(Name = "Enable Notifications")]
        public bool NotificationsEnabled { get; set; }

        [Display(Name = "Deadline Reminders")]
        public bool DeadlineRemindersEnabled { get; set; }
    }
}
