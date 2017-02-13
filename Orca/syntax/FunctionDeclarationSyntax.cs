using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 함수 선언 구문 패턴
     * 
     * 형식: define Target.name(parameters) -> returnType
     * 
     * @author 김 현준
     */
    class FunctionDeclarationSyntax : Syntax
    {


        public Token functionName;
        public Token returnType;
        public List<List<Token>> parameters;

        public FunctionDeclarationSyntax(Token functionName, Token returnType, List<List<Token>> parameters)
        {

            this.functionName = functionName;
            this.returnType = returnType;
            this.parameters = parameters;

        }

        /**
         * 토큰열이 함수 선언 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {

            // 기본적인 길이 조건을 체크한다.
            if (tokens.Count < 3)
                return false;

            // 최소 패턴을 검사한다.
            if (tokens[0].type != Type.Define || tokens[1].type != Type.ID)
                return false;

            return true;
        }

        /**
         * 토큰열을 분석하여 함수 선언 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static FunctionDeclarationSyntax analyze(List<Token> tokens, int lineNumber)
        {

            Token functionName = null;
            List<List<Token>> parameters = null;
            Token returnType = null;

            // 타겟형과 일반형을 구분한다.
            if (tokens[2].type == Type.Dot)
            {

                functionName = tokens[3];
                List<Token> functionTarget = new List<Token>();
                functionTarget.Add(new Token(Type.ID, "this"));
                functionTarget.Add(Token.findByType(Type.Colon));
                functionTarget.Add(tokens[1]);

                // 길이 조건을 새로 체크한다.
                if (tokens.Count < 5)
                {
                    Debug.reportError("Syntax error", "Unexpected Token.", lineNumber);
                    return null;
                }

                if (tokens[3].type != Type.ID)
                {
                    Debug.reportError("Syntax error", "함수에 이름이 없습니다.", lineNumber);
                    return null;
                }

                if (tokens[4].type != Type.ShellOpen)
                {
                    Debug.reportError("Syntax error", "(가 필요합니다.", lineNumber);
                    return null;
                }

                int indexOfShellClose = TokenTools.indexOfShellClose(tokens, 5);

                if (indexOfShellClose < 0)
                {
                    Debug.reportError("Syntax error", "괄호가 닫히지 않았습니다.", lineNumber);
                    return null;
                }

                // 리턴 타입 있음
                if (indexOfShellClose != tokens.Count - 1)
                {

                    // 길이 조건을 새로 체크한다.
                    if (tokens.Count <= indexOfShellClose + 2 || indexOfShellClose + 2 != tokens.Count - 1)
                    {
                        Debug.reportError("Syntax error", "Unexpected Token.", lineNumber);
                        return null;
                    }

                    if (tokens[tokens.Count - 2].type != Type.Right || tokens[tokens.Count - 1].type != Type.ID)
                    {
                        Debug.reportError("Syntax error", "Unexpected Token.", lineNumber);
                        return null;
                    }

                    returnType = tokens[tokens.Count - 1];
                }

                // 리턴 타입 없음
                else
                {
                    returnType = new Token(Type.ID, "void");
                }

                // 파라미터를 취득한다.
                parameters = TokenTools.split(tokens.GetRange(5, indexOfShellClose-5), Type.Comma, true);

                // 타겟을 파라미터의 첫 번째에 추가한다.
                parameters.Insert(0, functionTarget);
            }

            // 일반형 define name(parameters)->type
            else
            {

                functionName = tokens[1];

                // 길이 조건을 다시 체크한다.
                if (tokens.Count < 4)
                {
                    Debug.reportError("Syntax error", "Unexpected Token.", lineNumber);
                    return null;
                }

                if (tokens[2].type != Type.ShellOpen)
                {
                    Debug.reportError("Syntax error", "(가 필요합니다.", lineNumber);
                    return null;
                }

                int indexOfShellClose = TokenTools.indexOfShellClose(tokens, 3);

                if (indexOfShellClose < 0)
                {
                    Debug.reportError("Syntax error", "괄호가 닫히지 않았습니다.", lineNumber);
                    return null;
                }

                // 리턴 타입 있음
                if (indexOfShellClose != tokens.Count - 1)
                {

                    // 길이 조건을 새로 체크한다.
                    if (tokens.Count <= indexOfShellClose + 2 || indexOfShellClose + 2 != tokens.Count - 1)
                    {
                        Debug.reportError("Syntax error", "Unexpected Token.", lineNumber);
                        return null;
                    }

                    if (tokens[tokens.Count - 2].type != Type.Right || tokens[tokens.Count - 1].type != Type.ID)
                    {
                        Debug.reportError("Syntax error", "Unexpected Token.", lineNumber);
                        return null;
                    }

                    returnType = tokens[tokens.Count - 1];
                }

                // 리턴 타입 없음
                else
                {
                    returnType = new Token(Type.ID, "void");
                }

                // 파라미터를 취득한다.
                parameters = TokenTools.split(tokens.GetRange(3, indexOfShellClose-3), Type.Comma, true);
            }

            List<List<Token>> trimmedParameters = new List<List<Token>>();

            foreach (int i in Enumerable.Range(0, parameters.Count))
            {
                if (parameters[i].Count > 0) trimmedParameters.Add(parameters[i]);
            }

            return new FunctionDeclarationSyntax(functionName, returnType, trimmedParameters);
        }
    }
}
