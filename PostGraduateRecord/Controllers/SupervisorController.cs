using PostGraduateRecord.Models;
using PostgraduateRecordSystemApps;
using System;
using System.Linq;
using System.Web.Mvc;

namespace PostgraduateRecordSystemApps.Controllers
{
    public class SupervisorController : Controller
    {
        private readonly PostGradEntities db = new PostGradEntities();

        public ActionResult Dashboard()
        {
            // Temporary: first supervisor in the database
            var supervisor = db.Supervisors.FirstOrDefault();

            if (supervisor == null)
            {
                ViewBag.Message = "No supervisor found.";
                return View(Enumerable.Empty<SupervisorAssignment>());
            }

            var assignments = db.SupervisorAssignments
                                .Where(a => a.SupervisorID == supervisor.SupervisorID)
                                .ToList();

            ViewBag.SupervisorName = supervisor.User.FirstName + " " + supervisor.User.LastName;

            ViewBag.TotalStudents = assignments
                                    .Select(a => a.Proposal.StudentID)
                                    .Distinct()
                                    .Count();

            ViewBag.TotalProposals = assignments.Count();

            ViewBag.PendingProposals = assignments
                                        .Count(a => a.Proposal.Status == "Pending");

            ViewBag.ProgressReports = assignments
                                        .SelectMany(a => a.Proposal.ProgressReports)
                                        .Count();

            ViewBag.Notifications = db.Notifications.Count();

            return View(assignments);
        }
    }
}