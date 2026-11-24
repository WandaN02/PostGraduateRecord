using PostGraduateRecord.Models;
using PostGraduateRecord.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace PostGraduateRecord.Controllers
{
    public class EvaluatorsController : Controller
    {
        private readonly PostGradEntities db = new PostGradEntities();


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
                        "Evaluators",
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

            string redirectUrl =
                notification.Link;

            notification.IsRead = true;

            db.SaveChanges();

            if (!string.IsNullOrWhiteSpace(redirectUrl))
            {
                return Redirect(redirectUrl);
            }

            return RedirectToAction("Dashboard");
        }

        // ===========================
        // EVALUATOR DASHBOARD
        // ===========================
        public ActionResult Dashboard()
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            var evaluator = db.Evaluators
                              .FirstOrDefault(x => x.UserID == userId);

            var model = new EvaluatorDashboardViewModel
            {
                AssignedProposals = 0,
                PendingReviews = 0,
                CompletedReviews = 0,
                UpcomingDeadlines = 0,
                Evaluations = new List<Evaluation>()
            };

            if (evaluator == null)
            {
                ViewBag.FullName = Session["FullName"];
                ViewBag.Error = "You are not registered as an evaluator.";

                return View(model);
            }

            // Get all evaluations assigned to this evaluator
            var evaluations = db.Evaluations
                                .Include(x => x.Proposal)
                                .Include(x => x.Proposal.Student)
                                .Include(x => x.Proposal.Student.User)
                                .Where(x => x.EvaluatorID == evaluator.EvaluatorID)
                                .OrderByDescending(x => x.EvaluationDate)
                                .ToList();

            // ===========================
            // DASHBOARD COUNTS
            // ===========================

            // Total proposals assigned to evaluator
            model.AssignedProposals = evaluations.Count;

            // Reviews that have not yet been completed
            model.PendingReviews = evaluations.Count(x =>
                string.IsNullOrWhiteSpace(x.Recommendation));

            // Reviews that have already been completed
            model.CompletedReviews = evaluations.Count(x =>
                !string.IsNullOrWhiteSpace(x.Recommendation));

            // No deadline field has been confirmed yet
            model.UpcomingDeadlines = evaluations.Count(x =>
                x.Deadline.HasValue &&
                x.Deadline.Value >= DateTime.Now &&
                string.IsNullOrWhiteSpace(x.Recommendation));

            // Send evaluations to dashboard
            model.Evaluations = evaluations;

            ViewBag.FullName = Session["FullName"];

            return View(model);
        }

        // ===========================
        // EVALUATOR DEADLINES
        // ===========================
        public ActionResult Deadlines()
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            var evaluator = db.Evaluators
                              .FirstOrDefault(x => x.UserID == userId);

            if (evaluator == null)
            {
                return HttpNotFound("Evaluator record not found.");
            }

            var deadlines = db.Evaluations
                .Include(x => x.Proposal)
                .Include(x => x.Proposal.Student)
                .Include(x => x.Proposal.Student.User)
                .Where(x =>
                    x.EvaluatorID == evaluator.EvaluatorID
                    &&
                    x.Deadline.HasValue
                    &&
                    x.Deadline.Value >= DateTime.Now
                    &&
                    (x.Recommendation == null || x.Recommendation == "")
                )
                .OrderBy(x => x.Deadline)
                .ToList();

            ViewBag.FullName = Session["FullName"];

            return View(deadlines);
        }



        // =========================================================
        // REVIEW PROPOSAL - GET
        // =========================================================

        public ActionResult Review(int id)
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            LoadNotifications();

            // =====================================================
            // FIND EVALUATOR
            // =====================================================

            var evaluator = db.Evaluators
                .FirstOrDefault(x => x.UserID == userId);

            if (evaluator == null)
            {
                return HttpNotFound("Evaluator record not found.");
            }


            // =====================================================
            // FIND ASSIGNED EVALUATION
            // =====================================================

            var evaluation = db.Evaluations
                .Include(x => x.Proposal)
                .Include(x => x.Proposal.Student)
                .Include(x => x.Proposal.Student.User)
                .FirstOrDefault(x =>
                    x.EvaluationID == id &&
                    x.EvaluatorID == evaluator.EvaluatorID);

            if (evaluation == null)
            {
                return HttpNotFound(
                    "This evaluation is not assigned to you.");
            }


            // =====================================================
            // UPDATE PROPOSAL STATUS WHEN REVIEW IS OPENED
            // =====================================================

            if (evaluation.Proposal != null &&
                evaluation.Proposal.Status == "Assigned" &&
                string.IsNullOrWhiteSpace(evaluation.Recommendation))
            {
                evaluation.Proposal.Status = "Under Evaluation";

                db.SaveChanges();
            }


            // =====================================================
            // LOAD ACTIVE EVALUATION CRITERIA
            // =====================================================

            var criteria = db.EvaluationCriterias
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToList();


            // =====================================================
            // LOAD EXISTING CRITERION RATINGS
            // =====================================================

            var existingRatings = db.EvaluationCriterionRatings
                .Where(r =>
                    r.EvaluationID == evaluation.EvaluationID)
                .ToList();


            // =====================================================
            // BUILD VIEW MODEL
            // =====================================================

            var model = new EvaluatorReviewViewModel
            {
                EvaluationID = evaluation.EvaluationID,

                ProposalID = evaluation.ProposalID,

                Proposal = evaluation.Proposal,

                Recommendation = evaluation.Recommendation,

                Comments = evaluation.Comments,

                EvaluationDate = evaluation.EvaluationDate,

                Deadline = evaluation.Deadline,

                IsCompleted =
                    !string.IsNullOrWhiteSpace(
                        evaluation.Recommendation),

                Criteria = criteria.Select(c =>
                {
                    var existingRating =
                        existingRatings.FirstOrDefault(r =>
                            r.CriterionID == c.CriterionID);

                    return new EvaluatorCriterionViewModel
                    {
                        CriterionID = c.CriterionID,

                        CriterionName = c.CriterionName,

                        Description = c.Description,

                        MaxScore = c.MaxScore,

                        DisplayOrder = c.DisplayOrder,

                        IsActive = c.IsActive,

                        Rating =
                            existingRating != null
                                ? (int?)Convert.ToInt32(
                                    existingRating.Rating)
                                : null,

                        Comments =
                            existingRating != null
                                ? existingRating.Comments
                                : null
                    };
                }).ToList()
            };


            // =====================================================
            // CALCULATE CURRENT SCORE
            // =====================================================

            decimal totalScore = model.Criteria
                .Where(x => x.Rating.HasValue)
                .Sum(x => x.Rating.Value);


            decimal totalPossibleScore = model.Criteria
                .Sum(x => x.MaxScore);


            model.TotalScore = totalScore;

            model.TotalPossibleScore = totalPossibleScore;


            // =====================================================
            // CALCULATE PERCENTAGE AND FINAL MARK
            // =====================================================

            if (totalPossibleScore > 0)
            {
                model.OverallScore =
                    Math.Round(
                        (totalScore / totalPossibleScore) * 100,
                        2);

                model.ConvertedScore =
                    Math.Round(
                        totalScore / totalPossibleScore * 10,
                        2);
            }
            else
            {
                model.OverallScore = 0;

                model.ConvertedScore = 0;
            }


            ViewBag.FullName = Session["FullName"];

            return View(model);
        }


        // =========================================================
        // REVIEW PROPOSAL - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Review(EvaluatorReviewViewModel model)
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            LoadNotifications();

            // =====================================================
            // FIND EVALUATOR
            // =====================================================

            var evaluator = db.Evaluators
                .FirstOrDefault(x => x.UserID == userId);

            if (evaluator == null)
            {
                return HttpNotFound(
                    "Evaluator record not found.");
            }

            // =====================================================
            // FIND EVALUATION
            // =====================================================

            var evaluation = db.Evaluations
                .Include(x => x.Proposal)
                .Include(x => x.Proposal.Student)
                .Include(x => x.Proposal.Student.User)
                .FirstOrDefault(x =>
                    x.EvaluationID == model.EvaluationID &&
                    x.EvaluatorID == evaluator.EvaluatorID);

            if (evaluation == null)
            {
                return HttpNotFound(
                    "This evaluation is not assigned to you.");
            }

            // =====================================================
            // PREVENT DUPLICATE SUBMISSION
            // =====================================================

            if (!string.IsNullOrWhiteSpace(
                evaluation.Recommendation))
            {
                TempData["Error"] =
                    "This evaluation has already been submitted.";

                return RedirectToAction(
                    "Review",
                    new
                    {
                        id = evaluation.EvaluationID
                    });
            }

            // =====================================================
            // LOAD ACTIVE CRITERIA
            // =====================================================

            var criteria = db.EvaluationCriterias
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            if (criteria.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "No evaluation criteria have been configured.");
            }

            // =====================================================
            // VALIDATE CRITERIA
            // =====================================================

            if (model.Criteria == null)
            {
                ModelState.AddModelError(
                    "",
                    "Please complete all evaluation criteria.");
            }
            else
            {
                foreach (var criterion in criteria)
                {
                    var submittedCriterion =
                        model.Criteria.FirstOrDefault(x =>
                            x.CriterionID == criterion.CriterionID);

                    // Criterion must exist
                    if (submittedCriterion == null)
                    {
                        ModelState.AddModelError(
                            "",
                            "Please provide a rating for " +
                            criterion.CriterionName + ".");

                        continue;
                    }

                    // Rating required
                    if (!submittedCriterion.Rating.HasValue)
                    {
                        ModelState.AddModelError(
                            "",
                            "Please provide a rating for " +
                            criterion.CriterionName + ".");

                        continue;
                    }

                    int rating =
                        submittedCriterion.Rating.Value;

                    // Rating must be 1–5 in database
                    if (rating < 1 || rating > 5)
                    {
                        ModelState.AddModelError(
                            "",
                            "The rating for " +
                            criterion.CriterionName +
                            " must be between 1 and 5.");

                        continue;
                    }

                    // Comment required for ratings 1–3
                    if (rating <= 3 &&
                        string.IsNullOrWhiteSpace(
                            submittedCriterion.Comments))
                    {
                        ModelState.AddModelError(
                            "",
                            "A comment is required for " +
                            criterion.CriterionName +
                            " when a rating of " +
                            rating +
                            " is selected.");
                    }
                }
            }

            // =====================================================
            // VALIDATE RECOMMENDATION
            // =====================================================

            var validRecommendations = new[]
            {
        "Approved",
        "Approved with Changes",
        "Revision Required",
        "Rejected"
    };

            if (string.IsNullOrWhiteSpace(model.Recommendation) ||
                !validRecommendations.Contains(model.Recommendation))
            {
                ModelState.AddModelError(
                    "Recommendation",
                    "Please select a valid recommendation.");
            }

            // =====================================================
            // VALIDATE OVERALL COMMENTS
            // =====================================================

            if (string.IsNullOrWhiteSpace(model.Comments))
            {
                ModelState.AddModelError(
                    "Comments",
                    "Please provide evaluation comments.");
            }

            // =====================================================
            // IF VALIDATION FAILS
            // =====================================================

            if (!ModelState.IsValid)
            {
                model.Proposal =
                    evaluation.Proposal;

                model.ProposalID =
                    evaluation.ProposalID;

                model.EvaluationDate =
                    evaluation.EvaluationDate;

                model.Deadline =
                    evaluation.Deadline;

                model.IsCompleted = false;

                // Rebuild criteria while preserving entered values
                model.Criteria = criteria.Select(c =>
                {
                    var submitted =
                        model.Criteria?
                            .FirstOrDefault(x =>
                                x.CriterionID == c.CriterionID);

                    return new EvaluatorCriterionViewModel
                    {
                        CriterionID =
                            c.CriterionID,

                        CriterionName =
                            c.CriterionName,

                        Description =
                            c.Description,

                        MaxScore =
                            c.MaxScore,

                        DisplayOrder =
                            c.DisplayOrder,

                        IsActive =
                            c.IsActive,

                        Rating =
                            submitted?.Rating,

                        Comments =
                            submitted?.Comments
                    };
                }).ToList();

                // Recalculate display score
                decimal totalScore =
                    model.Criteria
                        .Where(x => x.Rating.HasValue)
                        .Sum(x => x.Rating.Value);

                decimal totalPossibleScore =
                    model.Criteria
                        .Sum(x => x.MaxScore);

                model.TotalScore =
                    totalScore;

                model.TotalPossibleScore =
                    totalPossibleScore;

                if (totalPossibleScore > 0)
                {
                    model.OverallScore =
                        Math.Round(
                            (totalScore /
                             totalPossibleScore) * 100,
                            2);

                    model.ConvertedScore =
                        Math.Round(
                            (totalScore /
                             totalPossibleScore) * 10,
                            2);
                }
                else
                {
                    model.OverallScore = 0;
                    model.ConvertedScore = 0;
                }

                ViewBag.FullName =
                    Session["FullName"];

                return View(model);
            }

            // =====================================================
            // CALCULATE FINAL SCORE
            // =====================================================

            decimal totalScoreObtained = 0;
            decimal totalPossibleScoreValue = 0;

            foreach (var criterion in criteria)
            {
                var submittedCriterion =
                    model.Criteria.FirstOrDefault(x =>
                        x.CriterionID == criterion.CriterionID);

                if (submittedCriterion == null ||
                    !submittedCriterion.Rating.HasValue)
                {
                    ModelState.AddModelError(
                        "",
                        "Please provide a rating for " +
                        criterion.CriterionName + ".");

                    // IMPORTANT:
                    // Return the view so every code path has a return value.
                    model.Proposal = evaluation.Proposal;
                    model.ProposalID = evaluation.ProposalID;
                    model.EvaluationDate = evaluation.EvaluationDate;
                    model.Deadline = evaluation.Deadline;
                    model.IsCompleted = false;

                    model.Criteria = criteria.Select(c =>
                    {
                        var submitted =
                            model.Criteria?
                                .FirstOrDefault(x =>
                                    x.CriterionID == c.CriterionID);

                        return new EvaluatorCriterionViewModel
                        {
                            CriterionID = c.CriterionID,
                            CriterionName = c.CriterionName,
                            Description = c.Description,
                            MaxScore = c.MaxScore,
                            DisplayOrder = c.DisplayOrder,
                            IsActive = c.IsActive,
                            Rating = submitted?.Rating,
                            Comments = submitted?.Comments
                        };
                    }).ToList();

                    ViewBag.FullName =
                        Session["FullName"];

                    return View(model);
                }

                int rating =
                    submittedCriterion.Rating.Value;

                if (rating < 1 || rating > 5)
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid rating for " +
                        criterion.CriterionName + ".");

                    model.Proposal = evaluation.Proposal;
                    model.ProposalID = evaluation.ProposalID;
                    model.EvaluationDate = evaluation.EvaluationDate;
                    model.Deadline = evaluation.Deadline;
                    model.IsCompleted = false;

                    ViewBag.FullName =
                        Session["FullName"];

                    return View(model);
                }

                totalScoreObtained += rating;

                totalPossibleScoreValue +=
                    criterion.MaxScore;
            }

            // =====================================================
            // CALCULATE FINAL PERCENTAGE
            // =====================================================

            decimal overallPercentage = 0;

            if (totalPossibleScoreValue > 0)
            {
                overallPercentage =
                    Math.Round(
                        (totalScoreObtained /
                         totalPossibleScoreValue) * 100,
                        2);
            }

            // =====================================================
            // CALCULATE FINAL MARK OUT OF 10
            // =====================================================

            decimal finalMark = 0;

            if (totalPossibleScoreValue > 0)
            {
                finalMark =
                    Math.Round(
                        (totalScoreObtained /
                         totalPossibleScoreValue) * 10,
                        2);
            }

            // =====================================================
            // SAVE MAIN EVALUATION
            // =====================================================

            evaluation.Score =
                overallPercentage;

            evaluation.Recommendation =
                model.Recommendation;

            evaluation.Comments =
                model.Comments;

            evaluation.EvaluationDate =
                DateTime.Now;

            // =====================================================
            // SAVE INDIVIDUAL CRITERION RATINGS
            // =====================================================

            foreach (var criterion in criteria)
            {
                var submittedCriterion =
                    model.Criteria.First(x =>
                        x.CriterionID ==
                        criterion.CriterionID);

                var rating =
                    db.EvaluationCriterionRatings
                        .FirstOrDefault(x =>
                            x.EvaluationID ==
                                evaluation.EvaluationID &&
                            x.CriterionID ==
                                criterion.CriterionID);

                if (rating == null)
                {
                    rating =
                        new EvaluationCriterionRating
                        {
                            EvaluationID =
                                evaluation.EvaluationID,

                            CriterionID =
                                criterion.CriterionID
                        };

                    db.EvaluationCriterionRatings.Add(
                        rating);
                }

                // Database stores the original 1–5 rating
                rating.Rating =
                    submittedCriterion.Rating.Value;

                rating.Comments =
                    string.IsNullOrWhiteSpace(
                        submittedCriterion.Comments)
                        ? null
                        : submittedCriterion.Comments.Trim();
            }

            // =====================================================
            // UPDATE PROPOSAL STATUS
            // =====================================================

            if (model.Recommendation == "Approved")
            {
                evaluation.Proposal.Status =
                    "Approved";
            }
            else if (model.Recommendation ==
                     "Approved with Changes")
            {
                evaluation.Proposal.Status =
                    "Approved with Changes";
            }
            else if (model.Recommendation ==
                     "Revision Required")
            {
                evaluation.Proposal.Status =
                    "Revision Required";
            }
            else if (model.Recommendation ==
                     "Rejected")
            {
                evaluation.Proposal.Status =
                    "Rejected";
            }

            // =====================================================
            // SAVE EVALUATION FIRST
            // =====================================================

            db.SaveChanges();

            // =====================================================
            // FIND SUPERVISOR ASSIGNED TO THE STUDENT
            // =====================================================

            var supervisorAssignment =
                db.SupervisorAssignments
                    .Include(a => a.Supervisor)
                    .FirstOrDefault(a =>
                        a.Application.StudentID ==
                        evaluation.Proposal.StudentID);

            // =====================================================
            // NOTIFY SUPERVISOR
            // =====================================================

            if (supervisorAssignment != null &&
                supervisorAssignment.Supervisor != null)
            {
                var supervisor =
                    supervisorAssignment.Supervisor;

                int supervisorUserId =
                    supervisor.UserID;

                string studentName =
                    evaluation.Proposal.Student.User.FirstName +
                    " " +
                    evaluation.Proposal.Student.User.LastName;

                var supervisorNotification =
                    new Notification
                    {
                        UserID =
                            supervisorUserId,

                        Message =
                            "Evaluation received: " +
                            "The evaluator has completed the evaluation " +
                            "for " +
                            studentName +
                            "'s research proposal. " +
                            "Please review the evaluation in Submissions.",

                        IsRead = false,

                        SentDate =
                            DateTime.Now,

                        Link =
                            Url.Action(
                                "Submissions",
                                "Supervisors")
                    };

                db.Notifications.Add(
                    supervisorNotification);

                db.SaveChanges();
            }

            // =====================================================
            // SUCCESS MESSAGE
            // =====================================================

            TempData["Success"] =
                "Evaluation submitted successfully. " +
                "Final mark: " +
                finalMark.ToString("0.00") +
                " / 10 (" +
                overallPercentage.ToString("0.00") +
                "%).";

            // =====================================================
            // RETURN TO EVALUATOR REVIEW
            // =====================================================

            return RedirectToAction(
                "Review",
                new
                {
                    id = evaluation.EvaluationID
                });
        }



        // ===========================
        // SWITCH TO SUPERVISOR
        // ===========================
        public ActionResult SwitchToSupervisor()
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            var supervisor = db.Supervisors
                               .FirstOrDefault(x => x.UserID == userId);

            if (supervisor == null)
            {
                return HttpNotFound(
                    "You are not registered as a supervisor.");
            }

            Session["ActiveRole"] = "Supervisor";

            return RedirectToAction("Dashboard", "Supervisors");
        }

        // ===========================
        // EVALUATOR PROFILE
        // ===========================

        public ActionResult Profile()

        {

            int userId = Convert.ToInt32(Session["UserID"]);

            LoadNotifications();

            var evaluator = db.Evaluators

                .Include(e => e.User)

                .Include(e => e.Department)

                .FirstOrDefault(e => e.UserID == userId);



            if (evaluator == null)

            {

                return HttpNotFound();

            }

            return View(evaluator);

        }

        public ActionResult CompletedReviews()
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            var evaluator = db.Evaluators
                .FirstOrDefault(x => x.UserID == userId);

            if (evaluator == null)
            {
                return HttpNotFound("Evaluator record not found.");
            }

            var completedReviews = db.Evaluations
                .Include(x => x.Proposal)
                .Include(x => x.Proposal.Student)
                .Include(x => x.Proposal.Student.User)
                .Where(x =>
                    x.EvaluatorID == evaluator.EvaluatorID &&
                    x.Recommendation != null &&
                    x.Recommendation != "")
                .OrderByDescending(x => x.EvaluationDate)
                .ToList();

            return View(completedReviews);
        }

        // ===========================
        // ASSIGNED / PENDING PROPOSALS
        // ===========================
        public ActionResult Proposals(string status)
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            LoadNotifications();

            var evaluator = db.Evaluators
                              .FirstOrDefault(x => x.UserID == userId);

            if (evaluator == null)
            {
                return RedirectToAction("Dashboard");
            }

            var evaluations = db.Evaluations
                                .Include(x => x.Proposal)
                                .Include(x => x.Proposal.Student)
                                .Include(x => x.Proposal.Student.User)
                                .Where(x => x.EvaluatorID == evaluator.EvaluatorID)
                                .ToList();

            // ===========================
            // PENDING REVIEWS ONLY
            // ===========================
            if (status != null && status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                evaluations = evaluations
                    .Where(x => string.IsNullOrWhiteSpace(x.Recommendation))
                    .ToList();

                ViewBag.PageTitle = "Pending Reviews";
            }
            else
            {
                ViewBag.PageTitle = "Assigned Proposals";
            }

            ViewBag.CurrentStatus = status;

            return View(evaluations);
        }

        // =====================================================
        // VIEW PROPOSAL DOCUMENT
        // =====================================================

        public ActionResult ViewDocument(int id)
        {
            var document =
                db.Proposals
                    .FirstOrDefault(p =>
                        p.ProposalID == id);

            if (document == null)
            {
                return HttpNotFound(
                    "No proposal was found.");
            }

            if (string.IsNullOrWhiteSpace(document.DocumentPath))
            {
                return HttpNotFound(
                    "The proposal document path is empty.");
            }

            string applicationPath =
                Request.ApplicationPath;

            if (applicationPath == "/")
            {
                applicationPath = "";
            }

            string documentUrl =
                applicationPath +
                document.DocumentPath;

            return Redirect(documentUrl);
        }
    }

  }
