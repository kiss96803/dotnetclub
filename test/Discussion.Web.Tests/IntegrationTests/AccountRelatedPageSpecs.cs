﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discussion.Core.Data;
using Discussion.Core.Models;
using Discussion.Core.Utilities;
using Discussion.Tests.Common;
using Discussion.Tests.Common.AssertionExtensions;
using Xunit;

namespace Discussion.Web.Tests.IntegrationTests
{
    [Collection("WebSpecs")]
    public class AccountRelatedPageSpecs
    {
        private readonly TestDiscussionWebApp _app;
        private readonly AntiForgeryRequestTokens _antiForgeryTokens;

        public AccountRelatedPageSpecs(TestDiscussionWebApp app) {
            _app = app.Reset();
            _antiForgeryTokens = _app.GetAntiForgeryTokens();
        }


        [Fact]
        public async Task should_serve_signin_page_correctly()
        {
            // arrange
            var request = _app.Server.CreateRequest("/signin");

            // act
            var response = await request.GetAsync();

            // assert
            response.StatusCode.ShouldEqual(HttpStatusCode.OK);
            response.ReadAllContent().ShouldContain("用户登录");            
        }
        
          
        [Fact]
        public async Task should_be_able_to_signin_new_user()
        {
            // arrange
            var username = StringUtility.Random();
            var password = "11111a";
            _app.CreateUser(username, password);

            // Act
            var request = _app.Server.CreateRequest("/signin")
                .WithFormContent(new Dictionary<string, string>()
                {
                    {"UserName", username}, 
                    {"Password", password},
                    {"__RequestVerificationToken", _antiForgeryTokens.VerificationToken}
                })
                .WithCookie(_antiForgeryTokens.Cookie);
            var response = await request.PostAsync();

            // assert
            response.StatusCode.ShouldEqual(HttpStatusCode.Redirect);
            var cookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
            cookieHeaders.ShouldContain(cookie => cookie.Contains(".AspNetCore.Identity.Application"));
        }
        
        [Fact]
        public async Task signed_in_users_should_be_able_to_view_pages_that_requires_authenticated_users()
        {
            // arrange
            var username = StringUtility.Random();
            var password = "11111a";
            _app.CreateUser(username, password);
            var signinResponse = await _app.RequestAntiForgeryForm("/signin",
                                                new Dictionary<string, string>
                                                {
                                                    {"UserName", username},
                                                    {"Password", password}
                                                })
                                                .PostAsync();
            // Act
            var request = _app.Server.CreateRequest("/topics/create")
                                        .WithCookiesFrom(signinResponse);
            
            var response = await request.GetAsync();

            // assert
            response.StatusCode.ShouldEqual(HttpStatusCode.OK);
            response.ReadAllContent().ShouldContain("注销");
        }

        [Fact]
        public async Task should_serve_register_page_correctly()
        {
            // arrange
            var request = _app.Server.CreateRequest("/register");

            // act
            var response = await request.GetAsync();

            // assert
            response.StatusCode.ShouldEqual(HttpStatusCode.OK);
            response.ReadAllContent().ShouldContain("用户注册");            
        }
  
        [Fact]
        public async Task should_be_able_to_register_new_user()
        {
            // arrange
            var username = StringUtility.Random();
            var password = "11111a";
            
            // Act
            var request = _app.Server.CreateRequest("/register")
                .WithFormContent(new Dictionary<string, string>()
                {
                    {"UserName", username},
                    {"Password", password},
                    {"__RequestVerificationToken", _antiForgeryTokens.VerificationToken}
                })
                .WithCookie(_antiForgeryTokens.Cookie);
            var response = await request.PostAsync();

            // assert
            response.StatusCode.ShouldEqual(HttpStatusCode.Redirect);
            var isRegistered = _app.GetService<IRepository<User>>().All().Any(u => u.UserName == username);
            isRegistered.ShouldEqual(true);
        }
     
        [Fact]
        public async Task should_signin_newly_registered_user()
        {
            // arrange
            var username = StringUtility.Random();
            var registerResponse = await _app.Server.CreateRequest("/register")
                                .WithFormContent(new Dictionary<string, string>()
                                    {
                                        {"UserName", username}, 
                                        {"Password", "11111a"},
                                        {"__RequestVerificationToken", _antiForgeryTokens.VerificationToken}
                                    })
                                .WithCookie(_antiForgeryTokens.Cookie)
                                .PostAsync();

            // Act
            var response = await _app.Server.CreateRequest("/topics/create")
                                                .WithCookiesFrom(registerResponse)
                                                .GetAsync();
            
            // assert
            response.StatusCode.ShouldEqual(HttpStatusCode.OK);
            response.ReadAllContent().ShouldContain("注销");
        }

        // todo: should login before settings
        // todo: should be able to bind email without login
    }
}