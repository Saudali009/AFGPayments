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

        private static bool IsHoliday(DateTime date, List<DateTime> Holidays)
        {
            return Holidays.Any(day => day.Day == date.Day && day.Month == date.Month && day.Year == date.Year);
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
                    holidays.Add(holidayDate);
                }
            }
            DateTime date = DateTime.Today;
            tracingService.Trace($"List of Holidays : {holidays}");
            tracingService.Trace($"Is Holiday? : {IsHoliday(date, holidays)}");
            tracingService.Trace($"Is Weekend? : {IsWeekend(date)}");

            while (IsHoliday(date, holidays) || IsWeekend(date))
            {
                date = date.AddDays(1);
            }
            return date;
        }
    }
}
