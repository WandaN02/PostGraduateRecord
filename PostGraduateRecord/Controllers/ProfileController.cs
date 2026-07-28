using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using PostGraduateRecord.Models;

namespace PostGraduateRecord.Controllers
{
    public class ProfileController : Controller
    {
        private PostGradEntities db = new PostGradEntities();


        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }


            int userID = (int)Session["UserID"];


            var student = db.Students
                .Include("User")
                .Include("Department.Faculty")
                .FirstOrDefault(s => s.UserID == userID);


            if (student == null)
            {
                return HttpNotFound("Student profile not found.");
            }


            return View(student);
        }

    }
}