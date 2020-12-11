using System;
using System.Collections.Generic;
using System.Text;

namespace PowerToFlyBot.Models
{
    public class Job
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string FullLink { get; set; }

        public string ShortLink { get; set; }

        /// <summary>
        /// Can subbmit CV witout join another site
        /// </summary>
        public bool WithRedirect { get; set; }

        /// <summary>
        /// If vacancy requried to upload CV
        /// </summary>
        public bool WithUploadCV { get; set; }
    }
}
