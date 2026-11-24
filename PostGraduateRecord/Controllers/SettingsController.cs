using PostGraduateRecord.Models;
using PostGraduateRecord.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PostGraduateRecord.Controllers
{
    public class SettingsController : Controller
    {
        private readonly PostGradEntities db = new PostGradEntities();

        // =========================================================
        // SETTINGS
        // GET: Settings
        // =========================================================
        [HttpGet]
        public ActionResult Index()
        {
            // -----------------------------------------------------
            // Check logged-in user
            // -----------------------------------------------------
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // -----------------------------------------------------
            // Find user's settings
            // -----------------------------------------------------
            var settings = db.UserSettings
                .FirstOrDefault(s => s.UserID == userId);

            // -----------------------------------------------------
            // Create default settings if none exist
            // -----------------------------------------------------
            if (settings == null)
            {
                settings = new UserSetting
                {
                    UserID = userId,

                    // Language
                    PreferredLanguage = "English (South Africa)",
                    InterfaceLanguage = "English",
                    SpellCheckEnabled = true,
                    SpellCheckLanguage = "English (South Africa)",

                    // Appearance
                    // Dark mode has been removed.
                    Theme = "Light",

                    // Downloads
                    OpenPdfInBrowser = true,
                    DownloadConfirmation = false,

                    // Notifications
                    NotificationsEnabled = true,
                    DeadlineRemindersEnabled = true,

                    CreatedDate = DateTime.Now
                };

                db.UserSettings.Add(settings);
                db.SaveChanges();
            }

            // -----------------------------------------------------
            // Protect against old/invalid theme values
            // -----------------------------------------------------
            if (settings.Theme != "Light" &&
                settings.Theme != "EyeComfort" &&
                settings.Theme != "System")
            {
                settings.Theme = "Light";
                db.SaveChanges();
            }

            // -----------------------------------------------------
            // Convert EF entity to ViewModel
            // -----------------------------------------------------
            var model = new UserSettingsViewModel
            {
                Theme = settings.Theme,

                PreferredLanguage = settings.PreferredLanguage,
                InterfaceLanguage = settings.InterfaceLanguage,
                SpellCheckEnabled = settings.SpellCheckEnabled,
                SpellCheckLanguage = settings.SpellCheckLanguage,

                OpenPdfInBrowser = settings.OpenPdfInBrowser,
                DownloadConfirmation = settings.DownloadConfirmation,

                NotificationsEnabled = settings.NotificationsEnabled,
                DeadlineRemindersEnabled = settings.DeadlineRemindersEnabled
            };

            // -----------------------------------------------------
            // Apply theme immediately to current session
            // -----------------------------------------------------
            Session["UserTheme"] = settings.Theme;

            return View(model);
        }


        // =========================================================
        // SETTINGS
        // POST: Settings
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(UserSettingsViewModel model)
        {
            // -----------------------------------------------------
            // Check logged-in user
            // -----------------------------------------------------
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // -----------------------------------------------------
            // Validate theme
            // -----------------------------------------------------
            var validThemes = new[]
            {
                "Light",
                "EyeComfort",
                "System"
            };

            if (string.IsNullOrWhiteSpace(model.Theme) ||
                !validThemes.Contains(model.Theme))
            {
                ModelState.AddModelError(
                    "Theme",
                    "Please select a valid interface theme."
                );
            }

            // -----------------------------------------------------
            // Validate model
            // -----------------------------------------------------
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // -----------------------------------------------------
            // Find existing settings
            // -----------------------------------------------------
            var settings = db.UserSettings
                .FirstOrDefault(s => s.UserID == userId);

            // -----------------------------------------------------
            // Create settings if they don't exist
            // -----------------------------------------------------
            if (settings == null)
            {
                settings = new UserSetting
                {
                    UserID = userId,
                    CreatedDate = DateTime.Now
                };

                db.UserSettings.Add(settings);
            }

            // -----------------------------------------------------
            // Save Appearance
            // -----------------------------------------------------
            settings.Theme = model.Theme;

            // -----------------------------------------------------
            // Save Language & Spell Check
            // -----------------------------------------------------
            settings.PreferredLanguage = model.PreferredLanguage;
            settings.InterfaceLanguage = model.InterfaceLanguage;
            settings.SpellCheckEnabled = model.SpellCheckEnabled;
            settings.SpellCheckLanguage = model.SpellCheckLanguage;

            // -----------------------------------------------------
            // Save Downloads
            // -----------------------------------------------------
            settings.OpenPdfInBrowser = model.OpenPdfInBrowser;
            settings.DownloadConfirmation = model.DownloadConfirmation;

            // -----------------------------------------------------
            // Save Notifications
            // -----------------------------------------------------
            settings.NotificationsEnabled = model.NotificationsEnabled;
            settings.DeadlineRemindersEnabled = model.DeadlineRemindersEnabled;

            // -----------------------------------------------------
            // Update modified date
            // -----------------------------------------------------
            settings.ModifiedDate = DateTime.Now;

            // -----------------------------------------------------
            // Save changes
            // -----------------------------------------------------
            db.SaveChanges();

            // -----------------------------------------------------
            // Apply selected theme immediately
            // -----------------------------------------------------
            Session["UserTheme"] = settings.Theme;

            // -----------------------------------------------------
            // Success message
            // -----------------------------------------------------
            TempData["SettingsSuccess"] =
                "Your settings have been saved successfully.";

            return RedirectToAction("Index");
        }


        // =========================================================
        // DISPOSE
        // =========================================================
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}