#region Header
////////////////////////////////////////////////////////////////////////////////////
//
//	File Description: Initializer
// ---------------------------------------------------------------------------------
//	Date Created	: February 9, 2014
//	Author			: Rob van Oostenrijk
// ---------------------------------------------------------------------------------
// 	Change History
//	Date Modified       : 
//	Changed By          : 
//	Change Description  : 
//
////////////////////////////////////////////////////////////////////////////////////
#endregion
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace TcmDevelopment
{
	public class MockAuthentication : IHttpModule
	{
		private const String AUTHORIZATION_HEADER = "Authorization";
		private const String AUTHENTICATE_HEADER = "WWW-Authenticate";
		
		private static Regex mRealmFilter = new Regex(@"[^a-zA-Z0-9\-\.]");
		private String mRealm = String.Empty;
		private Encoding mCredentialsEncoding;

		/// <summary>
		/// Authentication Scheme
		/// </summary>
		protected String Scheme
		{
			get
			{
				return "Basic";
			}
		}

		/// <summary>
		/// Authentication Realm
		/// </summary>
		protected String Realm
		{
			get
			{
				if (!String.IsNullOrEmpty(mRealm))
					return mRealm;
				else
				{
					// Return a filtered domain name
					return mRealmFilter.Replace(HttpContext.Current.Request.Url.Host, String.Empty).ToLower();
				}
			}
		}

		protected Encoding CredentialsEncoding
		{
			get
			{
				//
				// IMPORTANT! 
				//
				// RFC2617 (HTTP Authentication: Basic and Digest Access
				// Authentication) does not seems to provide any details or
				// guidance on how the user name and password are encoded into
				// bytes before creating the base64 cookie. If the user agent
				// and the server are not in agreement over the byte encoding
				// then the credentials will fail to match albeit the user
				// having provided the right ones. For example, if user name is
				// "Möller" and the user agent uses the UTF-8 encoding while the
				// server assumes something else like Latin-1, then a mismatch
				// will result. Rather than make some unsafe assumption, this
				// property provides a way for a derived module to override the
				// encoding bias of this implementation. 
				//
				// This implementation assumes ISO 8859-1 (Latin-1) so it
				// won't be able to support some exotic cases. This choice does
				// nonetheless seem like the right bet becasue I found the
				// following in section 2.1.1 of RFC2831 (Using Digest
				// Authentication as a SASL Mechanism):
				//
				//    charset
				//       This directive, if present, specifies that the server
				//       supports UTF-8 encoding for the username and password.
				//       If not present, the username and password must be
				//       encoded in ISO 8859-1 (of which US-ASCII is a subset).
				//       The directive is needed for backwards compatibility
				//       with HTTP Digest, which only supports ISO 8859-1.
				//
				if (mCredentialsEncoding == null)
					mCredentialsEncoding = Encoding.GetEncoding("iso-8859-1");

				return mCredentialsEncoding;
			}
		}

		/// <summary>
		/// Retrieves the Basic credentials from the authorization request.
		/// </summary>
		private void GetUserCredentials(String authorization, out String userName, out String password)
		{
			// Crack the basic-cookie, which is a base 64 encoded string of the user's credentials.
			String cookie = authorization.Substring(Scheme.Length).TrimStart(' ');
			String credentialsString;

			try
			{
				credentialsString = CredentialsEncoding.GetString(Convert.FromBase64String(cookie));
			}
			catch (FormatException e)
			{
				throw new HttpException(400, "Bad Request", e);
			}

			// The user name and password are separated by a colon (:) in the credentials string, so split these up.
			String[] credentials = credentialsString.Split(new char[] { ':' }, 2);

			if (credentials.Length != 2)
				throw new HttpException(400, "Bad Request");

			userName = credentials[0];
			password = credentials[1];
		}

		/// <summary>
		/// Disposes of the resources (other than memory) used by the module that implements <see cref="T:System.Web.IHttpModule" />.
		/// </summary>
		public virtual void Dispose()
		{
		}

		/// <summary>
		/// Initializes a module and prepares it to handle requests.
		/// </summary>
		/// <param name="context">An <see cref="T:System.Web.HttpApplication" /> that provides access to the methods, properties, and events common to all application objects within an ASP.NET application</param>
		/// <exception cref="System.ArgumentNullException">Context</exception>
		public virtual void Init(HttpApplication context)
		{
			if (context == null)
				throw new ArgumentNullException("Context");

			// Subscribe to events.
			context.AuthenticateRequest += context_AuthenticateRequest;
			context.EndRequest += context_EndRequest;
		}

		private void context_AuthenticateRequest(object sender, EventArgs e)
		{
			HttpApplication application = (HttpApplication)sender;

			// Do not re-authenticate already authenticated requests
			if (application.Request.IsAuthenticated)
				return;

			// Only authorize requests for the scheme implemented
			String authorization = application.Request.Headers[AUTHORIZATION_HEADER] as String;

			if (String.IsNullOrEmpty(authorization) || !authorization.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
				return;

			AuthenticateRequest(application, authorization);
		}

		private void context_EndRequest(object sender, EventArgs e)
		{
			HttpApplication application = (HttpApplication)sender;

			if (ShouldChallenge(application))
				IssueChallenge(application);
		}

		/// <summary>
		/// Determines whether to issue the challenge or not.
		/// </summary>
		protected bool ShouldChallenge(HttpApplication context)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			return context.Response.StatusCode == (int)HttpStatusCode.Unauthorized;
		}

		/// <summary>
		/// Issues an authentication challenge to the client.
		/// </summary>
		protected virtual void IssueChallenge(HttpApplication context)
		{
			context.Response.AppendHeader(AUTHENTICATE_HEADER, String.Format("{0} Realm=\"{1}\"", Scheme, Realm));
			context.Response.End();
		}

		/// <summary>
		/// Responds with 401 - Access Denied to the client
		/// </summary>
		protected void AccessDenied(HttpApplication context, String username)
		{
			HttpResponse response = context.Response;
			response.StatusCode = (int)HttpStatusCode.Unauthorized;
			response.StatusDescription = "Unauthorized";
			response.ContentType = "text/html";
			response.Write("Unauthorized");
			context.CompleteRequest();
		}

		/// <summary>
		/// Allows the specified user and registers them into the current <see cref="T:System.Web.HttpContext" />
		/// </summary>
		/// <param name="context"><see cref="T:System.Web.HttpContext" /></param>
		/// <param name="user">User</param>
		protected void Allow(HttpApplication context, String user)
		{
			if (context.User == null)
				context.Context.User = new GenericPrincipal(new GenericIdentity(user, "MockAuthentication"), new String[] { "administrators", "admins" });
		}

		/// <summary>
		/// Authenticates the current request
		/// </summary>
		/// <param name="context"><see cref="T:System.Web.HttpContext" /></param>
		/// <param name="authorization">Authorization Header</param>
		protected void AuthenticateRequest(HttpApplication context, String authorization)
		{
			String username;
			String password;

			GetUserCredentials(authorization, out username, out password);

			if (!String.IsNullOrEmpty(username))
			{
				Allow(context, username);
				return;
			}

			AccessDenied(context, username);
		}
	}
}
