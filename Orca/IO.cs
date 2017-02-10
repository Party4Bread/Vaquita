using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca
{
    public class IO
    {
        public static string console="";
        public static string input = "";
        public static bool isConsolefreeze = true,isInputpending=false;
        public static void print(object opt) {
            if(opt is string)
                console += opt as string + '\n';            
        }
        public static void read(string ipt)
        {
            input = ipt;
            isInputpending = true;
        }
        public static string wait4Input()
        {
            isConsolefreeze = false;
            while(!isInputpending)
            {
                Task.Delay(10);
            }
            isInputpending = false;
            string temp = input;
            input = "";
            return temp;
        }
    }
}
