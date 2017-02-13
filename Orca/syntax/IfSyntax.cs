using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 조건문 구문 패턴
     * 
     * 형식: if( C )
     * 
     * @author 김 현준
     */
    class IfSyntax : Syntax
    {


        public List<Token> condition;
        public List<Token> omittedForm;

        public IfSyntax(List<Token> condition)
        {

            this.condition = condition;

        }

        /**
         * 토큰열이 조건문 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens.Count > 0 && tokens[0].type == Type.If)
                return true;
            return false;
        }

        /**
         * 토큰열을 분석하여 조건문 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static IfSyntax analyze(List<Token> tokens, int lineNumber)
        {

            // 미완성된 제어문의 경우
            if (tokens.Count < 4)
            {
                Debug.reportError("Syntax error", "If syntax is too short.", lineNumber);
                return null;
            }

            // 괄호로 시작하는지 확인한다
            if (tokens[1].type != Type.ShellOpen)
            {
                Debug.reportError("Syntax error", "If condition must start with \"(\"", lineNumber);
                return null;
            }

            // 괄호로 끝나는지 확인한다.
            if (tokens[tokens.Count - 1].type != Type.ShellClose)
            {
                Debug.reportError("Syntax error", "insert \")\" to complete Expression", lineNumber);
                return null;
            }

            return new IfSyntax(tokens.GetRange(2, tokens.Count - 1 - 2));
        }
    }
}
