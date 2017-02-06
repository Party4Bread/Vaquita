using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.symbol
{
    class LiteralSymbol : Symbol
    {
        public static string NUMBER = "number";
        public static string STRING = "string";

        public string value;

        public LiteralSymbol(string value, string type) : base()
        {
            this.value = value;
            this.type = type;
        }
    }
}