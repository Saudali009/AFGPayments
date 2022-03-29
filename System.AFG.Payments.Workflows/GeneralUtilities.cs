using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.AFG.Payments.Workflows
{
    public class GeneralUtilities
    {
        public static string GenerateRandomAlphanumericString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var random = new Random();
            var randomString = new string(Enumerable.Repeat(chars, length)
                                                    .Select(s => s[random.Next(s.Length)]).ToArray());
            return randomString;
        }

        public static string WithMaxLength(string value, int maxLength)
        {
            return value?.Substring(0, Math.Min(value.Length, maxLength));
        }

        public static string GetStringOfLength(int totalLength, string input, string charcterToAppend)
        {
            return input.PadLeft(totalLength, Convert.ToChar(charcterToAppend));
        }

        private static bool IsHoliday(DateTime effectiveDate, List<DateTime> Holidays)
        {
            return Holidays.Any(holiday => holiday.Day == effectiveDate.Day &&
            holiday.Month == effectiveDate.Month && holiday.Year == effectiveDate.Year);
        }

        public static bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday
                || date.DayOfWeek == DayOfWeek.Sunday;
        }

        public static DateTime GetNextWorkingDay(ITracingService tracingService, IOrganizationService service)
        {
            List<DateTime> holidays = new List<DateTime>();
            EntityCollection listOfHolidays = CRMDataRetrievalHandler.GetEffectiveHolidays(tracingService, service);
            foreach (Entity holiday in listOfHolidays.Entities)
            {
                if (holiday.Contains("afg_date") && holiday["afg_date"] != null)
                {
                    DateTime holidayDate = Convert.ToDateTime(holiday["afg_date"]);
                    holidays.Add(holidayDate.Date);
                }
            }

            DateTime today = DateTime.Now;
            bool dateFound = false;
            int counter = 1;

            while (dateFound == false)
            {
                today = today.AddDays(counter);
                if (IsHoliday(today, holidays) || IsWeekend(today))
                {
                    counter++;
                    dateFound = false;
                }
                else
                {
                    dateFound = true;
                }
            }
            return today;
        }
    }
}
