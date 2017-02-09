using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * Return 구문 패턴
     * 
     * 형식: return V
     * 
     * @author 김 현준
     */
    class ReturnSyntax : Syntax
    {


        public List<Token> returnValue;

        public ReturnSyntax(List<Token> returnValue)
        {

            this.returnValue = returnValue;

        }

        /**
         * 토큰열이 Return 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens.Count > 0 && tokens[0].type == Type.Return)
                return true;
            return false;
        }

        /**
         * 토큰열을 분석하여 Return 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static ReturnSyntax analyze(List<Token> tokens, int lineNumber)
        {
            return new ReturnSyntax(tokens.GetRange(1, 1));
        }
    }
}
