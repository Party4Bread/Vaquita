using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 인스턴스 생성 구문 패턴
     * 
     * 형식: new A;
     * 
     * @author 김 현준
     */
    class InstanceCreationSyntax : Syntax
    {


        public Token instanceType;

        public InstanceCreationSyntax(Token instanceType)
        {

            this.instanceType = instanceType;

        }

        /**
         * 토큰열이 인스턴스 생성 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {

            if (tokens.Count != 2)
                return false;

            // 토큰의 첫 원소가 new여야 한다.
            if (tokens[0].type != Type.New)
                return false;

            // 토큰의 두 번째 원소가 id여야 한다.
            if (tokens[1].type != Type.ID)
                return false;

            return true;
        }

        /**
         * 토큰열을 분석하여 인스턴스 생성 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static InstanceCreationSyntax analyze(List<Token> tokens, int lineNumber)
        {

            // 생성자의 길이는 2이다.
            if (tokens.Count != 2)
            {
                Debug.reportError("Syntax error", "New syntax is too long.", lineNumber);
                return null;
            }

            return new InstanceCreationSyntax(tokens[1]);
        }
    }
}
