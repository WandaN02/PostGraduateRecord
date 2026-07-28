using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PostGraduateRecord.Controllers
{
    public class StudentController : Controller
    {
        public ActionResult Dashboard()
        {
            return View();
        }

        public ActionResult Proposal()
        {
            return View();
        }

        public ActionResult Ethics()
        {
            return View();
        }

        public ActionResult NewApplication()
        {
            return View();
        }

        public ActionResult Submissions()
        {
            return View();
        }

        public ActionResult NewSubmission()
        {
            return View();
        }
        //public ActionResult Applications()
        //{
        //    return View();
        //}
        public ActionResult ApplicationForm()
        {
            return View();
        }

        public ActionResult EthicsApplication()
        {
            return View();
        }
        public ActionResult NewProposal()
        {
            return View();
        }
        public ActionResult ProposalFeedback()
        {
            return View();
        }

    }
}