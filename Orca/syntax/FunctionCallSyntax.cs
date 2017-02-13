using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 함수 호출 구문 패턴
     * 
     * 형식: name(parameters);
     * 
     * @author 김 현준
     */
    class FunctionCallSyntax : Syntax
    {


        public Token functionName;
        public List<List<Token>> functionArguments;

        public FunctionCallSyntax(Token functionName, List<List<Token>> functionArguments = null)
        {
            this.functionName = functionName;
            this.functionArguments = functionArguments;
        }

        /**
         * 토큰열이 함수 호출 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {

            int indexOfLpo = TokenTools.indexOfLpo(tokens);

            // 어떠한 유효 연산자라도 있을 경우 함수 호출이 아님
            if (indexOfLpo >= 0)
                return false;

            // 최소 길이 조건 확인
            if (tokens.Count < 3)
                return false;

            // 첫 토큰이 ID이고 두 번째 토큰이 ShellOpen이면 조건 만족	
            if (tokens[0].type != Type.ID || tokens[1].type != Type.ShellOpen)
                return false;

            return true;
        }

        /**
         * 토큰열을 분석하여 함수 호출 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static FunctionCallSyntax analyze(List<Token> tokens, int lineNumber)
        {

            // 함수가 완전히 닫혔는지 확인
            if (TokenTools.indexOfShellClose(tokens, 2) != tokens.Count - 1)
            {
                Debug.reportError("Syntax error", "함수가 종결되지 않았습니다.", lineNumber);
                return null;
            }

            // 함수 매개 변수를 가져온다.
            List<List<Token>> arguments = TokenTools.split(tokens.GetRange(2, tokens.Count - 1 - 2), Type.Comma, true);
            List<List<Token>> trimmedArguments = new List<List<Token>>();

            foreach (int i in Enumerable.Range(0, arguments.Count))
            {
                if (arguments[i].Count > 0) trimmedArguments.Add(arguments[i]);
            }


            return new FunctionCallSyntax(tokens[0], trimmedArguments);
        }
    }
}
