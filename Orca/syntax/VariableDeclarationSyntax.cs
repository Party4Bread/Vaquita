using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 변수 선언 구문 패턴
     * 
     * 형식: var A:T = I
     * 
     * @author 김 현준
     */
    class VariableDeclarationSyntax : Syntax
    {

        // 기본 변수 정보
        public Token variableName;
        public Token variableType;

        // 변수의 초기화 정보
        public List<Token> initializer;

        public VariableDeclarationSyntax(Token variableName, Token variableType, List<Token> initializer = null)
        {

            this.variableName = variableName;
            this.variableType = variableType;
            this.initializer = initializer;

        }

        /**
         * 토큰열이 변수 선언 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens.Count > 0 && tokens[0].type == Type.Variable)
                return true;
            return false;
        }

        /**
         * 토큰열을 분석하여 변수 선언 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static VariableDeclarationSyntax analyze(List<Token> tokens, int lineNumber) {

            if (tokens.Count < 3)
            {
                Debug.reportError("Syntax error", "Variable declaration syntax is too short", lineNumber);
                return null;
            }

            // 식별자가 ID형식인지 검사한다.
            if (tokens[1].type != Type.ID)
            {
                Debug.reportError("Syntax error", "Variable name is not valid", lineNumber);
                return null;
            }

            // 콜론의 사용이 올바른지 체크한다.
            if (tokens[2].type != Type.Colon)
            {
                Debug.reportError("Syntax error", "Colon is needed to set type of the variable", lineNumber);
                return null;
            }

            // 변수 타입이 ID형식인지 검사한다.
            if (tokens[3].type != Type.ID)
            {
                Debug.reportError("Syntax error", "Variable type is not valid", lineNumber);
                return null;
            }

            // 변수의 초기화문이 존재하는지 확인한다.
            bool hasInitializer = false;
            if (tokens.Count > 5)
            {

                // 초기화문의 형식을 갖추었을 경우
                if (tokens[4].type == Type.Assignment)
                {
                    hasInitializer = true;
                }

                // 길기만 하고 형식은 잘못되었을 경우
                else
                {
                    TokenTools.view1D(tokens);
                    Debug.reportError("Syntax error", "Variable initializing statement is not valid", lineNumber);
                    return null;
                }
            }
            else if (tokens.Count == 5)
            {
                Debug.reportError("Syntax error", "Variable declaration has meaningless words at the end", lineNumber);
                return null;
            }

            List<Token> result = null;
            if (hasInitializer)
            {
                result = tokens.GetRange(4, tokens.Count);
                result.Insert(0, tokens[1]);
            }
            return new VariableDeclarationSyntax(tokens[1], tokens[3], hasInitializer ? result : null);
        }
    }
}
