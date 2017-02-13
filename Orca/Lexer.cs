using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca
{
    /**
     * Orca Lexer
     * 
     * 코드를 분석하여 어휘 계층 트리를 생성한다.
     * 
     * @author 김 현준
     */
    class Lexer
    {
        /**
         * 어휘 분석 중인 라인 넘버
         */
        private int processingLine = 1;

        public Lexer()
        {

            // 토큰 정의가 비었다면 예약 어휘를 추가한다.
            if (Token.definitions.Count < 1)

                defineTokens();
        }

        public Lextree analyze(string code)
        {
            processingLine = 1;

            // 어휘 트리를 생성한다.
            Lextree tree = new Lextree(true, processingLine);

            // 문자열 처리 상태 변수를 초기화한다.
            bool isString = false;
            string buffer = "";

            int i = -1;

            while (++i < code.Length)
            {

                char _char = code[i];

                // 줄바꿈 문자일 경우 줄 번호를 하나 증가시킨다.
                if (_char == '\n')
                    processingLine++;

                // 문자열의 시작과 종결 부분을 감지하여 상태를 업데이트한다.
                if (_char == '\"')
                {
                    isString = !isString;
                    buffer += _char;
                    continue;
                }

                // 모든 문자열 정보는 버퍼에 저장한다.
                if (isString)
                {
                    buffer += _char;
                    continue;
                }

                // 주석을 제거한다.
                if (_char == '/' && i + 1 < code.Length)
                {
                    int j = 2;

                    // 단일 행 주석일 경우
                    if (code[i + 1] == '/')
                    {

                        // 문장의 끝(줄바꿈 문자)를 만날 때까지 넘긴다.
                        while (i + j <= code.Length)
                        {
                            if (code[i + (j++)] == '\n')
                                break;
                        }
                        i += j - 1;
                        processingLine++;
                        continue;
                    }

                    // 여러 행 주석일 경우
                    else if (code[i + 1] == '*')
                    {

                        // 종결 문자열 시퀸스('*/')를 만날 때까지 넘긴다.
                        while (i + j < code.Length)
                        {
                            if (code[i + j] == '\n')
                                processingLine++;

                            if (code[i + (j++)] == '*')
                                if (code[i + (j++)] == '/')
                                    break;
                        }
                        i += j - 1;
                        continue;
                    }
                }

                // 세미콜론을 찾으면 진행 상황을 스택에 저장한다.
                if (_char == ';')
                {

                    // 진행 상황을 스택에 저장한다.
                    if (buffer.Length > 0)
                    {
                        Lextree lextree = new Lextree(false, processingLine);
                        lextree.lexData = tokenize(buffer);
                        tree.branch.Add(lextree);
                    }

                    // 버퍼를 초기화한다.
                    buffer = "";
                }

                // 중괄호 열기 문자('{')를 찾으면 괄호로 묶인 그룹을 재귀적으로 처리하여 저장한다.
                else if (_char == '{')
                {

                    // 중괄호 앞의 데이터를 저장한다.
                    if (buffer.Length > 0)
                    {
                        Lextree lextree = new Lextree(false, processingLine);
                        lextree.lexData = tokenize(buffer);
                        tree.branch.Add(lextree);
                    }

                    // 괄호의 끝을 찾는다.
                    int j = 1;
                    int depth = 0;

                    while (i + j <= code.Length)
                    {
                        char __char = code[i + (j++)];
                        if (__char == '{')
                            depth++;
                        else if (__char == '}')
                            depth--;
                        else if (__char == '\n')
                            processingLine++;
                        if (depth < 0)
                            break;
                    }

                    // 괄호의 전체 내용에 대해 구문 분석을 수행한 후, 유닛에 추가한다. (시작, 끝 괄호 제외)
                    Lextree block = analyze(code.Substring(i + 1, i + j - 1));
                    tree.branch.Add(block);

                    // 다음 과정을 준비한다.
                    buffer = "";
                    i += j - 1;
                }

                // 처리하지 않는 문자일 경우 버퍼에 쓴다.
                else
                {
                    buffer += _char;
                }
            }

            // 맨 뒤의 데이터도 쓴다.
            if (buffer.Length > 0)
            {
                Lextree lextree = new Lextree(false, processingLine);
                lextree.lexData = tokenize(buffer);
                tree.branch.Add(lextree);
            }

            // 분석 결과를 리턴한다.
            return tree;
        }

        /**
         * 정의된 토큰 정보를 바탕으로 문자열을 토큰화한다.
         * 
         * @param	code
         * @return
         */
        public List<Token> tokenize(string code)
        {

            List<Token> tokens = new List<Token>();
            string buffer = "";

            char? usingQuote_char = null;

            bool isString = false;
            bool isNumber = false;
            bool isFloat = false;

            int i = -1;

            while (++i < code.Length)
            {

                char _char = code[i];

                // 문자열 처리
                if (((_char == '\"' || _char == '\'') && !isString) || (_char == usingQuote_char && isString))
                {

                    // 처음일 경우
                    if (!isString) usingQuote_char = _char;

                    isString = !isString;

                    // 문자열이 시작되었을때 기존의 버퍼를 저장한다.
                    if (isString)
                    {
                        if (buffer.Length > 0)
                            tokens.Add(Token.findByValue(buffer, true));
                    }

                    // 문자열이 종결되었을 때 문자열 토큰 추가
                    if (!isString)
                        tokens.Add(new Token(Type.String, buffer));

                    // 버퍼 초기화
                    buffer = "";
                    continue;
                }

                if (isString)
                {
                    buffer += _char;
                    continue;
                }

                // 만약 숫자이고, 버퍼의 처음이라면 숫자 리터럴 처리를 시작한다.
                if (char.IsDigit(_char))
                {
                    if (buffer.Length < 1)

                        isNumber = true;

                    if (isNumber)
                    {
                        buffer += _char;
                        continue;
                    }
                }

                // 만약 숫자 리터럴 처리 중 '.'이 들어온다면 소수점 처리를 해 준다.
                if (isNumber && _char == '.')
                {

                    // .이 여러번 쓰였다면, .을 여러 번 쓴 게 어떤 의미가 있는 것이다.		
                    if (isFloat)
                    {
                        tokens.Add(new Token(Type.Number, buffer.Substring(0, buffer.Length - 1)));

                        // 버퍼 초기화
                        buffer = "";
                        isNumber = false;
                        isFloat = false;
                        i -= 2;
                        continue;
                    }
                    else
                    {
                        isFloat = true;
                        buffer += _char;
                        continue;
                    }
                }

                // 만약 그 외의 문자가 온다면 숫자 리터럴을 종료한다.
                if (isNumber)
                {

                    tokens.Add(new Token(Type.Number, buffer));

                    // 버퍼 초기화
                    buffer = "";
                    isNumber = false;
                    isFloat = false;
                }

                // 공백 문자가 나오면 토큰을 분리한다.
                if (_char == ' ' || _char == '	' || (int)_char == 10 || (int)_char == 13)
                {

                    Token token = Token.findByValue(buffer.Trim(), true);

                    if (buffer.Length > 0 && token != null)
                        tokens.Add(token);

                    // 버퍼 초기화
                    buffer = "";
                    continue;
                }

                // 토큰 분리 문자의 존재 여부를 검사한다.
                else if (i < code.Length)
                {

                    // 토큰을 찾는다.
                    Token result = Token.findByValue(code.Substring(i, (i + 2 < code.Length ? i + 3 : (i + 1 < code.Length ? i + 2 : i + 1))-i), false);

                    // 만약 토큰이 존재한다면,
                    if (result != null)
                    {

                        // 토큰을 이루는 문자만큼 건너 뛴다.
                        i += result.value.Length - 1;

                        // 버퍼를 쓴다

                        Token token = Token.findByValue(buffer.Trim(), true);
                        if (buffer.Length > 0 && token != null)
                            tokens.Add(token);


                        Token previousToken = null;
                        bool previousTarget = false;

                        if (tokens.Count > 0)
                            previousToken = tokens[tokens.Count - 1];
                        else
                            previousTarget = false;


                        // 더하기 연산자의 경우 앞에 더할 대상이 존재
                        if (tokens.Count > 0 && (previousToken.type == Type.ID || previousToken.type == Type.Number || previousToken.type == Type.String || previousToken.type == Type.ArrayClose || previousToken.type == Type.ShellClose))
                        {
                            previousTarget = true;
                        }

                        // 연산자 수정
                        if (result.type == Type.Addition && !previousTarget)
                            result = Token.findByType(Type.UnraryPlus);
                        else if (result.type == Type.UnraryPlus && previousTarget)
                            result = Token.findByType(Type.Addition);
                        else if (result.type == Type.Subtraction && !previousTarget)
                            result = Token.findByType(Type.UnraryMinus);
                        else if (result.type == Type.UnraryMinus && previousTarget)
                            result = Token.findByType(Type.Subtraction);
                        else if (result.type == Type.SuffixIncrement && !previousTarget)
                            result = Token.findByType(Type.PrefixIncrement);
                        else if (result.type == Type.PrefixIncrement && previousTarget)
                            result = Token.findByType(Type.SuffixIncrement);
                        else if (result.type == Type.SuffixDecrement && !previousTarget)
                            result = Token.findByType(Type.PrefixDecrement);
                        else if (result.type == Type.PrefixDecrement && previousTarget)
                            result = Token.findByType(Type.SuffixDecrement);

                        // 발견된 토큰을 쓴다
                        tokens.Add(result);

                        // 버퍼 초기화
                        buffer = "";
                        continue;
                    }
                }

                // 버퍼에 현재 문자를 쓴다
                buffer += _char;
            }

            // 버퍼가 남았다면 마지막으로 써 준다
            if (isNumber)
            {
                tokens.Add(new Token(Type.Number, buffer));
            }
            else
            {
                Token token = Token.findByValue(buffer.Trim(), true);
                if (buffer.Length > 0 && token != null)
                    tokens.Add(token);
            }

            if (isString)
                Debug.reportError("Syntax error", "insert \" to complete expression", processingLine);

            return tokens;
        }

        /**
         * 어휘 분석에 사용될 토큰을 정의한다.
         */
        public void defineTokens()
        {

            Token.define(null, Type.String);
            Token.define(null, Type.Number);
            Token.define(null, Type.Array);
            Token.define(null, Type.CastToNumber);
            Token.define(null, Type.CastToString);
            Token.define(null, Type.Append);
            Token.define(null, Type.AppendAssignment);
            Token.define(null, Type.ArrayReference);
            Token.define(null, Type.Instance);
            Token.define(null, Type.CharAt);
            Token.define(null, Type.PushParameters);

            Token.define("include", Type.Include, true);
            Token.define("define", Type.Define, true);
            Token.define("var", Type.Variable, true);
            Token.define("if", Type.If, true);
            Token.define("else", Type.Else, true);
            Token.define("for", Type.For, true);
            Token.define("while", Type.While, true);
            Token.define("continue", Type.Continue, true);
            Token.define("break", Type.Break, true);
            Token.define("return", Type.Return, true);
            Token.define("new", Type.New, true);
            Token.define("true", Type.True, true);
            Token.define("false", Type.False, true);
            Token.define("as", Type.As, true);
            Token.define("in", Type.In, true);

            Token.define("->", Type.Right, false);
            Token.define("[", Type.ArrayOpen, false);
            Token.define("]", Type.ArrayClose, false);
            Token.define("{", Type.BlockOpen, false);
            Token.define("}", Type.BlockClose, false);
            Token.define("(", Type.ShellOpen, false);
            Token.define(")", Type.ShellClose, false);
            Token.define("?", Type.RuntimeValueAccess, false);
            Token.define("...", Type.From, false);
            Token.define(".", Type.Dot, false);
            Token.define(",", Type.Comma, false);
            Token.define(":", Type.Colon, false);
            Token.define(";", Type.Semicolon, false);
            Token.define("++", Type.PrefixIncrement, false, Affix.PREFIX);
            Token.define("--", Type.PrefixDecrement, false, Affix.PREFIX);
            Token.define("++", Type.SuffixIncrement, false, Affix.SUFFIX);
            Token.define("--", Type.SuffixDecrement, false, Affix.SUFFIX);
            Token.define("+", Type.UnraryPlus, false, Affix.PREFIX);
            Token.define("-", Type.UnraryMinus, false, Affix.PREFIX);
            Token.define("=", Type.Assignment, false);
            Token.define("+=", Type.AdditionAssignment, false);
            Token.define("-=", Type.SubtractionAssignment, false);
            Token.define("*=", Type.MultiplicationAssignment, false);
            Token.define("/=", Type.DivisionAssignment, false);
            Token.define("%=", Type.ModuloAssignment, false);
            Token.define("&=", Type.BitwiseAndAssignment, false);
            Token.define("^=", Type.BitwiseXorAssignment, false);
            Token.define("|=", Type.BitwiseOrAssignment, false);
            Token.define("<<=", Type.BitwiseLeftShiftAssignment, false);
            Token.define(">>=", Type.BitwiseRightShiftAssignment, false);
            Token.define("==", Type.EqualTo, false);
            Token.define("!=", Type.NotEqualTo, false);
            Token.define(">", Type.GreaterThan, false);
            Token.define(">=", Type.GreaterThanOrEqualTo, false);
            Token.define("<", Type.LessThan, false);
            Token.define("<=", Type.LessThanOrEqualTo, false);
            Token.define("+", Type.Addition, false);
            Token.define("-", Type.Subtraction, false);
            Token.define("*", Type.Multiplication, false);
            Token.define("/", Type.Division, false);
            Token.define("%", Type.Modulo, false);
            Token.define("!", Type.LogicalNot, false, Affix.PREFIX);
            Token.define("not", Type.LogicalNot, true, Affix.PREFIX);
            Token.define("&&", Type.LogicalAnd, false);
            Token.define("and", Type.LogicalAnd, true);
            Token.define("||", Type.LogicalOr, false);
            Token.define("or", Type.LogicalOr, true);
            Token.define("~", Type.BitwiseNot, false, Affix.PREFIX);
            Token.define("&", Type.BitwiseAnd, false);
            Token.define("|", Type.BitwiseOr, false);
            Token.define("^", Type.BitwiseXor, false);
            Token.define("<<", Type.BitwiseLeftShift, false);
            Token.define(">>", Type.BitwiseRightShift, false);
        }

        /**
         * 어휘 분석이 끝난 계층 트리의 구조를 보여준다.
         * 
         * @param units
         * @param level
         */
        public void viewHierarchy(Lextree tree, int level)
        {

            string space = "";

            foreach (int i in Enumerable.Range(0,level))
                space += "      ";

            foreach (int i in Enumerable.Range(0,tree.branch.Count))
            {

                // 새 가지일 때
                if (tree.branch[i].hasBranch)
                {
                    //Sys.print(space + "<begin>\n");

                    viewHierarchy(tree.branch[i], level + 1);
                    //Sys.print(space + "<end>\n");
                }

                // 어휘 데이터일 때
                else
                {
                    if (tree.branch[i].lexData.Count < 1)
                        continue;


                    string buffer = "";
                    foreach (int j in Enumerable.Range(0,tree.branch[i].lexData.Count))
                    {
                        Token token = tree.branch[i].lexData[j];
                        buffer += token.value.Trim() + "@" + token.type;
                        if (j != tree.branch[i].lexData.Count - 1) buffer += ",  ";
                    }
                    //Sys.print(space + buffer + "\n");
                }
            }
        }
    }


    /**
     * 어휘 트리
     */
    class Lextree
    {
        /**
         * 파생 가지가 있는지의 여부
         */
        public bool hasBranch = false;

        /**
         * 파생 가지
         */
        public List<Lextree> branch;

        /**
         * 어휘 데이터 (잎사귀)
         */
        public List<Token> lexData;

        /**
         * 컴파일 시 에러 출력에 사용되는 라인 넘버
         */
        public int lineNumber = 1;

        public Lextree(bool hasBranch, int lineNumber)
        {

            this.hasBranch = hasBranch;
            this.lineNumber = lineNumber;

            if (hasBranch)
                branch = new List<Lextree>();
        }
    }
}