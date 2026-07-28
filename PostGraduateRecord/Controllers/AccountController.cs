using PostGraduateRecord.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;


namespace PostGraduateRecord.Controllers
{
    public class AccountController : Controller
    {
        private PostGradEntities db = new PostGradEntities();


        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }



        // POST: Account/Login
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            try
            {
                using (PrincipalContext context =
                    new PrincipalContext(ContextType.Domain, "mandela.ac.za"))
                {

                    // Check NMU login
                    bool isValid =
                        context.ValidateCredentials(username, password);


                    if (!isValid)
                    {
                        ViewBag.Message = "Invalid NMU username or password.";
                        return View();
                    }



                    // Find user from Active Directory
                    UserPrincipal adUser =
                        UserPrincipal.FindByIdentity(context, username);
                    DirectoryEntry de =
    (DirectoryEntry)adUser.GetUnderlyingObject();


                    foreach (string property in de.Properties.PropertyNames)
                    {
                        ViewBag.Message +=
                            property + " : " +
                            de.Properties[property].Value +
                            "<br/>";
                    }



                    if (adUser == null)
                    {
                        ViewBag.Message = "NMU user not found.";
                        return View();
                    }



                    // Get AD information

                    string studentNumber = username;


                    if (studentNumber.Contains("@"))
                    {
                        studentNumber =
                            studentNumber.Split('@')[0];
                    }


                    studentNumber =
                        studentNumber.ToUpper();



                    string fullName =
                        adUser.DisplayName ?? "";

                    string email =
                        adUser.UserPrincipalName ?? "";



                    // Separate names

                    string firstName = "";
                    string lastName = "";


                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        if (fullName.Contains(","))
                        {
                            string[] names =
                                fullName.Split(',');

                            lastName =
                                names[0].Trim();


                            if (names.Length > 1)
                            {
                                firstName =
                                    names[1].Trim();
                            }
                        }
                        else
                        {
                            string[] names =
                                fullName.Split(' ');


                            firstName =
                                names[0];


                            if (names.Length > 1)
                            {
                                lastName =
                                    names[names.Length - 1];
                            }
                        }
                    }



                    // Check database

                    User existingUser =
                        db.Users.FirstOrDefault(
                            u => u.StudentNumber == studentNumber);


                    // Create new user if it does not exist

                    if (existingUser == null)
                    {
                        User newUser = new User
                        {
                            StudentNumber = studentNumber,
                            EmployeeNumber = null,
                            FirstName = firstName,
                            LastName = lastName,
                            Email = email,
                            PhoneNumber = null,
                            Role = "Student",
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };


                        db.Users.Add(newUser);

                        db.SaveChanges();


                        existingUser = newUser;
                    }


                    // Create Student profile if it does not exist

                    Student studentProfile = db.Students
                        .FirstOrDefault(s => s.UserID == existingUser.UserID);

                    if (studentProfile == null)
                    {
                        // Read the department from Active Directory
                        string departmentName = "";

                        if (de.Properties["department"] != null &&
                            de.Properties["department"].Value != null)
                        {
                            string adDepartment = de.Properties["department"].Value.ToString();

                            // Example:
                            // "Engineering Built Environ & It: Software Engineering"

                            if (adDepartment.Contains(":"))
                            {
                                departmentName = adDepartment.Split(':')[1].Trim();
                            }
                            else
                            {
                                departmentName = adDepartment.Trim();
                            }
                        }

                        // Find the department in the database
                        Department department = db.Departments
                            .FirstOrDefault(d => d.DepartmentName == departmentName);

                        // If the department does not exist, use Software Engineering
                        if (department == null)
                        {
                            department = db.Departments
                                .FirstOrDefault(d => d.DepartmentName == "Software Engineering");
                        }

                        // If it still doesn't exist, stop
                        if (department == null)
                        {
                            ViewBag.Message = "Department '" + departmentName + "' was not found in the database.";
                            return View();
                        }

                        // Get qualification from Active Directory

                        string qualification = "";

                        if (de.Properties["description"] != null &&
                            de.Properties["description"].Value != null)
                        {
                            qualification = de.Properties["description"].Value.ToString();
                        }


                        // Get study type from AD

                        string studyType = "";

                        if (de.Properties["extensionAttribute8"] != null &&
                            de.Properties["extensionAttribute8"].Value != null)
                        {
                            studyType = de.Properties["extensionAttribute8"].Value.ToString();
                        }


                        // Get registration year from AD

                        int registrationYear = DateTime.Now.Year;

                        if (de.Properties["extensionAttribute7"] != null &&
                            de.Properties["extensionAttribute7"].Value != null)
                        {
                            int.TryParse(
                                de.Properties["extensionAttribute7"].Value.ToString(),
                                out registrationYear
                            );
                        }


                        // Create student profile

                        Student newStudent = new Student
                        {
                            UserID = existingUser.UserID,

                            DepartmentID = department.DepartmentID,

                            Qualification = qualification,

                            StudyType = studyType,

                            RegistrationYear = registrationYear
                        };

                        db.Students.Add(newStudent);
                        db.SaveChanges();

                        ViewBag.Message = "Student profile created successfully";
                    }

                    // Create session

                    Session["UserID"] =
                        existingUser.UserID;


                    Session["StudentNumber"] =
                        existingUser.StudentNumber;


                    Session["StudentName"] =
                        existingUser.FirstName + " "
                        + existingUser.LastName;


                    Session["Role"] =
                        existingUser.Role;



                    return RedirectToAction(
                        "Dashboard",
                        "Student");
                    // return Content(ViewBag.Message, "text/html");
                }
            }



            catch (Exception ex)
            {
                string message = ex.Message;

                if (ex.InnerException != null)
                {
                    message += "<br/><br/>INNER EXCEPTION:<br/>"
                             + ex.InnerException.Message;
                }

                if (ex.InnerException?.InnerException != null)
                {
                    message += "<br/><br/>SQL ERROR:<br/>"
                             + ex.InnerException.InnerException.Message;
                }

                ViewBag.Message = message;

                return View();
            }
        }



        // Logout

        public ActionResult Logout()
        {
            Session.Clear();

            Session.Abandon();


            return RedirectToAction("Login");
        }
    }
}
