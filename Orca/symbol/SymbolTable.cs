using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.symbol
{
    class SymbolTable
    {

        /**
         * 할당 가능한 메모리 주소
         */
        public int availableAddress = 0;
        public int assignAddress()
        {
            return availableAddress++;
        }

        /**
         * 심볼 테이블
         */
        public List<VariableSymbol> variables;
        public List<FunctionSymbol> functions;
        public List<ClassSymbol> classes;
        public List<LiteralSymbol> literals;

        public SymbolTable()
        {

            // 맵을 초기화한다.
            variables = new List<VariableSymbol>();
            functions = new List<FunctionSymbol>();
            classes = new List<ClassSymbol>();
            literals = new List<LiteralSymbol>();
        }

        /**
         * 테이블에 심볼을 추가한다. 추가와 동시에 모든 심볼은 고유의 메모리 주소를 할당받는다.
         * 
         * @param symbol
         * @return
         */
        public Symbol add(Symbol symbol)
        {

            // 메모리 어드레스 할당
            symbol.address = assignAddress();

            // 심볼의 타입에 따라 분류하여 추가한다.
            if (symbol is VariableSymbol)
                variables.Add(symbol as VariableSymbol);
            else if (symbol is FunctionSymbol)
                functions.Add(symbol as FunctionSymbol);
            else if (symbol is ClassSymbol)
                classes.Add(symbol as ClassSymbol);
            else if (symbol is LiteralSymbol)
                literals.Add(symbol as LiteralSymbol);

            return symbol;
        }

        /**
         * 테이블 내의 심볼을 제거한다.
         * 
         * @param symbol
         * @return
         */
        public Symbol remove(Symbol symbol)
        {

            // 심볼의 타입에 따라 분류하여 삭제한다.
            if (symbol is VariableSymbol)
            {
                variables.Remove(symbol as VariableSymbol);
            }
            else if (symbol is FunctionSymbol)
                functions.Remove(symbol as FunctionSymbol);
            else if (symbol is ClassSymbol)
                classes.Remove(symbol as ClassSymbol);
            else if (symbol is LiteralSymbol)
                literals.Remove(symbol as LiteralSymbol);

            return symbol;
        }

        /**
         * 변수 심볼을 찾는다.
         * 
         * @param	id
         * @return
         */
        public VariableSymbol getVariable(string id)
        {
            foreach (int i in Enumerable.Range(0, variables.Count))
            {
                if (variables[i].id == id)
                    return variables[i];
            }
            return null;
        }

        /**
         * 함수 심볼을 찾는다.
         * 
         * @param	id
         * @param	parameterType
         * @return
         */
        public FunctionSymbol getFunction(string id, List<string> parameterType)
        {
            foreach (int i in Enumerable.Range(0, functions.Count))
            {
                if (functions[i].id == id)
                {

                    // 파라미터 옵션이 없으면 첫 번째 찾은 함수를 리턴한다.
                    if (parameterType == null)
                        return functions[i];

                    if (functions[i].parameters.Count != parameterType.Count)
                        continue;

                    bool match = true;

                    foreach (int j in Enumerable.Range(0, functions[i].parameters.Count))
                    {
                        if (functions[i].parameters[j].type != parameterType[j] && functions[i].parameters[j].type != "*")
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                        return functions[i];
                }
            }

            return null;
        }

        /**
         * 클래스 심볼을 찾는다.
         * 
         * @param	id
         * @return
         */
        public ClassSymbol getClass(string id)
        {
            foreach (int i in Enumerable.Range(0, classes.Count))
            {
                if (classes[i].id == id)
                    return classes[i];
            }
            return null;
        }

        /**
         * 리터럴 테이블에 넘겨진 값이 존재하는 경우 그 참조를 리턴하고, 없으면 새로 추가한 후 리턴한다.
         * 
         * @param value
         * @param type
         * @return
         */
        public LiteralSymbol getLiteral(string value, string type)
        {

            foreach (int i in Enumerable.Range(0, literals.Count))
            {
                if (literals[i].type == type && literals[i].value == value)
                    return literals[i];
            }

            LiteralSymbol literal = new LiteralSymbol(value, type);

            add(literal);

            return literal;
        }

    }
}