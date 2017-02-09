using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{
    /**
     * 인클루드문 구문 패턴
     * 
     * 형식: include "code.orca";
     * 
     * @author 김 현준
     */
    class IncludeSyntax : Syntax
    {


        public string targetFile;

        public IncludeSyntax(string targetFile)
        {

            this.targetFile = targetFile;

        }

        /**
         * 토큰열이 인클루드문 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {
            if (tokens.Count < 1)
                return false;

            if (tokens[0].type != Type.Include)
                return false;
            return true;
        }

        /**
         * 토큰열을 분석하여 인클루드문 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static IncludeSyntax analyze(List<Token> tokens, int lineNumber)
        {

            if (tokens.Count != 2)
            {
                Debug.reportError("Syntax error", "Not valid include syntax", lineNumber);
                return null;
            }

            if (tokens[1].type != Type.String)
            {
                Debug.reportError("Syntax error", "Include target must be string", lineNumber);
                return null;
            }

            return new IncludeSyntax(tokens[1].value);
        }

    }
}
