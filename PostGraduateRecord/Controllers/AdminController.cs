using PostGraduateRecord.Models;
using PostGraduateRecord.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static System.Net.Mime.MediaTypeNames;

namespace PostGraduateRecord.Controllers
{
    public class AdminController : Controller
    {
        private PostGradEntities db = new PostGradEntities();

        // =========================================
        // ADMIN DASHBOARD
        // =========================================

        public ActionResult Dashboard()
        {
            var userId = Convert.ToInt32(Session["UserID"]);

            // =========================================
            // NOTIFICATIONS
            // =========================================

            ViewBag.UnreadNotifications =
                db.Notifications.Count(n =>
                    n.UserID == userId &&
                    n.IsRead != true);


            // =========================================
            // BASIC STATISTICS
            // =========================================

            ViewBag.TotalStudents =
                db.Students.Count();

            ViewBag.TotalSupervisors =
                db.Supervisors.Count();


            // =========================================
            // APPLICATION STATUS
            // =========================================

            ViewBag.PendingApplications =
                db.Applications.Count(a =>
                    a.Status == "Pending");

            ViewBag.ApprovedApplications =
                db.Applications.Count(a =>
                    a.Status == "Approved");

            ViewBag.RejectedApplications =
                db.Applications.Count(a =>
                    a.Status == "Rejected");

            ViewBag.TotalApplications =
                db.Applications.Count();


            // =========================================
            // PENDING SUPERVISOR ASSIGNMENTS
            // =========================================

            ViewBag.PendingAssignments =
                db.Applications.Count(a =>
                    a.Status == "Approved" &&
                    !db.SupervisorAssignments.Any(sa =>
                        sa.ApplicationID == a.ApplicationID));


            // =========================================
            // STUDENT PROGRESS
            // =========================================

            ViewBag.CompletedStudents =
                db.StudentMilestones
                    .Where(sm => sm.Status == "Completed")
                    .Select(sm => sm.StudentID)
                    .Distinct()
                    .Count();


            ViewBag.InProgressStudents =
                db.StudentMilestones
                    .Where(sm => sm.Status == "In Progress")
                    .Select(sm => sm.StudentID)
                    .Distinct()
                    .Count();


            ViewBag.PendingStudents =
                db.StudentMilestones
                    .Where(sm => sm.Status == "Pending")
                    .Select(sm => sm.StudentID)
                    .Distinct()
                    .Count();


            return View();
        }


        // =========================================
        // STUDENTS
        // =========================================

        public ActionResult Students()
        {
            var students = db.Students
                .Include(s => s.User)
                .ToList();

            return View(students);
        }


        // =========================================
        // SUPERVISORS
        // =========================================

        public ActionResult Supervisors()
        {
            var supervisors = db.Supervisors
                .Include(s => s.User)
                .ToList();

            return View(supervisors);
        }


        // =========================================
        // PENDING APPLICATIONS
        // =========================================

        public ActionResult PendingApplications()
        {
            var applications = db.Applications
                .Include(a => a.Student)
                .Include(a => a.Student.User)
                .Where(a => a.Status == "Pending")
                .OrderByDescending(a => a.SubmittedDate)
                .ToList();

            return View(applications);
        }


        // =========================================
        // PENDING SUPERVISOR ASSIGNMENTS
        // =========================================

        public ActionResult PendingAssignments()
        {
            var applications = db.Applications
                .Include(a => a.Student)
                .Include(a => a.Student.User)
                .Where(a =>
                    a.Status == "Approved" &&
                    !db.SupervisorAssignments.Any(sa =>
                        sa.ApplicationID == a.ApplicationID))
                .OrderByDescending(a => a.SubmittedDate)
                .ToList();

            return View(applications);
        }


        // =========================================
        // APPLICATIONS
        // =========================================

        public ActionResult Applications(string status)
        {
            var applications = db.Applications
                .Include(a => a.Student)
                .Include(a => a.Student.User)
                .AsQueryable();


            // =========================================
            // FILTER BY STATUS
            // =========================================

            if (!string.IsNullOrEmpty(status))
            {
                applications = applications
                    .Where(a => a.Status == status);
            }


            // =========================================
            // ORDER BY LATEST
            // =========================================

            var applicationList = applications
                .OrderByDescending(a => a.SubmittedDate)
                .ToList();


            ViewBag.SelectedStatus = status;


            return View(applicationList);
        }


        // =========================================
        // ETHICS APPLICATIONS
        // =========================================

        public ActionResult Ethics()
        {
            var ethicsApplications = db.EthicsApplications
                .Include(e => e.Student)
                .Include(e => e.Student.User)
                .Include(e => e.Proposal)
                .OrderByDescending(e => e.SubmissionDate)
                .ToList();


            // =========================================
            // ETHICS SUMMARY
            // =========================================

            ViewBag.TotalEthics =
                ethicsApplications.Count;

            ViewBag.SubmittedEthics =
                ethicsApplications.Count(e =>
                    e.Status == "Submitted");

            ViewBag.ResubmissionRequiredEthics =
                ethicsApplications.Count(e =>
                    e.Status == "Resubmission Required");

            ViewBag.ApprovedEthics =
                ethicsApplications.Count(e =>
                    e.Status == "Approved");


            return View(ethicsApplications);
        }


        // =========================================
        // VIEW ETHICS APPLICATION
        // =========================================

        public ActionResult ViewEthics(int id)
        {
            var ethicsApplication = db.EthicsApplications
                .Include(e => e.Student)
                .Include(e => e.Student.User)
                .Include(e => e.Proposal)
                .FirstOrDefault(e => e.EthicsID == id);

            if (ethicsApplication == null)
            {
                return HttpNotFound();
            }

            ViewBag.DocumentPath =
                ethicsApplication.DocumentPath;

            return View(ethicsApplication);
        }


        // =========================================
        // VIEW / DOWNLOAD ETHICS DOCUMENT
        // =========================================

        public ActionResult ViewEthicsDocument(int id)
        {
            var ethicsApplication = db.EthicsApplications
                .FirstOrDefault(e => e.EthicsID == id);

            if (ethicsApplication == null)
            {
                return HttpNotFound();
            }

            if (string.IsNullOrWhiteSpace(
                ethicsApplication.DocumentPath))
            {
                TempData["Error"] =
                    "No ethics document was uploaded.";

                return RedirectToAction(
                    "ViewEthics",
                    new { id = id });
            }

            var filePath =
                Server.MapPath(
                    ethicsApplication.DocumentPath);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] =
                    "The ethics document could not be found on the server.";

                return RedirectToAction(
                    "ViewEthics",
                    new { id = id });
            }

            string contentType =
                MimeMapping.GetMimeMapping(
                    Path.GetFileName(filePath));

