using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace UmbracoMemberHandling.Models
{
	public class ForgottenPasswordViewModel
	{
		//Forgotten Password View Model
		[Display(Name = "Email Address:")]
		[Required(ErrorMessage = "Please enter your email address")]
		[EmailAddress(ErrorMessage = "Please enter a valid email address")]
		public string EmailAddress { get; set; }

	}
}
