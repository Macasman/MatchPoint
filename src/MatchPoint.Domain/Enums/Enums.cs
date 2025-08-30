using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchPoint.Domain.Enums
{
    public class Enums
    {
        public enum ReservationStatus : byte
        {
            Scheduled = 1,
            Completed = 2,
            CanceledByUser = 3,
            CanceledByAdmin = 4,
            NoShow = 5
        }
        public enum PaymentIntentStatus : byte
        {
            Pending = 1,
            Authorized = 2,
            Captured = 3,
            Failed = 4,
            Canceled = 5
        }
    }
}
