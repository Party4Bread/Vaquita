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
     * 형식: elif( C )
     * 
     * @author 김 현준
     */
    class ElseIfSyntax : Syntax
    {


        public List<Token> condition;

        public ElseIfSyntax(List<Token> condition)
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
            if (tokens.Count > 1 && tokens[0].type == Type.Else && tokens[1].type == Type.If)
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
        public static ElseIfSyntax analyze(List<Token> tokens, int lineNumber)
        {

            // 미완성된 제어문의 경우
            if (tokens.Count < 5)
            {
                Debug.reportError("Syntax error", "Else - If syntax is not valid", lineNumber);
                return null;
            }

            // 괄호로 시작하는지 확인한다
            if (tokens[2].type != Type.ShellOpen)
            {
                Debug.reportError("Syntax error", "Condition must start with \"(\"", lineNumber);
                return null;
            }

            // 괄호로 끝나는지 확인한다.
            if (tokens[tokens.Count - 1].type != Type.ShellClose)
            {
                Debug.reportError("Syntax error", "insert \")\" to complete Expression", lineNumber);
                return null;
            }

            return new ElseIfSyntax(tokens.GetRange(3, tokens.Count - 1 - 3));
        }
    }
}
