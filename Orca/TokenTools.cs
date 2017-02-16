using Orca;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca
{
    /**
     * Orca Token Untility
     * 
     * @author 김 현준
     */
    class TokenTools
    {

        /**
         * 토큰열에서 매개변수열을 취득한다.
         * 
         * @param	tokens
         * @return
         */
        public static List<List<Token>> getArguments(List<Token> tokens)
        {
            return split(tokens, Type.Comma, true);
        }

        /**
         * 토큰열을 구분자를 기준으로 분리한다.
         * 
         * sensitive 플래그가 참일 경우, 소, 중괄호 뎁스가 모두 최상위일 경우에만 구분자를
         * 분리한다.
         * 
         * @param tokens
         * @param delimiter
         * @param sensitive
         * @return
         */
        public static List<List<Token>> split(List<Token> tokens, Type delimiter, bool sensitive = false)
        {

            // 유효 범위 내에 있는 나열을 구한다.
            int i = 0;
            int subscriptDepth = 0;
            int shellDepth = 0;
            // 원소
            List<List<Token>> elements = new List<List<Token>>();
            int lastIndex = i - 1;
            int elementIndex = 0;

            // 현재 스코프에서 유효한 매개변수 구분 문자를 찾는다.
            while (i < tokens.Count)
            {
                if (tokens[i].type == Type.ArrayOpen)
                    subscriptDepth++;
                else if (tokens[i].type == Type.ArrayClose)
                    subscriptDepth--;
                else if (tokens[i].type == Type.ShellOpen)
                    shellDepth++;
                else if (tokens[i].type == Type.ShellClose)
                    shellDepth--;
                else if (tokens[i].type == delimiter && ((subscriptDepth == 0 && shellDepth == 0) || !sensitive))
                {
                    elements.Insert(elementIndex++,tokens.GetRange(lastIndex + 1, i-(lastIndex + 1)));
                    lastIndex = i;
                }
                i++;
            }
            //elements[elementIndex++] = (tokens.GetRange(lastIndex + 1, tokens.Count-(lastIndex + 1)-1));
            elements.Insert(elementIndex++,(tokens.GetRange(lastIndex + 1, tokens.Count - (lastIndex + 1))));
            return elements;
        }

        /**
         * 토큰을 껍데기가 둘러싸고 있을 경우 벗긴다.
         * 
         * @param tokens
         * @return
         */
        public static List<Token> pill(List<Token> tokens)
        {
            if (tokens[0].type == Type.ShellOpen)
                if (indexOfShellClose(tokens, 1) == tokens.Count - 1)
                    return tokens.GetRange(1, tokens.Count - 1 - 1);
            return tokens;
        }

        /**
         * 토큰열에서 주어진 토큰 타입의 인덱스를 찾는다.
         * 
         * @param tokens
         * @param type
         * @param start
         * @return
         */
        public static int indexOf(List<Token> tokens, Type type, int start = 0)
        {
            foreach (int i in Enumerable.Range(start, tokens.Count))
                if (tokens[i].type == type)
                    return i;
            return -1;
        }

        /**
         * 토큰열에서 주어진 토큰 타입의 마지막 인덱스를 찾는다.
         * 
         * @param	tokens
         * @param	type
         * @param	start
         * @return
         */
        public static int lastIndexOf(List<Token> tokens, Type type, int start = 0)
        {
            foreach (int i in Enumerable.Range(0, tokens.Count - start))
                if (tokens[tokens.Count - 1 - i].type == type)
                    return tokens.Count - 1 - i;
            return -1;
        }

        /**
         * 유효한 껍데기 닫기 문자의 위치를 찾는다.
         * 
         * 단, 반드시 start는 첫 괄호의 인덱스보다 커야 한다.
         * 
         * @param tokens
         * @param start
         * @return
         */
        public static int indexOfShellClose(List<Token> tokens, int start = 1)
        {
            return indexOfClose(tokens, Type.ShellOpen, Type.ShellClose, start);
        }

        /**
         * 유효한 껍데기 닫기 문자의 위치를 찾는다.
         * 
         * 단, 반드시 start는 첫 괄호의 인덱스보다 커야 한다.
         * 
         * @param tokens
         * @param start
         * @return
         */
        public static int indexOfArrayClose(List<Token> tokens, int start = 1)
        {
            return indexOfClose(tokens, Type.ArrayOpen, Type.ArrayClose, start);
        }

        /**
         * 쌍이 있는 문자에서 닫기 문자의 위치를 반환한다.
         * 
         * @param	tokens
         * @param	open
         * @param	close
         * @param	start
         */
        public static int indexOfClose(List<Token> tokens, Type open, Type close, int start = 1)
        {

            int depth = 1;

            // 시작점부터 체크한다.
            foreach (int i in Enumerable.Range(start, tokens.Count))
            {

                // 껍데기 열기 문자를 만나면 뎁스를 증가시킨다.
                if (tokens[i].type == open)
                    depth++;

                // 껍데기 닫기 문자를 만나면 뎁스를 감소시킨다.
                else if (tokens[i].type == close)
                    depth--;

                // 유효하면
                if (depth == 0)
                    return i;
            }

            return -1;
        }

        /**
         * 최하위 우선순위의 연산자(lowest precedence operator, LPO)의 유효한 위치를 찾는다.
         * 
         * @param tokens
         * @param start
         * @return
         */
        public static int indexOfLpo(List<Token> tokens, int targetDepth = 0, int start = 0)
        {

            int shellDepth = 0;
            int subscriptDepth = 0;

            // 후보 토큰의 우선순위와 위치를 저장하기 위한 플래그
            int candidatePrecedence = 0;
            int candidateIndex = -1;

            foreach (int i in Enumerable.Range(start, tokens.Count))
            {

                // 중간에 괄호 부분(비유효 구간) 이 나오면 건너뛴다.
                if (tokens[i].type == Type.ShellOpen)
                    shellDepth++;
                else if (tokens[i].type == Type.ShellClose)
                    shellDepth--;

                // 유효 구간에서 연산자가 발견되면 후보 여부를 검토한다.
                if (targetDepth == shellDepth && subscriptDepth == 0)
                {

                    int precedence = tokens[i].getPrecedence();

                    if (candidatePrecedence <= precedence)
                    {
                        candidatePrecedence = precedence;
                        candidateIndex = i;
                    }
                }

                if (tokens[i].type == Type.ArrayOpen)
                    subscriptDepth++;
                else if (tokens[i].type == Type.ArrayClose)
                    subscriptDepth--;
            }

            return candidatePrecedence == 0 ? -1 : candidateIndex;
        }

        /**
         * 2차원 배열을 1차원으로 편다.
         * 
         * @param	args
         * @return
         */
        public static List<Token> merge(List<List<Token>> args)
        {
            List<Token> result = new List<Token>();
            foreach (int i in Enumerable.Range(0, args.Count))
            {
                foreach (int j in Enumerable.Range(0, args[i].Count))
                {
                    result.Add(args[i][j]);
                }
            }
            return result;
        }

        /**
         * 토큰이 스택에 쌓이지 않는 형태인지 확인한다.
         * 
         * @param	tokens
         * @return
         */
        public static bool checkStackSafety(List<Token> tokens)
        {

            // 대입 연산자가 있는지 체크한다.		
            foreach (int i in Enumerable.Range(0, tokens.Count))
            {
                if (tokens[i].getPrecedence() > 15)
                    return true;
            }

            // 증감 연산자는 끝에 토큰 추가하지 않기 명령을 추가하고, 다른 연산자가 나오면 false 리턴
            foreach (int i in Enumerable.Range(0, tokens.Count))
            {
                if (tokens[i].getPrecedence() == 0 || tokens[i].type == Type.RuntimeValueAccess)
                    continue;

                if (tokens[i].type == Type.PrefixDecrement ||
                 tokens[i].type == Type.PrefixIncrement ||
                 tokens[i].type == Type.SuffixDecrement ||
                 tokens[i].type == Type.SuffixIncrement)
                {
                    tokens[i].doNotPush = true;
                    continue;
                }

                return false;
            }

            return true;
        }


        /**
         * 1차원 토큰열의 내용을 출력한다.
         * 
         * @param	tokens
         */
        public static void view1D(List<Token> tokens)
        {
            string buffer = "[";

            foreach (int i in Enumerable.Range(0, tokens.Count))
            {

                if (tokens[i].value != null)
                    buffer += tokens[i].value.Trim() + "@" + tokens[i].type;
                else
                    buffer += "@" + tokens[i].type;

                if (i != tokens.Count - 1) buffer += ",  ";
            }
            buffer += "]";
            Debug.print(buffer);
        }

        /**
         * 2차원 토큰열의 내용을 출력한다.
         * @param	tokens
         */
        public static void view2D(List<List<Token>> tokens)
        {
            foreach (int i in Enumerable.Range(0, tokens.Count))

                view1D(tokens[i]);

        }
    }
}