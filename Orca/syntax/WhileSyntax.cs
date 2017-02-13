using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * While 문 구문 패턴
     * 
     * 형식: while( C )
     * 
     * @author 김 현준
     */
    class WhileSyntax : Syntax
    {


        public List<Token> condition;

        public WhileSyntax(List<Token> condition)
        {

            this.condition = condition;

        }

        /**
         * 토큰열이 While 문 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens.Count > 0 && tokens[0].type == Type.While)
                return true;
            return false;
        }

        /**
         * 토큰열을 분석하여 While 문 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static WhileSyntax analyze(List<Token> tokens, int lineNumber)
        {

            if (tokens.Count < 4)
            {
                Debug.reportError("Syntax error", "While statement is too short", lineNumber);
                return null;
            }

            // 괄호로 시작하는지 확인한다
            if (tokens[1].type != Type.ShellOpen)
            {
                Debug.reportError("Syntax error", "While condition must start with \"(\"", lineNumber);
                return null;
            }

            // 괄호로 끝나는지 확인한다.
            if (tokens[tokens.Count - 1].type != Type.ShellClose)
            {
                Debug.reportError("Syntax error", "insert \")\" to complete Expression", lineNumber);
                return null;
            }

            return new WhileSyntax(tokens.GetRange(2, tokens.Count - 1 - 2));
        }
    }
}
