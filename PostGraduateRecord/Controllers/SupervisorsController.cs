using PostGraduateRecord.Models;
using PostGraduateRecord.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PostGraduateRecord.Controllers
{
    public class SupervisorsController : Controller
    {
        private readonly PostGradEntities db = new PostGradEntities();

         //===========================
         //DASHBOARD
         //===========================
        public ActionResult Dashboard()
        {
            LoadNotifications();

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound("Supervisor record not found.");
            }

            // ==========================================
            // GET SUPERVISOR'S ASSIGNMENTS
            // ==========================================

            var assignments = db.SupervisorAssignments
                .Include(a => a.Application)
                .Include(a => a.Application.Student)
                .Include(a => a.Application.Student.User)
                .Where(a => a.SupervisorID == supervisor.SupervisorID)
                .ToList();

            // ==========================================
            // GET ASSIGNED STUDENT IDS
            // ==========================================

            var studentIds = assignments
                .Where(a => a.Application != null)
                .Select(a => a.Application.StudentID)
                .Distinct()
                .ToList();

            // ==========================================
            // GET ACTUAL PROPOSALS
            // ==========================================

            var proposals = db.Proposals
                .Include(p => p.Student)
                .Include(p => p.Student.User)
                .Where(p => studentIds.Contains(p.StudentID))
                .ToList();

            // ==========================================
            // ACTIVE PROPOSALS
            // ==========================================

            var activeProposals = proposals
                .Where(p => p.Status != "Rejected")
                .ToList();

            // ==========================================
            // PENDING PROPOSALS
            // ==========================================

            var pendingProposals = proposals
                .Where(p =>
                    p.Status == "Submitted" ||
                    p.Status == "Under Evaluation")
                .ToList();

            // ==========================================
            // DASHBOARD MODEL
            // ==========================================

            var model = new SupervisorDashboardViewModel
            {
                AssignedStudents = studentIds.Count,

                ActiveProposals = activeProposals.Count,

                PendingReviews = pendingProposals.Count,

                UpcomingDeadlines =
        db.UserSettings
            .Where(s => s.UserID == userId)
            .Select(s => s.DeadlineRemindersEnabled)
            .FirstOrDefault()
            ? db.TimelineEvents
                .Count(x =>
                    x.EndDate >= DateTime.Now)
            : 0,

                Assignments = assignments,

                PendingReviewList = pendingProposals
        .Select(p => new PendingReviewViewModel
        {
            ProposalID = p.ProposalID,

            StudentName =
                p.Student != null &&
                p.Student.User != null
                    ? p.Student.User.FirstName + " " +
                      p.Student.User.LastName
                    : "Unknown Student",

            ProposalTitle = p.Title,

            SubmissionDate = p.SubmissionDate,

            Status = p.Status
        })
        .OrderByDescending(p => p.SubmissionDate)
        .ToList()
            };

            ViewBag.SupervisorName = Session["FullName"];

            return View(model);
        }

     


        // ===========================
        // APPLICATIONS / PROPOSALS
        // ===========================
        public ActionResult Proposals(string status)
        {
            LoadNotifications();

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound();
            }

            var studentIds = db.SupervisorAssignments
                .Where(a => a.SupervisorID == supervisor.SupervisorID)
                .Select(a => a.Application.StudentID)
                .Distinct()
                .ToList();

            var proposals = db.Proposals
                .Include(p => p.Student)
                .Include(p => p.Student.User)
                .Where(p => studentIds.Contains(p.StudentID))
                .ToList();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                switch (status)
                {
                    case "Pending":

                        proposals = proposals
                            .Where(p =>
                                p.Status == "Submitted" ||
                                p.Status == "Under Evaluation")
                            .ToList();

                        break;

                    case "Approved":

                        proposals = proposals
                            .Where(p => p.Status == "Approved")
                            .ToList();

                        break;

                    case "Revision Required":

                        proposals = proposals
                            .Where(p => p.Status == "Revision Required")
                            .ToList();

                        break;

                    case "Rejected":

                        proposals = proposals
                            .Where(p => p.Status == "Rejected")
                            .ToList();

                        break;
                }
            }

            proposals = proposals
                .OrderByDescending(p => p.SubmissionDate)
                .ToList();

            return View(proposals);
        }


        // ===========================
        // REVIEW PROPOSAL (GET)
        // ===========================
        public ActionResult Review(int id)
        {
            LoadNotifications();

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound("Supervisor record not found.");
            }

            var proposal = db.Proposals
                .Include(p => p.Student)
                .Include(p => p.Student.User)
                .FirstOrDefault(p => p.ProposalID == id);

            if (proposal == null)
            {
                return HttpNotFound("Proposal not found.");
            }

            bool assignedToSupervisor = db.SupervisorAssignments
                .Any(a =>
                    a.SupervisorID == supervisor.SupervisorID &&
                    a.Application.StudentID == proposal.StudentID);

            if (!assignedToSupervisor)
            {
                return new HttpStatusCodeResult(
                    403,
                    "You are not authorized to access this proposal."
                );
            }

            var model = new ProposalReviewViewModel
            {
                Proposal = proposal,

                PreviousFeedback = db.Feedbacks
                    .Include(f => f.Supervisor)
                    .Include(f => f.Supervisor.User)
                    .Where(f => f.ProposalID == id)
                    .OrderByDescending(f => f.FeedbackDate)
                    .ToList()
            };

            ViewBag.Evaluators = db.Evaluators
                .Include(e => e.User)
                .ToList()
                .Select(e => new SelectListItem
                {
                    Value = e.EvaluatorID.ToString(),
                    Text = e.User.FirstName + " " + e.User.LastName
                })
                .ToList();

            ViewBag.EvaluatorAssignment = db.Evaluations
                .Include(e => e.Evaluator)
                .Include(e => e.Evaluator.User)
                .FirstOrDefault(e => e.ProposalID == id);

            return View(model);
        }



        // ===========================
        // REVIEW PROPOSAL (POST)
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Review(ProposalReviewViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Proposal = db.Proposals
                    .Include(p => p.Student)
                    .Include(p => p.Student.User)
                    .FirstOrDefault(p =>
                        p.ProposalID == model.Proposal.ProposalID);

                model.PreviousFeedback = db.Feedbacks
                    .Where(f =>
                        f.ProposalID == model.Proposal.ProposalID)
                    .OrderByDescending(f => f.FeedbackDate)
                    .ToList();

                return View(model);
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound("Supervisor record not found.");
            }

            var proposal = db.Proposals
                .Include(p => p.Student)
                .Include(p => p.Student.User)
                .FirstOrDefault(p =>
                    p.ProposalID == model.Proposal.ProposalID);

            if (proposal == null)
            {
                return HttpNotFound("Proposal not found.");
            }

            TempData["TestMessage"] = "Decision received: [" + model.Decision + "]";

            if (proposal.Student == null)
            {
                return HttpNotFound("Student record not found.");
            }


            // =====================================================
            // PREVENT ANOTHER REVIEW AFTER APPROVAL
            // =====================================================

            if (proposal.Status == "Approved")
            {
                TempData["Success"] =
                    "This proposal has already been approved and cannot be reviewed again.";

                return RedirectToAction("Review", new
                {
                    id = proposal.ProposalID
                });
            }


            // =====================================================
            // CREATE FEEDBACK
            // =====================================================

            Feedback feedback = new Feedback
            {
                ProposalID = proposal.ProposalID,
                StudentID = proposal.Student.StudentID,
                SupervisorID = supervisor.SupervisorID,
                Comments = model.Comments,
                Decision = model.Decision,
                FeedbackDate = DateTime.Now
            };

            db.Feedbacks.Add(feedback);


            /// =====================================================
            // UPDATE PROPOSAL STATUS
            // =====================================================

            string decision = model.Decision;

            switch (decision)
            {
                case "Approved":
                    proposal.Status = "Approved";
                    break;

                case "Revision Required":
                    proposal.Status = "Revision Required";
                    break;

                case "Rejected":
                    proposal.Status = "Rejected";
                    break;
            }

            // =====================================================
            // SAVE CHANGES
            // =====================================================

            db.SaveChanges();


            // =====================================================
            // STUDENT NOTIFICATION
            // =====================================================

            Notification notification = new Notification
            {
                UserID = proposal.Student.UserID,

                Message = model.Decision == "Approved"
                    ? "Your proposal has been approved by your supervisor and is now ready for evaluator review."
                    : "Your proposal has been " + model.Decision,

                Link = model.Decision == "Approved"
                    ? "/Student/ProposalDetails/" + proposal.ProposalID
                    : "/Student/EditProposal/" + proposal.ProposalID,

                IsRead = false,
                SentDate = DateTime.Now
            };

            db.Notifications.Add(notification);


            // =====================================================
            // SAVE
            // =====================================================

            db.SaveChanges();


            TempData["Success"] =
                "Review submitted successfully.";

            return RedirectToAction("Review", new
            {
                id = model.Proposal.ProposalID
            });
        }



        // ===========================
        // PROFILE
        // ===========================
        public ActionResult Profile()
        {
            LoadNotifications();

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound();
            }

            string currentMode =
                Convert.ToString(Session["CurrentMode"]);

            ViewBag.CurrentMode = currentMode;

            return View(supervisor);
        }


        // ===========================
        // PENDING REVIEWS
        // ===========================
        public ActionResult PendingReviews()
        {
            LoadNotifications();

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound("Supervisor record not found.");
            }

            var studentIds = db.SupervisorAssignments
                .Where(a => a.SupervisorID == supervisor.SupervisorID)
                .Select(a => a.Application.StudentID)
                .Distinct()
                .ToList();

            var reviews =
                new List<SupervisorPendingReviewViewModel>();


            // ==========================================
            // 1. RESEARCH PROPOSALS
            // ==========================================

            var proposals = db.Proposals
                .Include(p => p.Student)
                .Include(p => p.Student.User)
                .Where(p =>
                    studentIds.Contains(p.StudentID) &&
                    (
                        p.Status == "Submitted" ||
                        p.Status == "Under Evaluation"
                    ))
                .ToList()
                .Select(p => new SupervisorPendingReviewViewModel
                {
                    ReviewID = p.ProposalID,

                    ReviewType = "Research Proposal",

                    Title = p.Title,

                    StudentName =
                        p.Student != null &&
                        p.Student.User != null
                            ? p.Student.User.FirstName + " " +
                              p.Student.User.LastName
                            : "Unknown Student",

                    SubmittedDate = p.SubmissionDate,

                    Status = p.Status,

                    Action = "Review",

                    Controller = "Supervisors"
                })
                .ToList();

            reviews.AddRange(proposals);


            // ==========================================
            // 2. ETHICS APPLICATIONS
            // ==========================================

            var ethicsApplications = db.EthicsApplications
                .Include(e => e.Student)
                .Include(e => e.Student.User)
                .Where(e =>
                    e.StudentID.HasValue &&
                    studentIds.Contains(e.StudentID.Value) &&
                    e.Status == "Submitted")
                .ToList()
                .Select(e => new SupervisorPendingReviewViewModel
                {
                    ReviewID = e.EthicsID,

                    ReviewType = "Ethics Application",

                    Title = e.ResearchTitle,

                    StudentName =
                        e.Student != null &&
                        e.Student.User != null
                            ? e.Student.User.FirstName + " " +
                              e.Student.User.LastName
                            : "Unknown Student",

                    SubmittedDate = e.SubmissionDate,

                    Status = e.Status,

                    Action = "ReviewEthics",

                    Controller = "Supervisors"
                })
                .ToList();

            reviews.AddRange(ethicsApplications);


            reviews = reviews
                .OrderByDescending(r =>
                    r.SubmittedDate ?? DateTime.MinValue)
                .ToList();

            return View(reviews);
        }


        // ===========================
        // SUBMISSIONS
        // ===========================
        public ActionResult Submissions(string type = "All")
        {
            LoadNotifications();

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound();
            }

            var studentIds = db.SupervisorAssignments
                .Where(a => a.SupervisorID == supervisor.SupervisorID)
                .Select(a => a.Application.StudentID)
                .Distinct()
                .ToList();

            var submissions =
                new List<SupervisorSubmissionViewModel>();


            // =====================================================
            // 1. RESEARCH PROPOSALS
            // =====================================================

            var proposalDocuments = db.ProposalDocuments
                .Where(d =>
                    studentIds.Contains(d.Proposal.StudentID))
                .ToList()
                .Select(d => new SupervisorSubmissionViewModel
                {
                    SubmissionID = d.DocumentID,
                    ProposalID = d.ProposalID,

                    StudentName =
                        d.Proposal.Student.User.FirstName + " " +
                        d.Proposal.Student.User.LastName,

                    StudentNumber =
                        d.Proposal.Student.User.StudentNumber,

                    Qualification =
                        d.Proposal.Student.Qualification,

                    StudyType =
                        d.Proposal.Student.StudyType,

                    ProposalTitle =
                        d.Proposal.Title,

                    DocumentName =
                        d.FileName,

                    SubmissionType =
                        "Research Proposal",

                    SubmittedDate =
                        d.UploadDate,

                    Status =
                        d.Proposal.Status,

                    FilePath =
                        d.FilePath
                })
                .ToList();

            submissions.AddRange(proposalDocuments);


            /// =====================================================
            // 2. EVALUATOR EVALUATIONS
            // =====================================================

            var evaluatorEvaluations = db.Evaluations
                .Where(e =>
                    e.Proposal != null &&
                    studentIds.Contains(e.Proposal.StudentID) &&
                    e.Recommendation != null &&
                    e.Recommendation != "")
                .ToList()
                .Select(e => new SupervisorSubmissionViewModel
                {
                    SubmissionID =
                        e.EvaluationID,

                    StudentName =
                        e.Proposal.Student.User.FirstName + " " +
                        e.Proposal.Student.User.LastName,

                    StudentNumber =
                        e.Proposal.Student.User.StudentNumber,

                    Qualification =
                        e.Proposal.Student.Qualification,

                    StudyType =
                        e.Proposal.Student.StudyType,

                    ProposalTitle =
                        e.Proposal.Title,

                    DocumentName =
                        "Evaluator Evaluation",

                    SubmissionType =
                        "Evaluator Evaluation",

                    SubmittedDate =
                        e.EvaluationDate,

                    Status =
                        e.Recommendation,

                    FilePath =
                        null,

                    Feedback =
                        e.Comments
                })
                .ToList();

            submissions.AddRange(evaluatorEvaluations);


            // =====================================================
            // FILTER
            // =====================================================

            if (!string.IsNullOrWhiteSpace(type) &&
                type != "All")
            {
                submissions = submissions
                    .Where(s =>
                        s.SubmissionType == type)
                    .ToList();
            }


            // =====================================================
            // SORT
            // =====================================================

            submissions = submissions
                .OrderByDescending(s =>
                    s.SubmittedDate ?? DateTime.MinValue)
                .ToList();

            ViewBag.CurrentType = type;

            return View(submissions);
        }


        // =====================================================
        // REVIEW SUBMISSION - GET
        // Supervisor can review Research Proposals only.
        // Final Thesis is NOT available to Supervisors.
        // =====================================================
        [HttpGet]
        public ActionResult ReviewSubmission(int id, string type = null)
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // =====================================================
            // BLOCK FINAL THESIS
            // =====================================================

            if (!string.IsNullOrWhiteSpace(type) &&
                type.Equals("Final Thesis", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.Forbidden,
                    "Final Thesis submissions are not available to supervisors."
                );
            }

            // =====================================================
            // FIND LOGGED-IN SUPERVISOR
            // =====================================================

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound("Supervisor record not found.");
            }

            // =====================================================
            // LOAD RESEARCH PROPOSAL DOCUMENT ONLY
            // =====================================================

            var document = db.ProposalDocuments
                .Include(d => d.Proposal)
                .Include(d => d.Proposal.Student)
                .Include(d => d.Proposal.Student.User)
                .FirstOrDefault(d => d.DocumentID == id);

            if (document == null)
            {
                return HttpNotFound(
                    "Research proposal document not found."
                );
            }

            if (document.Proposal == null)
            {
                return HttpNotFound(
                    "Proposal not found."
                );
            }

            // =====================================================
            // VERIFY SUPERVISOR ASSIGNMENT
            // =====================================================

            bool assigned = db.SupervisorAssignments.Any(a =>
                a.SupervisorID == supervisor.SupervisorID &&
                a.Application.StudentID == document.Proposal.StudentID
            );

            if (!assigned)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.Forbidden,
                    "You are not authorized to access this submission."
                );
            }

            // =====================================================
            // RETURN RESEARCH PROPOSAL
            // =====================================================

            return View(document);
        }


        // ===========================
        // TIMELINE
        // ===========================
        public ActionResult Timeline(int? studentId)
        {
            LoadNotifications();

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound();
            }

            var assignments = db.SupervisorAssignments
                .Where(a =>
                    a.SupervisorID ==
                    supervisor.SupervisorID)
                .ToList();

            var students = assignments
                .Where(a =>
                    a.Application != null &&
                    a.Application.Student != null)
                .Select(a => a.Application.Student)
                .GroupBy(s => s.StudentID)
                .Select(g => g.First())
                .OrderBy(s =>
                    s.User != null
                        ? s.User.FirstName + " " +
                          s.User.LastName
                        : "Unknown Student")
                .ToList();

            ViewBag.Students = students;

            ViewBag.SelectedStudentId = studentId;

            var timeline =
                new List<SupervisorTimelineViewModel>();


            foreach (var assignment in assignments)
            {
                if (assignment.Application == null)
                {
                    continue;
                }

                var application =
                    assignment.Application;

                if (application.Student == null)
                {
                    continue;
                }

                var student =
                    application.Student;

                if (studentId.HasValue &&
                    student.StudentID != studentId.Value)
                {
                    continue;
                }

                string studentName = "";

                if (student.User != null)
                {
                    studentName =
                        student.User.FirstName + " " +
                        student.User.LastName;
                }

                var proposal = db.Proposals
                    .Where(p =>
                        p.StudentID ==
                        student.StudentID)
                    .OrderByDescending(
                        p => p.SubmissionDate)
                    .FirstOrDefault();

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
                                : application.ResearchTitle,

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
                                application.Status ==
                                "Approved"
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
                                "Postgraduate application.",

                            Status =
                                "Pending"
                        });
                }


                // =====================================================
                // 2. SUPERVISOR ASSIGNED
                // =====================================================

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
                        else if (
                            feedback.Decision ==
                            "Rejected")
                        {
                            reviewStatus =
                                "Completed";
                        }
                        else if (
                            feedback.Decision ==
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
                // 5. EVALUATOR ASSIGNED
                // =====================================================


                // =====================================================
                // 6. EVALUATION
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
                                "Evaluation will begin after an evaluator is assigned.",

                            Status =
                                "Pending"
                        });
                }


                // =====================================================
                // 7. ETHICS
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
                        string ethicsStatus =
                            "Pending";

                        if (!string.IsNullOrEmpty(
                            ethics.Status))
                        {
                            if (ethics.Status ==
                                "Submitted")
                            {
                                // Waiting for supervisor review
                                ethicsStatus =
                                    "Pending";
                            }
                            else if (
                                ethics.Status ==
                                "Approved")
                            {
                                ethicsStatus =
                                    "Completed";
                            }
                            else if (
                                ethics.Status ==
                                "Resubmission Required")
                            {
                                ethicsStatus =
                                    "In Progress";
                            }
                            else
                            {
                                ethicsStatus =
                                    ethics.Status;
                            }
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
                                    "Ethics application.",

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
                                "Ethics application will follow proposal approval.",

                            Status =
                                "Pending"
                        });
                }


                // =====================================================
                // 8. RESEARCH PROGRESS
                // =====================================================

                if (proposal != null)
                {
                    var progressReport =
                        db.ProgressReports
                            .Where(r =>
                                r.StudentID ==
                                student.StudentID)
                            .OrderByDescending(
                                r => r.ReportDate)
                            .FirstOrDefault();

                    if (progressReport != null)
                    {
                        string progressStatus =
                            "Pending";

                        if (!string.IsNullOrEmpty(
                            progressReport.Status))
                        {
                            if (
                                progressReport.Status ==
                                "Issued" ||
                                progressReport.Status ==
                                "Submitted" ||
                                progressReport.Status ==
                                "Approved" ||
                                progressReport.Status ==
                                "Completed")
                            {
                                progressStatus =
                                    "Completed";
                            }
                            else if (
                                progressReport.Status ==
                                "Revision Required")
                            {
                                progressStatus =
                                    "In Progress";
                            }
                            else
                            {
                                progressStatus =
                                    progressReport.Status;
                            }
                        }

                        model.Stages.Add(
                            new TimelineStageViewModel
                            {
                                StageName =
                                    "Research Progress",
                                Description =
                                    progressReport.ReportNumber.HasValue
                                        ? "Progress Report " +
                                          progressReport.ReportNumber.Value +
                                          " issued by the supervisor."
                                        : "Research progress report issued by the supervisor.",

                                Status =
                                    progressStatus,
                                Date =
                                      progressReport.ReportDate,

                                Comments =
                                    progressReport.SupervisorComments
                            });
                    }
                    else
                    {
                        model.Stages.Add(
                            new TimelineStageViewModel
                            {
                                StageName =
                                    "Research Progress",

                                Description =
                                    "No progress report has been issued yet.",

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
                                "Research Progress",

                            Description =
                                "Research progress will begin after the proposal stage.",

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
                    model.Stages.Count(
                        s =>
                            s.Status ==
                            "Completed" ||
                            s.Status ==
                            "Approved");

                if (model.TotalStages > 0)
                {
                    model.ProgressPercentage =
                        (model.CompletedStages * 100)
                        / model.TotalStages;
                }

                timeline.Add(model);
            }

            return View(timeline);
        }


        // ===========================
        // ASSIGNED STUDENTS
        // ===========================
        public ActionResult AssignedStudents()
        {
            LoadNotifications();

            int userId =
                Convert.ToInt32(Session["UserID"]);

            var supervisor =
                db.Supervisors
                    .FirstOrDefault(
                        s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound();
            }

            var students =
                db.SupervisorAssignments
                    .Where(a =>
                        a.SupervisorID ==
                        supervisor.SupervisorID)
                    .Select(a =>
                        a.Application.Student)
                    .ToList()
                    .GroupBy(s =>
                        s.StudentID)
                    .Select(g =>
                        g.First())
                    .ToList();

            return View(students);
        }


        // ===========================
        // STUDENTS
        // ===========================
        public ActionResult Students()
        {
            LoadNotifications();

            int userId =
                Convert.ToInt32(Session["UserID"]);

            var supervisor =
                db.Supervisors
                    .FirstOrDefault(
                        s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound();
            }

            var students =
                db.SupervisorAssignments
                    .Where(a =>
                        a.SupervisorID ==
                        supervisor.SupervisorID)
                    .Select(a =>
                        a.Application.Student)
                    .ToList()
                    .GroupBy(s =>
                        s.StudentID)
                    .Select(g =>
                        g.First())
                    .ToList();

            return View(students);
        }


        // ===========================
        // LOAD NOTIFICATIONS
        // ===========================
        private void LoadNotifications()
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            var settings = db.UserSettings
                .FirstOrDefault(s => s.UserID == userId);

            bool notificationsEnabled = settings == null
                ? true
                : settings.NotificationsEnabled;

            if (!notificationsEnabled)
            {
                ViewBag.Notifications =
                    new List<NotificationViewModel>();

                ViewBag.NotificationCount = 0;

                return;
            }

            var notifications = db.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.SentDate)
                .Take(5)
                .ToList()
                .Select(n => new NotificationViewModel
                {
                    NotificationID =
                        n.NotificationID,

                    Message =
                        n.Message,

                    IsRead =
                        n.IsRead ?? false,

                    SentDate =
                        n.SentDate,

                    Link = Url.Action(
                        "MarkNotificationRead",
                        "Supervisors",
                        new
                        {
                            id = n.NotificationID
                        })
                })
                .ToList();

            int unreadCount = db.Notifications
                .Count(n =>
                    n.UserID == userId &&
                    (n.IsRead == null ||
                     n.IsRead == false));

            ViewBag.Notifications =
                notifications;

            ViewBag.NotificationCount =
                unreadCount;
        }


        // ===========================
        // MARK NOTIFICATION AS READ
        // ===========================
        public ActionResult MarkNotificationRead(int id)
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            var notification = db.Notifications
                .FirstOrDefault(n =>
                    n.NotificationID == id &&
                    n.UserID == userId);

            if (notification == null)
            {
                return HttpNotFound();
            }

            string redirectUrl = notification.Link;

            notification.IsRead = true;

            db.SaveChanges();

            if (!string.IsNullOrWhiteSpace(redirectUrl))
            {
                return Redirect(redirectUrl);
            }

            return RedirectToAction("Dashboard", "Supervisors");
        }


        // ===========================
        // ETHICS
        // ===========================
        public ActionResult Ethics()
        {
            LoadNotifications();

            int userId =
                Convert.ToInt32(Session["UserID"]);

            var supervisor =
                db.Supervisors
                    .FirstOrDefault(
                        s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound();
            }

            var ethicsApplications =
                db.SupervisorAssignments
                    .Where(a =>
                        a.SupervisorID ==
                        supervisor.SupervisorID)
                    .Select(a =>
                        a.Application.Student)
                    .SelectMany(s =>
                        s.EthicsApplications)
                    .ToList()
                    .GroupBy(e =>
                        e.EthicsID)
                    .Select(g =>
                        g.First())
                    .ToList();

            return View(ethicsApplications);
        }


        // =====================================================
        // REVIEW ETHICS - GET
        // =====================================================
        public ActionResult ReviewEthics(int id)
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int userId =
                Convert.ToInt32(Session["UserID"]);

            var supervisor =
                db.Supervisors
                    .FirstOrDefault(
                        s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound(
                    "Supervisor record not found.");
            }

            var ethics =
                db.EthicsApplications
                    .Include(e =>
                        e.Student.User)
                    .FirstOrDefault(
                        e => e.EthicsID == id);

            if (ethics == null)
            {
                return HttpNotFound(
                    "Ethics application not found.");
            }

            var application =
                db.Applications
                    .FirstOrDefault(
                        a =>
                            a.StudentID ==
                            ethics.StudentID);

            if (application == null)
            {
                return HttpNotFound(
                    "Student application not found.");
            }

            // ==========================================
            // MAKE SURE THIS SUPERVISOR IS ASSIGNED
            // TO THIS STUDENT
            // ==========================================

            bool assigned =
                db.SupervisorAssignments
                    .Any(a =>
                        a.SupervisorID ==
                        supervisor.SupervisorID &&
                        a.ApplicationID ==
                        application.ApplicationID);

            if (!assigned)
            {
                return new HttpStatusCodeResult(
                    403,
                    "You are not authorized to review this ethics application."
                );
            }

            return View(ethics);
        }


        // =====================================================
        // REVIEW ETHICS - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReviewEthics(
            int EthicsID,
            string Decision,
            string Comments)
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int userId =
                Convert.ToInt32(Session["UserID"]);

            // ==========================================
            // FIND LOGGED-IN SUPERVISOR
            // ==========================================

            var supervisor =
                db.Supervisors
                    .FirstOrDefault(
                        s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound(
                    "Supervisor record not found.");
            }

            // ==========================================
            // FIND ETHICS APPLICATION
            // ==========================================

            var ethics =
                db.EthicsApplications
                    .Include(e =>
                        e.Student.User)
                    .FirstOrDefault(
                        e => e.EthicsID == EthicsID);

            if (ethics == null)
            {
                return HttpNotFound(
                    "Ethics application not found.");
            }

            // ==========================================
            // FIND STUDENT APPLICATION
            // ==========================================

            var application =
                db.Applications
                    .FirstOrDefault(
                        a =>
                            a.StudentID ==
                            ethics.StudentID);

            if (application == null)
            {
                return HttpNotFound(
                    "Student application not found.");
            }

            // ==========================================
            // SECURITY CHECK
            // MAKE SURE SUPERVISOR IS ASSIGNED
            // ==========================================

            bool assigned =
                db.SupervisorAssignments
                    .Any(a =>
                        a.SupervisorID ==
                        supervisor.SupervisorID &&
                        a.ApplicationID ==
                        application.ApplicationID);

            if (!assigned)
            {
                return new HttpStatusCodeResult(
                    403,
                    "You are not authorized to review this ethics application."
                );
            }

            // ==========================================
            // ONLY TWO VALID DECISIONS
            // ==========================================

            if (Decision != "Approved" &&
                Decision != "Resubmission Required")
            {
                TempData["Error"] =
                    "Invalid ethics decision.";

                return RedirectToAction(
                    "Ethics");
            }

            // ==========================================
            // SAVE DECISION
            // ==========================================

            ethics.Status =
                Decision;

            ethics.Comments =
                Comments;

            // ==========================================
            // APPROVAL DATE
            // ONLY SET WHEN APPROVED
            // ==========================================

            // ==========================================
            // APPROVAL DATE + REFERENCE NUMBER
            // ==========================================

            if (Decision == "Approved")
            {
                ethics.ApprovalDate = DateTime.Now;

                // Generate Ethics Reference Number
                int year = DateTime.Now.Year;

                int nextNumber =
                    db.EthicsApplications
                        .Count(e => e.ReferenceNumber != null) + 1;

                ethics.ReferenceNumber =
                    "ETH-" +
                    year +
                    "-" +
                    nextNumber.ToString("D4");
            }
            else
            {
                ethics.ApprovalDate = null;
            }

            // ==========================================
            // NOTIFY STUDENT
            // ==========================================

            string message;
           
            if (Decision == "Approved")
            {
                message =
                    "Your ethics application has been approved by your supervisor. " +
                    "Your Ethics Reference Number is: " +
                    ethics.ReferenceNumber +
                    ". You will need this reference number to view your Ethics Clearance Certificate.";
            }
            else
            {
                message =
                    "Your ethics application requires changes. Please review the supervisor comments and resubmit.";
            }
            Notification notification =
                new Notification
                {
                    UserID =
                        ethics.Student.UserID,

                    Message =
                        message,

                    IsRead =
                        false,

                    SentDate =
                        DateTime.Now,

                    Link =
                        Url.Action(
                            "Ethics",
                            "Student")
                };

            db.Notifications.Add(
                notification);

            db.SaveChanges();

            // ==========================================
            // SUCCESS MESSAGE
            // ==========================================

            if (Decision == "Approved")
            {
                TempData["Success"] =
                    "Ethics application has been approved.";
            }
            else
            {
                TempData["Success"] =
                    "Ethics application has been returned for resubmission.";
            }

            return RedirectToAction(
                "Ethics");
        }


        // =====================================================
        // VIEW PROPOSAL DOCUMENT
        // =====================================================
        public ActionResult ViewDocument(int id)
                {
                    var document =
                        db.ProposalDocuments
                            .FirstOrDefault(d =>
                                d.ProposalID == id);

                    if (document == null)
                    {
                        return HttpNotFound(
                            "No document found for this proposal.");
                    }

                    if (string.IsNullOrWhiteSpace(document.FilePath))
                    {
                        return HttpNotFound(
                            "The document path is empty.");
                    }

                    string applicationPath =
                        Request.ApplicationPath;

                    if (applicationPath == "/")
                    {
                        applicationPath = "";
                    }

                    string documentUrl =
                        applicationPath +
                        document.FilePath;

                    return Redirect(documentUrl);
               }



        // =====================================================
        // SWITCH TO EVALUATOR
        // =====================================================
        public ActionResult SwitchToEvaluator()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int userId =
                Convert.ToInt32(
                    Session["UserID"]);

            var evaluator =
                db.Evaluators
                    .FirstOrDefault(
                        x => x.UserID == userId);

            if (evaluator == null)
            {
                TempData["Error"] =
                    "You are not registered as an evaluator.";

                return RedirectToAction(
                    "Dashboard",
                    "Supervisors");
            }

            Session["ActiveRole"] =
                "Evaluator";

            return RedirectToAction(
                "Dashboard",
                "Evaluators");
        }


        // ===========================
        // ASSIGN EVALUATOR
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignEvaluator(
            int proposalId,
            int evaluatorId)
        {
            int userId =
                Convert.ToInt32(
                    Session["UserID"]);

            var supervisor =
                db.Supervisors
                    .FirstOrDefault(
                        s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound(
                    "Supervisor record not found.");
            }

            var proposal =
                db.Proposals
                    .Include(p =>
                        p.Student)
                    .FirstOrDefault(
                        p =>
                            p.ProposalID ==
                            proposalId);

            if (proposal == null)
            {
                return HttpNotFound(
                    "Proposal not found.");
            }

            if (proposal.Status != "Approved")
            {
                TempData["Error"] =
                    "An evaluator can only be assigned after the supervisor approves the proposal.";

                return RedirectToAction(
                    "Review",
                    new
                    {
                        id =
                            proposalId
                    });
            }

            bool assignedToSupervisor =
                db.SupervisorAssignments
                    .Any(a =>
                        a.SupervisorID ==
                        supervisor.SupervisorID &&
                        a.Application.StudentID ==
                        proposal.StudentID);

            if (!assignedToSupervisor)
            {
                return new HttpStatusCodeResult(
                    403,
                    "You are not authorized to assign an evaluator to this proposal."
                );
            }

            var evaluator =
                db.Evaluators
                    .Include(e =>
                        e.User)
                    .FirstOrDefault(
                        e =>
                            e.EvaluatorID ==
                            evaluatorId);

            if (evaluator == null)
            {
                TempData["Error"] =
                    "The selected evaluator could not be found.";

                return RedirectToAction(
                    "Review",
                    new
                    {
                        id =
                            proposalId
                    });
            }

            var evaluation =
                db.Evaluations
                    .FirstOrDefault(
                        e =>
                            e.ProposalID ==
                            proposalId);

            if (evaluation != null)
            {
                evaluation.EvaluatorID =
                    evaluatorId;
            }
            else
            {
                evaluation =
                    new Evaluation
                    {
                        ProposalID =
                            proposalId,

                        EvaluatorID =
                            evaluatorId
                    };

                db.Evaluations.Add(
                    evaluation);
            }

            proposal.Status =
                "Under Evaluation";

            db.SaveChanges();

            Notification notification =
                new Notification
                {
                    UserID =
                        evaluator.UserID,

                    Message =
                        "You have been assigned a research proposal to evaluate.",

                    Link =
                        Url.Action(
                            "Review",
                            "Evaluators",
                            new
                            {
                                id =
                                    evaluation.EvaluationID
                            }),

                    IsRead =
                        false,

                    SentDate =
                        DateTime.Now
                };

            db.Notifications.Add(
                notification);

            db.SaveChanges();

            TempData["Success"] =
                "Evaluator " +
                evaluator.User.FirstName +
                " " +
                evaluator.User.LastName +
                " has been assigned successfully.";

            return RedirectToAction(
                "Review",
                new
                {
                    id =
                        proposalId
                });
        }


        // ===========================
        // SUPERVISOR DEADLINES
        // ===========================
        public ActionResult Deadlines()
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            var settings = db.UserSettings
                .FirstOrDefault(s => s.UserID == userId);

            bool deadlineRemindersEnabled = settings == null
                ? true
                : settings.DeadlineRemindersEnabled;

            if (!deadlineRemindersEnabled)
            {
                ViewBag.FullName = Session["FullName"];

                return View(
                    new List<SupervisorDeadlineViewModel>()
                );
            }

            var supervisor = db.Supervisors
                .FirstOrDefault(x => x.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound(
                    "Supervisor record not found."
                );
            }

            var assignedStudentIds = db.SupervisorAssignments
                .Where(x =>
                    x.SupervisorID == supervisor.SupervisorID)
                .Select(x =>
                    x.Application.StudentID)
                .ToList();

            // =========================================================
            // PROPOSAL DEADLINES
            // =========================================================

            var proposalDeadlines = db.Proposals
                .Include(x => x.Student)
                .Include(x => x.Student.User)
                .Where(x =>
                    assignedStudentIds.Contains(x.StudentID) &&
                    x.SubmissionDate.HasValue)
                .ToList()
                .Select(x => new SupervisorDeadlineViewModel
                {
                    Type = "Proposal Review",

                    Title = x.Title,

                    StudentName =
                        x.Student != null &&
                        x.Student.User != null
                            ? x.Student.User.FirstName + " " +
                              x.Student.User.LastName
                            : "Unknown Student",

                    Date =
                        x.SubmissionDate.Value.AddDays(7),

                    Status =
                        x.Status == "Submitted"
                            ? "Pending"
                            : "Completed",

                    ActionText =
                        x.Status == "Submitted"
                            ? "Review"
                            : "View",

                    Action = "Review",

                    Controller = "Supervisors",

                    Id = x.ProposalID
                })
                .ToList();

            // =========================================================
            // ETHICS DEADLINES
            // =========================================================

            var ethicsDeadlines = db.EthicsApplications
                .Include(x => x.Student)
                .Include(x => x.Student.User)
                .Where(x =>
                    x.StudentID.HasValue &&
                    assignedStudentIds.Contains(
                        x.StudentID.Value) &&
                    x.SubmissionDate.HasValue)
                .ToList()
                .Select(x => new SupervisorDeadlineViewModel
                {
                    Type = "Ethics Review",

                    Title = x.ResearchTitle,

                    StudentName =
                        x.Student != null &&
                        x.Student.User != null
                            ? x.Student.User.FirstName + " " +
                              x.Student.User.LastName
                            : "Unknown Student",

                    Date =
                        x.SubmissionDate.Value.AddDays(7),

                    Status =
                        x.Status == "Submitted"
                            ? "Pending"
                            : "Completed",

                    ActionText =
                        x.Status == "Submitted"
                            ? "Review"
                            : "View",

                    Action = "ReviewEthics",

                    Controller = "Supervisors",

                    Id = x.EthicsID
                })
                .ToList();

            // =========================================================
            // EVALUATION DEADLINES
            // =========================================================

            var evaluationDeadlines = db.Evaluations
                .Include(x => x.Proposal)
                .Include(x => x.Proposal.Student)
                .Include(x => x.Proposal.Student.User)
                .Where(x =>
                    x.Proposal != null &&
                    assignedStudentIds.Contains(
                        x.Proposal.StudentID) &&
                    x.Deadline.HasValue)
                .ToList()
                .Select(x => new SupervisorDeadlineViewModel
                {
                    Type = "Evaluation Deadline",

                    Title =
                        x.Proposal.Title,

                    StudentName =
                        x.Proposal.Student != null &&
                        x.Proposal.Student.User != null
                            ? x.Proposal.Student.User.FirstName + " " +
                              x.Proposal.Student.User.LastName
                            : "Unknown Student",

                    Date =
                        x.Deadline.Value,

                    Status =
                        x.Recommendation != null
                            ? "Completed"
                            : "Pending",

                    ActionText =
                        x.Recommendation != null
                            ? "View"
                            : "Review",

                    Action = "Review",

                    Controller = "Supervisors",

                    Id = x.ProposalID
                })
                .ToList();

            // =========================================================
            // COMBINE ALL DEADLINES
            // =========================================================

            var deadlines = proposalDeadlines
                .Concat(ethicsDeadlines)
                .Concat(evaluationDeadlines)
                .OrderBy(x => x.Date)
                .ToList();

            ViewBag.FullName =
                Session["FullName"];

            return View(deadlines);
        }

        
        // =====================================================
        // CREATE PROGRESS REPORT - GET
        // =====================================================
        [HttpGet]
        public ActionResult CreateProgressReport(int studentId)
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound("Supervisor record not found.");
            }

            // =================================================
            // MAKE SURE THIS STUDENT IS ASSIGNED TO SUPERVISOR
            // =================================================

            bool assigned = db.SupervisorAssignments
                .Any(a =>
                    a.SupervisorID == supervisor.SupervisorID &&
                    a.Application.StudentID == studentId);

            if (!assigned)
            {
                return new HttpStatusCodeResult(
                    403,
                    "You are not authorized to create a progress report for this student."
                );
            }

            // =================================================
            // FIND STUDENT
            // =================================================

            var student = db.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.StudentID == studentId);

            if (student == null)
            {
                return HttpNotFound("Student not found.");
            }

            // =================================================
            // FIND LATEST PROPOSAL
            // =================================================

            var proposal = db.Proposals
                .Where(p => p.StudentID == studentId)
                .OrderByDescending(p => p.SubmissionDate)
                .FirstOrDefault();

            if (proposal == null)
            {
                TempData["Error"] =
                    "This student does not have a research proposal yet.";

                return RedirectToAction("Students");
            }

            // =================================================
            // DETERMINE NEXT REPORT NUMBER
            // =================================================

            int nextReportNumber =
                (db.ProgressReports
                    .Where(r => r.StudentID == studentId)
                    .Select(r => r.ReportNumber)
                    .Where(r => r.HasValue)
                    .Select(r => r.Value)
                    .DefaultIfEmpty(0)
                    .Max()) + 1;

            // =================================================
            // BUILD MODEL
            // =================================================

            var model = new SupervisorProgressReportViewModel
            {
                StudentID = student.StudentID,

                ProposalID = proposal.ProposalID,

                StudentName = student.User != null
                    ? student.User.FirstName + " " + student.User.LastName
                    : "Unknown Student",

                StudentNumber = student.User != null
                    ? student.User.StudentNumber
                    : null,

                Qualification = student.Qualification,

                StudyType = student.StudyType,

                ResearchTitle = proposal.Title,

                ReportNumber = nextReportNumber,

                ReportDate = DateTime.Now,

                Status = "Issued"
            };




            return View(model);
        }


        // =====================================================
        // CREATE PROGRESS REPORT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProgressReport(
            SupervisorProgressReportViewModel model)
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound("Supervisor record not found.");
            }

            // =================================================
            // VALIDATE STUDENT ASSIGNMENT
            // =================================================

            bool assigned = db.SupervisorAssignments
                .Any(a =>
                    a.SupervisorID == supervisor.SupervisorID &&
                    a.Application.StudentID == model.StudentID);

            if (!assigned)
            {
                return new HttpStatusCodeResult(
                    403,
                    "You are not authorized to create a progress report for this student."
                );
            }

            // =================================================
            // FIND STUDENT
            // =================================================

            var student = db.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.StudentID == model.StudentID);

            if (student == null)
            {
                return HttpNotFound("Student not found.");
            }

            // =================================================
            // FIND PROPOSAL
            // =================================================

            var proposal = db.Proposals
                .FirstOrDefault(p =>
                    p.ProposalID == model.ProposalID &&
                    p.StudentID == model.StudentID);

            if (proposal == null)
            {
                return HttpNotFound("Research proposal not found.");
            }


            if (model.ReportDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "ReportDate",
                    "The report date cannot be earlier than today."
                );
            }



            // =================================================
            // VALIDATE MODEL
            // =================================================

            if (!ModelState.IsValid)
            {
                model.StudentName = student.User != null
                    ? student.User.FirstName + " " + student.User.LastName
                    : "Unknown Student";

                model.StudentNumber = student.User != null
                    ? student.User.StudentNumber
                     : null;

                model.Qualification = student.Qualification;

                model.StudyType = student.StudyType;

                model.ResearchTitle = proposal.Title;

                return View(model);
            }

            // =================================================
            // CREATE PROGRESS REPORT
            // =================================================

            var report = new ProgressReport
            {
                ProposalID = proposal.ProposalID,

                StudentID = student.StudentID,

                ReportNumber = model.ReportNumber,

                ReportDate = model.ReportDate,

                ReportingPeriod = model.ReportingPeriod,

                Summary = model.Summary,

                CurrentProgress = model.CurrentProgress,

                ProgressStatus = model.ProgressStatus,

                Challenges = model.Challenges,

                SupervisorComments = model.SupervisorComments,

                NextSteps = model.NextSteps,

                Status = model.Status
            };

            db.ProgressReports.Add(report);
            db.SaveChanges();

            Notification notification = new Notification
            {
                UserID = student.UserID,

                Message =
                    "Your supervisor has issued Progress Report " +
                    model.ReportNumber +
                    ". Please review your progress report.",

                Link = Url.Action(
                    "ViewProgressReport",
                    "Student",
                    new
                    {
                        id = report.ProgressReportID
                    }),

                IsRead = false,

                SentDate = DateTime.Now
            };

            db.Notifications.Add(notification);

            db.SaveChanges();

            // =================================================
            // SUCCESS
            // =================================================
            // =====================================================
            // SUCCESS
            // =====================================================

            TempData["Success"] =
                "Progress Report " +
                model.ReportNumber +
                " has been issued to the student successfully.";

            return RedirectToAction(
                "ViewProgressReport",
                new
                {
                    id = report.ProgressReportID
                });
        }

        // ===========================
        // DISPOSE
        // ===========================
        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }

        // ===========================
        // AVAILABLE STUDENTS
        // ===========================
        public ActionResult AvailableStudents()
        {
            LoadNotifications();

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

     
        // ===========================
        // CHOOSE STUDENT
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChooseStudent(int applicationId)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId =
                Convert.ToInt32(Session["UserID"]);

            // Find the logged-in supervisor
            var supervisor =
                db.Supervisors
                    .Include(s => s.User)
                    .FirstOrDefault(s =>
                        s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound(
                    "Supervisor record not found.");
            }

            // Find the approved application
            var application =
                db.Applications
                    .Include(a => a.Student)
                    .Include(a => a.Student.User)
                    .FirstOrDefault(a =>
                        a.ApplicationID == applicationId &&
                        a.Status == "Approved");

            if (application == null)
            {
                return HttpNotFound(
                    "Approved application not found.");
            }

            // Make sure another supervisor has not already selected this student
            bool alreadyAssigned =
                db.SupervisorAssignments
                    .Any(a =>
                        a.ApplicationID == applicationId);

            if (alreadyAssigned)
            {
                TempData["Error"] =
                    "This student has already been assigned to another supervisor.";

                return RedirectToAction(
                    "AvailableStudents");
            }

            // Create supervisor assignment
            SupervisorAssignment assignment =
                new SupervisorAssignment
                {
                    ApplicationID = applicationId,
                    SupervisorID = supervisor.SupervisorID,
                    AssignedDate = DateTime.Now,
                    Status = "Assigned"
                };

            db.SupervisorAssignments.Add(assignment);


            // Get the supervisor's full name
            string supervisorName =
                supervisor.User.FirstName + " " +
                supervisor.User.LastName;


            // Notify the student
            Notification notification =
                new Notification
                {
                    UserID = application.Student.UserID,

                    Message =
                        "You have been assigned to " +
                        supervisorName +
                        " as your postgraduate research supervisor.",

                    Link =
                        Url.Action(
                            "Dashboard",
                            "Student"),

                    IsRead = false,

                    SentDate = DateTime.Now
                };

          
            db.Notifications.Add(notification);


            // =========================================
            // NOTIFY ADMIN
            // =========================================

            var adminUsers = db.Users
                .Where(u => u.Role == "Admin")
                .ToList();

            foreach (var admin in adminUsers)
            {
                Notification adminNotification =
                    new Notification
                    {
                        UserID = admin.UserID,

                        Message =
                            "Supervisor assigned to student.",

                        Link =
                            Url.Action(
                                "ApprovedStudents",
                                "Admin"),

                        IsRead = false,

                        SentDate = DateTime.Now
                    };

                db.Notifications.Add(
                    adminNotification);
            }


            db.SaveChanges();
         


            TempData["Success"] =
                "Student selected successfully. " +
                "The student has been assigned to you.";

            return RedirectToAction(
                "AvailableStudents");
        }


        public ActionResult EvaluationDetails(int id)
        {
            int? userId = Session["UserID"] as int?;

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId.Value);

            if (supervisor == null)
            {
                return HttpNotFound();
            }

            var evaluation = db.Evaluations
                .Include(e => e.Proposal)
                .Include(e => e.Proposal.Student)
                .Include(e => e.Evaluator)
                .Include(e => e.Evaluator.User)
                .FirstOrDefault(e => e.EvaluationID == id);

            if (evaluation == null)
            {
                return HttpNotFound();
            }

            /*
             * Make sure this evaluation belongs to a student
             * assigned to the logged-in supervisor.
             */
            bool isAssigned = db.SupervisorAssignments.Any(a =>
                a.SupervisorID == supervisor.SupervisorID &&
                a.Application.StudentID == evaluation.Proposal.StudentID);

            if (!isAssigned)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.Forbidden);
            }

            var criteria = db.EvaluationCriterias
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            var ratings = db.EvaluationCriterionRatings
                .Where(r => r.EvaluationID == evaluation.EvaluationID)
                .ToList();

            decimal totalScore = ratings.Sum(r => r.Rating);
            decimal totalPossibleScore = criteria.Sum(c => c.MaxScore);

            decimal percentage = 0;
            decimal finalMark = 0;

            if (totalPossibleScore > 0)
            {
                percentage = Math.Round(
                    (totalScore / totalPossibleScore) * 100,
                    2);

                finalMark = Math.Round(
                    (totalScore / totalPossibleScore) * 10,
                    2);
            }

            var model = new PostGraduateRecord.ViewModels
                .EvaluationDetailsViewModel
            {
                EvaluationID = evaluation.EvaluationID,

                ProposalID = evaluation.ProposalID,

                ResearchTitle = evaluation.Proposal.Title,

                StudentName =
                    evaluation.Proposal.Student.User.FirstName
                    + " "
                    + evaluation.Proposal.Student.User.LastName,

                StudentNumber =
                    evaluation.Proposal.Student.User.StudentNumber,

                EvaluatorName =
                    evaluation.Evaluator.User.FirstName
                    + " "
                    + evaluation.Evaluator.User.LastName,

                TotalScore = totalScore,

                TotalPossibleScore = totalPossibleScore,

                Percentage = percentage,

                FinalMark = finalMark,

                Recommendation = evaluation.Recommendation,

                OverallComments = evaluation.Comments,

                EvaluationDate = evaluation.EvaluationDate,

                EvaluationSentToStudent =
                    evaluation.EvaluationSentToStudent,

                EvaluationSentDate =
                    evaluation.EvaluationSentDate
            };

            foreach (var criterion in criteria)
            {
                var rating = ratings
                    .FirstOrDefault(r =>
                        r.CriterionID == criterion.CriterionID);

                model.Criteria.Add(
                    new PostGraduateRecord.ViewModels
                        .EvaluationDetailsCriterionViewModel
                    {
                        CriterionID = criterion.CriterionID,

                        CriterionName = criterion.CriterionName,

                        Description = criterion.Description,

                        MaxScore = criterion.MaxScore,

                        DisplayOrder = criterion.DisplayOrder,

                        Rating = rating != null
                            ? rating.Rating
                            : 0,

                        Comments = rating != null
                            ? rating.Comments
                            : null
                    });
            }

            return View(model);
        }


        // =====================================================
        // VIEW PROGRESS REPORT - SUPERVISOR
        // =====================================================
        [HttpGet]
        public ActionResult ViewProgressReport(int id)
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound("Supervisor record not found.");
            }

            // =================================================
            // FIND PROGRESS REPORT
            // =================================================

            var report = db.ProgressReports
                .Include(r => r.Student)
                .Include(r => r.Student.User)
                .Include(r => r.Proposal)
                .FirstOrDefault(r => r.ProgressReportID == id);

            if (report == null)
            {
                return HttpNotFound("Progress report not found.");
            }

            // =================================================
            // MAKE SURE THIS STUDENT IS ASSIGNED
            // TO THE LOGGED-IN SUPERVISOR
            // =================================================

            bool assigned = db.SupervisorAssignments
                .Any(a =>
                    a.SupervisorID == supervisor.SupervisorID &&
                    a.Application.StudentID == report.StudentID);

            if (!assigned)
            {
                return new HttpStatusCodeResult(
                    403,
                    "You are not authorized to view this progress report."
                );
            }

            return View(report);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendEvaluationToStudent(int evaluationId)
        {
            int? userId = Session["UserID"] as int?;

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var supervisor = db.Supervisors
                .FirstOrDefault(s => s.UserID == userId.Value);

            if (supervisor == null)
            {
                return HttpNotFound();
            }

            var evaluation = db.Evaluations
                .Include(e => e.Proposal)
                .FirstOrDefault(e => e.EvaluationID == evaluationId);

            if (evaluation == null)
            {
                return HttpNotFound();
            }

            /*
             * Make sure the evaluation belongs to a student
             * assigned to this supervisor.
             */
            bool isAssigned = db.SupervisorAssignments.Any(a =>
                a.SupervisorID == supervisor.SupervisorID &&
                a.Application.StudentID == evaluation.Proposal.StudentID);

            if (!isAssigned)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.Forbidden);
            }

            /*
             * Prevent sending the same evaluation more than once.
             */
            if (evaluation.EvaluationSentToStudent)
            {
                TempData["Info"] =
                    "This evaluation has already been sent to the student.";

                return RedirectToAction(
                    "EvaluationDetails",
                    new { id = evaluationId });
            }

            /*
             * Release the evaluation to the student.
             */
            evaluation.EvaluationSentToStudent = true;
            evaluation.EvaluationSentDate = DateTime.Now;

            /*
             * Create a notification for the student.
             */
            var studentUserId = evaluation.Proposal.Student.UserID;

            var notification = new Notification
            {
                UserID = studentUserId,

                Message =
                    "Your evaluator's proposal evaluation is now available to view.",

                IsRead = false,

                SentDate = DateTime.Now,

                Link =
                    Url.Action(
                        "EvaluationDetails",
                        "Student",
                        new { id = evaluation.EvaluationID })
            };

            db.Notifications.Add(notification);

            db.SaveChanges();

            TempData["Success"] =
                "The evaluation has been successfully sent to the student.";

            return RedirectToAction(
                "EvaluationDetails",
                new { id = evaluationId });
        }


    }
}