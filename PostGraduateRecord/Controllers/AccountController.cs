using PostGraduateRecord.Models;
using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Web;
using System.Web.Mvc;


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
        public ActionResult Login(string role, string username, string password, bool rememberMe = false)
        {
            
            try
            {
                // =========================================
                // SUPERVISOR & ADMIN DATABASE LOGIN
                // =========================================

                if (role == "Supervisor" || role == "Admin")
                {
                    User user = db.Users.FirstOrDefault(u =>
                        u.Email == username &&
                        u.Role == role &&
                        u.IsActive == true);

                    if (user == null)
                    {
                        ViewBag.Message = "Invalid username or password.";
                        return View();
                    }

                    Session["UserID"] = user.UserID;

                    Session["FullName"] =
                        user.FirstName + " " + user.LastName;

                    Session["Role"] = user.Role;

                    // Determine which roles this user can perform
                    Session["IsSupervisor"] = db.Supervisors
                        .Any(s => s.UserID == user.UserID);

                    Session["IsEvaluator"] = db.Evaluators
                        .Any(e => e.UserID == user.UserID);

                    // Start in the role they selected at login
                    Session["ActiveRole"] = user.Role;


                    // =========================================
                    // REMEMBER ME
                    // =========================================

                    if (rememberMe)
                    {
                        HttpCookie cookie =
                            new HttpCookie("RememberUser");

                        cookie["Username"] = username;

                        cookie.Expires =
                            DateTime.Now.AddDays(30);

                        Response.Cookies.Add(cookie);
                    }


                    // =========================================
                    // REDIRECT
                    // =========================================

                    if (role == "Supervisor")
                    {
                        return RedirectToAction(
                            "Dashboard",
                            "Supervisors");
                    }

                    if (role == "Admin")
                    {
                        return RedirectToAction(
                            "Dashboard",
                            "Admin");
                    }
                }

                // =========================================
                // STUDENT ACTIVE DIRECTORY LOGIN
                // =========================================
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
                        // Read Faculty from Active Directory
                        string facultyName = "";

                        if (de.Properties["extensionAttribute2"] != null &&
                            de.Properties["extensionAttribute2"].Value != null)
                        {
                            facultyName = de.Properties["extensionAttribute2"].Value.ToString().Trim();
                        }

                        // Read Department from Active Directory
                        string departmentName = "";

                        if (de.Properties["department"] != null &&
                            de.Properties["department"].Value != null)
                        {
                            string adDepartment = de.Properties["department"].Value.ToString();

                            if (adDepartment.Contains(":"))
                            {
                                departmentName = adDepartment.Split(':')[1].Trim();
                            }
                            else
                            {
                                departmentName = adDepartment.Trim();
                            }
                        }
                        // ==========================================
                        // VALIDATE AD FACULTY AND DEPARTMENT
                        // ==========================================

                        if (string.IsNullOrWhiteSpace(facultyName))
                        {
                            ViewBag.Message =
                                "Faculty information was not provided by Active Directory.";

                            return View();
                        }

                        if (string.IsNullOrWhiteSpace(departmentName))
                        {
                            ViewBag.Message =
                                "Department information was not provided by Active Directory.";

                            return View();
                        }
                        // ==========================================
                        // FIND OR CREATE FACULTY
                        // ==========================================

                        Faculty faculty = db.Faculties
                            .FirstOrDefault(f => f.FacultyName == facultyName);

                        if (faculty == null)
                        {
                            // Faculty does not exist.
                            // Create it automatically from Active Directory.

                            faculty = new Faculty
                            {
                                FacultyName = facultyName,
                                IsActive = true
                            };

                            db.Faculties.Add(faculty);
                            db.SaveChanges();
                        }


                        // ==========================================
                        // FIND OR CREATE DEPARTMENT
                        // ==========================================

                        Department department = db.Departments
                            .FirstOrDefault(d =>
                                d.ADDepartmentName == departmentName);

                        if (department == null)
                        {
                            // Department does not exist.
                            // Create it automatically from Active Directory.

                            department = new Department
                            {
                                FacultyID = faculty.FacultyID,

                                DepartmentName = departmentName,

                                ADDepartmentName = departmentName,

                                IsActive = true
                            };

                            db.Departments.Add(department);
                            db.SaveChanges();
                        }
                        else
                        {
                            // Make sure the department is connected
                            // to the correct faculty from Active Directory.

                            if (department.FacultyID != faculty.FacultyID)
                            {
                                department.FacultyID = faculty.FacultyID;

                                db.SaveChanges();
                            }
                        }


                        // ==========================================
                        // VERIFY DEPARTMENT
                        // ==========================================

                        if (department == null)
                        {
                            ViewBag.Message =
                                "Unable to create department '" +
                                departmentName + "'.";

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

                    if (rememberMe)
                    {
                        HttpCookie cookie = new HttpCookie("RememberUser");
                        cookie["Username"] = username;
                        cookie.Expires = DateTime.Now.AddDays(30);

                        Response.Cookies.Add(cookie);
                    }

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
