using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.vm
{
    /**
     * Orcinus Application Programming Interface
     * 
     * @author 김 현준
     */
    class Api
    {

        public static void print(object message)
        {
            Debug.print(message);
        }

        public static void whoAmI()
        {
            Debug.print("I am Orca Virtual Machine.");
        }

        public static decimal abs(decimal v)
        {
            return Math.Abs(v);
        }

        public static double acos(double v)
        {
            return Math.Acos(v);
        }

        public static double asin(double v)
        {
            return Math.Asin(v);
        }

        public static double atan(double v)
        {
            return Math.Atan(v);
        }

        public static double atan2(double y, double x)
        {
            return Math.Atan2(y, x);
        }

        public static decimal ceil(decimal v)
        {
            return Math.Ceiling(v);
        }

        public static decimal floor(decimal v)
        {
            return Math.Floor(v);
        }

        public static decimal round(decimal v)
        {
            return Math.Round(v);
        }

        public static double cos(double v)
        {
            return Math.Cos(v);
        }

        public static double sin(double v)
        {
            return Math.Sin(v);
        }

        public static double tan(double v)
        {
            return Math.Tan(v);
        }

        public static double log(double v)
        {
            return Math.Log(v);
        }

        public static double sqrt(double v)
        {
            return Math.Sqrt(v);
        }

        public static double pow(double v, double exp)
        {
            return Math.Pow(v, exp);
        }

        public static double exp(double v)
        {
            return Math.Exp(v);
        }
        public static decimal random()
        {
            return new Random().Next();
        }

    }
}
