using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * Else 문 구문 패턴
     * 
     * 형식: else
     * 
     * @author 김 현준
     */
    class ElseSyntax : Syntax
    {

        /**
         * 토큰열이 Else 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens.Count > 0 && tokens[0].type == Type.Else)
                return true;
            return false;
        }

        /**
         * 토큰열을 분석하여 Else 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static ElseSyntax analyze(List<Token> tokens, int lineNumber)
        {
            return new ElseSyntax();
        }
    }
}
