using System;
using System.Collections.Generic;
using System.Text;

namespace PowerToFlyBot.Exceptions
{
    public class NotAuthExcpetion : Exception
    {
        public NotAuthExcpetion(string message) : base(message)
        {

        }
    }
}
