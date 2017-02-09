using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 클래스 선언문 구문 패턴
     * 
     * 형식: class A
     * 
     * @author 김 현준
     */
    class ClassDeclarationSyntax : Syntax
    {


        public Token className;

        public ClassDeclarationSyntax(Token className)
        {

            this.className = className;

        }

        /**
         * 토큰열이 클래스 선언 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens.Count == 2 && tokens[0].type == Type.Define && tokens[1].type == Type.ID)
                return true;
            return false;
        }

        /**
         * 토큰열을 분석하여 클래스 선언 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static ClassDeclarationSyntax analyze(List<Token> tokens, int lineNumber)
        {

            // 토큰의 길이를 검사한다.
            if (tokens.Count != 2)
            {
                Debug.reportError("Syntax error", "structure declaration syntax is not valid.", lineNumber);
                return null;
            }

            return new ClassDeclarationSyntax(tokens[1]);
        }
    }
}
