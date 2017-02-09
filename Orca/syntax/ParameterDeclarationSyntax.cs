using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 파라미터 선언문 구문 패턴
     * 
     * 형식: v:T
     * 
     * @author 김 현준
     */
    class ParameterDeclarationSyntax : Syntax
    {


        public Token parameterName;
        public Token parameterType;

        public ParameterDeclarationSyntax(Token parameterName, Token parameterType)
        {

            this.parameterName = parameterName;
            this.parameterType = parameterType;

        }

        /**
         * 토큰열이 파라미터 선언문 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens.Count == 3)
                if (tokens[0].type == Type.ID && tokens[1].type == Type.Colon && tokens[2].type == Type.ID)
                    return true;
            return false;
        }

        /**
         * 토큰열을 분석하여 파라미터 선언문 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static ParameterDeclarationSyntax analyze(List<Token> tokens, int lineNumber)
        {
            return new ParameterDeclarationSyntax(tokens[0], tokens[2]);
        }
    }
}
