using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.nlib
{
    /**
     * 네이티브 함수
     * 
     * @author 김 현준
     */
    class NativeFunction
    {

        public string functionName;
        public List<string> parameters;
        public string returnType;

        public string assembly;

        public NativeFunction(string functionName, List<string> parameters, string returnType = "void")
        {
            this.functionName = functionName;
            this.parameters = parameters;
            this.returnType = returnType;

            this.assembly = "";
        }

        /**
         * 함수에 어셈블리 명령 쓰기
         * 
         * @param	code
         */
        public void write(string code)
        {
            if (assembly != "")

                this.assembly += "\n" + code;
            else
                this.assembly += code;

            this.assembly = assembly.Trim();

        }

    }
}