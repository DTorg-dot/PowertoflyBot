using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerToFlyBot
{
    public static class Logger
    {
        public static void LogResponse(IRestResponse response)
        {
            string fullErrorMessage = null;
            string shortErrorMessage = null;

            fullErrorMessage = "Status code: " + response.StatusCode.ToString() + " Stauts Description: " + response.StatusDescription + "\n";
            fullErrorMessage += "Message: " + response.ErrorMessage + "\n";
            fullErrorMessage += "Content: " + response.Content + "\n";
            fullErrorMessage += "Cockies: " + response.Cookies.Select(x => x.Name + ":" + x.Value + " ") + "\n";
            fullErrorMessage += "Headers: " + response.Headers.Select(x => x.Name + ":" + x.Value + " ") + "\n";

            shortErrorMessage = "Status code: " + response.StatusCode.ToString() + " Message: " + response.ErrorMessage + "\n";
        }
    }
}
