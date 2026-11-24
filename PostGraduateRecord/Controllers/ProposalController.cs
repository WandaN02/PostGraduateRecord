using System;
using System.Linq;
using System.Web.Mvc;
using PostGraduateRecord.Models;
using System.IO;
using System.Web;

namespace PostGraduateRecord.Controllers
{
    public class ProposalController : Controller
    {
        private PostGradEntities db = new PostGradEntities();


        // GET: Proposal/Create
        public ActionResult Create()
        {
            return View();
        }



        // POST: Proposal/Create
        [HttpPost]
        public ActionResult Create(
            string title,
            string researchField,
            string abstractText,
            string keywords,
            HttpPostedFileBase proposalFile)
        {
            try
            {
                int userID =
                    Convert.ToInt32(Session["UserID"]);



                // Find logged-in student

                Student student =
                    db.Students
                    .FirstOrDefault(s => s.UserID == userID);



                if (student == null)
                {
                    ViewBag.Message =
                    "Student profile not found.";

                    return View();
                }



                // Find active submission period

                var periods = db.SubmissionPeriods.ToList();

                if (periods.Count == 0)
                {
                    ViewBag.Message = "SubmissionPeriods table is empty.";
                    return View();
                }

                SubmissionPeriod period = db.SubmissionPeriods
                             .FirstOrDefault(p => p.IsActive == true);

                if (period == null)
                {
                    ViewBag.Message = "No active submission period.";
                    return View();
                }

                string documentPath = null;

                // Upload proposal document
                if (proposalFile != null && proposalFile.ContentLength > 0)
                {
                    string folder = Server.MapPath("~/Uploads/Proposals/");

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    string fileName =
                        Guid.NewGuid().ToString() + "_" +
                        Path.GetFileName(proposalFile.FileName);

                    string fullPath = Path.Combine(folder, fileName);

                    proposalFile.SaveAs(fullPath);

                    documentPath = "/Uploads/Proposals/" + fileName;
                }

                Proposal proposal = new Proposal();

                proposal.StudentID =
                    student.StudentID;

                proposal.SubmissionPeriodID =
                    period.SubmissionPeriodID;

                proposal.Title =
                    title;

                proposal.ResearchField =
                    researchField;

                proposal.Abstract =
                    abstractText;

                proposal.Keywords =
                    keywords;

                proposal.SubmissionDate =
                    DateTime.Now;

                proposal.Status =
                    "Submitted";
                proposal.DocumentPath = documentPath;



                db.Proposals.Add(proposal);

                db.SaveChanges();
                // Find the supervisor
                var supervisor = db.Users
                    .FirstOrDefault(u => u.Role == "Supervisor");

                if (supervisor != null)
                {
                    Notification notification = new Notification
                    {
                        UserID = supervisor.UserID,
                        Message = "A new research proposal has been submitted and is waiting for your review.",
                        IsRead = false,
                        SentDate = DateTime.Now
                    };

                    db.Notifications.Add(notification);
                    db.SaveChanges();
                }
                ViewBag.SuccessMessage = "Your research proposal has been submitted successfully.";

                // Clear the form fields
                ModelState.Clear();
                return View();

               

                //return RedirectToAction(
                //    "Dashboard",
                //    "Student");
            }
            catch (Exception ex)
            {
                ViewBag.Message =
                    ex.Message;

                return View();
            }
        }
    }
}