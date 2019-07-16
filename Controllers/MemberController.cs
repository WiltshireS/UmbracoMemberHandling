
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Web.Mvc;
using Umbraco.Core.Models;
using Umbraco.Web.Security;
using Umbraco.Core.Services;
using System;
using UmbracoMemberHandling.Helpers;
using UmbracoMemberHandling.Models;

namespace UmbracoMemberHandling.Controllers
{
	public class MemberController : SurfaceController
	{
		private const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/Member/";

		TokenHelper tokenHelper => new TokenHelper();

		public ActionResult RenderLogin()
		{
			return PartialView(PARTIAL_VIEW_FOLDER + "_Login.cshtml", new LoginModel());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult SubmitLogin(LoginModel model, string returnUrl)
		{
			if (ModelState.IsValid)
			{
				if (Membership.ValidateUser(model.Username, model.Password))
				{
					FormsAuthentication.SetAuthCookie(model.Username, false);
					UrlHelper myHelper = new UrlHelper(HttpContext.Request.RequestContext);
					if (myHelper.IsLocalUrl(returnUrl))
					{
						return Redirect(returnUrl);
					}
					else
					{
						return Redirect("/home/");
					}
				}
				else
				{
					ModelState.AddModelError("", "The username or password provided is incorrect");
				}
			}
				return CurrentUmbracoPage();

		}
		
		public ActionResult RenderLogout()
		{
			return PartialView(PARTIAL_VIEW_FOLDER + "_Logout.cshtml", null);
		}

		public ActionResult SubmitLogout()
		{
			TempData.Clear();
			Session.Clear();
			FormsAuthentication.SignOut();
			return RedirectToCurrentUmbracoPage();
		}


		/// <summary>
		/// Renders the Forgotten Password view
		/// </summary>
		/// <returns></returns>
		public ActionResult RenderForgottenPassword()
		{
			return PartialView(PARTIAL_VIEW_FOLDER + "_ForgottenPassword.cshtml", new ForgottenPasswordViewModel());
		}

		/// <summary>
		/// sends the forgotten password email 
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult HandleForgottenPassword(ForgottenPasswordViewModel model)
		{
			var membershipService = Services.MemberService;

			if (!ModelState.IsValid)
			{
				return CurrentUmbracoPage();
			}

			//Find the member with the email address
			var findMember = membershipService.GetByEmail(model.EmailAddress);
			
			if (findMember != null)
			{

				// This test is an absolute car wreck. Umbraco stores the field as a 1 or a null value, hence the tryparse. I'm tired I cant think
				bool newMember;

				int newMemberInt;

				// Get the new member status 
				if (findMember.HasProperty("newMember") && Int32.TryParse(findMember.Properties["newMember"].GetValue().ToString(), out newMemberInt))
				{
					if(newMemberInt==1)
					{
						newMember = true;
					}
					else
					{
						newMember = false;
					}

				} else
				{
					newMember = false;
				}
				

				// Time the password reset link expires 
				DateTime expiryTime;

				// Generate a password reset token
				string resetToken = tokenHelper.GetUniqueToken(64);

				//IF this is a new member, set the expiry time to 24 hours 
				if (newMember)
				{
					expiryTime = DateTime.Now.AddHours(24);
				} else
				{
					expiryTime = DateTime.Now.AddMinutes(15);
				}

				//Set the token expiry time. 
				findMember.Properties["passwordResetExpiry"].SetValue(expiryTime.ToString("ddMMyyyyHHmmssFFFF"));

				// Save the hash of the reset token to the database. 
				findMember.Properties["passwordResetTokenHash"].SetValue(tokenHelper.GetHash(resetToken));

				//Save the member with the updated property value
				membershipService.Save(findMember);

				//Send user an email to reset password with GUID in it
				EmailHelper email = new EmailHelper();

				email.SendResetPasswordEmail(findMember.Email, resetToken, newMember, findMember.Name);

				TempData["ResetEmailSent"] = true;
			}
			else
			{
				ModelState.AddModelError("ForgottenPasswordForm.", "No member found");
				return CurrentUmbracoPage();
			}

			return RedirectToCurrentUmbracoPage();
		}

		/// <summary>
		/// Renders the Reset Password View
		/// @Html.Action("RenderResetPassword","AuthSurface");
		/// </summary>
		/// <returns></returns>
		public ActionResult RenderResetPassword()
		{
			return PartialView(PARTIAL_VIEW_FOLDER +  "_ResetPassword.cshtml", new ResetPasswordViewModel());
		}

		/// <summary>
		/// Renders the Reset Password View
		/// @Html.Action("RenderResetPassword","AuthSurface");
		/// </summary>
		/// <returns></returns>
		public ActionResult RenderSetPassword()
		{
			return PartialView(PARTIAL_VIEW_FOLDER + "_setPassword.cshtml", new ResetPasswordViewModel());
		}

		/// <summary>
		/// Reset the password using the reset token, username and password
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult HandleResetPassword(ResetPasswordViewModel model)
		{
			var membershipService = Services.MemberService;

			if (!ModelState.IsValid)
			{
				return CurrentUmbracoPage();
			}

			//Get member from email
			var resetMember = membershipService.GetByUsername(model.UserName);

			//Ensure we have that member
			if (resetMember != null)
			{
				//Get the querystring GUID
				var resetQueryString = Request.QueryString["resetToken"];

				//Ensure we have a value in QS
				if (!string.IsNullOrEmpty(resetQueryString))
				{
					//See if the QS matches the value on the member property
					if (resetMember.Properties["passwordResetTokenHash"].GetValue().ToString() == tokenHelper.GetHash(resetQueryString))
					{
						//Got a match, now check to see if the 15min window hasnt expired
						DateTime expiryTime = DateTime.ParseExact((string)resetMember.Properties["passwordResetExpiry"].GetValue(), "ddMMyyyyHHmmssFFFF", null);

						//Check the current time is less than the expiry time
						DateTime currentTime = DateTime.Now;

						//Check if date has NOT expired (been and gone)
						if (currentTime.CompareTo(expiryTime) < 0)
						{
							//Got a match, we can allow user to update password
							//resetMember.RawPasswordValue.Password = model.Password;
							membershipService.SavePassword(resetMember, model.Password);

							//Reset the values used in the reset process
							resetMember.Properties["passwordResetExpiry"].SetValue(string.Empty);
							resetMember.Properties["passwordResetTokenHash"].SetValue(string.Empty);
							resetMember.Properties["newMember"].SetValue(string.Empty);

							//Save the member
							membershipService.Save(resetMember);

							TempData["PasswordSet"] = true;

							// redirect to the home node
							RedirectToCurrentUmbracoPage();
						}
						else
						{
							//ERROR: Reset GUID has expired
							ModelState.AddModelError("ResetPasswordForm.", "Reset GUID has expired");
							return CurrentUmbracoPage();
						}
					}
					else
					{
						//ERROR: QS does not match what is stored on member property
						//Invalid GUID
						ModelState.AddModelError("ResetPasswordForm.", "Invalid GUID")
						return CurrentUmbracoPage();
					}
				}
				else
				{
					//ERROR: No QS present
					//Invalid GUID
					ModelState.AddModelError("ResetPasswordForm.", "Invalid GUID");
					return CurrentUmbracoPage();
				}
			}

			return RedirectToCurrentUmbracoPage();
		}


	}
}
