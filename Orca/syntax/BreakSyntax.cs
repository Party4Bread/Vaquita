using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * Break 구문 패턴
     * 
     * 형식: break
     * 
     * @author 김 현준
     */
    class BreakSyntax : Syntax
    {

        /**
         * 토큰열이 Break 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens.Count > 0 && tokens[0].type == Type.Break)
                return true;
            return false;
        }

        /**
         * 토큰열을 분석하여 Break 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static BreakSyntax analyze(List<Token> tokens, int lineNumber)
        {
            return new BreakSyntax();
        }
    }
}