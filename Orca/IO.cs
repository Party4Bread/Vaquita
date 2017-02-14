using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca
{
    public class IO
    {
        public string console="";
        public string input = "";
        public bool isConsolefreeze = true,isInputpending=false;
        public void print(object opt) {
            if(opt is string)
                console += opt as string + Environment.NewLine;            
        }
        public void read(string ipt)
        {
            input = ipt;
            isInputpending = true;
        }
        public string wait4Input()
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
        public string getConsole()
        {
            return console;
        }
    }
}
