using Final.Models;
using System.Data;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Authentication;
using MySql.Data.Entity;
using MySql.Web.SessionState;
using MySql.Web.Security;
using System.Net.Mail;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.SessionState;

namespace Final.Controllers
{
    public class AccountController : Controller
    {
        private const string mysqlconnection = "server=myvmlab.senecacollege.ca;port=6079;user id=student;password=Group09;database=g9;charset=utf8;persistsecurityinfo=True; SslMode=none; Integrated Security=True;allowuservariables=True;";

        MySqlConnection conn = new MySqlConnection();

        public AccountController()
        {

        }

        [Authorize]
        public ActionResult Account()

        {
            return View();
        }
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login()
        {

            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }


            using (MySqlConnection connection = new MySqlConnection(mysqlconnection))
            {
                try
                {

                    MySqlCommand loginCommand = new MySqlCommand();
                    MySqlCommand checkUserisActivatedCommand = new MySqlCommand();


                    // open the connection
                    connection.Open();

                    loginCommand = new MySqlCommand("SELECT * FROM USER WHERE Email = @Email AND Password = @Password", connection);
                    loginCommand.Parameters.AddWithValue("@Email", model.UserEmail.ToString());

                    // Encrypt the password
#pragma warning disable CS0618 // Type or member is obsolete
                    string encryptedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(model.UserPassword.ToString(), "MD5");
#pragma warning restore CS0618 // Type or member is obsolete
                    loginCommand.Parameters.AddWithValue("@Password", encryptedPassword);

                    //Now we are going to read the data imput
                    MySqlDataReader myLoginReader = loginCommand.ExecuteReader();

                    //if the data matches the rows (username, password), then you enter to the page
                    if (myLoginReader.Read())
                    {
                        myLoginReader.Close();


                        // check if user is acitvated
                        checkUserisActivatedCommand = new MySqlCommand("SELECT COUNT(Email) FROM USER WHERE Email = @Email AND isApproved =1", connection);
                        checkUserisActivatedCommand.Parameters.AddWithValue("@Email", model.UserEmail.ToString());


                        long result = (long)checkUserisActivatedCommand.ExecuteScalar();


                        // if user is activated
                        if (result == 1)
                        {

                            // login user

                            FormsAuthentication.SetAuthCookie(model.UserEmail, true);

                            // Redirect to homepage - user has logged in successfully
                            return this.RedirectToAction("Index", "Home");

                        }
                        else
                        {
                            // Redirect to Confirmation page
                            return this.RedirectToAction("ConfirmEmail", "Account");
                        }
                    }
                    else
                    {
                        // no user was found

                        // Display Error Message
                        ViewBag.ErrorMessage = "Error: Invalid Email/Password";
                    }


                }
                catch (Exception e)
                {

                }
                finally
                {

                    // close the connection
                    connection.Close();
                }


            }