            return File(
                filePath,
                contentType,
                Path.GetFileName(filePath)
            );
        }


        // =========================================
        // ADMIN TIMELINE
        // =========================================

        public ActionResult Timeline()
        {
            // ==========================================
            // GET ALL POSTGRADUATE STUDENTS
            // ==========================================

            var students = db.Students
                .Include(s => s.User)
                .ToList();

            var timeline =
                new List<SupervisorTimelineViewModel>();


            // ==========================================
            // BUILD TIMELINE FOR EACH STUDENT
            // ==========================================

            foreach (var student in students)
            {
                if (student == null)
                {
                    continue;
                }


                // ==========================================
                // STUDENT NAME
                // ==========================================

                string studentName = "N/A";

                if (student.User != null)
                {
                    studentName =
                        student.User.FirstName + " " +
                        student.User.LastName;
                }


                // ==========================================
                // APPLICATION
                // ==========================================

                var application = db.Applications
                    .Where(a =>
                        a.StudentID == student.StudentID)
                    .OrderByDescending(a => a.SubmittedDate)
                    .FirstOrDefault();


                // ==========================================
                // PROPOSAL
                // ==========================================

                var proposal = db.Proposals
                    .Where(p =>
                        p.StudentID == student.StudentID)
                    .OrderByDescending(p => p.SubmissionDate)
                    .FirstOrDefault();


                // ==========================================
                // CHECK ACTIVITY
                // ==========================================

                if (application == null &&
                    proposal == null)
                {
                    continue;
                }


                // ==========================================
                // CREATE TIMELINE MODEL
                // ==========================================

                var model =
                    new SupervisorTimelineViewModel
                    {
                        StudentID =
                            student.StudentID,

                        StudentName =
                            studentName,

                        ResearchTitle =
                            proposal != null
                                ? proposal.Title
                                : application != null
                                    ? application.ResearchTitle
                                    : "No research title",

                        Stages =
                            new List<TimelineStageViewModel>()
                    };


                // =====================================================
                // 1. APPLICATION
                // =====================================================

                if (application != null)
                {
                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Application",

                            Description =
                                "Postgraduate application submitted.",

                            Status =
                                application.Status == "Approved"
                                    ? "Completed"
                                    : "In Progress",

                            Date =
                                application.SubmittedDate,

                            Comments =
                                application.AdminComments
                        });
                }
                else
                {
                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Application",

                            Description =
                                "Postgraduate application has not been submitted.",

                            Status =
                                "Pending"
                        });
                }


                // =====================================================
                // 2. SUPERVISOR ASSIGNED
                // =====================================================

                SupervisorAssignment assignment = null;

                if (application != null)
                {
                    assignment =
                        db.SupervisorAssignments
                            .Where(a =>
                                a.ApplicationID ==
                                application.ApplicationID)
                            .OrderByDescending(
                                a => a.AssignedDate)
                            .FirstOrDefault();
                }


                if (assignment != null)
                {
                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Supervisor Assigned",

                            Description =
                                "A supervisor has been assigned to the student.",

                            Status =
                                "Completed",

                            Date =
                                assignment.AssignedDate
                        });
                }
                else
                {
                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Supervisor Assigned",

                            Description =
                                "A supervisor has not yet been assigned.",

                            Status =
                                "Pending"
                        });
                }


                // =====================================================
                // 3. RESEARCH PROPOSAL
                // =====================================================

                if (proposal != null)
                {
                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Research Proposal",

                            Description =
                                "Research proposal submitted by the student.",

                            Status =
                                proposal.SubmissionDate.HasValue
                                    ? "Completed"
                                    : "Pending",

                            Date =
                                proposal.SubmissionDate
                        });
                }
                else
                {
                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Research Proposal",

                            Description =
                                "Research proposal has not been submitted yet.",

                            Status =
                                "Pending"
                        });
                }


                // =====================================================
                // 4. SUPERVISOR REVIEW
                // =====================================================

                if (proposal != null)
                {
                    var feedback =
                        proposal.Feedbacks
                            .OrderByDescending(
                                f => f.FeedbackDate)
                            .FirstOrDefault();

                    if (feedback != null)
                    {
                        string reviewStatus;

                        if (feedback.Decision ==
                            "Approved")
                        {
                            reviewStatus =
                                "Completed";
                        }
                        else if (feedback.Decision ==
                                 "Rejected")
                        {
                            reviewStatus =
                                "Completed";
                        }
                        else if (feedback.Decision ==
                                 "Revision Required")
                        {
                            reviewStatus =
                                "In Progress";
                        }
                        else
                        {
                            reviewStatus =
                                "Pending";
                        }

                        model.Stages.Add(
                            new TimelineStageViewModel
                            {
                                StageName =
                                    "Supervisor Review",

                                Description =
                                    "Supervisor reviewed the research proposal.",

                                Status =
                                    reviewStatus,

                                Date =
                                    feedback.FeedbackDate,

                                Comments =
                                    feedback.Comments
                            });
                    }
                    else
                    {
                        model.Stages.Add(
                            new TimelineStageViewModel
                            {
                                StageName =
                                    "Supervisor Review",

                                Description =
                                    "Supervisor review is pending.",

                                Status =
                                    "Pending"
                            });
                    }
                }
                else
                {
                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Supervisor Review",

                            Description =
                                "Supervisor review will begin after the proposal is submitted.",

                            Status =
                                "Pending"
                        });
                }


                // =====================================================
                // 5. EVALUATION
                // =====================================================

                if (proposal != null)
                {
                    var evaluation =
                        proposal.Evaluations
                            .OrderByDescending(
                                e => e.EvaluationDate)
                            .FirstOrDefault();

                    if (evaluation != null)
                    {
                        string evaluationStatus;

                        if (!string.IsNullOrEmpty(
                            evaluation.Recommendation))
                        {
                            evaluationStatus =
                                "Completed";
                        }
                        else
                        {
                            evaluationStatus =
                                "In Progress";
                        }

                        model.Stages.Add(
                            new TimelineStageViewModel
                            {
                                StageName =
                                    "Evaluation",

                                Description =
                                    "Assigned evaluator reviews the research proposal.",

                                Status =
                                    evaluationStatus,

                                Date =
                                    evaluation.EvaluationDate,

                                Comments =
                                    evaluation.Comments
                            });
                    }
                    else
                    {
                        model.Stages.Add(
                            new TimelineStageViewModel
                            {
                                StageName =
                                    "Evaluation",

                                Description =
                                    "Evaluator evaluation is pending.",

                                Status =
                                    "Pending"
                            });
                    }
                }
                else
                {
                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Evaluation",

                            Description =
                                "Evaluation will begin after the proposal is approved.",

                            Status =
                                "Pending"
                        });
                }


                // =====================================================
                // 6. ETHICS
                // =====================================================

                if (proposal != null)
                {
                    var ethics =
                        proposal.EthicsApplications
                            .OrderByDescending(
                                e => e.SubmissionDate)
                            .FirstOrDefault();

                    if (ethics != null)
                    {
                        string ethicsStatus;

                        if (ethics.Status ==
                            "Approved")
                        {
                            ethicsStatus =
                                "Completed";
                        }
                        else if (
                            ethics.Status == "Submitted" ||
                            ethics.Status ==
                                "Resubmission Required")
                        {
                            ethicsStatus =
                                "In Progress";
                        }
                        else if (
                            ethics.Status == "Rejected")
                        {
                            ethicsStatus =
                                "Completed";
                        }
                        else
                        {
                            ethicsStatus =
                                "Pending";
                        }

                        DateTime? ethicsDate =
                            ethics.ApprovalDate.HasValue
                                ? ethics.ApprovalDate
                                : ethics.SubmissionDate;

                        model.Stages.Add(
                            new TimelineStageViewModel
                            {
                                StageName =
                                    "Ethics",

                                Description =
                                    "Ethics application and approval process.",

                                Status =
                                    ethicsStatus,

                                Date =
                                    ethicsDate,

                                Comments =
                                    ethics.Comments
                            });
                    }
                    else
                    {
                        model.Stages.Add(
                            new TimelineStageViewModel
                            {
                                StageName =
                                    "Ethics",

                                Description =
                                    "Ethics application has not been submitted.",

                                Status =
                                    "Pending"
                            });
                    }
                }
                else
                {
                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Ethics",

                            Description =
                                "Ethics application will follow proposal submission.",

                            Status =
                                "Pending"
                        });
                }


               


                // =====================================================
                // 7. FINAL Thesis
                // =====================================================

                var finalThesis =
                    db.Theses
                        .Where(t =>
                            t.StudentID ==
                            student.StudentID)
                        .OrderByDescending(
                            t => t.SubmittedDate)
                        .FirstOrDefault();

                if (finalThesis != null)
                {
                    string thesisStatus =
                        "Pending";

                    if (
                        finalThesis.Status ==
                            "Submitted" ||
                        finalThesis.Status ==
                            "Approved" ||
                        finalThesis.Status ==
                            "Completed")
                    {
                        thesisStatus =
                            "Completed";
                    }
                    else if (
                        finalThesis.Status ==
                            "Revision Required")
                    {
                        thesisStatus =
                            "In Progress";
                    }
                    else if (
                        finalThesis.SubmittedDate !=
                        DateTime.MinValue)
                    {
                        thesisStatus =
                            "Completed";
                    }

                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Final Submission",

                            Description =
                                "Final dissertation or thesis submitted.",

                            Status =
                                thesisStatus,

                            Date =
                                finalThesis.SubmittedDate
                        });
                }
                else
                {
                    model.Stages.Add(
                        new TimelineStageViewModel
                        {
                            StageName =
                                "Final Submission",

                            Description =
                                "Final dissertation or thesis has not been submitted yet.",

                            Status =
                                "Pending"
                        });
                }


                // =====================================================
                // CALCULATE PROGRESS
                // =====================================================

                model.TotalStages =
                    model.Stages.Count;

                model.CompletedStages =
                    model.Stages.Count(s =>
                        s.Status == "Completed" ||
                        s.Status == "Approved");


                if (model.TotalStages > 0)
                {
                    model.ProgressPercentage =
                        (model.CompletedStages * 100)
                        / model.TotalStages;
                }


                timeline.Add(model);
            }


            // =====================================================
            // SUMMARY
            // =====================================================

            ViewBag.StudentsWithProgress =
                timeline.Count(t =>
                    t.CompletedStages > 0);


            ViewBag.CompletedMilestones =
                timeline.Sum(t =>
                    t.CompletedStages);


            ViewBag.InProgressMilestones =
                timeline.SelectMany(t => t.Stages)
                    .Count(s =>
                        s.Status == "In Progress");


            ViewBag.PendingMilestones =
                timeline.SelectMany(t => t.Stages)
                    .Count(s =>
                        s.Status == "Pending");


            ViewBag.TotalMilestones =
                timeline.SelectMany(t => t.Stages)
                    .Count();


            return View(timeline);
        }


        // =========================================
        // ADMIN REPORTS
        // =========================================

        public ActionResult Reports()
        {
            // =========================================
            // GET ALL STUDENTS
            // =========================================

            var students = db.Students
                .Include(s => s.User)
                .ToList();


            // =========================================
            // CREATE REPORT VIEW MODEL
            // =========================================

            var report =
                new AdminReportViewModel();


            // =========================================
            // SUMMARY
            // =========================================

            report.TotalStudents =
                students.Count;

            report.CompletedStudents =
                0;


            // =========================================
            // ACTIVE STUDENTS
            // =========================================

            report.ActiveStudents =
                students.Count(s =>
                    db.Applications.Any(a =>
                        a.StudentID == s.StudentID)

                    ||

                    db.Proposals.Any(p =>
                        p.StudentID == s.StudentID)

                    ||

                    db.SupervisorAssignments.Any(sa =>
                        sa.Application.StudentID ==
                        s.StudentID)

                    ||

                    db.EthicsApplications.Any(e =>
                        e.StudentID == s.StudentID)

                    ||

                    db.StudentMilestones.Any(sm =>
                        sm.StudentID == s.StudentID)
                );


            // =========================================
            // TOTAL PROPOSALS
            // =========================================

            report.TotalProposals =
                db.Proposals.Count();


            // =========================================
            // TOTAL ETHICS APPLICATIONS
            // =========================================

            report.TotalEthicsApplications =
                db.EthicsApplications.Count();


            // =========================================
            // TOTAL EVALUATIONS
            // =========================================

            report.TotalEvaluations =
                db.Evaluations.Count();


            // =========================================
            // PROPOSAL STATUS
            // =========================================

            report.PendingProposals =
                db.Proposals.Count(p =>
                    p.Status == "Pending");

            report.ApprovedProposals =
                db.Proposals.Count(p =>
                    p.Status == "Approved");

            report.UnderEvaluationProposals =
                db.Proposals.Count(p =>
                    p.Status == "Under Evaluation");

            report.RejectedProposals =
                db.Proposals.Count(p =>
                    p.Status == "Rejected");

            report.RevisionRequiredProposals =
                db.Proposals.Count(p =>
                    p.Status == "Revision Required");


            // =========================================
            // ETHICS STATUS
            // =========================================

            report.PendingEthics =
                db.EthicsApplications.Count(e =>
                    e.Status == "Pending");

            report.SubmittedEthics =
                db.EthicsApplications.Count(e =>
                    e.Status == "Submitted");

            report.ApprovedEthics =
                db.EthicsApplications.Count(e =>
                    e.Status == "Approved");

            report.RejectedEthics =
                db.EthicsApplications.Count(e =>
                    e.Status == "Rejected");

            report.RevisionRequiredEthics =
                db.EthicsApplications.Count(e =>
                    e.Status == "Resubmission Required");


            // =========================================
            // EVALUATION STATUS
            // =========================================

            report.PendingEvaluations =
                db.Evaluations.Count(e =>
                    string.IsNullOrEmpty(
                        e.Recommendation) &&
                    e.EvaluationDate == null);

            report.InProgressEvaluations =
                db.Evaluations.Count(e =>
                    string.IsNullOrEmpty(
                        e.Recommendation) &&
                    e.EvaluationDate != null);

            report.CompletedEvaluations =
                db.Evaluations.Count(e =>
                    !string.IsNullOrEmpty(
                        e.Recommendation));


            // =========================================
            // STUDENT PROGRESS
            // =========================================

            foreach (var student in students)
            {
                if (student == null)
                {
                    continue;
                }


                // =========================================
                // STUDENT NAME
                // =========================================

                string studentName = "N/A";

                if (student.User != null)
                {
                    studentName =
                        student.User.FirstName + " " +
                        student.User.LastName;
                }


                // =========================================
                // STUDENT NUMBER
                // =========================================

                string studentNumber = "N/A";

                if (student.User != null)
                {
                    studentNumber =
                        student.User.StudentNumber;
                }


                // =========================================
                // GET APPLICATION
                // =========================================

                var application =
                    db.Applications
                        .Where(a =>
                            a.StudentID ==
                            student.StudentID)
                        .OrderByDescending(
                            a => a.SubmittedDate)
                        .FirstOrDefault();


                // =========================================
                // GET PROPOSAL
                // =========================================

                var proposal =
                    db.Proposals
                        .Where(p =>
                            p.StudentID ==
                            student.StudentID)
                        .OrderByDescending(
                            p => p.SubmissionDate)
                        .FirstOrDefault();


                // =========================================
                // RESEARCH TITLE
                // =========================================

                string researchTitle =
                    proposal != null
                        ? proposal.Title
                        : application != null
                            ? application.ResearchTitle
                            : "No research title";


                // =========================================
                // 7 POSTGRADUATE STAGES
                // =========================================

                int completedStages = 0;

                int totalStages = 7;


                // =====================================================
                // 1. APPLICATION
                // =====================================================

                if (
                    application != null &&
                    application.Status == "Approved")
                {
                    completedStages++;
                }


                // =====================================================
                // 2. SUPERVISOR ASSIGNED
                // =====================================================

                if (application != null)
                {
                    bool supervisorAssigned =
                        db.SupervisorAssignments.Any(sa =>
                            sa.ApplicationID ==
                            application.ApplicationID);

                    if (supervisorAssigned)
                    {
                        completedStages++;
                    }
                }


                // =====================================================
                // 3. RESEARCH PROPOSAL
                // =====================================================

                if (
                    proposal != null &&
                    proposal.SubmissionDate.HasValue)
                {
                    completedStages++;
                }


                // =====================================================
                // 4. SUPERVISOR REVIEW
                // =====================================================

                if (proposal != null)
                {
                    var feedback =
                        proposal.Feedbacks
                            .OrderByDescending(
                                f => f.FeedbackDate)
                            .FirstOrDefault();

                    if (
                        feedback != null &&
                        (
                            feedback.Decision ==
                                "Approved" ||
                            feedback.Decision ==
                                "Rejected"
                        ))
                    {
                        completedStages++;
                    }
                }


                // =====================================================
                // 5. EVALUATION
                // =====================================================

                if (proposal != null)
                {
                    var evaluation =
                        proposal.Evaluations
                            .OrderByDescending(
                                e => e.EvaluationDate)
                            .FirstOrDefault();

                    if (
                        evaluation != null &&
                        !string.IsNullOrEmpty(
                            evaluation.Recommendation))
                    {
                        completedStages++;
                    }
                }


                // =====================================================
                // 6. ETHICS
                // =====================================================

                if (proposal != null)
                {
                    var ethics =
                        proposal.EthicsApplications
                            .OrderByDescending(
                                e => e.SubmissionDate)
                            .FirstOrDefault();

                    if (
                        ethics != null &&
                        (
                            ethics.Status ==
                                "Submitted" ||
                            ethics.Status ==
                                "Approved" ||
                            ethics.Status ==
                                "Resubmission Required"
                        ))
                    {
                        completedStages++;
                    }
                }


             

                // =====================================================
                // 7. FINAL SUBMISSION
                // =====================================================

                bool finalThesisCompleted =
                    db.Theses.Any(t =>
                        t.StudentID ==
                        student.StudentID &&
                        (
                            t.Status ==
                                "Submitted" ||
                            t.Status ==
                                "Approved" ||
                            t.Status ==
                                "Completed"
                        ));

                if (finalThesisCompleted)
                {
                    completedStages++;
                }


                // =========================================
                // CALCULATE PERCENTAGE
                // =========================================

                int progressPercentage =
                    (completedStages * 100) /
                    totalStages;


                // =========================================
                // DETERMINE STATUS
                // =========================================

                string progressStatus;

                if (completedStages == 0)
                {
                    progressStatus =
                        "Not Started";
                }
                else if (
                    completedStages ==
                    totalStages)
                {
                    progressStatus =
                        "Completed";
                }
                else
                {
                    progressStatus =
                        "In Progress";
                }


                if (progressStatus ==
                    "Completed")
                {
                    report.CompletedStudents++;
                }


                // =========================================
                // ADD STUDENT REPORT
                // =========================================

                report.StudentProgress.Add(
                    new AdminStudentProgressViewModel
                    {
                        StudentID =
                            student.StudentID,

                        StudentName =
                            studentName,

                        StudentNumber =
                            studentNumber,

                        ResearchTitle =
                            researchTitle,

                        ProgressPercentage =
                            progressPercentage,

                        Status =
                            progressStatus
                    }
                );
            }


            // =========================================
            // SUPERVISOR WORKLOAD
            // =========================================

            var supervisors =
                db.Supervisors
                    .Include(s => s.User)
                    .ToList();


            foreach (var supervisor in supervisors)
            {
                if (supervisor == null)
                {
                    continue;
                }


                // =========================================
                // SUPERVISOR NAME
                // =========================================

                string supervisorName = "N/A";

                if (supervisor.User != null)
                {
                    supervisorName =
                        supervisor.User.FirstName + " " +
                        supervisor.User.LastName;
                }


                // =========================================
                // ASSIGNED STUDENTS
                // =========================================

                int assignedStudents =
                    db.SupervisorAssignments
                        .Where(sa =>
                            sa.SupervisorID ==
                            supervisor.SupervisorID)
                        .Select(sa =>
                            sa.Application.StudentID)
                        .Distinct()
                        .Count();


                // =========================================
                // PENDING REVIEWS
                // =========================================

                int pendingReviews =
                    db.SupervisorAssignments
                        .Where(sa =>
                            sa.SupervisorID ==
                            supervisor.SupervisorID)
                        .Select(sa =>
                            sa.Application.StudentID)
                        .Distinct()
                        .Count(studentID =>
                            !db.Proposals.Any(p =>
                                p.StudentID ==
                                studentID)
                        );


                // =========================================
                // ADD SUPERVISOR REPORT
                // =========================================

                report.SupervisorWorkload.Add(
                    new AdminSupervisorReportViewModel
                    {
                        SupervisorID =
                            supervisor.SupervisorID,

                        SupervisorName =
                            supervisorName,

                        AssignedStudents =
                            assignedStudents,

                        PendingReviews =
                            pendingReviews
                    }
                );
            }


            // =========================================
            // RETURN REPORT
            // =========================================

            return View(report);
        }


        // =========================================
        // REVIEW APPLICATION
        // =========================================

        public ActionResult ReviewApplication(int id)
        {
            var application =
                db.Applications
                    .Include(a => a.Student.User)
                    .Include(a =>
                        a.ApplicationDocuments)
                    .FirstOrDefault(
                        a => a.ApplicationID == id);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        // =========================================
        // DOWNLOAD APPLICATION DOCUMENT
        // =========================================

        public ActionResult DownloadApplication(int id)
        {
            var document =
                db.ApplicationDocuments
                    .FirstOrDefault(d =>
                        d.ApplicationID == id);

            if (document == null)
            {
                TempData["Error"] =
                    "No supporting document was found for this application.";

                return RedirectToAction("Applications");
            }

            if (string.IsNullOrWhiteSpace(document.FilePath))
            {
                TempData["Error"] =
                    "The application document has no file path.";

                return RedirectToAction("Applications");
            }

            string virtualPath =
                document.FilePath;

            // =========================================
            // CONVERT OLD SERVER PATH
            // =========================================

            if (virtualPath.StartsWith(@"\\"))
            {
                int uploadsIndex =
                    virtualPath.IndexOf(
                        @"\Uploads\",
                        StringComparison.OrdinalIgnoreCase);

                if (uploadsIndex >= 0)
                {
                    virtualPath =
                        virtualPath
                            .Substring(uploadsIndex)
                            .Replace("\\", "/");
                }
            }

            // =========================================
            // MAKE SURE PATH STARTS WITH /
            // =========================================

            if (!virtualPath.StartsWith("/"))
            {
                virtualPath =
                    "/" + virtualPath;
            }

            // =========================================
            // GET PHYSICAL SERVER PATH
            // =========================================

            string fullPath =
                Server.MapPath("~" + virtualPath);

            // =========================================
            // CHECK FILE EXISTS
            // =========================================

            if (!System.IO.File.Exists(fullPath))
            {
                return HttpNotFound(
                    "The application document could not be found.");
            }

            // =========================================
            // FORCE DOWNLOAD
            // =========================================

            string fileName =
                System.IO.Path.GetFileName(fullPath);

            string contentType =
                MimeMapping.GetMimeMapping(fileName);

            return File(
                fullPath,
                contentType,
                fileName);
        }

        // =========================================
        // VIEW APPLICATION DOCUMENT
        // =========================================

        public ActionResult ViewApplicationDocument(int id)
        {
            var document =
                db.ApplicationDocuments
                    .FirstOrDefault(d =>
                        d.ApplicationID == id);

            if (document == null)
            {
                TempData["Error"] =
                    "No supporting document was found for this application.";

                return RedirectToAction("Applications");
            }

            if (string.IsNullOrWhiteSpace(document.FilePath))
            {
                TempData["Error"] =
                    "The application document has no file path.";

                return RedirectToAction("Applications");
            }

            string virtualPath = document.FilePath;

            // =========================================
            // CONVERT OLD SERVER PATH
            // =========================================

            if (virtualPath.StartsWith(@"\\"))
            {
                int uploadsIndex =
                    virtualPath.IndexOf(
                        @"\Uploads\",
                        StringComparison.OrdinalIgnoreCase);

                if (uploadsIndex >= 0)
                {
                    virtualPath =
                        virtualPath
                            .Substring(uploadsIndex)
                            .Replace("\\", "/");
                }
            }

            // =========================================
            // MAKE SURE PATH STARTS WITH /
            // =========================================

            if (!virtualPath.StartsWith("/"))
            {
                virtualPath =
                    "/" + virtualPath;
            }

            // =========================================
            // OPEN THROUGH THE WEBSITE
            // =========================================

            string url =
                Url.Content("~" + virtualPath);

            return Redirect(url);
        }
        // =========================================
        // APPROVE APPLICATION
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveApplication(
     int id,
     string adminComments)
        {
            var application =
                db.Applications
                    .FirstOrDefault(
                        a => a.ApplicationID == id);

            if (application == null)
            {
                return HttpNotFound();
            }

            application.Status =
                "Approved";

            application.ReviewedDate =
                DateTime.Now;

            application.AdminComments =
                adminComments;

            db.SaveChanges();

            TempData["Success"] =
                "Application approved successfully.";

            return RedirectToAction(
                "Applications",
                "Admin"
            );
        }
        // =========================================
        // REJECT APPLICATION
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectApplication(
            int id)
        {
            var application =
                db.Applications
                    .FirstOrDefault(
                        a => a.ApplicationID == id);

            if (application == null)
            {
                return HttpNotFound();
            }

            application.Status =
                "Rejected";

            application.ReviewedDate =
                DateTime.Now;

            db.SaveChanges();

            TempData["Success"] =
                "Application has been rejected.";

            return RedirectToAction(
                "Applications");
        }


        // =========================================
        // ASSIGN SUPERVISOR - GET
        // =========================================

        public ActionResult AssignSupervisor(
            int applicationId)
        {
            var application =
                db.Applications
                    .FirstOrDefault(
                        a => a.ApplicationID ==
                             applicationId);

            if (application == null)
            {
                return HttpNotFound();
            }

            ViewBag.Supervisors =
                db.Supervisors.ToList();

            return View(application);
        }


        // =========================================
        // ASSIGN SUPERVISOR - POST
        // =========================================

        [HttpPost]
        public ActionResult AssignSupervisor(
            int applicationId,
            int supervisorId)
        {
            var existingAssignment =
                db.SupervisorAssignments
                    .FirstOrDefault(
                        a =>
                            a.ApplicationID ==
                            applicationId);

            // Get application and student
            var application =
                db.Applications
                    .Include(a => a.Student.User)
                    .FirstOrDefault(
                        a =>
                            a.ApplicationID ==
                            applicationId);

            if (application == null)
            {
                return HttpNotFound();
            }


            // Get supervisor and user
            var supervisor =
                db.Supervisors
                    .Include(s => s.User)
                    .FirstOrDefault(
                        s =>
                            s.SupervisorID ==
                            supervisorId);

            if (supervisor == null)
            {
                return HttpNotFound();
            }


            // Create supervisor assignment
            SupervisorAssignment assignment =
                new SupervisorAssignment
                {
                    ApplicationID =
                        applicationId,

                    SupervisorID =
                        supervisorId,

                    AssignedDate =
                        DateTime.Now,

                    Status =
                        "Assigned"
                };

            db.SupervisorAssignments.Add(
                assignment);


            // Notify student
            Notification studentNotification =
                new Notification
                {
                    UserID =
                        application.Student.UserID,

                    Message =
                        "You have been assigned to Supervisor: "
                        + supervisor.User.FirstName
                        + " "
                        + supervisor.User.LastName,

                    IsRead =
                        false,

                    SentDate =
                        DateTime.Now
                };

            db.Notifications.Add(
                studentNotification);


            // Notify supervisor
            Notification supervisorNotification =
                new Notification
                {
                    UserID =
                        supervisor.UserID,

                    Message =
                        "A new research application has been assigned to you for review.",

                    IsRead =
                        false,

                    SentDate =
                        DateTime.Now
                };

            db.Notifications.Add(
                supervisorNotification);


            db.SaveChanges();


            return RedirectToAction(
                "Applications");
        }


        // =========================================
        // NOTIFICATIONS
        // =========================================

        public ActionResult Notifications()
        {
            var userId =
                Convert.ToInt32(
                    Session["UserID"]);

            var notifications =
                db.Notifications
                    .Where(n =>
                        n.UserID == userId)
                    .OrderByDescending(
                        n => n.SentDate)
                    .ToList();

            return View(notifications);
        }


        public ActionResult NotificationDropdown()
        {
            var userId =
                Convert.ToInt32(
                    Session["UserID"]);

            var notifications =
                db.Notifications
                    .Where(n =>
                        n.UserID == userId)
                    .OrderByDescending(
                        n => n.SentDate)
                    .Take(5)
                    .ToList();

            return PartialView(
                "_NotificationDropdown",
                notifications);
        }


        [ChildActionOnly]
        public ActionResult NotificationCount()
        {
            var userId =
                Convert.ToInt32(
                    Session["UserID"]);

            var count =
                db.Notifications.Count(n =>
                    n.UserID == userId &&
                    n.IsRead != true);

            return Content(
                count.ToString());
        }


        // =========================================
        // REGISTER STUDENT - GET
        // =========================================

        public ActionResult RegisterStudent()
        {
            ViewBag.Departments =
                new SelectList(
                    db.Departments
                        .Where(d => d.IsActive),
                    "DepartmentID",
                    "DepartmentName"
                );

            return View();
        }


        // =========================================
        // REGISTER STUDENT - POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterStudent(
            string StudentNumber,
            string FirstName,
            string LastName,
            string Email,
            string PhoneNumber,
            int DepartmentID,
            string Qualification,
            string DegreeType,
            string StudyType,
            int RegistrationYear)
        {
            try
            {
                // ==============================
                // SERVER-SIDE VALIDATION
                // ==============================

                if (
                    string.IsNullOrWhiteSpace(
                        StudentNumber) ||

                    string.IsNullOrWhiteSpace(
                        FirstName) ||

                    string.IsNullOrWhiteSpace(
                        LastName) ||

                    string.IsNullOrWhiteSpace(
                        Email) ||

                    string.IsNullOrWhiteSpace(
                        PhoneNumber) ||

                    string.IsNullOrWhiteSpace(
                        Qualification) ||

                    string.IsNullOrWhiteSpace(
                        DegreeType) ||

                    string.IsNullOrWhiteSpace(
                        StudyType))
                {
                    TempData["Error"] =
                        "Please complete all required fields.";

                    return RedirectToAction(
                        "RegisterStudent");
                }


                // Student number
                if (
                    StudentNumber.Length != 9 ||
                    !StudentNumber.All(
                        char.IsDigit))
                {
                    TempData["Error"] =
                        "Student number must contain exactly 9 digits.";

                    return RedirectToAction(
                        "RegisterStudent");
                }


                // Phone number
                if (
                    PhoneNumber.Length != 10 ||
                    !PhoneNumber.All(
                        char.IsDigit) ||
                    !PhoneNumber.StartsWith("0"))
                {
                    TempData["Error"] =
                        "Phone number must contain exactly 10 digits and start with 0.";

                    return RedirectToAction(
                        "RegisterStudent");
                }


                // Registration year
                if (
                    RegistrationYear <
                    DateTime.Now.Year)
                {
                    TempData["Error"] =
                        "Registration year cannot be earlier than the current year.";

                    return RedirectToAction(
                        "RegisterStudent");
                }


                // ==============================
                // CHECK DUPLICATES
                // ==============================

                var existingStudentNumber =
                    db.Users
                        .FirstOrDefault(
                            u =>
                                u.StudentNumber ==
                                StudentNumber);

                if (existingStudentNumber != null)
                {
                    TempData["Error"] =
                        "A student with this student number already exists.";

                    return RedirectToAction(
                        "Students");
                }


                var existingEmail =
                    db.Users
                        .FirstOrDefault(
                            u =>
                                u.Email ==
                                Email);

                if (existingEmail != null)
                {
                    TempData["Error"] =
                        "A user with this email address already exists.";

                    return RedirectToAction(
                        "Students");
                }


                // ==============================
                // CHECK DEPARTMENT
                // ==============================

                var department =
                    db.Departments
                        .FirstOrDefault(
                            d =>
                                d.DepartmentID ==
                                DepartmentID &&
                                d.IsActive);

                if (department == null)
                {
                    TempData["Error"] =
                        "Please select a valid active department.";

                    return RedirectToAction(
                        "RegisterStudent");
                }


                // ==============================
                // CREATE USER
                // ==============================

                User newUser =
                    new User
                    {
                        StudentNumber =
                            StudentNumber.Trim(),

                        EmployeeNumber =
                            null,

                        FirstName =
                            FirstName.Trim(),

                        LastName =
                            LastName.Trim(),

                        Email =
                            Email.Trim(),

                        PhoneNumber =
                            PhoneNumber.Trim(),

                        Role =
                            "Student",

                        IsActive =
                            true,

                        CreatedDate =
                            DateTime.Now
                    };

                db.Users.Add(newUser);

                db.SaveChanges();


                // ==============================
                // CREATE STUDENT PROFILE
                // ==============================

                Student newStudent =
                    new Student
                    {
                        UserID =
                            newUser.UserID,

                        DepartmentID =
                            DepartmentID,

                        Qualification =
                            Qualification.Trim(),

                        DegreeType =
                            DegreeType,

                        StudyType =
                            StudyType,

                        RegistrationYear =
                            RegistrationYear
                    };

                db.Students.Add(
                    newStudent);

                db.SaveChanges();


                // ==============================
                // SUCCESS
                // ==============================

                TempData["Success"] =
                    "Student registered successfully.";

                return RedirectToAction(
                    "Students");
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "An unexpected error occurred while registering the student. Please try again.";

                return RedirectToAction(
                    "RegisterStudent");
            }
        }

        // =========================================
        // SEND STUDENT PROGRESS NOTIFICATION
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendProgressNotification(
            int studentId,
            string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] =
                    "Please enter a message before sending.";

                return RedirectToAction("Reports");
            }

            var student =
                db.Students
                    .Include(s => s.User)
                    .FirstOrDefault(s =>
                        s.StudentID == studentId);

            if (student == null ||
                student.User == null)
            {
                TempData["Error"] =
                    "Student could not be found.";

                return RedirectToAction("Reports");
            }

            Notification notification =
                new Notification
                {
                    UserID =
                        student.User.UserID,

                    Message =
                        message.Trim(),

                    IsRead =
                        false,

                    SentDate =
                        DateTime.Now
                };

            db.Notifications.Add(
                notification);

            db.SaveChanges();

            TempData["Success"] =
                "Notification sent successfully to "
                + student.User.FirstName
                + " "
                + student.User.LastName
                + ".";

            return RedirectToAction("Reports");
        }
        // =========================================
        // COMPLETED STUDENTS
        // =========================================

        public ActionResult CompletedStudents()
        {
            var students =
                db.Students
                    .Include(s => s.User)
                    .ToList();

            var completedStudents =
                new List<Student>();

            foreach (var student in students)
            {
                int completedStages = 0;

                int totalStages = 7;


                // 1. APPLICATION
                var application =
                    db.Applications
                        .Where(a =>
                            a.StudentID ==
                            student.StudentID)
                        .OrderByDescending(
                            a => a.SubmittedDate)
                        .FirstOrDefault();

                if (
                    application != null &&
                    application.Status ==
                        "Approved")
                {
                    completedStages++;
                }


                // 2. SUPERVISOR ASSIGNED
                if (application != null)
                {
                    bool supervisorAssigned =
                        db.SupervisorAssignments.Any(
                            sa =>
                                sa.ApplicationID ==
                                application.ApplicationID);

                    if (supervisorAssigned)
                    {
                        completedStages++;
                    }
                }


                // 3. RESEARCH PROPOSAL
                var proposal =
                    db.Proposals
                        .Where(p =>
                            p.StudentID ==
                            student.StudentID)
                        .OrderByDescending(
                            p => p.SubmissionDate)
                        .FirstOrDefault();

                if (
                    proposal != null &&
                    proposal.SubmissionDate.HasValue)
                {
                    completedStages++;
                }


                // 4. SUPERVISOR REVIEW
                if (proposal != null)
                {
                    var feedback =
                        proposal.Feedbacks
                            .OrderByDescending(
                                f => f.FeedbackDate)
                            .FirstOrDefault();

                    if (
                        feedback != null &&
                        (
                            feedback.Decision ==
                                "Approved" ||
                            feedback.Decision ==
                                "Rejected"
                        ))
                    {
                        completedStages++;
                    }
                }


                // 5. EVALUATION
                if (proposal != null)
                {
                    var evaluation =
                        proposal.Evaluations
                            .OrderByDescending(
                                e => e.EvaluationDate)
                            .FirstOrDefault();

                    if (
                        evaluation != null &&
                        !string.IsNullOrEmpty(
                            evaluation.Recommendation))
                    {
                        completedStages++;
                    }
                }


                // 6. ETHICS
                if (proposal != null)
                {
                    var ethics =
                        proposal.EthicsApplications
                            .OrderByDescending(
                                e => e.SubmissionDate)
                            .FirstOrDefault();

                    if (
                        ethics != null &&
                        (
                            ethics.Status ==
                                "Submitted" ||
                            ethics.Status ==
                                "Approved" ||
                            ethics.Status ==
                                "Resubmission Required"
                        ))
                    {
                        completedStages++;
                    }
                }


                // 7. RESEARCH PROGRESS
                bool progressCompleted =
                    db.ProgressReports.Any(
                        r =>
                            r.StudentID ==
                            student.StudentID &&
                            (
                                r.Status ==
                                    "Submitted" ||
                                r.Status ==
                                    "Approved" ||
                                r.Status ==
                                    "Completed"
                            ));

                if (progressCompleted)
                {
                    completedStages++;
                }


                // 8. FINAL SUBMISSION
                bool thesisCompleted =
                    db.Theses.Any(
                        t =>
                            t.StudentID ==
                            student.StudentID &&
                            (
                                t.Status ==
                                    "Submitted" ||
                                t.Status ==
                                    "Approved" ||
                                t.Status ==
                                    "Completed"
                            ));

                if (thesisCompleted)
                {
                    completedStages++;
                }


                // ONLY ADD FULLY COMPLETED STUDENTS
                if (
                    completedStages ==
                    totalStages)
                {
                    completedStudents.Add(
                        student);
                }
            }

            return View(
                completedStudents);
        }
        // =========================================
        // APPROVED STUDENTS
        // =========================================

        public ActionResult ApprovedStudents()
        {
            // Get all approved applications
            var applications = db.Applications
                .Include(a => a.Student)
                .Include(a => a.Student.User)
                .Where(a => a.Status == "Approved")
                .OrderByDescending(a => a.SubmittedDate)
                .ToList();

            // Build the Admin table data
            var model = applications.Select(application =>
            {
                // Find the supervisor assignment for this application
                var assignment = db.SupervisorAssignments
                    .Include(sa => sa.Supervisor)
                    .Include(sa => sa.Supervisor.User)
                    .Where(sa =>
                        sa.ApplicationID == application.ApplicationID)
                    .OrderByDescending(sa => sa.AssignedDate)
                    .FirstOrDefault();

                return new PostGraduateRecord.ViewModels.ApprovedStudentViewModel
                {
                    ApplicationID = application.ApplicationID,

                    StudentName =
                        application.Student != null &&
                        application.Student.User != null
                            ? application.Student.User.FirstName + " " +
                              application.Student.User.LastName
                            : "Unknown Student",

                    ResearchTitle =
                        application.ResearchTitle,

                    DegreeType =
                        application.DegreeType,

                    ResearchField =
                        application.ResearchField,

                    SubmittedDate =
                        application.SubmittedDate,

                    SupervisorName =
                        assignment != null &&
                        assignment.Supervisor != null &&
                        assignment.Supervisor.User != null
                            ? assignment.Supervisor.User.FirstName + " " +
                              assignment.Supervisor.User.LastName
                            : "Not Assigned",

                    AssignedDate =
                        assignment != null
                            ? assignment.AssignedDate
                            : null,
                    AssignmentStatus =
                          assignment != null
                             ? "Assigned"
                      : "Pending Assignment"
                };
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendApprovedStudentsToSupervisors()
        {
            var supervisors = db.Supervisors
                .Include(s => s.User)
                .ToList();

            if (!supervisors.Any())
            {
                TempData["Error"] =
                    "There are no supervisors available to notify.";

                return RedirectToAction("ApprovedStudents");
            }

            var approvedStudents = db.Applications
                .Where(a =>
                    a.Status == "Approved" &&
                    !db.SupervisorAssignments.Any(sa =>
                        sa.ApplicationID == a.ApplicationID))
                .ToList();

            if (!approvedStudents.Any())
            {
                TempData["Error"] =
                    "There are no approved students available to send.";

                return RedirectToAction("ApprovedStudents");
            }

            foreach (var supervisor in supervisors)
            {
                Notification notification =
                    new Notification
                    {
                        UserID = supervisor.UserID,

                        Message =
                            "A list of approved postgraduate students is now available. Please visit Available Students to choose a student to supervise.",

                        Link =
                            Url.Action(
                                "AvailableStudents",
                                "Supervisors"),

                        IsRead = false,

                        SentDate = DateTime.Now
                    };

                db.Notifications.Add(notification);
            }

            db.SaveChanges();

            TempData["Success"] =
                "The approved students list has been sent to all supervisors.";

            return RedirectToAction("ApprovedStudents");
        }

        // =========================================
        // DYNAMIC REPORTS - GET
        // =========================================

        public ActionResult DynamicReports()
        {
            var model = new ReportFilterViewModel();

            LoadReportFilterData(model);

            return View(model);
        }


        // =========================================
        // DYNAMIC REPORTS - POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DynamicReports(ReportFilterViewModel model)
        {
            LoadReportFilterData(model);

            if (string.IsNullOrWhiteSpace(model.ReportType))
            {
                ModelState.AddModelError(
                    "ReportType",
                    "Please select a report type.");

                return View(model);
            }

            // =========================================
            // CREATE REPORT MODEL
            // =========================================

            var report = new DynamicReportViewModel
            {
                ReportType = model.ReportType,
                ReportTitle = GetReportTitle(model.ReportType),
                ReportDescription = GetReportDescription(model.ReportType),
                GeneratedDate = DateTime.Now.ToString("dd MMMM yyyy HH:mm"),
                GeneratedBy = "SPGS Administrator",
                Filters = model
            };


            // =========================================
            // STUDENTS REPORT
            // =========================================

            if (model.ReportType == "Students")
            {
                var students = db.Students
                    .Include("User")
                    .Include("Department")
                    .AsQueryable();

                if (model.DepartmentID.HasValue)
                {
                    students = students.Where(s =>
                        s.DepartmentID == model.DepartmentID.Value);
                }

                if (model.RegistrationYear.HasValue)
                {
                    students = students.Where(s =>
                        s.RegistrationYear == model.RegistrationYear.Value);
                }

                if (!string.IsNullOrWhiteSpace(model.Qualification))
                {
                    students = students.Where(s =>
                        s.Qualification == model.Qualification);
                }

                if (!string.IsNullOrWhiteSpace(model.StudyType))
                {
                    students = students.Where(s =>
                        s.StudyType == model.StudyType);
                }

                if (!string.IsNullOrWhiteSpace(model.Search))
                {
                    string search = model.Search.Trim();

                    students = students.Where(s =>
                        s.User.FirstName.Contains(search) ||
                        s.User.LastName.Contains(search) ||
                        s.User.StudentNumber.Contains(search) ||
                        s.Qualification.Contains(search));
                }

                var studentList = students
                    .OrderBy(s => s.User.LastName)
                    .ThenBy(s => s.User.FirstName)
                    .ToList();

                report.Columns = new List<string>
        {
            "Student Number",
            "Student Name",
            "Department",
            "Qualification",
            "Study Type",
            "Registration Year"
        };

                foreach (var student in studentList)
                {
                    report.Rows.Add(new Dictionary<string, string>
            {
                {
                    "Student Number",
                    student.User.StudentNumber ?? "-"
                },
                {
                    "Student Name",
                    ((student.User.FirstName ?? "") + " " +
                     (student.User.LastName ?? "")).Trim()
                },
                {
                    "Department",
                    student.Department != null
                        ? student.Department.DepartmentName
                        : "-"
                },
                {
                    "Qualification",
                    student.Qualification ?? "-"
                },
                {
                    "Study Type",
                    student.StudyType ?? "-"
                },
                {
                    "Registration Year",
                    student.RegistrationYear.HasValue
                        ? student.RegistrationYear.Value.ToString()
                        : "-"
                }
            });
                }

                report.TotalRecords = studentList.Count;
                report.ActiveRecords = studentList.Count;

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Total Students",
                        report.TotalRecords,
                        "Students matching the selected filters",
                        "fa-users",
                        "#001B44"));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Postgraduate Students",
                        studentList.Count,
                        "Students included in this report",
                        "fa-graduation-cap",
                        "#FDB913"));

                int masters = studentList.Count(s =>
                    !string.IsNullOrWhiteSpace(s.Qualification) &&
                    s.Qualification.IndexOf(
                        "Master",
                        StringComparison.OrdinalIgnoreCase) >= 0);

                int doctoral = studentList.Count(s =>
                    !string.IsNullOrWhiteSpace(s.Qualification) &&
                    (
                        s.Qualification.IndexOf(
                            "Doctor",
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        s.Qualification.IndexOf(
                            "PhD",
                            StringComparison.OrdinalIgnoreCase) >= 0
                    ));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Master's",
                        masters,
                        "Students registered at Master's level",
                        "fa-book",
                        "#2563eb",
                        report.TotalRecords));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Doctoral",
                        doctoral,
                        "Students registered at doctoral level",
                        "fa-flask",
                        "#7c3aed",
                        report.TotalRecords));

                AddStudentCharts(report, studentList);

                if (studentList.Count > 0)
                {
                    report.AnalysisSummary =
                        "The report contains " +
                        studentList.Count +
                        " postgraduate student record(s) matching the selected criteria.";

                    report.KeyFinding =
                        GetLargestCategory(
                            studentList
                                .GroupBy(s => s.Qualification ?? "Not Specified")
                                .Select(g => new
                                {
                                    Name = g.Key,
                                    Count = g.Count()
                                })
                                .OrderByDescending(x => x.Count)
                                .ToList(),
                            "qualification");

                    report.Insights.Add(
                        CreateInsight(
                            "Student Distribution",
                            report.KeyFinding,
                            "fa-chart-pie",
                            "info"));
                }
            }


            // =========================================
            // APPLICATIONS REPORT
            // =========================================

            else if (model.ReportType == "Applications")
            {
                var applications = db.Applications
                    .Include("Student.User")
                    .AsQueryable();

                if (model.DepartmentID.HasValue)
                {
                    applications = applications.Where(a =>
                        a.Student.DepartmentID == model.DepartmentID.Value);
                }

                if (!string.IsNullOrWhiteSpace(model.Status))
                {
                    applications = applications.Where(a =>
                        a.Status == model.Status);
                }

                if (model.StartDate.HasValue)
                {
                    applications = applications.Where(a =>
                        a.SubmittedDate >= model.StartDate.Value);
                }

                if (model.EndDate.HasValue)
                {
                    DateTime endDate =
                        model.EndDate.Value.Date.AddDays(1);

                    applications = applications.Where(a =>
                        a.SubmittedDate < endDate);
                }

                if (!string.IsNullOrWhiteSpace(model.Search))
                {
                    string search = model.Search.Trim();

                    applications = applications.Where(a =>
                        a.ResearchTitle.Contains(search) ||
                        a.Student.User.FirstName.Contains(search) ||
                        a.Student.User.LastName.Contains(search) ||
                        a.Student.User.StudentNumber.Contains(search));
                }

                var applicationList = applications
                    .OrderByDescending(a => a.SubmittedDate)
                    .ToList();

                report.Columns = new List<string>
        {
            "Student Number",
            "Student",
            "Research Title",
            "Degree Type",
            "Research Field",
            "Status",
            "Submitted Date"
        };

                foreach (var application in applicationList)
                {
                    report.Rows.Add(new Dictionary<string, string>
            {
                {
                    "Student Number",
                    application.Student.User.StudentNumber ?? "-"
                },
                {
                    "Student",
                    ((application.Student.User.FirstName ?? "") + " " +
                     (application.Student.User.LastName ?? "")).Trim()
                },
                {
                    "Research Title",
                    application.ResearchTitle ?? "-"
                },
                {
                    "Degree Type",
                    application.DegreeType ?? "-"
                },
                {
                    "Research Field",
                    application.ResearchField ?? "-"
                },
                {
                    "Status",
                    application.Status ?? "Pending"
                },
                {
                    "Submitted Date",
                    application.SubmittedDate.ToString("dd MMM yyyy")
                }
            });
                }

                report.TotalRecords = applicationList.Count;

                report.CompletedRecords = applicationList.Count(a =>
                    IsStatus(a.Status, "Approved"));

                report.PendingRecords = applicationList.Count(a =>
                    IsStatus(a.Status, "Pending") ||
                    IsStatus(a.Status, "Submitted") ||
                    IsStatus(a.Status, "Under Review"));

                int rejectedApplications = applicationList.Count(a =>
                    IsStatus(a.Status, "Rejected"));

                report.ActiveRecords =
                    report.TotalRecords -
                    rejectedApplications;

                report.AtRiskRecords = rejectedApplications;

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Total Applications",
                        report.TotalRecords,
                        "Applications matching the selected filters",
                        "fa-file-text",
                        "#001B44"));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Approved",
                        report.CompletedRecords,
                        "Applications approved",
                        "fa-check-circle",
                        "#16a34a",
                        report.TotalRecords));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Pending",
                        report.PendingRecords,
                        "Applications awaiting processing",
                        "fa-clock-o",
                        "#FDB913",
                        report.TotalRecords));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Rejected",
                        rejectedApplications,
                        "Applications that were rejected",
                        "fa-times-circle",
                        "#dc2626",
                        report.TotalRecords));

                AddStatusChart(
                    report,
                    applicationList
                        .GroupBy(a =>
                            string.IsNullOrWhiteSpace(a.Status)
                                ? "Pending"
                                : a.Status)
                        .Select(g => new
                        {
                            Label = g.Key,
                            Count = g.Count()
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList(),
                    "Applications by Status");

                AddCategoryChart(
                    report,
                    applicationList
                        .GroupBy(a =>
                            string.IsNullOrWhiteSpace(a.DegreeType)
                                ? "Not Specified"
                                : a.DegreeType)
                        .Select(g => new
                        {
                            Label = g.Key,
                            Count = g.Count()
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList(),
                    "Applications by Degree Type",
                    "bar");

                if (applicationList.Count > 0)
                {
                    report.AnalysisSummary =
                        "A total of " +
                        report.TotalRecords +
                        " application(s) were identified. " +
                        report.CompletedRecords +
                        " are approved, while " +
                        report.PendingRecords +
                        " remain pending.";

                    report.KeyFinding =
                        report.PendingRecords > 0
                            ? report.PendingRecords +
                              " application(s) still require administrative attention."
                            : "There are currently no pending applications in the selected dataset.";

                    report.Insights.Add(
                        CreateInsight(
                            "Processing Status",
                            report.KeyFinding,
                            "fa-info-circle",
                            report.PendingRecords > 0
                                ? "warning"
                                : "success"));
                }
            }


            // =========================================
            // PROPOSALS REPORT
            // =========================================

            else if (model.ReportType == "Proposals")
            {
                var proposals = db.Proposals
                    .Include("Student.User")
                    .AsQueryable();

                if (model.DepartmentID.HasValue)
                {
                    proposals = proposals.Where(p =>
                        p.Student.DepartmentID == model.DepartmentID.Value);
                }

                if (!string.IsNullOrWhiteSpace(model.Status))
                {
                    proposals = proposals.Where(p =>
                        p.Status == model.Status);
                }

                if (model.StartDate.HasValue)
                {
                    proposals = proposals.Where(p =>
                        p.SubmissionDate >= model.StartDate.Value);
                }

                if (model.EndDate.HasValue)
                {
                    DateTime endDate =
                        model.EndDate.Value.Date.AddDays(1);

                    proposals = proposals.Where(p =>
                        p.SubmissionDate < endDate);
                }

                if (!string.IsNullOrWhiteSpace(model.Search))
                {
                    string search = model.Search.Trim();

                    proposals = proposals.Where(p =>
                        p.Title.Contains(search) ||
                        p.Student.User.FirstName.Contains(search) ||
                        p.Student.User.LastName.Contains(search) ||
                        p.Student.User.StudentNumber.Contains(search));
                }

                var proposalList = proposals
                    .OrderByDescending(p => p.SubmissionDate)
                    .ToList();

                report.Columns = new List<string>
        {
            "Student Number",
            "Student",
            "Proposal Title",
            "Status",
            "Submission Date"
        };

                foreach (var proposal in proposalList)
                {
                    report.Rows.Add(new Dictionary<string, string>
            {
                {
                    "Student Number",
                    proposal.Student.User.StudentNumber ?? "-"
                },
                {
                    "Student",
                    ((proposal.Student.User.FirstName ?? "") + " " +
                     (proposal.Student.User.LastName ?? "")).Trim()
                },
                {
                    "Proposal Title",
                    proposal.Title ?? "-"
                },
                {
                    "Status",
                    proposal.Status ?? "Pending"
                },
                {
                    "Submission Date",
                    proposal.SubmissionDate.HasValue
                        ? proposal.SubmissionDate.Value.ToString("dd MMM yyyy")
                        : "-"
                }
            });
                }

                report.TotalRecords = proposalList.Count;

                report.CompletedRecords = proposalList.Count(p =>
                    IsStatus(p.Status, "Approved"));

                report.PendingRecords = proposalList.Count(p =>
                    IsStatus(p.Status, "Pending") ||
                    IsStatus(p.Status, "Submitted") ||
                    IsStatus(p.Status, "Under Evaluation"));

                int revisionRequired = proposalList.Count(p =>
                    IsStatus(p.Status, "Revision Required"));

                int rejectedProposals = proposalList.Count(p =>
                    IsStatus(p.Status, "Rejected"));

                report.AtRiskRecords =
                    revisionRequired + rejectedProposals;

                report.ActiveRecords =
                    report.TotalRecords -
                    rejectedProposals;

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Total Proposals",
                        report.TotalRecords,
                        "Research proposals matching the filters",
                        "fa-file-text-o",
                        "#001B44"));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Approved",
                        report.CompletedRecords,
                        "Approved research proposals",
                        "fa-check-circle",
                        "#16a34a",
                        report.TotalRecords));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Pending",
                        report.PendingRecords,
                        "Proposals still being processed",
                        "fa-clock-o",
                        "#FDB913",
                        report.TotalRecords));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Needs Attention",
                        report.AtRiskRecords,
                        "Revision required or rejected",
                        "fa-exclamation-triangle",
                        "#dc2626",
                        report.TotalRecords));

                AddStatusChart(
                    report,
                    proposalList
                        .GroupBy(p =>
                            string.IsNullOrWhiteSpace(p.Status)
                                ? "Pending"
                                : p.Status)
                        .Select(g => new
                        {
                            Label = g.Key,
                            Count = g.Count()
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList(),
                    "Research Proposals by Status");

                if (proposalList.Count > 0)
                {
                    report.AnalysisSummary =
                        "The selected dataset contains " +
                        report.TotalRecords +
                        " research proposal(s). " +
                        report.CompletedRecords +
                        " have been approved, while " +
                        report.PendingRecords +
                        " remain in the processing pipeline.";

                    report.KeyFinding =
                        report.AtRiskRecords > 0
                            ? report.AtRiskRecords +
                              " proposal(s) require attention because they are marked for revision or have been rejected."
                            : "No proposals in the selected dataset are currently classified as requiring attention.";

                    report.Insights.Add(
                        CreateInsight(
                            "Proposal Progress",
                            report.KeyFinding,
                            "fa-exclamation-triangle",
                            report.AtRiskRecords > 0
                                ? "warning"
                                : "success"));
                }
            }


            // =========================================
            // EVALUATIONS REPORT
            // =========================================

            else if (model.ReportType == "Evaluations")
            {
                var evaluations = db.Evaluations
                    .Include("Proposal.Student.User")
                    .AsQueryable();

                if (model.DepartmentID.HasValue)
                {
                    evaluations = evaluations.Where(e =>
                        e.Proposal.Student.DepartmentID ==
                        model.DepartmentID.Value);
                }

                if (!string.IsNullOrWhiteSpace(model.Status))
                {
                    evaluations = evaluations.Where(e =>
                        e.Recommendation == model.Status);
                }

                if (model.StartDate.HasValue)
                {
                    evaluations = evaluations.Where(e =>
                        e.EvaluationDate >= model.StartDate.Value);
                }

                if (model.EndDate.HasValue)
                {
                    DateTime endDate =
                        model.EndDate.Value.Date.AddDays(1);

                    evaluations = evaluations.Where(e =>
                        e.EvaluationDate < endDate);
                }

                if (!string.IsNullOrWhiteSpace(model.Search))
                {
                    string search = model.Search.Trim();

                    evaluations = evaluations.Where(e =>
                        e.Proposal.Title.Contains(search) ||
                        e.Proposal.Student.User.FirstName.Contains(search) ||
                        e.Proposal.Student.User.LastName.Contains(search) ||
                        e.Proposal.Student.User.StudentNumber.Contains(search));
                }

                var evaluationList = evaluations
                    .OrderByDescending(e => e.EvaluationDate)
                    .ToList();

                report.Columns = new List<string>
        {
            "Student Number",
            "Student",
            "Proposal",
            "Score",
            "Recommendation",
            "Evaluation Date"
        };

                foreach (var evaluation in evaluationList)
                {
                    report.Rows.Add(new Dictionary<string, string>
            {
                {
                    "Student Number",
                    evaluation.Proposal.Student.User.StudentNumber ?? "-"
                },
                {
                    "Student",
                    ((evaluation.Proposal.Student.User.FirstName ?? "") +
                     " " +
                     (evaluation.Proposal.Student.User.LastName ?? "")).Trim()
                },
                {
                    "Proposal",
                    evaluation.Proposal.Title ?? "-"
                },
                {
                    "Score",
                    evaluation.Score.HasValue
                        ? evaluation.Score.Value.ToString("0.##") + "%"
                        : "-"
                },
                {
                    "Recommendation",
                    evaluation.Recommendation ?? "Pending"
                },
                {
                    "Evaluation Date",
                    evaluation.EvaluationDate.HasValue
                        ? evaluation.EvaluationDate.Value.ToString("dd MMM yyyy")
                        : "-"
                }
            });
                }

                report.TotalRecords = evaluationList.Count;

                report.CompletedRecords = evaluationList.Count(e =>
                    IsStatus(e.Recommendation, "Approved"));

                report.PendingRecords = evaluationList.Count(e =>
                    string.IsNullOrWhiteSpace(e.Recommendation));

                int revisionRequired = evaluationList.Count(e =>
                    IsStatus(e.Recommendation, "Revision Required"));

                int rejectedEvaluations = evaluationList.Count(e =>
                    IsStatus(e.Recommendation, "Rejected"));

                report.AtRiskRecords =
                    revisionRequired +
                    rejectedEvaluations;

                report.ActiveRecords =
                    report.TotalRecords -
                    rejectedEvaluations;

                double averageScore = evaluationList
                .Where(e => e.Score.HasValue)
                .Select(e => (double)e.Score.Value)
                .DefaultIfEmpty(0.0)
                .Average();

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Total Evaluations",
                        report.TotalRecords,
                        "Evaluator records matching the filters",
                        "fa-clipboard",
                        "#001B44"));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Approved",
                        report.CompletedRecords,
                        "Evaluations recommending approval",
                        "fa-check-circle",
                        "#16a34a",
                        report.TotalRecords));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Needs Revision",
                        revisionRequired,
                        "Evaluations requiring revisions",
                        "fa-refresh",
                        "#FDB913",
                        report.TotalRecords));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Average Score",
                        (int)Math.Round(averageScore),
                        "Average evaluation score",
                        "fa-bar-chart",
                        "#7c3aed",
                        100,
                        "%"));

                AddStatusChart(
                    report,
                    evaluationList
                        .GroupBy(e =>
                            string.IsNullOrWhiteSpace(e.Recommendation)
                                ? "Pending"
                                : e.Recommendation)
                        .Select(g => new
                        {
                            Label = g.Key,
                            Count = g.Count()
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList(),
                    "Evaluations by Recommendation");

                AddScoreChart(
                    report,
                    evaluationList);

                if (evaluationList.Count > 0)
                {
                    report.AnalysisSummary =
                        "The report contains " +
                        report.TotalRecords +
                        " evaluation(s), with an average score of " +
                        averageScore.ToString("0.0") +
                        "%.";

                    report.KeyFinding =
                        report.AtRiskRecords > 0
                            ? report.AtRiskRecords +
                              " evaluation(s) indicate that further work is required."
                            : "No evaluations in the selected dataset are currently classified as requiring further action.";

                    report.Insights.Add(
                        CreateInsight(
                            "Evaluation Outcome",
                            report.KeyFinding,
                            "fa-line-chart",
                            report.AtRiskRecords > 0
                                ? "warning"
                                : "success"));
                }
            }


            // =========================================
            // PROGRESS REPORTS
            // =========================================

            else if (model.ReportType == "ProgressReports")
            {
                var progressReports = db.ProgressReports
                    .Include("Student.User")
                    .Include("Proposal")
                    .AsQueryable();

                if (model.DepartmentID.HasValue)
                {
                    progressReports = progressReports.Where(r =>
                        r.Student.DepartmentID ==
                        model.DepartmentID.Value);
                }

                if (!string.IsNullOrWhiteSpace(model.Status))
                {
                    progressReports = progressReports.Where(r =>
                        r.Status == model.Status);
                }

                if (model.StartDate.HasValue)
                {
                    progressReports = progressReports.Where(r =>
                        r.ReportDate >= model.StartDate.Value);
                }

                if (model.EndDate.HasValue)
                {
                    DateTime endDate =
                        model.EndDate.Value.Date.AddDays(1);

                    progressReports = progressReports.Where(r =>
                        r.ReportDate < endDate);
                }

                if (!string.IsNullOrWhiteSpace(model.Search))
                {
                    string search = model.Search.Trim();

                    progressReports = progressReports.Where(r =>
                        r.Summary.Contains(search) ||
                        r.Status.Contains(search) ||
                        r.Student.User.FirstName.Contains(search) ||
                        r.Student.User.LastName.Contains(search) ||
                        r.Student.User.StudentNumber.Contains(search));
                }

                var progressList = progressReports
                    .OrderByDescending(r => r.ReportDate)
                    .ToList();

                report.Columns = new List<string>
        {
            "Student Number",
            "Student",
            "Report Number",
            "Report Date",
            "Status",
            "Summary"
        };

                foreach (var progress in progressList)
                {
                    report.Rows.Add(new Dictionary<string, string>
            {
                {
                    "Student Number",
                    progress.Student.User.StudentNumber ?? "-"
                },
                {
                    "Student",
                    ((progress.Student.User.FirstName ?? "") +
                     " " +
                     (progress.Student.User.LastName ?? "")).Trim()
                },
                {
                    "Report Number",
                    progress.ReportNumber.HasValue
                        ? progress.ReportNumber.Value.ToString()
                        : "-"
                },
                {
                    "Report Date",
                    progress.ReportDate.HasValue
                        ? progress.ReportDate.Value.ToString("dd MMM yyyy")
                        : "-"
                },
                {
                    "Status",
                    progress.Status ?? "-"
                },
                {
                    "Summary",
                    progress.Summary ?? "-"
                }
            });
                }

                report.TotalRecords = progressList.Count;

                int onTrack = progressList.Count(r =>
                    IsStatus(r.Status, "On Track"));

                int needsAttention = progressList.Count(r =>
                    IsStatus(r.Status, "Needs Attention"));

                int atRisk = progressList.Count(r =>
                    IsStatus(r.Status, "At Risk"));

                int offTrack = progressList.Count(r =>
                    IsStatus(r.Status, "Off Track"));

                report.CompletedRecords = onTrack;

                report.PendingRecords = needsAttention;

                report.AtRiskRecords =
                    atRisk +
                    offTrack;

                report.ActiveRecords =
                    report.TotalRecords -
                    offTrack;

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Total Reports",
                        report.TotalRecords,
                        "Progress reports matching the selected filters",
                        "fa-file-text-o",
                        "#001B44"));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "On Track",
                        onTrack,
                        "Students progressing as expected",
                        "fa-check-circle",
                        "#16a34a",
                        report.TotalRecords));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "Needs Attention",
                        needsAttention,
                        "Students requiring monitoring",
                        "fa-exclamation-circle",
                        "#FDB913",
                        report.TotalRecords));

                report.SummaryCards.Add(
                    CreateSummaryCard(
                        "At Risk",
                        atRisk + offTrack,
                        "Students requiring intervention",
                        "fa-warning",
                        "#dc2626",
                        report.TotalRecords));

                AddStatusChart(
                    report,
                    progressList
                        .GroupBy(r =>
                            string.IsNullOrWhiteSpace(r.Status)
                                ? "Not Specified"
                                : r.Status)
                        .Select(g => new
                        {
                            Label = g.Key,
                            Count = g.Count()
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList(),
                    "Progress Reports by Assessment Status");

                if (progressList.Count > 0)
                {
                    report.AnalysisSummary =
                        "The report contains " +
                        report.TotalRecords +
                        " progress report(s). " +
                        onTrack +
                        " student(s) are on track, while " +
                        (needsAttention + atRisk + offTrack) +
                        " require monitoring or intervention.";

                    report.KeyFinding =
                        report.AtRiskRecords > 0
                            ? report.AtRiskRecords +
                              " student progress record(s) are classified as At Risk or Off Track."
                            : "No students in the selected dataset are currently classified as At Risk or Off Track.";

                    report.Insights.Add(
                        CreateInsight(
                            "Progress Monitoring",
                            report.KeyFinding,
                            "fa-line-chart",
                            report.AtRiskRecords > 0
                                ? "danger"
                                : "success"));
                }
            }


            // =========================================
            // COMMON REPORT INFORMATION
            // =========================================

            report.FilterSummary =
                BuildFilterSummary(model);

            if (report.TotalRecords == 0)
            {
                report.AnalysisSummary =
                    "No records were found matching the selected report criteria.";

                report.KeyFinding =
                    "Try changing or removing one or more filters.";

                report.Insights.Add(
                    CreateInsight(
                        "No Results",
                        "No records matched the selected filters.",
                        "fa-search",
                        "warning"));
            }

            // =========================================
            // STORE REPORT
            // =========================================

            Session["DynamicReport"] = report;

            return RedirectToAction("DynamicReportResult");
        }


        // =========================================
        // DYNAMIC REPORT RESULT
        // =========================================

        public ActionResult DynamicReportResult()
        {
            var report =
                Session["DynamicReport"] as DynamicReportViewModel;

            if (report == null)
            {
                return RedirectToAction("DynamicReports");
            }

            return View(report);
        }


        // =========================================
        // EXPORT DYNAMIC REPORT TO EXCEL
        // =========================================

        public ActionResult ExportDynamicReportToExcel()
        {
            var report = Session["DynamicReport"] as DynamicReportViewModel;

            if (report == null)
            {
                return RedirectToAction("DynamicReports");
            }

            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Report");

                // ============================================================
                // REPORT TITLE
                // ============================================================

                worksheet.Cells["A1"].Value = report.ReportTitle;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.Font.Size = 20;
                worksheet.Cells["A1"].Style.Font.Color.SetColor(
                    System.Drawing.Color.FromArgb(0, 27, 68));

                worksheet.Cells["A2"].Value = "Generated: " + report.GeneratedDate;
                worksheet.Cells["A2"].Style.Font.Italic = true;

                if (!string.IsNullOrWhiteSpace(report.ReportDescription))
                {
                    worksheet.Cells["A3"].Value = report.ReportDescription;
                    worksheet.Cells["A3"].Style.WrapText = true;
                }

                // ============================================================
                // SUMMARY SECTION
                // ============================================================

                int summaryRow = 5;

                worksheet.Cells[summaryRow, 1].Value = "REPORT SUMMARY";
                worksheet.Cells[summaryRow, 1].Style.Font.Bold = true;
                worksheet.Cells[summaryRow, 1].Style.Font.Size = 14;
                worksheet.Cells[summaryRow, 1].Style.Font.Color.SetColor(
                    System.Drawing.Color.FromArgb(0, 27, 68));

                summaryRow++;

                worksheet.Cells[summaryRow, 1].Value = "Total Records";
                worksheet.Cells[summaryRow, 2].Value = report.TotalRecords;

                worksheet.Cells[summaryRow + 1, 1].Value = "Active Records";
                worksheet.Cells[summaryRow + 1, 2].Value = report.ActiveRecords;

                worksheet.Cells[summaryRow + 2, 1].Value = "Pending Records";
                worksheet.Cells[summaryRow + 2, 2].Value = report.PendingRecords;

                worksheet.Cells[summaryRow + 3, 1].Value = "Completed Records";
                worksheet.Cells[summaryRow + 3, 2].Value = report.CompletedRecords;

                worksheet.Cells[summaryRow + 4, 1].Value = "At-Risk Records";
                worksheet.Cells[summaryRow + 4, 2].Value = report.AtRiskRecords;

                using (var range = worksheet.Cells[summaryRow, 1, summaryRow + 4, 2])
                {
                    range.Style.Border.BorderAround(
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin);

                    range.Style.Border.Top.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    range.Style.Border.Bottom.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    range.Style.Border.Left.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                    range.Style.Border.Right.Style =
                        OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                worksheet.Cells[summaryRow, 1, summaryRow + 4, 1]
                    .Style.Font.Bold = true;

                // ============================================================
                // KEY FINDINGS
                // ============================================================

                int findingsRow = summaryRow + 7;

                if (report.KeyFindings != null && report.KeyFindings.Count > 0)
                {
                    worksheet.Cells[findingsRow, 1].Value = "KEY FINDINGS";
                    worksheet.Cells[findingsRow, 1].Style.Font.Bold = true;
                    worksheet.Cells[findingsRow, 1].Style.Font.Size = 14;
                    worksheet.Cells[findingsRow, 1].Style.Font.Color.SetColor(
                        System.Drawing.Color.FromArgb(0, 27, 68));

                    findingsRow++;

                    foreach (var finding in report.KeyFindings)
                    {
                        worksheet.Cells[findingsRow, 1].Value = "• " + finding;
                        worksheet.Cells[findingsRow, 1].Style.WrapText = true;

                        findingsRow++;
                    }
                }

                // ============================================================
                // CHARTS
                // ============================================================

                int chartStartRow = findingsRow + 2;

                if (report.Charts != null && report.Charts.Count > 0)
                {
                    worksheet.Cells[chartStartRow, 1].Value = "REPORT CHARTS";
                    worksheet.Cells[chartStartRow, 1].Style.Font.Bold = true;
                    worksheet.Cells[chartStartRow, 1].Style.Font.Size = 14;
                    worksheet.Cells[chartStartRow, 1].Style.Font.Color.SetColor(
                        System.Drawing.Color.FromArgb(0, 27, 68));

                    chartStartRow += 2;

                    int chartIndex = 0;

                    foreach (var reportChart in report.Charts)
                    {
                        if (reportChart == null ||
                            reportChart.Labels == null ||
                            reportChart.Values == null ||
                            reportChart.Labels.Count == 0 ||
                            reportChart.Values.Count == 0)
                        {
                            continue;
                        }

                        // ----------------------------------------------------
                        // Create a hidden data area for the Excel chart
                        // ----------------------------------------------------

                        int dataStartRow = worksheet.Dimension != null
                            ? worksheet.Dimension.End.Row + 3
                            : chartStartRow + 20;

                        int dataStartColumn = 15 + (chartIndex * 4);

                        worksheet.Cells[dataStartRow, dataStartColumn].Value = "Category";
                        worksheet.Cells[dataStartRow, dataStartColumn + 1].Value = "Value";

                        for (int i = 0; i < reportChart.Labels.Count; i++)
                        {
                            worksheet.Cells[dataStartRow + i + 1, dataStartColumn]
                                .Value = reportChart.Labels[i];

                            int value = 0;

                            if (i < reportChart.Values.Count)
                            {
                                value = reportChart.Values[i];
                            }

                            worksheet.Cells[dataStartRow + i + 1, dataStartColumn + 1]
                                .Value = value;
                        }

                        int dataEndRow =
                            dataStartRow + reportChart.Labels.Count;

                        // ----------------------------------------------------
                        // Determine Excel chart type
                        // ----------------------------------------------------

                        OfficeOpenXml.Drawing.Chart.eChartType chartType;

                        string type = (reportChart.ChartType ?? "")
                            .Trim()
                            .ToLower();

                        switch (type)
                        {
                            case "pie":
                                chartType =
                                    OfficeOpenXml.Drawing.Chart.eChartType.Pie;
                                break;

                            case "line":
                                chartType =
                                    OfficeOpenXml.Drawing.Chart.eChartType.Line;
                                break;

                            case "doughnut":
                                chartType =
                                    OfficeOpenXml.Drawing.Chart.eChartType.Doughnut;
                                break;

                            case "bar":
                            default:
                                chartType =
                                    OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered;
                                break;
                        }

                        // ----------------------------------------------------
                        // Add native Excel chart
                        // ----------------------------------------------------

                        string chartName = "ReportChart" + chartIndex;

                        var chart = worksheet.Drawings.AddChart(
                            chartName,
                            chartType);

                        chart.Title.Text = reportChart.Title;

                        chart.SetPosition(
                            chartStartRow - 1,
                            0,
                            0,
                            0);

                        chart.SetSize(650, 350);

                        var series = chart.Series.Add(
                            worksheet.Cells[
                                dataStartRow + 1,
                                dataStartColumn + 1,
                                dataEndRow,
                                dataStartColumn + 1
                            ],
                            worksheet.Cells[
                                dataStartRow + 1,
                                dataStartColumn,
                                dataEndRow,
                                dataStartColumn
                            ]);

                        series.Header = "Value";

                        // ----------------------------------------------------
                        // Extra formatting for pie charts
                        // ----------------------------------------------------

                        if (chartType ==
                            OfficeOpenXml.Drawing.Chart.eChartType.Pie)
                        {
                            chart.Legend.Position =
                                OfficeOpenXml.Drawing.Chart.eLegendPosition.Right;
                        }
                        else
                        {
                            chart.Legend.Position =
                                OfficeOpenXml.Drawing.Chart.eLegendPosition.Bottom;
                        }

                        chartIndex++;

                        // Place subsequent charts below the previous one
                        chartStartRow += 20;
                    }

                    // Hide the chart data columns
                    for (int col = 15; col <= 15 + (chartIndex * 4); col++)
                    {
                        worksheet.Column(col).Hidden = true;
                    }
                }

                // ============================================================
                // DETAILED REPORT TABLE
                // ============================================================

                int tableStartRow = chartStartRow + 2;

                worksheet.Cells[tableStartRow, 1].Value = "DETAILED REPORT";
                worksheet.Cells[tableStartRow, 1].Style.Font.Bold = true;
                worksheet.Cells[tableStartRow, 1].Style.Font.Size = 14;
                worksheet.Cells[tableStartRow, 1].Style.Font.Color.SetColor(
                    System.Drawing.Color.FromArgb(0, 27, 68));

                tableStartRow += 2;

                // ------------------------------------------------------------
                // Column headings
                // ------------------------------------------------------------

                for (int i = 0; i < report.Columns.Count; i++)
                {
                    worksheet.Cells[tableStartRow, i + 1].Value =
                        report.Columns[i];

                    worksheet.Cells[tableStartRow, i + 1]
                        .Style.Font.Bold = true;

                    worksheet.Cells[tableStartRow, i + 1]
                        .Style.Font.Color.SetColor(
                            System.Drawing.Color.White);

                    worksheet.Cells[summaryRow, 1].Style.Fill.PatternType =
                            OfficeOpenXml.Style.ExcelFillStyle.Solid;

                    worksheet.Cells[summaryRow, 1].Style.Fill.BackgroundColor
                        .SetColor(System.Drawing.Color.FromArgb(0, 27, 68));

                    worksheet.Cells[tableStartRow, i + 1]
                        .Style.HorizontalAlignment =
                            OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                    worksheet.Cells[tableStartRow, i + 1]
                        .Style.VerticalAlignment =
                            OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                }

                // ------------------------------------------------------------
                // Data rows
                // ------------------------------------------------------------

                int dataRow = tableStartRow + 1;

                foreach (var row in report.Rows)
                {
                    for (int col = 0; col < report.Columns.Count; col++)
                    {
                        string columnName = report.Columns[col];

                        string value = "";

                        if (row.ContainsKey(columnName) &&
                            row[columnName] != null)
                        {
                            value = row[columnName];
                        }

                        worksheet.Cells[dataRow, col + 1].Value = value;

                        worksheet.Cells[dataRow, col + 1]
                            .Style.VerticalAlignment =
                                OfficeOpenXml.Style.ExcelVerticalAlignment.Top;

                        worksheet.Cells[dataRow, col + 1]
                            .Style.WrapText = true;
                    }

                    dataRow++;
                }

                // ============================================================
                // TABLE FORMATTING
                // ============================================================

                if (report.Columns.Count > 0)
                {
                    int lastDataRow = Math.Max(
                        tableStartRow,
                        dataRow - 1);

                    using (var tableRange = worksheet.Cells[
                        tableStartRow,
                        1,
                        lastDataRow,
                        report.Columns.Count])
                    {
                        tableRange.Style.Border.Top.Style =
                            OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                        tableRange.Style.Border.Bottom.Style =
                            OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                        tableRange.Style.Border.Left.Style =
                            OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                        tableRange.Style.Border.Right.Style =
                            OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                        tableRange.Style.Border.Top.Color.SetColor(
                            System.Drawing.Color.LightGray);

                        tableRange.Style.Border.Bottom.Color.SetColor(
                            System.Drawing.Color.LightGray);

                        tableRange.Style.Border.Left.Color.SetColor(
                            System.Drawing.Color.LightGray);

                        tableRange.Style.Border.Right.Color.SetColor(
                            System.Drawing.Color.LightGray);
                    }
                }

                // ============================================================
                // FILTER / PRINT SETTINGS
                // ============================================================

                if (report.Columns.Count > 0 &&
                    report.Rows.Count > 0)
                {
                    worksheet.Cells[
                        tableStartRow,
                        1,
                        dataRow - 1,
                        report.Columns.Count
                    ].AutoFilter = true;
                }

                worksheet.View.FreezePanes(tableStartRow + 1, 1);

                worksheet.PrinterSettings.Orientation =
                    OfficeOpenXml.eOrientation.Landscape;

                worksheet.PrinterSettings.FitToPage = true;

                worksheet.PrinterSettings.FitToWidth = 1;

                worksheet.PrinterSettings.FitToHeight = 0;

                worksheet.PrinterSettings.HorizontalCentered = false;

                // ============================================================
                // COLUMN WIDTHS
                // ============================================================

                if (report.Columns.Count > 0)
                {
                    for (int i = 1; i <= report.Columns.Count; i++)
                    {
                        worksheet.Column(i).Width = 22;
                    }
                }

                worksheet.Column(1).Width = 25;

                // ============================================================
                // FOOTER
                // ============================================================

                worksheet.HeaderFooter.OddFooter.CenteredText =
                    "SPGS Dynamic Report";

                worksheet.HeaderFooter.OddFooter.RightAlignedText =
                    "Page &P of &N";

                // ============================================================
                // DOWNLOAD
                // ============================================================

                string fileName =
                    "SPGS_" +
                    report.ReportTitle.Replace(" ", "_") +
                    "_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmss") +
                    ".xlsx";

                byte[] fileBytes = package.GetAsByteArray();

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
        }


        // ============================================================
        // REPORT FILTER DATA
        // ============================================================

        private void LoadReportFilterData(
            ReportFilterViewModel model)
        {
            model.Departments =
                db.Departments
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.DepartmentName)
                    .Select(d => new SelectListItem
                    {
                        Value =
                            d.DepartmentID.ToString(),

                        Text =
                            d.DepartmentName
                    })
                    .ToList();


            model.ReportTypes =
                new List<SelectListItem>
                {
            new SelectListItem
            {
                Value = "Students",
                Text = "Students"
            },

            new SelectListItem
            {
                Value = "Applications",
                Text = "Applications"
            },

            new SelectListItem
            {
                Value = "Proposals",
                Text = "Research Proposals"
            },

            new SelectListItem
            {
                Value = "Evaluations",
                Text = "Evaluations"
            },

            new SelectListItem
            {
                Value = "ProgressReports",
                Text = "Progress Reports"
            }
                };


            model.Qualifications =
                db.Students
                    .Where(s =>
                        s.Qualification != null &&
                        s.Qualification != "")
                    .Select(s => s.Qualification)
                    .Distinct()
                    .OrderBy(q => q)
                    .Select(q => new SelectListItem
                    {
                        Value = q,
                        Text = q
                    })
                    .ToList();


            model.StudyTypes =
                db.Students
                    .Where(s =>
                        s.StudyType != null &&
                        s.StudyType != "")
                    .Select(s => s.StudyType)
                    .Distinct()
                    .OrderBy(s => s)
                    .Select(s => new SelectListItem
                    {
                        Value = s,
                        Text = s
                    })
                    .ToList();


            model.RegistrationYears =
                db.Students
                    .Where(s =>
                        s.RegistrationYear.HasValue)
                    .Select(s =>
                        s.RegistrationYear.Value)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .Select(y => new SelectListItem
                    {
                        Value = y.ToString(),
                        Text = y.ToString()
                    })
                    .ToList();


            var statuses =
                new List<string>();


            statuses.AddRange(
                db.Applications
                    .Where(a => a.Status != null)
                    .Select(a => a.Status)
                    .Distinct()
                    .ToList());


            statuses.AddRange(
                db.Proposals
                    .Where(p => p.Status != null)
                    .Select(p => p.Status)
                    .Distinct()
                    .ToList());


            statuses.AddRange(
                db.Evaluations
                    .Where(e => e.Recommendation != null)
                    .Select(e => e.Recommendation)
                    .Distinct()
                    .ToList());


            statuses.AddRange(
                db.ProgressReports
                    .Where(r => r.Status != null)
                    .Select(r => r.Status)
                    .Distinct()
                    .ToList());


            model.Statuses =
                statuses
                    .Where(s =>
                        !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .Select(s => new SelectListItem
                    {
                        Value = s,
                        Text = s
                    })
                    .ToList();
        }


        // ============================================================
        // REPORT TITLE
        // ============================================================

        private string GetReportTitle(
            string reportType)
        {
            switch (reportType)
            {
                case "Students":
                    return "Postgraduate Student Report";

                case "Applications":
                    return "Postgraduate Application Report";

                case "Proposals":
                    return "Research Proposal Report";

                case "Evaluations":
                    return "Research Evaluation Report";

                case "ProgressReports":
                    return "Postgraduate Progress Report";

                default:
                    return "Dynamic Postgraduate Report";
            }
        }


        // ============================================================
        // REPORT DESCRIPTION
        // ============================================================

        private string GetReportDescription(
            string reportType)
        {
            switch (reportType)
            {
                case "Students":
                    return "A filtered overview of postgraduate students registered in the system.";

                case "Applications":
                    return "An analysis of postgraduate applications and their current processing status.";

                case "Proposals":
                    return "An overview of research proposals and their current academic processing status.";

                case "Evaluations":
                    return "An analysis of evaluator outcomes, recommendations and evaluation scores.";

                case "ProgressReports":
                    return "A monitoring report showing postgraduate research progress and assessment status.";

                default:
                    return "A dynamically generated postgraduate management report.";
            }
        }


        // ============================================================
        // SUMMARY CARD
        // ============================================================

        private ReportSummaryViewModel CreateSummaryCard(
            string title,
            int value,
            string description,
            string icon,
            string colour,
            int total = 0,
            string suffix = "")
        {
            double percentage = 0;

            if (total > 0)
            {
                percentage =
                    Math.Round(
                        ((double)value / total) * 100,
                        1);
            }

            return new ReportSummaryViewModel
            {
                Title = title,
                Value = value,
                Description = description,
                Icon = icon,
                Colour = colour,
                Percentage = percentage,
                ValueSuffix = suffix
            };
        }


        // ============================================================
        // STATUS HELPER
        // ============================================================

        private bool IsStatus(
            string value,
            string expected)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Equals(
                       expected,
                       StringComparison.OrdinalIgnoreCase);
        }


        // ============================================================
        // STUDENT CHARTS
        // ============================================================

        private void AddStudentCharts(
            DynamicReportViewModel report,
            IEnumerable<PostGraduateRecord.Models.Student> students)
        {
            var studentList =
                students.ToList();


            AddCategoryChart(
                report,
                studentList
                    .GroupBy(s =>
                        s.Qualification ??
                        "Not Specified")
                    .Select(g => new
                    {
                        Label = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                "Students by Qualification",
                "bar");


            AddCategoryChart(
                report,
                studentList
                    .GroupBy(s =>
                        s.StudyType ??
                        "Not Specified")
                    .Select(g => new
                    {
                        Label = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList(),
                "Students by Study Type",
                "doughnut");


            AddCategoryChart(
                report,
                studentList
                    .GroupBy(s =>
                        s.Department != null
                            ? s.Department.DepartmentName
                            : "Not Specified")
                    .Select(g => new
                    {
                        Label = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToList(),
                "Students by Department",
                "bar");
        }


        // ============================================================
        // STATUS CHART
        // ============================================================

        private void AddStatusChart<T>(
            DynamicReportViewModel report,
            IEnumerable<T> data,
            string title)
        {
            var items =
                data.ToList();

            var chart =
                new ReportChartViewModel
                {
                    Title = title,
                    Description =
                        "Distribution of records according to their current status.",
                    ChartType = "doughnut"
                };

            foreach (var item in items)
            {
                var type = item.GetType();

                var labelProperty =
                    type.GetProperty("Label");

                var countProperty =
                    type.GetProperty("Count");

                if (labelProperty == null ||
                    countProperty == null)
                {
                    continue;
                }

                string label =
                    Convert.ToString(
                        labelProperty.GetValue(item, null));

                int count =
                    Convert.ToInt32(
                        countProperty.GetValue(item, null));

                chart.Labels.Add(label);
                chart.Values.Add(count);
            }

            if (chart.HasData)
            {
                report.Charts.Add(chart);
            }
        }


        // ============================================================
        // CATEGORY CHART
        // ============================================================

        private void AddCategoryChart<T>(
            DynamicReportViewModel report,
            IEnumerable<T> data,
            string title,
            string chartType)
        {
            var items =
                data.ToList();

            var chart =
                new ReportChartViewModel
                {
                    Title = title,
                    Description =
                        "Distribution of records across the selected category.",
                    ChartType = chartType
                };

            foreach (var item in items)
            {
                var type = item.GetType();

                var labelProperty =
                    type.GetProperty("Label");

                var countProperty =
                    type.GetProperty("Count");

                if (labelProperty == null ||
                    countProperty == null)
                {
                    continue;
                }

                string label =
                    Convert.ToString(
                        labelProperty.GetValue(item, null));

                int count =
                    Convert.ToInt32(
                        countProperty.GetValue(item, null));

                chart.Labels.Add(label);
                chart.Values.Add(count);
            }

            if (chart.HasData)
            {
                report.Charts.Add(chart);
            }
        }


        // ============================================================
        // EVALUATION SCORE CHART
        // ============================================================

        private void AddScoreChart(
            DynamicReportViewModel report,
            IEnumerable<PostGraduateRecord.Models.Evaluation> evaluations)
        {
            var evaluationList =
                evaluations
                    .Where(e => e.Score.HasValue)
                    .ToList();

            if (!evaluationList.Any())
            {
                return;
            }

            var chart =
                new ReportChartViewModel
                {
                    Title = "Evaluation Score Distribution",
                    Description =
                        "Distribution of evaluation scores across performance ranges.",
                    ChartType = "bar"
                };


            chart.Labels.Add("0–49%");
            chart.Labels.Add("50–59%");
            chart.Labels.Add("60–69%");
            chart.Labels.Add("70–79%");
            chart.Labels.Add("80–100%");


            chart.Values.Add(
                evaluationList.Count(e =>
                    e.Score.Value < 50));

            chart.Values.Add(
                evaluationList.Count(e =>
                    e.Score.Value >= 50 &&
                    e.Score.Value < 60));

            chart.Values.Add(
                evaluationList.Count(e =>
                    e.Score.Value >= 60 &&
                    e.Score.Value < 70));

            chart.Values.Add(
                evaluationList.Count(e =>
                    e.Score.Value >= 70 &&
                    e.Score.Value < 80));

            chart.Values.Add(
                evaluationList.Count(e =>
                    e.Score.Value >= 80));


            report.Charts.Add(chart);
        }


        // ============================================================
        // INSIGHT
        // ============================================================

        private ReportInsightViewModel CreateInsight(
            string title,
            string description,
            string icon,
            string type)
        {
            string colour = "#001B44";

            if (type == "success")
            {
                colour = "#16a34a";
            }
            else if (type == "warning")
            {
                colour = "#d97706";
            }
            else if (type == "danger")
            {
                colour = "#dc2626";
            }

            return new ReportInsightViewModel
            {
                Title = title,
                Description = description,
                Icon = icon,
                Type = type,
                Colour = colour
            };
        }


        // ============================================================
        // LARGEST CATEGORY
        // ============================================================

        private string GetLargestCategory<T>(
            IList<T> categories,
            string categoryName)
        {
            if (categories == null ||
                categories.Count == 0)
            {
                return "No " +
                       categoryName +
                       " distribution data is available.";
            }

            var first =
                categories[0];

            var type =
                first.GetType();

            var nameProperty =
                type.GetProperty("Name");

            var countProperty =
                type.GetProperty("Count");

            if (nameProperty == null ||
                countProperty == null)
            {
                return "The report contains category distribution data.";
            }

            string name =
                Convert.ToString(
                    nameProperty.GetValue(first, null));

            int count =
                Convert.ToInt32(
                    countProperty.GetValue(first, null));

            return "The largest " +
                   categoryName +
                   " group is " +
                   name +
                   " with " +
                   count +
                   " record(s).";
        }


        // ============================================================
        // FILTER SUMMARY
        // ============================================================

        private string BuildFilterSummary(
            ReportFilterViewModel model)
        {
            var filters =
                new List<string>();

            if (model.DepartmentID.HasValue)
            {
                var department =
                    db.Departments.FirstOrDefault(
                        d => d.DepartmentID ==
                             model.DepartmentID.Value);

                if (department != null)
                {
                    filters.Add(
                        "Department: " +
                        department.DepartmentName);
                }
            }

            if (model.RegistrationYear.HasValue)
            {
                filters.Add(
                    "Registration Year: " +
                    model.RegistrationYear.Value);
            }

            if (!string.IsNullOrWhiteSpace(
                model.Qualification))
            {
                filters.Add(
                    "Qualification: " +
                    model.Qualification);
            }

            if (!string.IsNullOrWhiteSpace(
                model.StudyType))
            {
                filters.Add(
                    "Study Type: " +
                    model.StudyType);
            }

            if (!string.IsNullOrWhiteSpace(
                model.Status))
            {
                filters.Add(
                    "Status: " +
                    model.Status);
            }

            if (model.StartDate.HasValue)
            {
                filters.Add(
                    "From: " +
                    model.StartDate.Value
                        .ToString("dd MMM yyyy"));
            }

            if (model.EndDate.HasValue)
            {
                filters.Add(
                    "To: " +
                    model.EndDate.Value
                        .ToString("dd MMM yyyy"));
            }

            if (!string.IsNullOrWhiteSpace(
                model.Search))
            {
                filters.Add(
                    "Search: \"" +
                    model.Search.Trim() +
                    "\"");
            }

            if (filters.Count == 0)
            {
                return "All available records";
            }

            return string.Join(
                " • ",
                filters);
        }

    }
}