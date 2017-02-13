using Orca.nlib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.symbol
{
    /**
     * 함수 심볼
     */
    class FunctionSymbol : Symbol
    {


        public int functionEntry;
        public int functionExit;

        public List<VariableSymbol> parameters;

        public bool isRecursive = false;

        public bool isNative = false;
        public NativeFunction nativeFunction;

        public FunctionSymbol(string id, string type, List<VariableSymbol> parameters = null) : base()
        {
            this.id = id;
            this.type = type;
            this.parameters = parameters;
        }
        /**
         * 함수가 값을 반환하지 않는 지 확인한다.
         * 
         * @return
         */
        public bool isVoid()
        {
            if (type == "void")
                return true;
            return false;
        }
    }
}