            return Login();

        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(Account model)
        {
            /*http://localhost:52289/Properties/ */
            if (!ModelState.IsValid)
            {
                //string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                //var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                //await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                return View();
            }
            else
            {


                using (MySqlConnection connection = new MySqlConnection(mysqlconnection))
                {
                    try
                    {

                        MySqlCommand registerCommand = new MySqlCommand();
                        MySqlCommand checkIfRegisteredCommand = new MySqlCommand();


                        // connect to database
                        connection.Open();

                        // check if there is any user registered with the email address provided

                        checkIfRegisteredCommand = new MySqlCommand("SELECT COUNT(email) FROM USER WHERE email=@Email", connection);
                        checkIfRegisteredCommand.Parameters.AddWithValue("@Email", model.UserEmail.ToString());

                        long registeredUsersCount = (long)checkIfRegisteredCommand.ExecuteScalar();


                        // if there is no user registered with the specified email address then register the user
                        if (registeredUsersCount < 1)
                        {



                            registerCommand = new MySqlCommand("INSERT INTO USER(Name, Email, Password, User_Type_Id, isApproved, EmailConfirmationToken)" +
                                " VALUES(@Name, @Email, @PWD, @UserID, @isApproved, @EmailConfirmationToken)", connection);

                            registerCommand.Parameters.AddWithValue("@Name", model.UserName.ToString());
                            registerCommand.Parameters.AddWithValue("@Email", model.UserEmail.ToString());

                            // Encrypt the password
#pragma warning disable CS0618 // Type or member is obsolete
                            string encryptedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(model.UserPassword.ToString(), "MD5");
#pragma warning restore CS0618 // Type or member is obsolete


                            registerCommand.Parameters.AddWithValue("@PWD", encryptedPassword);
                            registerCommand.Parameters.AddWithValue("@UserID", model.UserTypeId);
                            registerCommand.Parameters.AddWithValue("@isApproved", 0);


                            String EmailConfirmationToken = Guid.NewGuid().ToString();
                            registerCommand.Parameters.AddWithValue("@EmailConfirmationToken", EmailConfirmationToken);


                            // insert data
                            int result = registerCommand.ExecuteNonQuery();




                            // send confirmation email

                            MailMessage mail = new MailMessage();
                            String emailId = model.UserEmail.Trim().ToString();
                            mail.From = new MailAddress("astrobladez.crusher@gmail.com");
                            mail.To.Add(emailId);
                            mail.Subject = "Confirmation email for account activation.";

                            String ActivationUrl = Server.HtmlEncode("http://localhost:59505/Account/ConfirmEmail?&email="
                                + emailId + "&token=" + EmailConfirmationToken);
                            mail.Body = "Thanks for being a part of our world!\n " +
                            "Please <a href='" + ActivationUrl + "'>click here to activate</a> your account and enjoy our application. \nThanks!";


                            mail.IsBodyHtml = true;


                            SmtpClient smtp = new SmtpClient
                            {
                                Host = "smtp.gmail.com",
                                Port = 587,
                                UseDefaultCredentials = false,
                                Credentials = new System.Net.NetworkCredential
                            ("astrobladez.crusher@gmail.com", "014EGY2540PT205"),// Enter senders User name and password
                                EnableSsl = true
                            };
                            smtp.Send(mail);

                            // redirect user to Confirmation page
                            return RedirectToAction("ConfirmEmail", "Account");

                        }
                        else
                        {

                            ViewBag.ErrorMessage = "Error: This email address is already registered.";
                        }

                    }
                    catch (Exception ex)
                    {

                    }
                    finally
                    {
                        // close the connection
                        connection.Close();

                    }

                }

                return View();

            }

        }
        // If we got this far, something failed, redisplay form


        [Authorize]
        public ActionResult Logout()

        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }


        [HttpGet]
        [AllowAnonymous]
        public ActionResult ConfirmEmail(string email, string token)
        {


            if (email == null && token == null)
            {
                ViewBag.Message = "<p>An email has been sent to your email addressing containing the activation link. Please confirm your account.</p>";
                return View();

            }

            using (MySqlConnection connection = new MySqlConnection(mysqlconnection))
            {
                try
                {


                    // connect to database
                    connection.Open();

                    MySqlCommand validateConfirmationCommand = new MySqlCommand();


                    // check if email and token exist in database  -

                    validateConfirmationCommand = new MySqlCommand("SELECT COUNT(email) FROM USER WHERE email=@Email AND EmailConfirmationToken=@Token", connection);
                    validateConfirmationCommand.Parameters.AddWithValue("@Email", email);
                    validateConfirmationCommand.Parameters.AddWithValue("@Token", token);

                    long result = (long)validateConfirmationCommand.ExecuteScalar();


                    // if user's email and token match then confirm the user
                    if (result == 1)
                    {


                        // confirm the user

                        MySqlCommand confirmUserCommand = new MySqlCommand();

                        confirmUserCommand = new MySqlCommand("UPDATE USER SET isApproved=1 WHERE email=@Email AND EmailConfirmationToken=@Token", connection);
                        confirmUserCommand.Parameters.AddWithValue("@Email", email);
                        confirmUserCommand.Parameters.AddWithValue("@Token", token);

                        confirmUserCommand.ExecuteNonQuery();


                        ViewBag.Message = "<p style='color:green'>Thank you for confirming your email. Please <a href='/Account/Login'>click here to login</a> </p>";

                    }
                    else
                    {

                        ViewBag.Message = "<p style='color:red'>Error: Invalid token/email combination.</p>";
                    }
                }
                catch (Exception ex)
                {

                }

                finally
                {
                    connection.Close();
                }

            }


            return View();


        }


        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgetPassword model)
        {

            if (!ModelState.IsValid)

            {
                ViewBag.Message = "<p style='color:red'>Please fill in Forgot Password Form</p>";
                return View();

            }

            else
            {
                using (MySqlConnection connection = new MySqlConnection(mysqlconnection))
                {
                    try
                    {


                        // connect to database
                        connection.Open();

                        MySqlCommand validateEmailCommand = new MySqlCommand();


                        // check if email exists in database  -

                        validateEmailCommand = new MySqlCommand("SELECT COUNT(EMAIL) FROM USER WHERE EMAIL = @Email", connection);
                        validateEmailCommand.Parameters.AddWithValue("@Email", model.UserEmail.ToString());

                        long result = (long)validateEmailCommand.ExecuteScalar();


                        // if user's email match then send reset password email
                        if (result == 1)
                        {

                            
                            // change user's password to temporary password
                            string tempPassword = GenerateRandomPassword();

                            MySqlCommand setTemporaryPasswordCommand = new MySqlCommand();

                            setTemporaryPasswordCommand = new MySqlCommand("UPDATE USER SET PASSWORD =  @PWD WHERE EMAIL = @Email", connection);


                            // Encrypt the password
#pragma warning disable CS0618 // Type or member is obsolete
                            string encryptedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(tempPassword.ToString(), "MD5");
#pragma warning restore CS0618 // Type or member is obsolete

                            setTemporaryPasswordCommand.Parameters.AddWithValue("@PWD", encryptedPassword.ToString());
                            setTemporaryPasswordCommand.Parameters.AddWithValue("@Email",model.UserEmail.ToString());
                            setTemporaryPasswordCommand.ExecuteNonQuery();
                            

                            MailMessage mail = new MailMessage();
                            String emailId = model.UserEmail.Trim().ToString();
                            mail.From = new MailAddress("astrobladez.crusher@gmail.com");
                            mail.To.Add(emailId);
                            mail.Subject = "Forgot Password Email";
 

                            mail.Body = "Your password has been reset. \b Your temporary password is :  <strong>" + tempPassword + "</strong>\n";


                            mail.IsBodyHtml = true;


                            SmtpClient smtp = new SmtpClient
                            {
                                Host = "smtp.gmail.com",
                                Port = 587,
                                UseDefaultCredentials = false,
                                Credentials = new System.Net.NetworkCredential
                            ("astrobladez.crusher@gmail.com", "014EGY2540PT205"),// Enter senders User name and password
                                EnableSsl = true
                            };
                            smtp.Send(mail);

                            ViewBag.Message = "<p style='color:green'>A temporary password has been sent to your email.</p>";


                        }
                        else
                        {

                            ViewBag.Message = "<p style='color:red'>Error: Invalid Email Address.</p>";
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    finally
                    {
                        connection.Close();
                    }

                }
            }

            return View();
        }



        public string GenerateRandomPassword()
        {
            string allowedChars = string.Empty;

            allowedChars = "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,";

            allowedChars += "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,";

            allowedChars += "1,2,3,4,5,6,7,8,9,0,!,@,#,$,%,&,?";

            char[] sep = { ',' };

            string[] arr = allowedChars.Split(sep);

            string passwordString = string.Empty;

            string temp = string.Empty;

            Random rand = new Random();

            for (int i = 0; i < Convert.ToInt32(10); i++)

            {

                temp = arr[rand.Next(0, arr.Length)];

                passwordString += temp;

            }

            return passwordString;
        }


        [Authorize]
        //User Can Change Password when Logged In
        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePassword model)
        {


            if (!ModelState.IsValid)

            {
                ViewBag.ErrorMessage = "<p>Please fill in Change Password Form</p>";
                return View();

            }

            using (MySqlConnection connection = new MySqlConnection(mysqlconnection))
            {
                try
                {


                    // connect to database
                    connection.Open();

                    MySqlCommand validateConfirmationCommand = new MySqlCommand();


                    // check if email and token exist in database  -

                    validateConfirmationCommand = new MySqlCommand("SELECT COUNT(EMAIL) FROM USER WHERE EMAIL = @Email", connection);
                    validateConfirmationCommand.Parameters.AddWithValue("@Email", User.Identity.Name.ToString());

                    long result = (long)validateConfirmationCommand.ExecuteScalar();


                    // if user's email and token match then confirm the user
                    if (result == 1)
                    {


                        // confirm the updated password

                        MySqlCommand changePasswordCommand = new MySqlCommand();

                        changePasswordCommand = new MySqlCommand("UPDATE USER SET PASSWORD =  @PWD WHERE email=@Email", connection);


                        // Encrypt the password
#pragma warning disable CS0618 // Type or member is obsolete
                        string encryptedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(model.ConfirmPassword.ToString(), "MD5");
#pragma warning restore CS0618 // Type or member is obsolete

                        changePasswordCommand.Parameters.AddWithValue("@PWD", encryptedPassword.ToString());
                        changePasswordCommand.Parameters.AddWithValue("@Email", User.Identity.Name.ToString());
                        changePasswordCommand.ExecuteNonQuery();


                        ViewBag.Message = "<p style='color:green'>Password Change Successful.\n You'll be logged out to login with the new password</p>";


                        return Logout();


                    }
                  
                }
                catch (Exception ex)
                {

                }

                finally
                {
                    connection.Close();
                }

            }


            return View();
        }

    }

}
