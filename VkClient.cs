//-----------------------------------------------------------------------
// <copyright file="VkClient.cs" company="www.unconnected.info">
// Copyright (c) 2013
// </copyright>
// <author>Alexander Kouznetsov</author>
// <date>14/04/2013</date>
// <summary>Extension for DotNetOpenAuth to support vk.com OAuth2 methods</summary>
// <disclamer> 
// Use at your own risk, no warranties provided.
// </disclaimer>
// <license>
// Creative Commons Attribution-Share Alike
// </license>
// <version>0.alfa.1</version>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.AspNet.Clients
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Web;
    using DotNetOpenAuth.Messaging;
    using System.IO;

    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Vk", Justification = "Brand name")]
    public sealed class VkClient : OAuth2Client
    {
        /// <summary>
        /// The authorization endpoint.
        /// </summary>
        private const string AuthorizationEndpoint = "https://oauth.vk.com/authorize";

        /// <summary>
        /// The token endpoint.
        /// </summary>
        private const string TokenEndpoint = "https://oauth.vk.com/access_token";

        private const string VkApiEndpoint = "https://api.vk.com/method/";
        /// <summary>
        /// The _app id.
        /// </summary>
        private readonly string appId;

        /// <summary>
        /// The _app secret.
        /// </summary>
        private readonly string appSecret;

        /// <summary>
        /// Skope for user prameters requested by application, check actual list at http://vk.com/dev/permissions
        /// </summary>
        private readonly string scope;

        public int UserId { get; private set; }

        public VkClient(string appId, string appSecret)
            : this(appId, appSecret, null)
        {
        }

        public VkClient(string appId, string appSecret, string skope)
            : base("vk")
        {
            this.appId = NotNullOrEmpty(appId, "appId");
            this.appSecret = NotNullOrEmpty(appSecret, "appSecret");
            this.scope = skope ?? "";
        }
        /// <summary>
        /// The get service login url.
        /// </summary>
        /// <param name="returnUrl">
        /// The return url.
        /// </param>
        /// <returns>An absolute URI.</returns>
        protected override Uri GetServiceLoginUrl(Uri returnUrl)
        {
            // Note: Facebook doesn't like us to url-encode the redirect_uri value
            var builder = new UriBuilder(AuthorizationEndpoint);

            builder.AppendQueryArgument("client_id",this.appId);
            builder.AppendQueryArgument("redirect_uri", returnUrl.AbsoluteUri);
            builder.AppendQueryArgument("scope", this.scope);
            builder.AppendQueryArgument("response_type", "code");

            return builder.Uri;
        }

        /// <summary>
        /// The get user data.
        /// </summary>
        /// <param name="accessToken">
        /// The access token.
        /// </param>
        /// <returns>A dictionary of profile data.</returns>
        protected override IDictionary<string, string> GetUserData(string accessToken)
        {
            if (this.UserId == 0)
            {
                return null;
            }


            var userData = new Dictionary<string, string>();
            userData["id"] = this.UserId.ToString();

            var request=WebRequest.Create(VkApiEndpoint+"users.get?uids="+this.UserId);
            using (var response = request.GetResponse())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(responseStream))
                    {
                        string responseString = sr.ReadToEnd();
                        var user_data = (System.Web.Helpers.Json.Decode(responseString))["response"][0];
                        userData["username"] = user_data["first_name"] + " " + user_data["last_name"];
                    }
                }
            }


            

           
            return userData;
        }


        /// <summary>
        /// Obtains an access token given an authorization code and callback URL.
        /// </summary>
        /// <param name="returnUrl">
        /// The return url.
        /// </param>
        /// <param name="authorizationCode">
        /// The authorization code.
        /// </param>
        /// <returns>
        /// The access token.
        /// </returns>
        protected override string QueryAccessToken(Uri returnUrl, string authorizationCode)
        {
            // Note: Facebook doesn't like us to url-encode the redirect_uri value
            var builder = new UriBuilder(TokenEndpoint);
            builder.AppendQueryArgument("client_id", this.appId );
            builder.AppendQueryArgument("redirect_uri", returnUrl.AbsoluteUri );
            builder.AppendQueryArgument("client_secret", this.appSecret);
            builder.AppendQueryArgument("code", authorizationCode);

            using (WebClient client = new WebClient())
            {
                string data = client.DownloadString(builder.Uri);
                if (string.IsNullOrEmpty(data))
                {
                    return null;
                }
                var parsedQueryString = System.Web.Helpers.Json.Decode(data);

                if (parsedQueryString["error"] != null)
                {
                    return null;
                }

                this.UserId = parsedQueryString["user_id"];

                return parsedQueryString["access_token"];
            }
        }

        /// <summary>
        /// Replace for Requires.NotNullOrEmpty 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        private static string NotNullOrEmpty(string value, string parameterName)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(parameterName, "Parameter can't be null or empty string");
            }
            return value;
        }
    }
}

namespace Unconnected
{
    //Unfortunately we can't as yet write extention methods for static classes, which OAuthWebSecurity is.
    using Microsoft.Web.WebPages.OAuth;

    public static class VkClient
    {
        public static void Register(string appId, string appSecret)
        {
            OAuthWebSecurity.RegisterClient(new DotNetOpenAuth.AspNet.Clients.VkClient(appId,appSecret), "VK", null);                       
        }
    }
}