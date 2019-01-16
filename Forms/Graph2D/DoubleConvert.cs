using System;
using System.Collections.Generic;
using System.Text;

namespace SummerGUI.Charting.Graph2D
{
    public static class DoubleConvert
    {
        public static DateTime dtRef = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

        public static IFormatProvider USformat = new System.Globalization.CultureInfo("en-US");
        public static IFormatProvider DEformat = new System.Globalization.CultureInfo("de-DE");

        //private int tickspersecond = 10000000;
        //private int ticksperday = TimeSpan.TicksPerDay;

        public static double Date2Double(DateTime value)
        {
            //return value.ToOADate();   
            //DateTime dtRef = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            //return (double)value.Ticks - dtRef.Ticks;
            return ((double)value.Ticks - dtRef.Ticks) / (double)TimeSpan.TicksPerDay;
        }

        public static DateTime Double2Date(Decimal value)
        {
            return Double2Date(value);
        }

        public static DateTime Double2Date(double value)
        {
            //return DateTime.FromOADate(value);

            //DateTime dtRef = new DateTime(2007, 1, 1);
            //return new DateTime((long)(value + dtRef.Ticks));
            return new DateTime((long)(value * (double)TimeSpan.TicksPerDay + dtRef.Ticks));

            //try
            //{
            //    return new DateTime((long)value);
            //}
            //catch (Exception)
            //{
            //    if (value < DateTime.MinValue.Ticks)
            //        return DateTime.MinValue;
            //    else
            //        return DateTime.MaxValue;
            //}            
        }

        public static DateTime AutoString2Date(string S)
        {
            DateTime dtRet = DateTime.MinValue;
            if (!DateTime.TryParse(S, USformat, System.Globalization.DateTimeStyles.None, out dtRet))
                if (!DateTime.TryParse(S, DEformat, System.Globalization.DateTimeStyles.None, out dtRet))
                    return DateTime.MinValue;

            return dtRet;
        }

        public static double AutoString2Double(string S)
        {
            double dRet = 0.0;

            if (!Double.TryParse(S, System.Globalization.NumberStyles.Any, USformat, out dRet))
                if (!Double.TryParse(S, System.Globalization.NumberStyles.Any, DEformat, out dRet))
                    return Double.MinValue;

            return dRet;
        }

        // static decimal golden_ratio = (1.0M + DecSqrt(5.0M)) / 2.0M;

        private static decimal DecSqrt(decimal x)
        {
            decimal y;
            y = (decimal)Math.Sqrt((double)x);
            // Two Newton-Raphson iterations suffice for decimal precision
            y = (y + x / y) / 2.0M;
            y = (y + x / y) / 2.0M;
            return (y);
        }

        //public static double DiffMonths(DateTime dt1, DateTime dt2)
        //{
        //    TimeSpan ts = dt1 - (new DateTime(1, 1, 1));
        //    DateTime difference = dt2.Subtract(dt1);

        //    int numMonths = difference.Month + (difference.AddYears(-1).Year * 12);
        //    return difference.AddYears(-1).Year;            
        //}
    }
}
