using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{

    /**
     * 속성 선택문 구문 패턴
     * 
     * 형식: else
     * 
     * @author 김 현준
     */
    class AttributeSyntax : Syntax
    {


        public List<List<Token>> attributes;

        public AttributeSyntax(List<List<Token>> attributes)
        {

            this.attributes = attributes;

        }

        /**
         * 토큰열이 속성 선택문 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            int indexOfLpo = TokenTools.indexOfLpo(tokens);

            if (indexOfLpo < 0)
                return false;

            // 도트가 가장 최하위 연산자이면, 즉 도트를 제외하고 다른 연산자가 없을 때
            if (tokens[indexOfLpo].type == Type.Dot)
                return true;

            return false;
        }

        /**
         * 토큰열을 분석하여 속성 선택문 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static AttributeSyntax analyze(List<Token> tokens, int lineNumber)
        {

            List<List<Token>> attributes = TokenTools.split(tokens, Type.Dot, true);

            foreach (int i in Enumerable.Range(0, attributes.Count))
            {
                if (attributes[i].Count < 1)
                {
                    Debug.reportError("Syntax error 333", "속성이 비었습니다.", lineNumber);
                    return null;
                }
            }

            return new AttributeSyntax(attributes);
        }
    }
}