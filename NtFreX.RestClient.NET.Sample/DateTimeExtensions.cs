using System;

namespace NtFreX.RestClient.NET.Sample
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTimeMilliseconds(this DateTime dateTime)
        {
            DateTimeOffset offset = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return offset.ToUnixTimeMilliseconds();
        }

        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            DateTimeOffset offset = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return offset.ToUnixTimeSeconds();
        }

        public static DateTime UnixTimeSecondsToDateTime(double unixTimeStamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        }
    }
}
