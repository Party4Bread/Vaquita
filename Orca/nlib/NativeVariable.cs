using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.nlib
{
    /**
     * 네이티브 변수
     * 
     * @author 김 현준
     */
    class NativeVariable
    {

        public string variableName;
        public string value;

        public NativeVariable(string variableName, string value)
        {
            this.value = value;
            this.variableName = variableName;
        }

    }
}
