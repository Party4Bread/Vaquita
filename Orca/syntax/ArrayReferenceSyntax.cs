using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 배열 참조 구문 패턴
     * 
     * 형식: A[B][C][D]...[Z]
     * 
     * @author 김 현준
     */
    class ArrayReferenceSyntax : Syntax
    {


        public Token array;
        public List<List<Token>> references;

        public ArrayReferenceSyntax(Token array, List<List<Token>> references)
        {

            this.array = array;
            this.references = references;

        }

        /**
         * 토큰열이 배열 참조 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {

            int indexOfLpo = TokenTools.indexOfLpo(tokens);

            if (indexOfLpo < 0)
                return false;

            if (tokens[indexOfLpo].type != Type.ArrayOpen)
                return false;

            return true;
        }

        /**
         * 토큰열을 분석하여 배열 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static ArrayReferenceSyntax analyze(List<Token> tokens, int lineNumber)
        {

            int depth = 0;
            List<int> seperations = new List<int>();

            foreach (int i in Enumerable.Range(0, tokens.Count))
            {
                if (tokens[i].type == Type.ArrayOpen)
                {
                    if (depth == 0)
                        seperations.Add(i);
                    depth++;
                }
                else if (tokens[i].type == Type.ArrayClose)
                {
                    if (depth == 1)
                        seperations.Add(i);
                    depth--;
                }
            }

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

            // 대상 변수의 타입이 ID가 아닐 경우 에러 발생
            if (tokens[0].type != Type.ID)
            {
                Debug.reportError("Syntax error", "The type of the expression must be an array type.", lineNumber);
                return null;
            }

            // 배열의 인덱스 배열이 저장될 공간
            List<List<Token>> references = new List<List<Token>>();

            int indexCount = 0;
            int indexStart = 0;

            // 데이터를 뽑아 낸다
            foreach (int i in Enumerable.Range(0, seperations.Count))
            {
                if (i % 2 == 0)
                    indexStart = seperations[i] + 1;
                else
                    references[indexCount++] = tokens.GetRange(indexStart, seperations[i] - indexStart);
            }

            // 레퍼런스를 리턴한다.
            return new ArrayReferenceSyntax(tokens[0], references);
        }
    }
}