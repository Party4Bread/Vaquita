using Orca.nlib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca
{
    /**
     * 네이티브 클래스
     * 
     * @author 김 현준
     */
    class NativeClass
    {

        public string className;
        public List<NativeFunction> classMembers;

        public NativeClass(string className, List<NativeFunction> classMembers)
        {

            this.className = className;
            this.classMembers = classMembers == null ? new List<NativeFunction>() : classMembers;
        }

        /**
         * 네이티브 클래스에 맴버 함수 추가
         * 
         * @param	member
         */
        public void addMember(NativeFunction member)
        {
            classMembers.Add(member);
        }

    }
}