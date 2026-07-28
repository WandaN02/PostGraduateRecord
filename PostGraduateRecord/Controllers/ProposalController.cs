using System;
using System.Linq;
using System.Web.Mvc;
using PostGraduateRecord.Models;

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
            string keywords)
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

                SubmissionPeriod period =
                    db.SubmissionPeriods
                    .FirstOrDefault(p => p.IsActive == true);



                if (period == null)
                {
                    ViewBag.Message =
                    "No active submission period.";

                    return View();
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



                db.Proposals.Add(proposal);

                db.SaveChanges();



                return RedirectToAction(
                    "Dashboard",
                    "Student");
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