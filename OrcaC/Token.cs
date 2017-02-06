using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orca.symbol;
namespace Orca
{
    class Token
    {
        public static List<Token> definitions = new List<Token>();

        public Type type;
        public string value;
        public Affix? affix;

        /**
         * 토큰 태그 정보
         */
        private Symbol tag;
        public bool tagged = false;

        /**
         * 어휘 분석 시 단어별로 처리할 것인지의 여부
         */
        public bool wholeWord = false;
        public bool useAsAddress = false;
        public bool useAsArrayReference = false;
        public bool doNotPush = false;

        /**
         * 토큰을 생성한다. 두 번째 인수를 넣을 경우 이 토큰은 데이터 토큰이 된다.
         * @param	type
         */
        public Token(Type type)
        {
            this.type = type;
            this.value = null;
        }

        public Token(Type type, string value)
        {
            this.type = type;
            this.value = value;
        }

        /**
         * 토큰의 종류가 단항 접두사인지 확인한다.
         * 
         * @return
         */
        public bool isPrefix()
        {
            if (affix == Affix.PREFIX)
                return true;
            return false;
        }

        /**
         * 토큰의 종류가 단항 접미사인지 확인한다.
         * 
         * @return
         */
        public bool isSuffix()
        {
            if (affix == Affix.SUFFIX)
                return true;
            return false;
        }

        /**
         * 토큰의 태그를 취득한다.
         * 
         * @return
         */
        public Symbol getTag()
        {
            return tag;
        }

        /**
         * 토큰에 태그를 설정한다.
         * 
         * @param	tag
         * @return
         */
        public void setTag(Symbol tag)
        {

            this.tag = tag;

            tagged = true;

        }

        /**
         * 토큰에서 태그를 제거한다.
         */
        public void removeTag()
        {

            this.tag = null;
            this.tagged = false;

        }

        public Token copy()
        {
            Token token = new Token(type, value);
            token.affix = affix;
            token.tag = tag;
            token.tagged = tagged;
            token.wholeWord = wholeWord;
            token.useAsAddress = useAsAddress;
            token.useAsArrayReference = useAsArrayReference;
            token.doNotPush = doNotPush;

            return token;
        }


        /**
         * 현재 토큰이 연산자일 경우, 연산자의 우선순위를 구한다.
         * 
         * ※ 연산자 우선순위는 C++의 것을 따른다.
         * 
         * @param token
         * @return
         */
        public int getPrecedence()
        {
            switch (type)
            {
                case Type.Dot:
                    return 1;
                case Type.ArrayOpen:
                    return 2;
                case Type.As:
                    return 3;
                case Type.SuffixIncrement:
                case Type.SuffixDecrement:
                    return 4;
                case Type.PrefixIncrement:
                case Type.PrefixDecrement:
                case Type.UnraryMinus:
                case Type.UnraryPlus:
                case Type.LogicalNot:
                case Type.BitwiseNot:
                    return 5;
                case Type.Multiplication:
                case Type.Division:
                case Type.Modulo:
                    return 6;
                case Type.Addition:
                case Type.Subtraction:
                    return 7;
                case Type.BitwiseLeftShift:
                case Type.BitwiseRightShift:
                    return 8;
                case Type.LessThan:
                case Type.LessThanOrEqualTo:
                case Type.GreaterThan:
                case Type.GreaterThanOrEqualTo:
                    return 9;
                case Type.EqualTo:
                case Type.NotEqualTo:
                    return 10;
                case Type.BitwiseAnd:
                    return 11;
                case Type.BitwiseXor:
                    return 12;
                case Type.BitwiseOr:
                    return 13;
                case Type.LogicalAnd:
                    return 14;
                case Type.LogicalOr:
                case Type.RuntimeValueAccess:
                    return 15;
                case Type.Assignment:
                case Type.AdditionAssignment:
                case Type.SubtractionAssignment:
                case Type.MultiplicationAssignment:
                case Type.DivisionAssignment:
                case Type.ModuloAssignment:
                case Type.BitwiseAndAssignment:
                case Type.BitwiseXorAssignment:
                case Type.BitwiseOrAssignment:
                case Type.BitwiseLeftShiftAssignment:
                case Type.BitwiseRightShiftAssignment:
                case Type.AppendAssignment:
                    return 16;
                default:
                    return 0;
            }
        }

