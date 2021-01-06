using HtmlAgilityPack;
using PowerToFlyBot.Exceptions;
using PowerToFlyBot.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerToFlyBot
{
    public class PowerToFlyAPI
    {
        private Dictionary<string, string> Cookies = new Dictionary<string, string>
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

        private Dictionary<string, string> Headers = new Dictionary<string, string>
        {
            { "accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9" },
            { "cache-control", "no-cache" },
            { "pragma", "no-cache" },
            { "sec-fetch-dest", "document" },
            { "sec-fetch-mode", "navigate" },
            { "sec-fetch-site", "none" },
            { "sec-fetch-user", "?1" },
            { "upgrade-insecure-requests", "1" },
            { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.198 Safari/537.36" }
        };

        private HtmlDocument document;
        private RestClient restClient;

        private string csrfToken;

        public bool IsAuth { get; set; }

        public PowerToFlyAPI()
        {
            document = new HtmlDocument();
            restClient = new RestClient("https://powertofly.com");
        }

        public void SetSession(string session)
        {
            string sessionTmp = null;
            Cookies.TryGetValue("session", out sessionTmp);
            if (sessionTmp != null)
            {
                Cookies["session"] = session;
            }
            else
            {
                Cookies.Add("session", session);
            }
        }

        public void SetToken(string token)
        {
            this.csrfToken = token;
        }

        public void Login(string email, string password)
        {
            // First join to site
            var request = new RestRequest("/accounts/login");
            SetHeaders(request);
            SetCookies(request);

            var response = restClient.Get(request);
            CheackResponse(response);
            var content = response.Content;

            // Get session
            var session = response.Cookies.First(x => x.Name == "session").Value;
            Cookies.Add("session", session);

            // Get CsrfToken
            document.LoadHtml(content);

            var node = document.GetElementbyId("csrf_token");

            if (node == null)
            {
                node = document.GetElementbyId("login-csrf_token");
            }

            csrfToken = node.Attributes.First(x => x.Name == "value").Value;

            // Login reqeuest
            request = new RestRequest("/accounts/login");

            request.AddParameter("login-csrf_token", csrfToken);
            request.AddParameter("login-next", "");
            request.AddParameter("login-role", "");
            request.AddParameter("login-email", email);
            request.AddParameter("login-password", password);

            SetHeaders(request);
            SetCookies(request);

            response = restClient.Post(request);
            CheackResponse(response);

            // Success request
            request = new RestRequest("/accounts/login/success");
            request.AddHeader("authority", "powertofly.com");

            SetHeaders(request);
            SetCookies(request);

            response = restClient.Get(request);
            CheackResponse(response);

            IsAuth = true;
        }

        public ICollection<Job> GetJobsByLink(string link, int page = 0)
        {
            if (!IsAuth)
            {
                throw new NotAuthExcpetion("Auth first");
            }

            var uri = link.Replace("https://powertofly.com", "");

            if (page != default)
            {
                uri += $"&page={page}";
            }

            var request = new RestRequest(uri);
            SetHeaders(request);
            SetCookies(request);

            var response = restClient.Get(request);
            CheackResponse(response);

            var jobs = ParseJobList(response.Content);
            return jobs;
        }

        public ICollection<Job> GetJobs(string searchBy = null, int page = 0)
        {
            if (!IsAuth)
            {
                throw new NotAuthExcpetion("Auth first");
            }

            string uri = "/jobs";

            if (searchBy != null)
            {
                searchBy = searchBy.Replace(' ', '+').Trim();
                uri += $"?keywords={searchBy}";
            }
            else
            {
                uri += "?=undefined";
            }

            if (page != default)
            {
                uri += $"&page={page}";
            }

            var request = new RestRequest(uri);
            SetHeaders(request);
            SetCookies(request);

            var response = restClient.Get(request);
            CheackResponse(response);

            var jobs = ParseJobList(response.Content);
            return jobs;
        }

        public Job GetJobByLink(string jobLink)
        {
            if (!IsAuth)
            {
                throw new NotAuthExcpetion("Auth first");
            }

            jobLink = jobLink.Replace("https://powertofly.com", "");

            var request = new RestRequest(jobLink);
            SetHeaders(request);
            SetCookies(request);

            var response = restClient.Get(request);
            CheackResponse(response);

            return ParseSingleJob(response.Content, jobLink);
        }

        public void ApplyForJob(string shortLink, string coverLetter)
        {
            if (!IsAuth)
            {
                throw new NotAuthExcpetion("Auth first");
            }

            Console.WriteLine($"{csrfToken}");
            Console.WriteLine($"{shortLink}");

            string uri = shortLink.Replace("detail", "apply") + ".json?confirm_skills=True";

            var request = new RestRequest(uri);

            request.AddHeader("authority", "powertofly.com");
            request.AddParameter("csrf_token", csrfToken);
            request.AddParameter("cover_letter", coverLetter);
            request.AddParameter("submit", "Apply");
            request.AddParameter("next", "https://powertofly.com" + shortLink);

            SetHeaders(request);
            SetCookies(request);

            var response = restClient.Post(request);
            CheackResponse(response);
        }

        private void SetHeaders(RestRequest restRequest)
        {
            foreach (var header in Headers)
            {
                restRequest.AddHeader(header.Key, header.Value);
            }
        }

        private void SetCookies(RestRequest restRequest)
        {
            string value = null;

            foreach (var item in Cookies)
            {
                value += item.Key + "=" + item.Value + "; ";
            }
            //string value = @"im_not_a_new_user=true; GDPR=require; vcf-notified=; pushalert_9378_1_subs_status=canceled; session=.eJw1zcsKwjAQheF3mbWLTG7NFLoT3IovUJLJBEWtkrQr8d2NiMsDP995wWF_PMG41k12MJcq7Qxjibf2nZcMIzBFRBdcToZzQM1GSEX2NqAEyx56-JR6j4ss61_iVsu8Pq6ydCEmRE2xDDqp4q3nzEgqaKOoq5QGx9EEr7u0Nam_W-094WDdNGl4fwCgZTEC.Eq1FUA.JzddX2FGa9lBfUe0kYCqUuYzAgA";
            restRequest.AddHeader("cookie", value);
        }

        private void CheackResponse(IRestResponse response)
        {
            if (!response.IsSuccessful)
            {
                Logger.LogResponse(response);

                if (response.ErrorException != null)
                {
                    throw response.ErrorException;
                }
            }
        }

        private ICollection<Job> ParseJobList(string html)
        {
            document.LoadHtml(html);
            ICollection<Job> jobs = new List<Job>();

            foreach (var item in document.DocumentNode.SelectNodes("//div[contains(@class, 'js-elem')]"))
            {
                var link = item.SelectSingleNode("a").GetAttributeValue("href", "");
                var name = item.SelectSingleNode("//a/div[2]/div[1]").InnerText.Replace("\n", "").Trim();

                jobs.Add(new Job
                {
                   Id = Convert.ToInt32(link.Split('/').LastOrDefault().Trim()),
                   FullLink = "https://powertofly.com" + link,
                   ShortLink = link,
                   Name = name
                });
            }

            return jobs;
        }

        private Job ParseSingleJob(string html, string shortLink)
        {
            document.LoadHtml(html);

            var interestedButton = document.DocumentNode.SelectSingleNode("//button[contains(@class, 'stat-i-am-intersted-btn')]");

            // Already intresting register. skip
            if (interestedButton == null)
            {
                return null;
            }

            bool isAuth = interestedButton.Attributes.First(x => x.Name == "action").Value.Contains("/jobs/apply/") && !interestedButton.Attributes.First(x => x.Name == "action").Value.Contains("unauth");

            Console.WriteLine($"Is Auth: {isAuth}");

            var name = document.DocumentNode.SelectSingleNode("//h1[contains(@class, 'job title')]").GetAttributeValue("title", "");

            return new Job
            {
                Id = Convert.ToInt32(shortLink.Split('/').LastOrDefault().Trim()),
                Name = name,
                ShortLink = shortLink,
                FullLink = "https://powertofly.com" + shortLink,
                WithRedirect = interestedButton.Attributes.Any(x => x.Name == "data-is-custom-link"),
                WithUploadCV = !interestedButton.Attributes.Any(x => x.Name == "data-is-custom-link") && interestedButton.Attributes.First(x => x.Name == "action").Value.Contains("/jobs/apply/")
            };
        }
    }
}
