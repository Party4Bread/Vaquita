using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 이항 연산자 구문 패턴
     * 
     * 형식: A (OP) B
     * 
     * @author 김 현준
     */
    class InfixSyntax : Syntax
    {


        public List<Token> left;
        public List<Token> right;
        public Token _operator;


        public InfixSyntax(Token _operator, List<Token> left, List<Token> right)
        {
            this.left = left;
            this.right = right;
            this._operator = _operator;
        }

        /**
         * 토큰열이 이항 연산자 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            int indexOfLPO = TokenTools.indexOfLpo(tokens);
            if (indexOfLPO < 0)
                return false;

            if (!tokens[indexOfLPO].isPrefix() && !tokens[indexOfLPO].isSuffix())
                return true;

            return false;
        }

        /**
         * 토큰열을 분석하여 이항 연산자 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static InfixSyntax analyze(List<Token> tokens, int lineNumber)
        {

            int indexOfLpo = TokenTools.indexOfLpo(tokens);

            int depth = 0;
            foreach (int i in Enumerable.Range(0, tokens.Count))
            {
                if (tokens[i].type == Type.ShellOpen)
                    depth++;
                else if (tokens[i].type == Type.ShellClose)
                    depth--;
            }

            // 껍데기가 온전히 닫혀 있는지 검사한다.
            if (depth > 0)
            {

                Debug.reportError("Syntax error", "insert \")\" to complete Expression", lineNumber);
                return null;
            }

            if (depth < 0)
            {
                Debug.reportError("Syntax error", "delete \"(\"", lineNumber);
                return null;
            }

            // 연산자 취득
            Token _operator = tokens[indexOfLpo];

            // 좌항과 우항
            List<Token> left = tokens.GetRange(0, indexOfLpo);

            List<Token> right = tokens.GetRange(indexOfLpo + 1, tokens.Count);

            return new InfixSyntax(_operator, left, right);
        }
    }
}
