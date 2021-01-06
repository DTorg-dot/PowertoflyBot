using HtmlAgilityPack;
using PowerToFlyBot.Selenium;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace PowerToFlyBot
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        public static bool IsProduction { get; set; } = false;

        private static Dictionary<string, string> coockiesDictionary = new Dictionary<string, string>
        {
            { "im_not_a_new_user", "true" },
            //{ "_hjid", "7ebcd989-bb7c-4ad6-bc4f-b06c5560f478" },
            //{ "_hjFirstSeen", "1" },
            //{ "_fbp", "fb.1.1607182224346.1240280417" },
            { "GDPR", "require" },
            { "pushalert_9378_1_subs_status", "canceled" },
            //{ "blog_user", "{\"uid\": 26691745\054 \"mid\": \"4b3ef8797a8940dac93bf1beeeefcd87\"}" },
            //{ "PTF_UID", "26691745" }
        };

        // Configuration 
        public static string UserDataDir { get; set; }

        public static bool IsHeadless { get; set; }

        public static string AdminPanelApi { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Started.");

            if (IsProduction)
            {
                UserDataDir = "/usr/bin/";
                IsHeadless = true;
                AdminPanelApi = "http://127.0.0.1/api/Powertofly";
            }
            else
            {
                UserDataDir = "./ChromeDriver/";
                IsHeadless = false;
                AdminPanelApi = "http://localhost:54379/api/Powertofly";
            }

            AdminPanelApi adminPanelApi = new AdminPanelApi(AdminPanelApi);
            PowerToFlyAPI powerToFlyAPI = new PowerToFlyAPI();
            BotSignalDto botSignal = null;

            while (true)
            {
                try
                {
                    botSignal = adminPanelApi.GetBotSignal();
                    if (botSignal != null)
                    {
                        Console.WriteLine("Found signal");

                        #region LoginUsingSelenium
                        SeleniumWebDriver seleniumWebDriver = null;
                        try
                        {
                            Console.WriteLine("Start signal");


                            seleniumWebDriver = new SeleniumWebDriver(new SeleniumSettings
                            {
                                IsDisableExtensions = true,
                                IsHeadless = IsHeadless,
                                ProfileDirectory = "Default",
                                UserDataDir = UserDataDir,
                                Port = 39969
                            });

                            _ = seleniumWebDriver.ChromeDriver.Manage().Timeouts().ImplicitWait;
                            seleniumWebDriver.ChromeDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(20);

                            // Clear cashe

                            seleniumWebDriver.GoToUrl("https://powertofly.com/accounts/login");
                            Thread.Sleep(5000);
                            seleniumWebDriver.ChromeDriver.Manage().Cookies.DeleteAllCookies();
                            seleniumWebDriver.GoToUrl("https://powertofly.com/accounts/login");
                            seleniumWebDriver.ChromeDriver.FindElementById("login-email").SendKeys(botSignal.Email);
                            seleniumWebDriver.ChromeDriver.FindElementById("login-password").SendKeys(botSignal.Password);
                            seleniumWebDriver.ChromeDriver.FindElementById("login-button").Click();
                            Thread.Sleep(5000);

                            var cookies = seleniumWebDriver.ChromeDriver.Manage().Cookies.AllCookies.ToDictionary(cookie => cookie.Name, cookie => cookie.Value);
                            var session = cookies["session"];

                            var doc = new HtmlDocument();

                            doc.LoadHtml(seleniumWebDriver.ChromeDriver.PageSource);

                            var attributes = doc.GetElementbyId("csrf_token").Attributes;

                            var token = attributes.First(x => x.Name == "value").Value;

                            Console.WriteLine("Token: " + token);

                            powerToFlyAPI.SetSession(session);
                            powerToFlyAPI.SetToken(token);

                            seleniumWebDriver.ChromeDriver.Close();

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Catch exception from selenium.....");
                            Console.WriteLine(e.Message);
                            adminPanelApi.ChangeSignalStatus(botSignal.Id, BotSignalStatus.Waiting);
                            if (seleniumWebDriver != null)
                            {
                                seleniumWebDriver.ChromeDriver.Close();
                            }

                            continue;
                        }


                        #endregion

                        //var links = botSignal.JobLinks.Split(";").Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim());

                        var searchLink = botSignal.JobLinks.Trim();
                        var pageCount = botSignal.MaxPageCount; // Get from bot signal

                        //powerToFlyAPI.Login(botSignal.Email, botSignal.Password);
                        powerToFlyAPI.IsAuth = true;

                        // Get 

                        for (int i = 0; i < pageCount; i++)
                        {
                            ICollection<string> jobsByLink = new List<string>();
                            try
                            {
                                jobsByLink = powerToFlyAPI.GetJobsByLink(searchLink, i).Select(x => x.FullLink).ToList();
                            }
                            catch (Exception e)
                            {
                                break;
                            }

                            foreach (var item in jobsByLink)
                            {
                                try
                                {
                                    var job = powerToFlyAPI.GetJobByLink(item);
                                    
                                    if (job == null)
                                    {
                                        continue;
                                    }

                                    Console.WriteLine("Job find(WithUploadCV): " + job.WithUploadCV + " Redirect: " + job.WithRedirect);
                                    if (!job.WithRedirect && job.WithUploadCV)
                                    {
                                        powerToFlyAPI.ApplyForJob(job.ShortLink, botSignal.CoverLetter);
                                        adminPanelApi.SaveJob(new JobDto { Name = job.Name, CoverLetter = botSignal.CoverLetter, Link = job.FullLink, SignalId = botSignal.Id });
                                    }
                                    Thread.Sleep(1000);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                    Thread.Sleep(1000);
                                }
                            }
                        }

                        adminPanelApi.ChangeStatus(botSignal.Email, "1");
                        adminPanelApi.ChangeSignalStatus(botSignal.Id, BotSignalStatus.Finished);
                        botSignal = null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Global exception");
                    Console.WriteLine(e.Message);
                    Thread.Sleep(5000);
                    try
                    {
                        if (botSignal != null)
                        {
                            adminPanelApi.ChangeSignalStatus(botSignal.Id, BotSignalStatus.Waiting);
                            adminPanelApi.ChangeStatus(botSignal.Email, "1");
                            botSignal = null;
                        }
                    }
                    catch (Exception)
                    {
                       
                    }
                    
                }
            }
        }

        private (string session, string crsf_token) LoginUsingSelenium(SeleniumWebDriver seleniumWebDriver)
        {
            seleniumWebDriver.GoToUrl("https://powertofly.com/accounts/login");
            //seleniumWebDriver.ChromeDriver.FindElementById("login-email").SendKeys();
           // seleniumWebDriver.ChromeDriver.FindElementById("login-password").SendKeys();
            seleniumWebDriver.ChromeDriver.FindElementById("login-button").Click();

            var cookies = seleniumWebDriver.ChromeDriver.Manage().Cookies.AllCookies.ToDictionary(cookie => cookie.Name, cookie => cookie.Value);
            var session = cookies["session"];

            var doc = new HtmlDocument();

            doc.LoadHtml(seleniumWebDriver.ChromeDriver.PageSource);

            var attributes = doc.GetElementbyId("csrf_token").Attributes;

            var token = attributes.First(x => x.Name == "value").Value;

            return (session, token);
        }

        private static void TestGetCookies()
        {
            //#region Get csrf token using HttpClient
            //var doc = new HtmlDocument();

            //var loginString = Program.client.GetStringAsync("https://powertofly.com/accounts/login").GetAwaiter().GetResult();
            //doc.LoadHtml(loginString);

            //var attributes = doc.GetElementbyId("login-csrf_token").Attributes;

            //var token = attributes.First(x => x.Name == "value").Value;
            //#endregion
            PowerToFlyAPI powerToFlyAPI = new PowerToFlyAPI();
            //powerToFlyAPI.Login();

            //var job = powerToFlyAPI.GetJobByLink("https://powertofly.com/jobs/detail/331367");

            SeleniumWebDriver seleniumWebDriver = new SeleniumWebDriver(new SeleniumSettings
            {
                IsDisableExtensions = true,
                IsHeadless = false,
                ProfileDirectory = "./ChromeDriver/Profile 2",
                UserDataDir = @"./ChromeDriver/"
            });

            seleniumWebDriver.GoToUrl("https://powertofly.com/accounts/login");
            //seleniumWebDriver.ChromeDriver.FindElementById("login-email").SendKeys();
            //seleniumWebDriver.ChromeDriver.FindElementById("login-password").SendKeys();
            seleniumWebDriver.ChromeDriver.FindElementById("login-button").Click();
            seleniumWebDriver.ChromeDriver.Manage().Cookies.DeleteAllCookies();
            Thread.Sleep(5000);
            var cookies = seleniumWebDriver.ChromeDriver.Manage().Cookies.AllCookies.ToDictionary(cookie => cookie.Name, cookie => cookie.Value);
            var session = cookies["session"];

            var client = new RestClient("https://powertofly.com");
            #region Check request for get first session
            var request = new RestRequest("/api/v1/events/?filter=(ends_at%3E%222020-12-09T22:11:15.639Z%22%20or%20starts_at%3E%222020-12-09T22:11:15.639Z%22)%20and%20etype==%22Chat%20n%20Learn%22%20and%20published_on!=null&order_by=starts_at:asc&fields=tags,slug&include_default=true&per_page=1");

            SetHeaders(request, session);
            var response = client.Get(request);
            var content = response.Content;

            //var session = "";
            //Program.coockiesDictionary.Add("session", session);
            #endregion

            #region First request to site using RestSharp for get session

            // client.Authenticator = new HttpBasicAuthenticator(username, password);
            request = new RestRequest("/accounts/login");

            SetHeaders(request, session);

            response = client.Get(request);
            content = response.Content; // Raw content as string

            session = response.Cookies.First(x => x.Name == "session").Value;
            Program.coockiesDictionary.Add("session", session);
            #endregion

            #region GetToken

            var doc = new HtmlDocument();

            doc.LoadHtml(content);

            var attributes = doc.GetElementbyId("csrf_token").Attributes;

            var token = attributes.First(x => x.Name == "value").Value;

            #endregion

            #region Login
            request = new RestRequest("/accounts/login");

            request.AddParameter("login-csrf_token", token);
            request.AddParameter("login-next", "");
            request.AddParameter("login-role", "");
            //request.AddParameter("login-email", );
            //request.AddParameter("login-password",);

            SetHeaders(request, session);

            response = client.Post(request);
            content = response.Content; // Raw content as string
            #endregion

            #region Success request
            // client.Authenticator = new HttpBasicAuthenticator(username, password);
            request = new RestRequest("/accounts/login/success");
            request.AddHeader("authority", "powertofly.com");

            SetHeaders(request, session);

            response = client.Get(request);
            content = response.Content; // Raw content as string

            #endregion

            #region Load job
            //https://powertofly.com/jobs/detail/331367
            //https://powertofly.com/jobs/detail/331367
            request = new RestRequest("/jobs/detail/331367");
            request.AddHeader("authority", "powertofly.com");

            SetHeaders(request, session);

            response = client.Get(request);
            content = response.Content; // Raw content as string
            #endregion

            #region Apply on job
            request = new RestRequest("/jobs/apply/371282.json?confirm_skills=True");
            request.AddHeader("authority", "powertofly.com");
            request.AddParameter("csrf_token", token);
            request.AddParameter("cover_letter", "Sorry I just test how this site work");
            request.AddParameter("submit", "Apply");
            request.AddParameter("next", "https://powertofly.com/jobs/detail/371282");

            SetHeaders(request, session);

            response = client.Post(request);
            content = response.Content; // Raw content as string
            #endregion

            //client = new RestClient("https://powertofly.com");
            //// client.Authenticator = new HttpBasicAuthenticator(username, password);
            //request = new RestRequest("/accounts/login");
            //request.AddParameter("login-csrf_token", token);
            //request.AddParameter("login-next", "");
            //request.AddParameter("login-role", "");
            //request.AddParameter("login-email", "torgkrypto@gmail.com");
            //request.AddHeader("header", "value");
            //response = client.Post(request);
            //content = response.Content; // Raw content as string

            //foreach (var item in response.Cookies)
            //{
            //    Console.WriteLine(item);
            //}


            //var values = new Dictionary<string, string>
            //{
            //    { "login-csrf_token", token },
            //    { "login-next", "" },
            //    { "login-role", "" },
            //};

            //var content = new FormUrlEncodedContent(values);

            //var response = client.PostAsync("https://powertofly.com/accounts/login", content).GetAwaiter().GetResult();

            //var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            //string uri = "https://powertofly.com/accounts/login";
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            //request.Method = WebRequestMethods.Http.Get;
            //request.AllowAutoRedirect = false;
            //request.CookieContainer = new CookieContainer();
            //request.KeepAlive = true;
            //request.ContentType = "application/x-www-form-urlencoded";
            //request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.0.5) Gecko/2008120122 Firefox/3.0.5";

            //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //Stream receiveStream = response.GetResponseStream();
            //StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            //string oku = readStream.ReadToEnd();


            //foreach (Cookie cook in response.Cookies)
            //{
            //    Console.WriteLine("Domain: {0}, Name: {1}, value: {2}", cook.Domain, cook.Name, cook.Value);

            //}
        }

        private static void SetHeaders(RestRequest restRequest, string session)
        {
            //restRequest.AddHeader(":authority", "powertofly.com");
            //restRequest.AddHeader(":method", "GET");
            //restRequest.AddHeader(":path", "/accounts/login");
            //restRequest.AddHeader(":scheme", "https");
            restRequest.AddHeader("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            restRequest.AddHeader("cache-control", "no-cache");
            restRequest.AddHeader("pragma", "no-cache");
            restRequest.AddHeader("sec-fetch-dest", "document");
            restRequest.AddHeader("sec-fetch-mode", "navigate");
            restRequest.AddHeader("sec-fetch-site", "none");
            restRequest.AddHeader("sec-fetch-user", "?1");
            restRequest.AddHeader("upgrade-insecure-requests", "1");
            restRequest.AddHeader("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36");

            SetCoockies(restRequest, session);
        }

        private static void SetCoockies(RestRequest restRequest, string session)
        {
            //string value = @"im_not_a_new_user=true; GDPR=require; vcf-notified=; pushalert_9378_1_subs_status=canceled; session=.eJw1zcsKwjAQheF3mbWLTG7NFLoT3IovUJLJBEWtkrQr8d2NiMsDP995wWF_PMG41k12MJcq7Qxjibf2nZcMIzBFRBdcToZzQM1GSEX2NqAEyx56-JR6j4ss61_iVsu8Pq6ydCEmRE2xDDqp4q3nzEgqaKOoq5QGx9EEr7u0Nam_W-094WDdNGl4fwCgZTEC.Eq1FUA.JzddX2FGa9lBfUe0kYCqUuYzAgA";
            string value = $"im_not_a_new_user=true; GDPR=require; vcf-notified=; pushalert_9378_1_subs_status=canceled; session={session};";

            //foreach (var item in coockiesDictionary)
            //{
            //    value = value + item.Key + "=" + item.Value + "; ";
            //}

            restRequest.AddHeader("cookie", value);
        }
    }
}
