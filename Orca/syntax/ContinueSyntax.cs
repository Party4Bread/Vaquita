using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * Continue 구문 패턴
     * 
     * 형식: continue
     * 
     * @author 김 현준
     */
    class ContinueSyntax : Syntax
    {
        /**
         * 토큰열이 Continue 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens.Count > 0 && tokens[0].type == Type.Continue)
                return true;
            return false;
        }

        /**
         * 토큰열을 분석하여 Continue 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static ContinueSyntax analyze(List<Token> tokens, int lineNumber)
        {
            return new ContinueSyntax();
        }
    }
}
