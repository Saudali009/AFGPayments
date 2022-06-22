using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.AFG.Payments.GIACTAccountVerification
{
    public class CommonUtilities
    {
        public static string GetUniqueNumber()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var randomString = new string(Enumerable.Repeat(chars, 6)
                                                    .Select(s => s[random.Next(s.Length)]).ToArray());
            return $"GIACT{randomString}";
        }
    }
}
