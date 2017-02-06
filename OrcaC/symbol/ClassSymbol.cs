using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.symbol
{
    class ClassSymbol : Symbol
    {
        /**
         * 클래스 맴버 (함수, 변수)
         */
        public List<VariableSymbol> members;
        string id;
        public ClassSymbol(string id):base()
        {
            this.id = id;
        }

        /**
         * 클래스의 맴버를 검색한다.
         * 
         * @param id
         * @return
         */
        public VariableSymbol findMemberByID(string id)
        {
            foreach (int i in Enumerable.Range(0, members.Count))
            {
                if (members[i].id == id)
                {
                    return members[i];
                }
            }
            return null;
        }
    }
}
