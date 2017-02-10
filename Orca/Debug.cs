using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca
{
    /**
     * 디버거
     * 
     * @author 김 현준
     */
    class Debug
    {

        /**
         * 에러 표시를 허용하는지의 여부
         */
        public static bool supressed = false;
        public static bool errorReported = false;

        /**
         * 에러를 출력한다.
         * 
         * @param	errorType
         * @param	errorMessage
         * @param	lineNumber
         */
        public static void reportError(string errorType, string errorMessage, int lineNumber = 1)
        {
            if (!supressed) print(errorType + " :" + errorMessage + " at " + lineNumber.ToString());
            errorReported = true;
        }

        public static void print(object message)
        {
            IO.print(message);
        }

        /**
         * 잠시 에러 출력을 중지한다.
         * 
         * @param	status
         */
        public static void supressError(bool status)
        {
            supressed = status;
        }

    }
}