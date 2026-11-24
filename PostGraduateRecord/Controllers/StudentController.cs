#pragma warning disable OPENAI001
using Newtonsoft.Json.Linq;
using OpenAI.Responses;
using PostGraduateRecord.Models;
using PostGraduateRecord.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using static System.Web.Razor.Parser.SyntaxConstants;

namespace PostGraduateRecord.Controllers
{
    public class StudentController : Controller
    {
        private PostGradEntities db = new PostGradEntities();

        public ActionResult Dashboard()
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // =========================
            // FIND STUDENT
            // =========================

            var student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }


            // =========================
            // APPLICATION
            // =========================

            var application = db.Applications
                .Where(a => a.StudentID == student.StudentID)
                .OrderByDescending(a => a.SubmittedDate)
                .FirstOrDefault();


            // =========================
            // SUPERVISOR ASSIGNMENT
            // =========================

            var assignment = application == null
                ? null
                : db.SupervisorAssignments
                    .Where(a =>
                        a.ApplicationID == application.ApplicationID)
                    .OrderByDescending(a => a.AssignmentID)
                    .FirstOrDefault();


            /// =========================
            // RESEARCH PROPOSAL
            // =========================

            var proposal = db.Proposals
                .Where(p =>
                    p.StudentID == student.StudentID)
                .OrderByDescending(p => p.SubmissionDate)
                .FirstOrDefault();


            // =========================
            // RELEASED EVALUATOR EVALUATION
            // =========================

            var releasedEvaluation = proposal == null
                ? null
                : db.Evaluations
                    .Where(e =>
                        e.ProposalID == proposal.ProposalID &&
                        e.EvaluationSentToStudent)
                    .OrderByDescending(e => e.EvaluationDate)
                    .FirstOrDefault();


            // =========================
            // ETHICS APPLICATION
            // =========================

            var ethics = db.EthicsApplications
                .Where(e =>
                    e.StudentID == student.StudentID)
                .OrderByDescending(e => e.SubmissionDate)
                .FirstOrDefault();


            // =========================
            // FINAL DISSERTATION
            // =========================

            var thesis = db.Theses
                .Where(t =>
                    t.StudentID == student.StudentID)
                .OrderByDescending(t => t.SubmittedDate)
                .FirstOrDefault();


            // =========================
            // SEND STATUS TO VIEW
            // =========================

            ViewBag.Application = application;
            ViewBag.Assignment = assignment;
            ViewBag.Proposal = proposal;
            ViewBag.Ethics = ethics;
            ViewBag.Thesis = thesis;

            ViewBag.ReleasedEvaluation = releasedEvaluation;


            // =========================
            // NEXT MILESTONE
            // =========================

            string nextMilestone = "Application";

            if (application == null)
            {
                nextMilestone = "Submit Application";
            }
            else if (application.Status == "Rejected")
            {
                nextMilestone = "Application Rejected";
            }
            else if (assignment == null)
            {
                nextMilestone = "Supervisor Assignment";
            }
            else if (proposal == null)
            {
                nextMilestone = "Research Proposal";
            }
            else if (ethics == null)
            {
                nextMilestone = "Ethics Application";
            }
            else if (thesis == null)
            {
                nextMilestone = "Final Dissertation";
            }
            else
            {
                nextMilestone = "Research Completed";
            }

            ViewBag.NextMilestone = nextMilestone;


            // =========================
            // CURRENT JOURNEY STAGE
            // =========================

            ViewBag.CurrentStage = nextMilestone;


            // =========================
            // STUDENT DETAILS
            // =========================

            ViewBag.StudentName =
                student.User.FirstName +
                " " +
                student.User.LastName;


