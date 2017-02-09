﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.syntax
{

    /**
     * 접두형 단항 연산 구문 패턴
     * 
     * 형식: A (OP)
     * 
     * @author 김 현준
     */
    class PrefixSyntax : Syntax
    {


        public List<Token> operand;
        public Token _operator;


        public PrefixSyntax(Token _operator, List<Token> operand)
        {
            this._operator = _operator;
            this.operand = operand;
        }

        /**
         * 토큰열이 접두형 단항 연산 구문 패턴과 일치하는지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool match(List<Token> tokens)
        {

            int indexOfLPO = TokenTools.indexOfLpo(tokens);

            if (indexOfLPO < 0)
                return false;

            if (tokens[indexOfLPO].isPrefix())
                return true;

            return false;
        }

        /**
         * 토큰열을 분석하여 접두형 단항 연산 구문 요소를 추출한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        public static PrefixSyntax analyze(List<Token> tokens, int lineNumber)
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

            return new PrefixSyntax(tokens[indexOfLpo], tokens.GetRange(indexOfLpo + 1, tokens.Count));
        }
    }
}
