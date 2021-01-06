using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace PowerToFlyBot
{
    public class BotSignalDto
    {
        public int Id { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string JobLinks { get; set; }

        public string CoverLetter { get; set; }

        public bool IgnoreAlreadySended { get; set; }

        public int MaxPageCount { get; set; }
    }

    public class JobDto
    {
        public string Name { get; set; }

        public string Link { get; set; }

        public string CoverLetter { get; set; }

        public int SignalId { get; set; }
    }

    public enum BotSignalStatus
    {
        Waiting = 1,
        InProgress = 2,
        Finished = 3
    }

    public class AdminPanelApi
    {
        private RestClient RestClient { get; set; }

        public AdminPanelApi(string url)
        {
            RestClient = new RestClient(url);
        }

        public BotSignalDto GetBotSignal()
        {
            var request = new RestRequest("/BotSignal");
            var response = RestClient.Get(request);

            var result = JsonConvert.DeserializeObject<BotSignalDto>(response.Content);

            return result;
        }

        public void ChangeStatus(string email, string status)
        {
            var request = new RestRequest($"/ChangeStatus");
            request.AddParameter("email", email);
            request.AddParameter("status", status);

            var response = RestClient.Get(request);
        }

        public void ChangeSignalStatus(int botSignalId, BotSignalStatus status)
        {
            var request = new RestRequest($"/ChangeSignalStatus");

            request.AddParameter("botSignalId", botSignalId);
            request.AddParameter("status", status);


            var response = RestClient.Get(request);
        }

        public void SaveJob(JobDto jobDto)
        {
            var request = new RestRequest("/SaveJob");
            request.RequestFormat = DataFormat.Json;
            request.AddJsonBody(jobDto);

            var response = RestClient.Post(request);
        }
    }
}
