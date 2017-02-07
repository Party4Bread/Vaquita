using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 캐스팅 구문 패턴
     * 
     * 형식 A as B
     * 
     * @author 김 현준
     */
    class CastingSyntax : Syntax
    {

        // 캐스팅 될 대상
        public List<Token> target;

        // 캐스팅 종류
        public string castingType;

        public CastingSyntax(List<Token> target, string castingType)
        {

            this.target = target;
            this.castingType = castingType;

        }

        /**
         * 토큰열이 캐스팅 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            int indexOfLPO = TokenTools.indexOfLpo(tokens);

            if (indexOfLPO < 0)
                return false;

            if (tokens[indexOfLPO].type != Type.As)
                return false;

            return true;
        }

        /**
         * 토큰열을 분석하여 캐스팅 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static CastingSyntax analyze(List<Token> tokens, int lineNumber)
        {
            int indexOfLpo = TokenTools.indexOfLpo(tokens);

            // 캐스팅 대상이 없다면
            if (tokens.Count <= indexOfLpo + 1)
            {
                Debug.reportError("Syntax error", "Cannot find casting target.", lineNumber);
                return null;
            }

            List<Token> target = tokens.GetRange(0, indexOfLpo);
            string castingType = tokens[indexOfLpo + 1].value;

            return new CastingSyntax(target, castingType);
        }
    }
}