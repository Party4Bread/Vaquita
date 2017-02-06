using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.symbol
{
    /**
     * 변수 심볼
     */
    class VariableSymbol : Symbol
    {
        public bool initialized = false;

        public VariableSymbol(string id, string type):base()
        {
            this.id = id;
            this.type = type;
        }

        /**
         * 변수가 배열 타입인지 체크한다.
         * 
         * @return
         */

        public bool isArray()
        {
            if (type == "array" || type == "arr")
                return true;
            return false;
        }

        /**
         * 변수가 리터럴 타입인지 체크한다.
         * 
         * @return
         */
        public bool isNumber()
        {
            if (type == "number" || type == "bool")
                return true;
            return false;
        }

        /**
         * 변수가 리터럴 타입인지 체크한다.
         * 
         * @return
         */
        public bool isString()
        {
            if (type == "string" || type == "str")
                return true;
            return false;
        }
    }
}