        /**
         * 어휘 분석 시 사용될 토큰 정의를 추가한다.
         * 
         * @param	type
         * @param	value
         * @param	wholeWord
         * @param	affix
         */

        public static Token define(string value, Type type, bool wholeWord = false, Affix? affix = null)
        {
            var token = new Token(type, value);
            token.wholeWord = wholeWord;
            token.affix = affix;

            definitions.Add(token);

            return token;
        }

        /**
         * 토큰 타입으로 토큰 정의를 가져온다.
         * 
         * @param	type
         */
        public Token findByType(Type type)
        {
            foreach (int i in Enumerable.Range(0, definitions.Count))
            {
                if (definitions[i].type == type)
                    return definitions[i].copy();
            }
            return null;
        }

        public static Token findByValue(string value, bool wholeWord)
        {

            value = value.Trim();

            // 빈 값이면 아무것도 출력하지 않는다.
            if (value.Length == 0)
                return null;

            // 단어 단위 검색일 경우, 심볼 전체가 매치될 경우에만 해당
            if (wholeWord)
            {
                foreach (int i in Enumerable.Range(0, definitions.Count))
                {
                    if (definitions[i].wholeWord && definitions[i].value == value)
                        return definitions[i];
                }
                return new Token(Type.ID, value);
            }

            // 전 범위 검색일 경우
            else
            {
                int maxMatched = 0;
                Token candidate = null;

                foreach (int i in Enumerable.Range(0, definitions.Count))
                {
                    if (definitions[i].wholeWord || definitions[i].value == null)
                        continue;

                    // 정의 범위가 겹치는 것이 있을 수 있는데, 이 때는 더 많이 겹친 것을 선택한다.
                    int j = 0;
                    while (definitions[i].value.Length > j && value.Length > j)
                    {
                        if (definitions[i].value[j++] != value[j - 1])
                        {
                            j--;
                            break;
                        }
                    }
                    if (definitions[i].value.Length == j && j > maxMatched)
                    {
                        maxMatched = j;
                        candidate = definitions[i];
                    }
                }
                if (candidate == null) return null;
                else return candidate.copy();
            }

        }

    }

    /**
     * 토큰의 접사 종류
     */
    enum Affix
    {
        PREFIX,
        SUFFIX,
        NONE
    }

    /**
     * 토큰 종류
     */
    public enum Type
    {
        Define,
        Right,

        Include,

        ID,
        Variable,
        New,
        ArrayReference,

        If,
        Else,
        For,
        While,
        Continue,
        Break,
        Return,
        RuntimeValueAccess,

        True,
        False,
        String,
        Number,
        Array,

        ArrayOpen,
        ArrayClose,
        BlockOpen,
        BlockClose,
        ShellOpen,
        ShellClose,

        Dot,
        Comma,
        Colon,
        Semicolon,
        From,
        In,

        PrefixIncrement,
        PrefixDecrement,
        SuffixIncrement,
        SuffixDecrement,
        UnraryPlus,
        UnraryMinus,

        Addition,
        Append,
        Subtraction,
        Multiplication,
        Division,
        Modulo,
        Assignment,
        AdditionAssignment,
        AppendAssignment,
        SubtractionAssignment,
        MultiplicationAssignment,
        DivisionAssignment,
        ModuloAssignment,

        BitwiseNot,
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseLeftShift,
        BitwiseRightShift,
        BitwiseAndAssignment,
        BitwiseXorAssignment,
        BitwiseOrAssignment,
        BitwiseLeftShiftAssignment,
        BitwiseRightShiftAssignment,

        EqualTo,
        NotEqualTo,
        GreaterThan,
        GreaterThanOrEqualTo,
        LessThan,
        LessThanOrEqualTo,
        LogicalNot,
        LogicalAnd,
        LogicalOr,

        CastToNumber,
        CastToString,

        Instance,
        CharAt,
        PushParameters,
        As
    }
}