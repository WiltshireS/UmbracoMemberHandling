using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace UmbracoMemberHandling.Models
{
	public class ResetPasswordViewModel
	{
		//[Display(Name = "Email Address:")]
		//[Required(ErrorMessage = "Please enter your email address")]
		//[EmailAddress(ErrorMessage = "Please enter a valid email address")]
		//public string EmailAddress { get; set; }

		[Display(Name = "Username:")]
		[Required(ErrorMessage = "Please enter your username")]
		public string UserName { get; set; }

		[DataType(DataType.Password)]
		[Required(ErrorMessage = "Please enter your password")]
		public string Password { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Confirm Password:")]
		[Required(ErrorMessage = "Please enter your password")]
		[Compare("Password", ErrorMessage = "Your passwords do not match")]
		public string ConfirmPassword { get; set; }
	}
} 
