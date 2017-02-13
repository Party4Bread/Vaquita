using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 배열 구문 패턴
     * 
     * 형식: [A, B, C, D, ... , Z]
     * 
     * @author 김 현준
     */
    class ArraySyntax : Syntax
    {
        public List<List<Token>> elements;

        public ArraySyntax(List<List<Token>> elements)
        {
            this.elements = elements;
        }

        /**
         * 토큰열이 배열 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens[0].type == Type.ArrayOpen)
                if (TokenTools.indexOfArrayClose(tokens) == tokens.Count - 1)
                    return true;
            return false;
        }

        /**
         * 토큰열을 분석하여 배열 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static ArraySyntax analyze(List<Token> tokens, int lineNumber)
        {
            List<List<Token>> elements = TokenTools.getArguments(tokens.GetRange(1, tokens.Count - 1 - 1));

            ArraySyntax syntax = new ArraySyntax(elements);
            return syntax;
        }
    }
}