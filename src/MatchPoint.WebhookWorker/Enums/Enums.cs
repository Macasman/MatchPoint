using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchPoint.WebhookWorker.Enums
{
    public class Enums
    {
        public enum WebhookStatus : byte
        {
            Pending = 0, 
            Processing = 1, 
            Sent = 2,
            Failed = 3, 
            DeadLetter = 4
        }
    }
}