            return View(student);
        }

        public ActionResult AI()
        {
            return View();
        }
        public ActionResult Proposal()
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userID = Convert.ToInt32(Session["UserID"]);

            // ==========================================
            // APPLICATION MUST BE APPROVED
            // ==========================================

            if (!IsApplicationApproved(userID))
            {
                TempData["Error"] =
                    "You must have an approved application before accessing the proposal section.";

                return RedirectToAction("Dashboard");
            }

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userID);

            if (student == null)
            {
                return RedirectToAction("Dashboard");
            }

            var proposals = db.Proposals
                .Where(p => p.StudentID == student.StudentID)
                .OrderByDescending(p => p.SubmissionDate)
                .ToList();

            return View(proposals);
        }
        public ActionResult Ethics()
        {
            LoadNotifications();

            // ==========================================
            // CHECK LOGIN
            // ==========================================

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // ==========================================
            // CHECK APPLICATION APPROVAL
            // ==========================================

            if (!IsApplicationApproved(userId))
            {
                TempData["Error"] =
                    "You must have an approved application before accessing the ethics section.";

                return RedirectToAction("Dashboard");
            }

            // ==========================================
            // GET STUDENT
            // ==========================================

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // CHECK PROPOSAL APPROVAL
            // ==========================================

            var proposal = db.Proposals
            .Where(p =>
                p.StudentID == student.StudentID &&
                (p.Status == "Approved" ||
                 p.Status == "Approved with Changes"))
            .OrderByDescending(p => p.SubmissionDate)
            .FirstOrDefault();

            if (proposal == null)
            {
                TempData["Error"] =
                    "Your research proposal must be approved before you can access the ethics section.";

                return RedirectToAction("Dashboard");
            }

            // ==========================================
            // GET LATEST ETHICS RECORD
            // ==========================================

            EthicsApplication ethics = db.EthicsApplications
                .Where(e => e.StudentID == student.StudentID)
                .OrderByDescending(e => e.SubmissionDate)
                .FirstOrDefault();

            return View(ethics);
        }

        public ActionResult NewApplication()
        {
            LoadNotifications();

            int userID = Convert.ToInt32(Session["UserID"]);

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userID);

            if (student == null)
            {
                return RedirectToAction("Dashboard");
            }

            Application application = db.Applications
                .FirstOrDefault(a => a.StudentID == student.StudentID);

            if (application != null)
            {
                return View("MyApplication", application);
            }

            return View();
        }
        [HttpPost]
        public ActionResult ApplicationForm(
       string ResearchTitle,
       string DegreeType,
       string ResearchField,
       HttpPostedFileBase SupportingDocument)
        {
            // ==========================================
            // CHECK LOGIN
            // ==========================================

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userID = Convert.ToInt32(Session["UserID"]);


            // ==========================================
            // FIND STUDENT
            // ==========================================

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userID);

            if (student == null)
            {
                TempData["Error"] =
                    "Student profile could not be found.";

                return RedirectToAction("NewApplication");
            }


            // ==========================================
            // PREVENT DUPLICATE APPLICATION
            // ==========================================

            bool applicationExists = db.Applications
                .Any(a => a.StudentID == student.StudentID);

            if (applicationExists)
            {
                TempData["Error"] =
                    "You have already submitted an application.";

                return RedirectToAction("NewApplication");
            }


            // ==========================================
            // VALIDATE RESEARCH TITLE
            // ==========================================

            if (string.IsNullOrWhiteSpace(ResearchTitle))
            {
                ViewBag.Message = "Please enter your research title.";

                ViewBag.ResearchTitle = ResearchTitle;
                ViewBag.DegreeType = DegreeType;
                ViewBag.ResearchField = ResearchField;

                return View("ApplicationForm");
            }


            // ==========================================
            // VALIDATE DEGREE TYPE
            // ==========================================

            if (string.IsNullOrWhiteSpace(DegreeType))
            {
                ViewBag.Message = "Please select a degree type.";

                ViewBag.ResearchTitle = ResearchTitle;
                ViewBag.DegreeType = DegreeType;
                ViewBag.ResearchField = ResearchField;

                return View("ApplicationForm");
            }

            if (DegreeType != "Masters" &&
                DegreeType != "PhD")
            {
                ViewBag.Message = "Please select a valid degree type.";

                ViewBag.ResearchTitle = ResearchTitle;
                ViewBag.DegreeType = DegreeType;
                ViewBag.ResearchField = ResearchField;

                return View("ApplicationForm");
            }


            // ==========================================
            // VALIDATE RESEARCH FIELD
            // ==========================================

            if (string.IsNullOrWhiteSpace(ResearchField))
            {
                ViewBag.Message = "Please enter your research field.";

                ViewBag.ResearchTitle = ResearchTitle;
                ViewBag.DegreeType = DegreeType;
                ViewBag.ResearchField = ResearchField;

                return View("ApplicationForm");
            }


            // ==========================================
            // VALIDATE SUPPORTING DOCUMENT
            // ==========================================

            if (SupportingDocument == null ||
                SupportingDocument.ContentLength == 0)
            {
                ViewBag.Message =
                    "Invalid document. Please upload a PDF or Word document (.pdf, .doc, or .docx).";

                ViewBag.ResearchTitle = ResearchTitle;
                ViewBag.DegreeType = DegreeType;
                ViewBag.ResearchField = ResearchField;

                return View("ApplicationForm");
            }


            // ==========================================
            // GET FILE EXTENSION
            // ==========================================

            string extension =
                Path.GetExtension(
                    SupportingDocument.FileName)
                .ToLower();


            // ==========================================
            // ALLOW ONLY PDF AND WORD
            // ==========================================

            if (extension != ".pdf" &&
                extension != ".doc" &&
                extension != ".docx")
            {
                ViewBag.Message =
                    "Invalid file type. Only PDF and Word documents (.pdf, .doc, .docx) are allowed.";

                ViewBag.ResearchTitle = ResearchTitle;
                ViewBag.DegreeType = DegreeType;
                ViewBag.ResearchField = ResearchField;

                return View("ApplicationForm");
            }

            // ==========================================
            // CREATE APPLICATION UPLOAD FOLDER
            // ==========================================

            string uploadsFolder =
                Server.MapPath("~/Uploads/Applications/");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }


            // ==========================================
            // CREATE SAFE UNIQUE FILE NAME
            // ==========================================

            string originalFileName =
                Path.GetFileName(
                    SupportingDocument.FileName);

            string fileName =
                Guid.NewGuid().ToString()
                + "_"
                + originalFileName;

            string fullPath =
                Path.Combine(
                    uploadsFolder,
                    fileName);


            // ==========================================
            // SAVE SUPPORTING DOCUMENT
            // ==========================================

            SupportingDocument.SaveAs(fullPath);


            // ==========================================
            // CREATE APPLICATION
            // ==========================================

            Application application = new Application
            {
                StudentID = student.StudentID,

                ResearchTitle = ResearchTitle.Trim(),

                DegreeType = DegreeType,

                ResearchField = ResearchField.Trim(),

                Status = "Pending",

                SubmittedDate = DateTime.Now
            };


            // ==========================================
            // SAVE APPLICATION
            // ==========================================

            db.Applications.Add(application);

            db.SaveChanges();


            // ==========================================
            // CREATE APPLICATION DOCUMENT
            // ==========================================

            ApplicationDocument applicationDocument =
                new ApplicationDocument
                {
                    ApplicationID = application.ApplicationID,

                    FileName =
                        Path.GetFileName(
                            SupportingDocument.FileName),

                    FilePath =
                        "/Uploads/Applications/" +
                        fileName,

                    UploadDate =
                        DateTime.Now
                };

            db.ApplicationDocuments.Add(
                applicationDocument);

            db.SaveChanges();

            // ==========================================
            // NOTIFY ADMIN
            // ==========================================

            var adminUsers = db.Users
                .Where(u => u.Role == "Admin")
                .ToList();

            foreach (var admin in adminUsers)
            {
                Notification notification =
                    new Notification
                    {
                        UserID = admin.UserID,

                        Message =
                            "Student submitted a new application.",

                        IsRead = false,

                        SentDate = DateTime.Now
                    };

                db.Notifications.Add(notification);
            }

            db.SaveChanges();
            // ==========================================
            // SUCCESS MESSAGE
            // ==========================================

            TempData["Success"] =
                "Your application and supporting document have been submitted successfully and are awaiting admin approval.";


            return RedirectToAction("NewApplication");
        }
        private bool IsApplicationRejected(int userId)
        {
            var student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return false;
            }

            var application = db.Applications
                .Where(a => a.StudentID == student.StudentID)
                .OrderByDescending(a => a.SubmittedDate)
                .FirstOrDefault();

            return application != null &&
                   application.Status == "Rejected";
        }

        private bool IsApplicationApproved(int userId)
        {
            var student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return false;
            }

            var application = db.Applications
                .Where(a => a.StudentID == student.StudentID)
                .OrderByDescending(a => a.SubmittedDate)
                .FirstOrDefault();

            return application != null &&
                   application.Status == "Approved";
        }


        // ==========================================
        // FINAL DISSERTATION PAGE
        // ==========================================

        public ActionResult FinalDissertation()
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // BLOCK REJECTED STUDENTS
            if (IsApplicationRejected(userId))
            {
                TempData["Error"] =
                    "Your application has been rejected. You cannot submit your final dissertation.";

                return RedirectToAction("Dashboard");
            }

            // FIND STUDENT
            var student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return HttpNotFound();
            }

            // CHECK APPROVED PROPOSAL
            var proposal = db.Proposals
             .Where(p =>
                 p.StudentID == student.StudentID &&
                 (p.Status == "Approved" ||
                  p.Status == "Approved with Changes"))
             .OrderByDescending(p => p.SubmissionDate)
             .FirstOrDefault();

            if (proposal == null)
            {
                TempData["Error"] =
                    "Your research proposal must be approved before submitting your final dissertation.";

                return RedirectToAction("Dashboard");
            }

            // FIND EXISTING FINAL DISSERTATION
            var thesis = db.Theses
                .Where(t => t.StudentID == student.StudentID)
                .OrderByDescending(t => t.SubmittedDate)
                .FirstOrDefault();

            // IF ALREADY SUBMITTED, SHOW THE RECORD
            if (thesis != null)
            {
                return View(thesis);
            }

            // OTHERWISE SHOW THE SUBMISSION FORM
            return View();
        }

        // ==========================================
        // FINAL DISSERTATION - SAVE
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FinalDissertation(
            string submissionType,
            string description,
            HttpPostedFileBase document)
        {
            LoadNotifications();

            try
            {
                // ==========================================
                // CHECK LOGIN
                // ==========================================

                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId = Convert.ToInt32(Session["UserID"]);


                // ==========================================
                // BLOCK REJECTED STUDENTS
                // ==========================================

                if (IsApplicationRejected(userId))
                {
                    TempData["Error"] =
                        "Your application has been rejected. You cannot submit your final dissertation.";

                    return RedirectToAction("Dashboard");
                }


                // ==========================================
                // FIND STUDENT
                // ==========================================

                var student = db.Students
                    .FirstOrDefault(s => s.UserID == userId);

                if (student == null)
                {
                    TempData["Error"] =
                        "Student profile not found.";

                    return RedirectToAction("FinalDissertation");
                }


                // ==========================================
                // VALIDATE SUBMISSION TYPE
                // ==========================================

                if (submissionType != "Final Dissertation")
                {
                    TempData["Error"] =
                        "Only Final Dissertation submissions are allowed.";

                    return RedirectToAction("FinalDissertation");
                }


                // ==========================================
                // VALIDATE DESCRIPTION
                // ==========================================

                if (string.IsNullOrWhiteSpace(description))
                {
                    TempData["Error"] =
                        "Please enter a description.";

                    return RedirectToAction("FinalDissertation");
                }


                // ==========================================
                // VALIDATE DOCUMENT
                // ==========================================

                if (document == null ||
                    document.ContentLength == 0)
                {
                    TempData["Error"] =
                        "Please select a document to upload.";

                    return RedirectToAction("FinalDissertation");
                }


                // ==========================================
                // VALIDATE FILE TYPE
                // ==========================================

                string extension =
                    Path.GetExtension(document.FileName)
                        .ToLower();

                if (extension != ".pdf" &&
                    extension != ".doc" &&
                    extension != ".docx")
                {
                    TempData["Error"] =
                        "Only PDF, DOC and DOCX files are allowed.";

                    return RedirectToAction("FinalDissertation");
                }


                // ==========================================
                // CHECK APPROVED PROPOSAL
                // ==========================================

                var proposal = db.Proposals
                    .Where(p =>
                        p.StudentID == student.StudentID &&
                        (p.Status == "Approved" ||
                         p.Status == "Approved with Changes"))
                    .OrderByDescending(
                        p => p.SubmissionDate)
                    .FirstOrDefault();

                if (proposal == null)
                {
                    TempData["Error"] =
                        "Your research proposal must be approved before submitting your final dissertation.";

                    return RedirectToAction("Dashboard");
                }


                // ==========================================
                // UPLOAD FOLDER
                // ==========================================

                string folderName = "Thesis";

                string uploadsFolder =
                    Server.MapPath(
                        "~/Uploads/" +
                        folderName +
                        "/");


                if (!Directory.Exists(
                    uploadsFolder))
                {
                    Directory.CreateDirectory(
                        uploadsFolder);
                }


                // ==========================================
                // CREATE UNIQUE FILE NAME
                // ==========================================

                string fileName =
                    Guid.NewGuid().ToString() +
                    "_" +
                    Path.GetFileName(
                        document.FileName);


                string fullPath =
                    Path.Combine(
                        uploadsFolder,
                        fileName);


                // ==========================================
                // SAVE FILE
                // ==========================================

                document.SaveAs(fullPath);


                string documentPath =
                    "/Uploads/" +
                    folderName +
                    "/" +
                    fileName;


                // ==========================================
                // CREATE FINAL DISSERTATION
                // ==========================================

                Thesis thesis = new Thesis
                {
                    StudentID =
                        student.StudentID,

                    Title =
                        "Final Dissertation",

                    Description =
                        description.Trim(),

                    DocumentPath =
                        documentPath,

                    SubmittedDate =
                        DateTime.Now,

                    Status =
                        "Submitted"
                };


                db.Theses.Add(thesis);

                db.SaveChanges();


                // ==========================================
                // SUCCESS
                // ==========================================

                TempData["Success"] =
                    "Final Dissertation submitted successfully.";

                return RedirectToAction(
                    "FinalDissertation");
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    ex.InnerException?.InnerException?.Message
                    ?? ex.Message;

                return RedirectToAction(
                    "FinalDissertation");
            }
        }


        public ActionResult ApplicationForm()
        {
            int userID = Convert.ToInt32(Session["UserID"]);

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userID);

            if (student == null)
            {
                return RedirectToAction("Dashboard");
            }

            Application application = db.Applications
                .FirstOrDefault(a => a.StudentID == student.StudentID);

            if (application != null)
            {
                return View("MyApplication", application);
            }

            return View();
        }

        public ActionResult EthicsApplication()
        {
            // ==========================================
            // CHECK LOGIN
            // ==========================================

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // ==========================================
            // GET STUDENT
            // ==========================================

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // CHECK APPLICATION APPROVAL
            // ==========================================

            Application application = db.Applications
                .Where(a => a.StudentID == student.StudentID)
                .OrderByDescending(a => a.SubmittedDate)
                .FirstOrDefault();

            if (application == null ||
                application.Status != "Approved")
            {
                TempData["Error"] =
                    "Your postgraduate application must be approved before accessing ethics.";

                return RedirectToAction("Dashboard");
            }

            // ==========================================
            // CHECK PROPOSAL APPROVAL
            // ==========================================

            Proposal proposal = db.Proposals
                .Where(p =>
                    p.StudentID == student.StudentID &&
                    (p.Status == "Approved" ||
                     p.Status == "Approved with Changes"))
                .OrderByDescending(p => p.SubmissionDate)
                .FirstOrDefault();

            if (proposal == null)
            {
                TempData["Error"] =
                    "Your research proposal must be approved before accessing ethics.";

                return RedirectToAction("Dashboard");
            }

            // ==========================================
            // FIND EXISTING ETHICS RECORD
            // ==========================================

            EthicsApplication ethics = db.EthicsApplications
                .FirstOrDefault(e =>
                    e.StudentID == student.StudentID);

            // ==========================================
            // CREATE VIEW MODEL
            // ==========================================

            var model = new EthicsApplicationViewModel();

            // ==========================================
            // LOAD EXISTING INFORMATION
            // ==========================================

            if (ethics != null)
            {
                if (ethics.Status == "Approved")
                {
                    model.EthicsSubmitted = true;
                    model.EthicsNumber = ethics.ReferenceNumber;
                }
                else if (ethics.Status == "Not Submitted")
                {
                    model.EthicsSubmitted = false;
                }
            }

            return View(model);
        }


        // GET: Student/NewProposal
        [HttpGet]
        public ActionResult NewProposal()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // ==========================================
            // APPLICATION MUST BE APPROVED
            // ==========================================

            if (!IsApplicationApproved(userId))
            {
                TempData["Error"] =
                    "You must have an approved application before submitting a research proposal.";

                return RedirectToAction("Dashboard");
            }

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            bool proposalExists = db.Proposals
                .Any(p => p.StudentID == student.StudentID);

            if (proposalExists)
            {
                TempData["Error"] =
                    "You have already submitted a research proposal.";

                return RedirectToAction("Proposal");
            }

            return View(new ProposalViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EthicsApplication(EthicsApplicationViewModel model)
        {
            // ==========================================
            // CHECK LOGIN
            // ==========================================

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // GET LOGGED-IN STUDENT
            // ==========================================

            int userId = Convert.ToInt32(Session["UserID"]);

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // CHECK APPLICATION APPROVAL
            // ==========================================

            Application application = db.Applications
                .Where(a => a.StudentID == student.StudentID)
                .OrderByDescending(a => a.SubmittedDate)
                .FirstOrDefault();

            if (application == null ||
                application.Status != "Approved")
            {
                TempData["Error"] =
                    "Your postgraduate application must be approved before accessing ethics.";

                return RedirectToAction("Dashboard");
            }

            // ==========================================
            // CHECK PROPOSAL APPROVAL
            // ==========================================

            Proposal proposal = db.Proposals
            .Where(p =>
                p.StudentID == student.StudentID &&
                (p.Status == "Approved" ||
                 p.Status == "Approved with Changes"))
            .OrderByDescending(p => p.SubmissionDate)
            .FirstOrDefault();

            if (proposal == null)
            {
                TempData["Error"] =
                    "Your research proposal must be approved before accessing ethics.";

                return RedirectToAction("Dashboard");
            }

            // ==========================================
            // YES / NO MUST BE SELECTED
            // ==========================================

            if (!model.EthicsSubmitted.HasValue)
            {
                ModelState.AddModelError(
                    "EthicsSubmitted",
                    "Please select Yes or No.");

                return View(model);
            }

            // ==========================================
            // FIND EXISTING ETHICS RECORD
            // ==========================================

            EthicsApplication ethics = db.EthicsApplications
                .FirstOrDefault(e =>
                    e.StudentID == student.StudentID);

            // ==========================================
            // STUDENT SELECTED NO
            // ==========================================

            if (model.EthicsSubmitted.Value == false)
            {
                if (ethics == null)
                {
                    ethics = new EthicsApplication();

                    ethics.StudentID = student.StudentID;
                    ethics.ProposalID = proposal.ProposalID;
                    ethics.Status = "Not Submitted";
                    ethics.SubmissionDate = DateTime.Now;

                    db.EthicsApplications.Add(ethics);
                }
                else
                {
                    ethics.ProposalID = proposal.ProposalID;
                    ethics.Status = "Not Submitted";
                    ethics.ReferenceNumber = null;
                    ethics.ApprovalDate = null;
                    ethics.DocumentPath = null;
                }

                db.SaveChanges();

                TempData["Success"] =
     "Your ethics information has been saved.";

                return RedirectToAction("Ethics");
            }

            // ==========================================
            // STUDENT SELECTED YES
            // ==========================================

            if (string.IsNullOrWhiteSpace(model.EthicsNumber))
            {
                ModelState.AddModelError(
                    "EthicsNumber",
                    "Please enter your Ethics Number.");

                return View(model);
            }

            // ==========================================
            // DEMO ETHICS NUMBER
            // ==========================================

            const string validEthicsNumber = "10002026";

            if (model.EthicsNumber.Trim() != validEthicsNumber)
            {
                ModelState.AddModelError(
                    "EthicsNumber",
                    "Invalid Ethics Number. Please check the number and try again.");

                return View(model);
            }

            // ==========================================
            // CREATE OR UPDATE ETHICS RECORD
            // ==========================================

            if (ethics == null)
            {
                ethics = new EthicsApplication();

                ethics.StudentID = student.StudentID;
                ethics.ProposalID = proposal.ProposalID;

                db.EthicsApplications.Add(ethics);
            }
            else
            {
                ethics.ProposalID = proposal.ProposalID;
            }

            // ==========================================
            // SAVE VERIFIED ETHICS INFORMATION
            // ==========================================

            ethics.ReferenceNumber = validEthicsNumber;
            ethics.Status = "Approved";
            ethics.ApprovalDate = DateTime.Now;
            ethics.SubmissionDate = DateTime.Now;

            db.SaveChanges();

            TempData["Success"] =
          "Your Ethics Number has been verified successfully.";

            return RedirectToAction("Ethics");
        }


        // POST: Student/NewProposal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewProposal(ProposalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get logged in user
                int userId = Convert.ToInt32(Session["UserID"]);

                // ==========================================
                // BLOCK REJECTED STUDENTS
                // ==========================================
                if (IsApplicationRejected(userId))
                {
                    TempData["Error"] =
                        "Your application has been rejected. You cannot submit a proposal.";

                    return RedirectToAction("Dashboard");
                }
                if (!IsApplicationApproved(userId))
                {
                    TempData["Error"] =
                        "You must have an approved application before submitting a research proposal.";

                    return RedirectToAction("Dashboard");
                }
                // Find student profile
                Student student = db.Students
                    .FirstOrDefault(s => s.UserID == userId);

                if (student == null)
                {
                    ViewBag.Message = "Student profile not found.";
                    return View(model);
                }

                bool proposalExists = db.Proposals
                    .Any(p => p.StudentID == student.StudentID);

                if (proposalExists)
                {
                    TempData["Error"] =
                        "You have already submitted a research proposal.";

                    return RedirectToAction("Proposal");
                }
                string filePath = null;

                // Check that a file was selected
                if (model.ProposalFile == null ||
                  model.ProposalFile.ContentLength == 0)
                {
                    ViewBag.Message =
                        "Invalid document. Please upload a PDF or Word document (.pdf, .doc, or .docx).";

                    return View("NewProposal", model);
                }
                // Check file type
                string extension =
                    Path.GetExtension(model.ProposalFile.FileName)
                        .ToLower();

                if (extension != ".pdf" &&
                    extension != ".doc" &&
                    extension != ".docx")
                {
                    ViewBag.Message =
                        "Invalid file type. Only PDF and Word documents (.pdf, .doc, .docx) are allowed.";

                    return View("NewProposal", model);
                }

                // Upload file
                string uploadsFolder =
                    Server.MapPath("~/Uploads/Proposals/");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName =
                    Guid.NewGuid().ToString() + "_" +
                    Path.GetFileName(model.ProposalFile.FileName);

                string fullPath =
                    Path.Combine(uploadsFolder, fileName);

                model.ProposalFile.SaveAs(fullPath);

                filePath =
                    "/Uploads/Proposals/" + fileName;

                Proposal proposal = new Proposal
                {
                    StudentID = student.StudentID,
                    Title = model.Title,
                    ResearchField = model.ResearchField,
                    Abstract = model.Abstract,
                    Keywords = model.Keywords,
                    SubmissionDate = DateTime.Now,
                    Status = "Submitted",
                    DocumentPath = filePath
                };

                db.Proposals.Add(proposal);
                db.SaveChanges();


                // ==========================================
                // CREATE PROPOSAL DOCUMENT
                // ==========================================
                if (!string.IsNullOrEmpty(filePath))
                {
                    ProposalDocument proposalDocument =
                        new ProposalDocument
                        {
                            ProposalID = proposal.ProposalID,
                            FileName =
                                Path.GetFileName(
                                    model.ProposalFile.FileName),
                            FilePath = filePath,
                            FileType =
                                Path.GetExtension(
                                    model.ProposalFile.FileName),
                            VersionNo = 1,
                            UploadDate = DateTime.Now
                        };

                    db.ProposalDocuments.Add(proposalDocument);
                    db.SaveChanges();
                }


                // ==========================================
                // FIND ASSIGNED SUPERVISOR
                // ==========================================
                var application = db.Applications
                    .FirstOrDefault(a =>
                        a.StudentID == student.StudentID);

                if (application != null)
                {
                    var assignment = db.SupervisorAssignments
                        .FirstOrDefault(a =>
                            a.ApplicationID ==
                            application.ApplicationID);

                    if (assignment != null)
                    {
                        assignment.Status = "Proposal Submitted";

                        db.SaveChanges();
                    


                    // ==========================================
                    // NOTIFY SUPERVISOR
                    // ==========================================
                    var supervisor = db.Supervisors
                            .Include(s => s.User)
                            .FirstOrDefault(s =>
                                s.SupervisorID ==
                                assignment.SupervisorID);

                        if (supervisor != null)
                        {
                            Notification notification =
                                new Notification
                                {
                                    UserID = supervisor.UserID,

                                    Message =
                                        "A research proposal has been submitted by "
                                        + student.User.FirstName
                                        + " "
                                        + student.User.LastName,

                                    IsRead = false,

                                    SentDate = DateTime.Now,

                                    Link =
                                        "/Supervisors/Review/"
                                        + proposal.ProposalID
                                };

                            db.Notifications.Add(notification);
                            db.SaveChanges();
                        }
                    }
                }


                TempData["Success"] =
                    "Proposal submitted successfully.";

                return RedirectToAction("Proposal");
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
                return View(model);
            }
        }

        public ActionResult ProposalDetails(int id)
        {
            int userID = Convert.ToInt32(Session["UserID"]);

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userID);

            if (student == null)
            {
                return RedirectToAction("Dashboard");
            }

            Proposal proposal = db.Proposals
                .FirstOrDefault(p =>
                    p.ProposalID == id &&
                    p.StudentID == student.StudentID);

            if (proposal == null)
            {
                return HttpNotFound();
            }


            // ===========================
            // SUPERVISOR FEEDBACK
            // ===========================

            var feedback = db.Feedbacks
                .Where(f => f.ProposalID == proposal.ProposalID)
                .OrderByDescending(f => f.FeedbackDate)
                .FirstOrDefault();

            ViewBag.Feedback = feedback;


            // ===========================
            // EVALUATOR EVALUATION
            // ===========================

            var evaluation = db.Evaluations
                .Include(e => e.Evaluator)
                .Include(e => e.Evaluator.User)
                .Where(e => e.ProposalID == proposal.ProposalID)
                .FirstOrDefault();

            ViewBag.Evaluation = evaluation;


            return View(proposal);
        }
        public ActionResult ProposalFeedback()
        {
            LoadNotifications();
            return View();
        }
        public ActionResult DownloadProposal(int id)
        {
            Proposal proposal = db.Proposals.FirstOrDefault(p => p.ProposalID == id);

            if (proposal == null)
            {
                // return HttpNotFound();
                TempData["error"] = "proposal not found";
                return RedirectToAction("Proposal");

            }

            if (string.IsNullOrEmpty(proposal.DocumentPath))
            {
                // return HttpNotFound();
                TempData["error"] = "proposal document is not available on the server";
                return RedirectToAction("ProposalDetails", new {id= id});


            }

            string fullPath = Server.MapPath(proposal.DocumentPath);

            if (!System.IO.File.Exists(fullPath))
            {
                //return HttpNotFound("File not found.");
                TempData["error"] = "proposal file not found on the server";
                return RedirectToAction("ProposalDetails", new { id = id });

            }

            string fileName = Path.GetFileName(fullPath);

            return File(fullPath,
                        "application/octet-stream",
                        fileName);
        }
        public ActionResult EditProposal(int id)
        {
            Proposal proposal = db.Proposals.Find(id);

            if (proposal == null)
            {
                return HttpNotFound();
            }

            ProposalViewModel model = new ProposalViewModel
            {
                ProposalID = proposal.ProposalID,
                Title = proposal.Title,
                ResearchField = proposal.ResearchField,
                Abstract = proposal.Abstract,
                Keywords = proposal.Keywords
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProposal(ProposalViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Proposal proposal = db.Proposals.Find(model.ProposalID);

            if (proposal == null)
            {
                return HttpNotFound();
            }

            // Update proposal details
            proposal.Title = model.Title;
            proposal.ResearchField = model.ResearchField;
            proposal.Abstract = model.Abstract;
            proposal.Keywords = model.Keywords;

            // Check if a new document was uploaded
            if (model.ProposalFile != null &&
                model.ProposalFile.ContentLength > 0)
            {
                // Check file type
                string extension =
                    Path.GetExtension(model.ProposalFile.FileName).ToLower();

                if (extension != ".pdf" &&
                    extension != ".doc" &&
                    extension != ".docx")
                {
                    ViewBag.Message =
                        "Invalid file type. Only PDF and Word documents (.pdf, .doc, .docx) are allowed.";

                    return View(model);
                }

                string uploadsFolder =
                    Server.MapPath("~/Uploads/Proposals/");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName =
                    Guid.NewGuid().ToString() + "_" +
                    Path.GetFileName(model.ProposalFile.FileName);

                string fullPath =
                    Path.Combine(uploadsFolder, fileName);

                model.ProposalFile.SaveAs(fullPath);

                proposal.DocumentPath =
                    "/Uploads/Proposals/" + fileName;
            }

            // IMPORTANT:
            // Student has submitted the revised proposal
            // so the supervisor must review it again.
            proposal.Status = "Submitted";

            proposal.SubmissionDate = DateTime.Now;

            db.SaveChanges();

            // Notify the assigned supervisor
            var application = db.Applications
     .FirstOrDefault(a => a.StudentID == proposal.StudentID);

            var assignment = application == null
                ? null
                : db.SupervisorAssignments
                    .FirstOrDefault(a =>
                        a.ApplicationID == application.ApplicationID);

            if (assignment != null)
            {
                var supervisor = db.Supervisors
                    .Include(s => s.User)
                    .FirstOrDefault(s =>
                        s.SupervisorID == assignment.SupervisorID);

                if (supervisor != null)
                {
                    var student = db.Students
                        .Include(s => s.User)
                        .FirstOrDefault(s =>
                            s.StudentID == proposal.StudentID);

                    if (student != null)
                    {
                        Notification notification =
                            new Notification
                            {
                                UserID = supervisor.UserID,
                                Message =
                                    "A revised research proposal has been submitted by "
                                    + student.User.FirstName
                                    + " "
                                    + student.User.LastName,
                                IsRead = false,
                                SentDate = DateTime.Now,
                                Link =
                                    "/Supervisors/Review/"
                                    + proposal.ProposalID
                            };

                        db.Notifications.Add(notification);
                        db.SaveChanges();
                    }
                }
            }

            TempData["Success"] =
                "Your revised proposal has been submitted successfully and is awaiting supervisor review.";

            return RedirectToAction("Proposal");
        }
        public ActionResult DeleteProposal(int id)
        {
            try
            {
                Proposal proposal = db.Proposals.Find(id);

                if (proposal == null)
                {
                    TempData["Error"] = "Proposal not found.";
                    return RedirectToAction("Proposal");
                }

                if (proposal.Status != "Rejected" && proposal.Status != "Revision Required")
                {
                    TempData["Error"] = "Only rejected proposals can be deleted.";
                    return RedirectToAction("Proposal");
                }

                // Delete related feedback
                var feedbacks = db.Feedbacks
                                  .Where(f => f.ProposalID == proposal.ProposalID)
                                  .ToList();

                db.Feedbacks.RemoveRange(feedbacks);

                // Delete related ethics application
                var ethics = db.EthicsApplications
                               .Where(e => e.ProposalID == proposal.ProposalID)
                               .ToList();

                db.EthicsApplications.RemoveRange(ethics);

                // Delete uploaded document
                if (!string.IsNullOrEmpty(proposal.DocumentPath))
                {
                    string fullPath = Server.MapPath(proposal.DocumentPath);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                // Find the application belonging to this proposal's student
                var application = db.Applications
                    .FirstOrDefault(a => a.StudentID == proposal.StudentID);

                // Delete supervisor assignments for that application
                var assignments = new List<SupervisorAssignment>();

                if (application != null)
                {
                    assignments = db.SupervisorAssignments
                        .Where(a => a.ApplicationID == application.ApplicationID)
                        .ToList();
                }
                db.SupervisorAssignments.RemoveRange(assignments);

                db.Proposals.Remove(proposal);

                db.SaveChanges();

                TempData["Success"] = "Proposal deleted successfully.";

                return RedirectToAction("Proposal");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.InnerException?.InnerException?.Message ?? ex.Message;
                return RedirectToAction("Proposal");
            }
        }

        private void LoadNotifications()
        {
            int userId = Convert.ToInt32(Session["UserID"]);

            var notifications = db.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.SentDate)
                .ToList();

            ViewBag.Notifications = notifications;

            ViewBag.NotificationCount = notifications.Count(n => n.IsRead != true);
        }
        public ActionResult OpenNotification(int id)
        {
            int userID = Convert.ToInt32(Session["UserID"]);

            var notification = db.Notifications
                .FirstOrDefault(n => n.NotificationID == id && n.UserID == userID);

            if (notification != null)
            {
                notification.IsRead = true;
                db.SaveChanges();

                if (!string.IsNullOrEmpty(notification.Link))
                {
                    return Redirect(notification.Link);
                }
            }

            return RedirectToAction("Dashboard");
        }

       
       
        // ==========================================
        // AI RESEARCH ASSISTANT - GEMINI CHAT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async System.Threading.Tasks.Task<ActionResult> AskAI(string question)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                TempData["AIError"] = "Please enter a question.";
                return RedirectToAction("AI");
            }

            question = question.Trim();

            try
            {
                string apiKey = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_API_KEY");
                // ==========================================
                // GET EXISTING CHAT HISTORY
                // ==========================================

                var chatHistory =
                    Session["AIChatHistory"]
                    as List<Dictionary<string, string>>;

                if (chatHistory == null)
                {
                    chatHistory =
                        new List<Dictionary<string, string>>();
                }


                // ==========================================
                // ADD STUDENT QUESTION TO HISTORY
                // ==========================================

                chatHistory.Add(
                    new Dictionary<string, string>
                    {
                { "Role", "User" },
                { "Message", question }
                    }
                );


                // ==========================================
                // BUILD GEMINI CONVERSATION
                // ==========================================

        
// ==========================================
// BUILD GEMINI CONVERSATION
// KEEP ONLY RECENT CHAT HISTORY
// ==========================================

var contents = new JArray();

                // Keep the most recent 6 messages.
                // This prevents the request from becoming
                // larger after every question.

                var recentMessages =
                    chatHistory
                        .Skip(Math.Max(0, chatHistory.Count - 6))
                        .ToList();

                foreach (var message in recentMessages)
                {
                    string role =
                        message["Role"] == "User"
                        ? "user"
                        : "model";

                    contents.Add(
                        new JObject
                        {
                            ["role"] = role,
                            ["parts"] = new JArray
                            {
                new JObject
                {
                    ["text"] = message["Message"]
                }
                            }
                        }
                    );
                }




                // ==========================================
                // RESEARCH ASSISTANT INSTRUCTIONS
                // ==========================================

                string systemInstruction =
                    "You are an AI Research Assistant for a postgraduate student. " +
                    "Your purpose is to provide helpful guidance throughout the student's postgraduate research journey. " +
                    "You can help with research topics, research questions, research objectives, " +
                    "research methodology, literature reviews, problem statements, academic writing, " +
                    "research design and general postgraduate research guidance. " +
                    "Give clear, accurate and academically appropriate answers. " +
                    "Explain difficult concepts in a simple way when appropriate. " +
                    "Maintain the context of the conversation and use previous messages when answering follow-up questions. " +
                    "Do not invent academic references, sources, statistics or facts. " +
                    "If the student asks for something unclear, ask for clarification. " +
                    "The student's current project is a web-based postgraduate record-keeping system.";


                // ==========================================
                // GEMINI API URL
                // ==========================================

                string url =
                    "https://generativelanguage.googleapis.com/v1beta/models/" +
                    "gemini-3.8-flash:generateContent";


                // ==========================================
                // CREATE REQUEST
                // ==========================================

                var requestBody = new JObject
                {
                    ["system_instruction"] = new JObject
                    {
                        ["parts"] = new JArray
                {
                    new JObject
                    {
                        ["text"] = systemInstruction
                    }
                }
                    },

                    ["contents"] = contents
                };


                string json =
                    Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);


                // ==========================================
                // SEND REQUEST TO GEMINI
                // WITH AUTOMATIC RETRIES
                // ==========================================

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add(
                        "x-goog-api-key",
                        apiKey
                    );

                    string responseJson = "";
                    HttpResponseMessage response = null;

                    // Maximum of 4 attempts
                    int maxAttempts = 4;

                    for (int attempt = 1;
                         attempt <= maxAttempts;
                         attempt++)
                    {
                        using (var content = new StringContent(
                            json,
                            System.Text.Encoding.UTF8,
                            "application/json"))
                        {
                            response =
                                await client.PostAsync(
                                    url,
                                    content);
                        }

                        responseJson =
                            await response.Content.ReadAsStringAsync();


                        // ==========================================
                        // SUCCESS
                        // ==========================================

                        if (response.IsSuccessStatusCode)
                        {
                            break;
                        }


                        // ==========================================
                        // RETRY TEMPORARY SERVER ERRORS
                        // ==========================================

                        if (
                            (int)response.StatusCode >= 500 &&
                            (int)response.StatusCode <= 599 &&
                            attempt < maxAttempts)
                        {
                            // 2 seconds, 4 seconds, 8 seconds
                            int delayMilliseconds =
                                (int)Math.Pow(2, attempt) * 1000;

                            await System.Threading.Tasks.Task.Delay(
                                delayMilliseconds);

                            continue;
                        }


                        // ==========================================
                        // FINAL ERROR
                        // ==========================================

                        if (!response.IsSuccessStatusCode)
                        {
                            // Remove the question because Gemini
                            // did not successfully answer it.

                            if (chatHistory.Count > 0)
                            {
                                chatHistory.RemoveAt(
                                    chatHistory.Count - 1);
                            }

                            Session["AIChatHistory"] =
                                chatHistory;

                            TempData["AIError"] =
                                "Gemini could not process your request. " +
                                "HTTP " +
                                (int)response.StatusCode +
                                ": " +
                                responseJson;

                            return RedirectToAction("AI");
                        }
                    }


                    // ==========================================
                    // READ GEMINI RESPONSE
                    // ==========================================

                    JObject result =
                        JObject.Parse(responseJson);


                    string answer =
                        (string)result["candidates"]?[0]?
                        ["content"]?["parts"]?[0]?["text"];


                    // ==========================================
                    // CHECK FOR EMPTY RESPONSE
                    // ==========================================

                    if (string.IsNullOrWhiteSpace(answer))
                    {
                        if (chatHistory.Count > 0)
                        {
                            chatHistory.RemoveAt(
                                chatHistory.Count - 1);
                        }

                        Session["AIChatHistory"] =
                            chatHistory;

                        TempData["AIError"] =
                            "Gemini returned an empty response.";

                        return RedirectToAction("AI");
                    }


                    // ==========================================
                    // ADD AI RESPONSE TO CHAT HISTORY
                    // ==========================================

                    chatHistory.Add(
                        new Dictionary<string, string>
                        {
            {
                "Role",
                "AI"
            },

            {
                "Message",
                answer
            }
                        }
                    );


                    // ==========================================
                    // SAVE CHAT HISTORY
                    // ==========================================

                    Session["AIChatHistory"] =
                        chatHistory;
                }
            }
            catch (Exception ex)
            {
                TempData["AIError"] =
                    "Sorry, the AI assistant could not process your request. " +
                    "Error: " +
                    ex.Message;
            }


            return RedirectToAction("AI");
        }


        // ==========================================
        // CLEAR AI CHAT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearAIChat()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            Session.Remove("AIChatHistory");

            return RedirectToAction("AI");
        }
        public ActionResult EthicsCertificate()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            EthicsApplication ethics = db.EthicsApplications
                .Where(e =>
                    e.StudentID == student.StudentID &&
                    e.Status == "Approved")
                .OrderByDescending(e => e.ApprovalDate)
                .FirstOrDefault();

            if (ethics == null)
            {
                TempData["EthicsError"] =
                    "Your ethics application has not been approved.";

                return RedirectToAction("Ethics");
            }

            return RedirectToAction(
                "EthicsCertificateResult",
                new { id = ethics.EthicsID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EthicsCertificate(EthicsCertificateViewModel model)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View("VerifyEthicsCertificate", model);
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            EthicsApplication ethics = db.EthicsApplications
                .FirstOrDefault(e =>
                    e.StudentID == student.StudentID &&
                    e.ReferenceNumber == model.ReferenceNumber);

            if (ethics == null)
            {
                ModelState.AddModelError(
                    "ReferenceNumber",
                    "Invalid ethics reference number. Please check the number and try again."
                );

                return View("VerifyEthicsCertificate", model);
            }

            if (ethics.Status != "Approved")
            {
                ModelState.AddModelError(
                    "ReferenceNumber",
                    "This ethics application has not been approved yet."
                );

                return View("VerifyEthicsCertificate", model);
            }

            // Reference is valid and belongs to the logged-in student.
            return RedirectToAction(
                "EthicsCertificateResult",
                new { id = ethics.EthicsID });
        }

        public ActionResult EthicsCertificateResult(int id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // FIND APPROVED ETHICS APPLICATION
            // ==========================================

            EthicsApplication ethics = db.EthicsApplications
                .Include(e => e.Student.User)
                .FirstOrDefault(e =>
                    e.EthicsID == id &&
                    e.StudentID == student.StudentID &&
                    e.Status == "Approved");

            if (ethics == null)
            {
                TempData["Error"] =
                    "You are not authorized to view this ethics certificate.";

                return RedirectToAction("EthicsCertificate");
            }

            // ==========================================
            // FIND APPROVED APPLICATION
            // ==========================================

            Application application = db.Applications
                .Where(a =>
                    a.StudentID == student.StudentID &&
                    a.Status == "Approved")
                .OrderByDescending(a => a.SubmittedDate)
                .FirstOrDefault();

            // ==========================================
            // SEND RESEARCH INFORMATION TO VIEW
            // ==========================================

            if (application != null)
            {
                ViewBag.ResearchTitle =
                    application.ResearchTitle;

                ViewBag.ResearchField =
                    application.ResearchField;
            }
            else
            {
                ViewBag.ResearchTitle =
                    "Not available";

                ViewBag.ResearchField =
                    "Not available";
            }

            // ==========================================
            // FIND ASSIGNED SUPERVISOR
            // ==========================================

            Supervisor supervisor = null;

            if (application != null)
            {
                var assignment = db.SupervisorAssignments
                    .Where(a =>
                        a.ApplicationID == application.ApplicationID)
                    .OrderByDescending(a => a.AssignmentID)
                    .FirstOrDefault();

                if (assignment != null)
                {
                    supervisor = db.Supervisors
                        .Include(s => s.User)
                        .FirstOrDefault(s =>
                            s.SupervisorID == assignment.SupervisorID);
                }
            }

            // ==========================================
            // SEND SUPERVISOR NAME TO CERTIFICATE
            // ==========================================

            if (supervisor != null &&
                supervisor.User != null)
            {
                ViewBag.SupervisorName =
                    supervisor.User.FirstName +
                    " " +
                    supervisor.User.LastName;
            }
            else
            {
                ViewBag.SupervisorName =
                    "Assigned Supervisor";
            }

            return View(ethics);
        }

        // ==========================================
        // VIEW PROGRESS REPORT
        // ==========================================

        public ActionResult ProgressReportDetails(int id)
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // ==========================================
            // FIND LOGGED-IN STUDENT
            // ==========================================

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // FIND PROGRESS REPORT
            // ==========================================

            ProgressReport report = db.ProgressReports
                .Include(r => r.Student.User)
                .Include(r => r.Proposal)
                .FirstOrDefault(r =>
                    r.ProgressReportID == id &&
                    r.StudentID == student.StudentID);

            if (report == null)
            {
                TempData["Error"] =
                    "The progress report could not be found or you are not authorized to view it.";

                return RedirectToAction("Dashboard");
            }

            return View(report);
        }


        public ActionResult ViewProgressReport(int id)
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            var student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return HttpNotFound("Student record not found.");
            }

            var report = db.ProgressReports
                .Include(r => r.Proposal)
                .FirstOrDefault(r =>
                    r.ProgressReportID == id &&
                    r.StudentID == student.StudentID);

            if (report == null)
            {
                return HttpNotFound("Progress report not found.");
            }

            return View(report);
        }



        public ActionResult ProgressReports()
        {
            LoadNotifications();

            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            var student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return HttpNotFound("Student record not found.");
            }

            var reports = db.ProgressReports
                .Include(r => r.Proposal)
                .Where(r => r.StudentID == student.StudentID)
                .OrderByDescending(r => r.ReportDate)
                .ToList();

            return View(reports);
        }

        public ActionResult DownloadEthicsCertificate(int id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            Student student = db.Students
                .FirstOrDefault(s => s.UserID == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // GET APPROVED ETHICS RECORD
            // ==========================================

            EthicsApplication ethics = db.EthicsApplications
                .Include(e => e.Student.User)
                .FirstOrDefault(e =>
                    e.EthicsID == id &&
                    e.StudentID == student.StudentID &&
                    e.Status == "Approved");

            if (ethics == null)
            {
                return HttpNotFound(
                    "Ethics certificate not found.");
            }


            // ==========================================
            // GET APPROVED APPLICATION
            // ==========================================

            Application application = db.Applications
                .Where(a =>
                    a.StudentID == student.StudentID &&
                    a.Status == "Approved")
                .OrderByDescending(a => a.SubmittedDate)
                .FirstOrDefault();

            if (application == null)
            {
                return HttpNotFound(
                    "Approved postgraduate application not found.");
            }


            // ==========================================
            // GET APPROVED PROPOSAL
            // ==========================================

            Proposal proposal = db.Proposals
                .Where(p =>
                    p.StudentID == student.StudentID &&
                    (p.Status == "Approved" ||
                     p.Status == "Approved with Changes"))
                .OrderByDescending(p => p.SubmissionDate)
                .FirstOrDefault();

            if (proposal == null)
            {
                return HttpNotFound(
                    "Approved research proposal not found.");
            }


            // ==========================================
            // CREATE PDF DOCUMENT
            // ==========================================

            using (var document =
                new iTextSharp.text.Document(
                    iTextSharp.text.PageSize.A4,
                    50,
                    50,
                    50,
                    50))
            {
                using (var memoryStream =
                    new System.IO.MemoryStream())
                {
                    iTextSharp.text.pdf.PdfWriter.GetInstance(
                        document,
                        memoryStream);

                    document.Open();


                    // ==========================================
                    // FONTS
                    // ==========================================

                    var navy =
                        new iTextSharp.text.BaseColor(
                            0,
                            27,
                            68);

                    var gold =
                        new iTextSharp.text.BaseColor(
                            253,
                            185,
                            19);


                    var navyFont =
                        iTextSharp.text.FontFactory.GetFont(
                            "Arial",
                            22,
                            iTextSharp.text.Font.BOLD,
                            navy);


                    var universityFont =
                        iTextSharp.text.FontFactory.GetFont(
                            "Arial",
                            12,
                            iTextSharp.text.Font.BOLD,
                            gold);


                    var sectionFont =
                        iTextSharp.text.FontFactory.GetFont(
                            "Arial",
                            11,
                            iTextSharp.text.Font.BOLD,
                            navy);


                    var normalFont =
                        iTextSharp.text.FontFactory.GetFont(
                            "Arial",
                            10,
                            iTextSharp.text.Font.NORMAL);


                    var boldFont =
                        iTextSharp.text.FontFactory.GetFont(
                            "Arial",
                            10,
                            iTextSharp.text.Font.BOLD);


                    // ==========================================
                    // HEADER
                    // ==========================================

                    var university =
                        new iTextSharp.text.Paragraph(
                            "Nelson Mandela University",
                            universityFont);

                    university.Alignment =
                        iTextSharp.text.Element.ALIGN_CENTER;

                    university.SpacingAfter = 8;

                    document.Add(university);


                    var title =
                        new iTextSharp.text.Paragraph(
                            "ETHICS CLEARANCE CERTIFICATE",
                            navyFont);

                    title.Alignment =
                        iTextSharp.text.Element.ALIGN_CENTER;

                    title.SpacingAfter = 10;

                    document.Add(title);


                    var line =
                        new iTextSharp.text.Paragraph(
                            "____________________________________________");

                    line.Alignment =
                        iTextSharp.text.Element.ALIGN_CENTER;

                    document.Add(line);


                    // ==========================================
                    // APPROVED STATUS
                    // ==========================================

                    var approved =
                        new iTextSharp.text.Paragraph(
                            "ETHICS APPLICATION APPROVED",
                            universityFont);

                    approved.Alignment =
                        iTextSharp.text.Element.ALIGN_CENTER;

                    approved.SpacingBefore = 18;
                    approved.SpacingAfter = 20;

                    document.Add(approved);


                    // ==========================================
                    // INTRODUCTION
                    // ==========================================

                    var introduction =
                        new iTextSharp.text.Paragraph(
                            "This certificate confirms that the " +
                            "postgraduate research project identified " +
                            "below has received ethics clearance " +
                            "recorded in the Postgraduate Research " +
                            "Management System.",
                            normalFont);

                    introduction.Alignment =
                        iTextSharp.text.Element.ALIGN_CENTER;

                    introduction.SpacingAfter = 20;

                    document.Add(introduction);


                    // ==========================================
                    // STUDENT INFORMATION
                    // ==========================================

                    var studentHeading =
                        new iTextSharp.text.Paragraph(
                            "STUDENT INFORMATION",
                            sectionFont);

                    studentHeading.SpacingAfter = 8;

                    document.Add(studentHeading);


                    var studentTable =
                        new iTextSharp.text.pdf.PdfPTable(2);

                    studentTable.WidthPercentage = 100;

                    studentTable.SetWidths(
                        new float[] { 35f, 65f });


                    AddCertificateRow(
                        studentTable,
                        "Student Name",
                        ethics.Student.User.FirstName +
                        " " +
                        ethics.Student.User.LastName,
                        boldFont,
                        normalFont);


                    AddCertificateRow(
                        studentTable,
                        "Student Number",
                        ethics.Student.User.StudentNumber,
                        boldFont,
                        normalFont);


                    document.Add(studentTable);


                    // ==========================================
                    // RESEARCH INFORMATION
                    // ==========================================

                    var researchHeading =
                        new iTextSharp.text.Paragraph(
                            "RESEARCH INFORMATION",
                            sectionFont);

                    researchHeading.SpacingBefore = 20;
                    researchHeading.SpacingAfter = 8;

                    document.Add(researchHeading);


                    var researchTable =
                        new iTextSharp.text.pdf.PdfPTable(2);

                    researchTable.WidthPercentage = 100;

                    researchTable.SetWidths(
                        new float[] { 35f, 65f });


                    // RESEARCH TOPIC FROM APPLICATION

                    AddCertificateRow(
                        researchTable,
                        "Research Topic",
                        application.ResearchTitle,
                        boldFont,
                        normalFont);


                    // RESEARCH FIELD FROM PROPOSAL

                    AddCertificateRow(
                        researchTable,
                        "Research Field",
                        proposal.ResearchField,
                        boldFont,
                        normalFont);


                    document.Add(researchTable);


                    // ==========================================
                    // ETHICS INFORMATION
                    // ==========================================

                    var ethicsHeading =
                        new iTextSharp.text.Paragraph(
                            "ETHICS INFORMATION",
                            sectionFont);

                    ethicsHeading.SpacingBefore = 20;
                    ethicsHeading.SpacingAfter = 8;

                    document.Add(ethicsHeading);


                    var ethicsTable =
                        new iTextSharp.text.pdf.PdfPTable(2);

                    ethicsTable.WidthPercentage = 100;

                    ethicsTable.SetWidths(
                        new float[] { 35f, 65f });


                    AddCertificateRow(
                        ethicsTable,
                        "Ethics Number",
                        ethics.ReferenceNumber,
                        boldFont,
                        normalFont);


                    AddCertificateRow(
                        ethicsTable,
                        "Approval Date",
                        ethics.ApprovalDate.HasValue
                            ? ethics.ApprovalDate.Value.ToString(
                                "dd MMMM yyyy")
                            : "N/A",
                        boldFont,
                        normalFont);


                    AddCertificateRow(
                        ethicsTable,
                        "Status",
                        "Approved",
                        boldFont,
                        normalFont);


                    document.Add(ethicsTable);


                    // ==========================================
                    // ETHICS NUMBER
                    // ==========================================

                    var reference =
                        new iTextSharp.text.Paragraph(
                            "ETHICS REFERENCE NUMBER\n" +
                            ethics.ReferenceNumber,
                            navyFont);

                    reference.Alignment =
                        iTextSharp.text.Element.ALIGN_CENTER;

                    reference.SpacingBefore = 25;
                    reference.SpacingAfter = 20;

                    document.Add(reference);


                    // ==========================================
                    // CERTIFICATE STATEMENT
                    // ==========================================

                    var statement =
                        new iTextSharp.text.Paragraph(
                            "This certificate is issued as a " +
                            "system-generated record of the ethics " +
                            "approval associated with the research " +
                            "project identified above.",
                            normalFont);

                    statement.Alignment =
                        iTextSharp.text.Element.ALIGN_CENTER;

                    statement.SpacingAfter = 12;

                    document.Add(statement);


                    var retention =
                        new iTextSharp.text.Paragraph(
                            "The ethics reference number should be " +
                            "retained for future verification and " +
                            "postgraduate research record-keeping.",
                            normalFont);

                    retention.Alignment =
                        iTextSharp.text.Element.ALIGN_CENTER;

                    document.Add(retention);


                    // ==========================================
                    // FOOTER
                    // ==========================================

                    var footer =
                        new iTextSharp.text.Paragraph(
                            "\n\nPostgraduate Research Management System\n" +
                            "System-generated certificate",
                            normalFont);

                    footer.Alignment =
                        iTextSharp.text.Element.ALIGN_CENTER;

                    footer.SpacingBefore = 30;

                    document.Add(footer);


                    // ==========================================
                    // CLOSE PDF
                    // ==========================================

                    document.Close();


                    return File(
                        memoryStream.ToArray(),
                        "application/pdf",
                        "Ethics-Clearance-" +
                        ethics.ReferenceNumber +
                        ".pdf");
                }
            }
        }


        public ActionResult EvaluationDetails(int id)
        {
            int? userId = Session["UserID"] as int?;

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var student = db.Students
                .FirstOrDefault(s => s.UserID == userId.Value);

            if (student == null)
            {
                return HttpNotFound();
            }

            var evaluation = db.Evaluations
                .Include(e => e.Proposal)
                .Include(e => e.Evaluator)
                .Include(e => e.Evaluator.User)
                .FirstOrDefault(e =>
                    e.EvaluationID == id &&
                    e.Proposal.StudentID == student.StudentID);

            if (evaluation == null)
            {
                return HttpNotFound();
            }

            /*
             * The evaluator's evaluation must first be
             * released by the Supervisor.
             */
            if (!evaluation.EvaluationSentToStudent)
            {
                TempData["Info"] =
                    "This evaluation has not yet been released to you.";

                return RedirectToAction("Proposal");
            }

            var criteria = db.EvaluationCriterias
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            var ratings = db.EvaluationCriterionRatings
                .Where(r => r.EvaluationID == evaluation.EvaluationID)
                .ToList();

            decimal totalScore = ratings.Sum(r => r.Rating);

            decimal totalPossibleScore =
                criteria.Sum(c => c.MaxScore);

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



        private void AddCertificateRow(
            iTextSharp.text.pdf.PdfPTable table,
            string label,
            string value,
            iTextSharp.text.Font labelFont,
            iTextSharp.text.Font valueFont)
        {
            var labelCell =
                new iTextSharp.text.pdf.PdfPCell(
                    new iTextSharp.text.Phrase(
                        label,
                        labelFont));

            labelCell.BackgroundColor =
                new iTextSharp.text.BaseColor(
                    245,
                    247,
                    250);

            labelCell.Padding = 8;

            var valueCell =
                new iTextSharp.text.pdf.PdfPCell(
                    new iTextSharp.text.Phrase(
                        value ?? "",
                        valueFont));

            valueCell.Padding = 8;

            table.AddCell(labelCell);
            table.AddCell(valueCell);
        }
    }

}
