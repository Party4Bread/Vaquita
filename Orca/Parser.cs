using Orca;
using Orca.nlib;
using Orca.symbol;
using Orca.syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca
{

    public class Parser
    {
        Lexer lexer;
        Optimizer optimizer;
        SymbolTable symbolTable;
        private Assembly assembly;
        NativeLibrary nlib;
        public string buildPath;
        private int flagCount = 0;
        private int assignFlag()
        {
            return flagCount++;
        }
        public string compile(string code, string buildPath = "")
        {
            // 파싱 시 필요한 객체를 초기화한다.
            lexer = new Lexer();
            optimizer = new Optimizer();
            symbolTable = new SymbolTable();
            assembly = new Assembly(symbolTable);
            this.buildPath = buildPath;
            flagCount = 0;
            // 네이티브 라이브러리를 로드한다.
            nlib = new NativeLibrary();
            nlib.load(symbolTable);
            // 어휘 트리를 취득한다.
            Lextree lextree = lexer.analyze(code);
            //lexer.viewHierarchy(lextree, 0);
            // 현재 스코프를 스캔한다. 현재 스코프에서는 오브젝트 정의와 프로시저 정의만을 스캔한다.
            scan(lextree, new ScanOption());
            parseBlock(lextree, new ParseOption());
            assembly.freeze();
            // 리터럴을 어셈블리에 쓴다.
            foreach (int i in Enumerable.Range(0, symbolTable.literals.Count))
            {
                LiteralSymbol literal = symbolTable.literals[i];
                assembly.writeCode("SAL " + literal.address);

                if (literal.type == "number")
                    assembly.writeCode("PSH " + literal.value);
                else if (literal.type == "string")
                    assembly.writeCode("PSH " + literal.value + "s");

                assembly.writeCode("PSH " + literal.address);
                assembly.writeCode("STO");
            }
            assembly.melt();
            assembly.writeCode("END");
            // 모든 파싱이 끝나면 어셈블리 코드를 최적화한다.
            assembly.code = optimizer.optimize(assembly.code);
            // 메타데이터 추가
            assembly.code = symbolTable.availableAddress.ToString() + "\n" + assembly.code;
            return assembly.code;
        }

        void parseBlock(Lextree block, ParseOption option)
        {
            // 현재 스코프에서 생성된 변수와 프로시져를 저장한다. (맨 마지막에 심볼 테이블에서 삭제를 위함)
            List<Symbol> definedSymbols = new List<Symbol>();
            // 확장된 조건문 사용 여부
            bool extendedConditional = false;
            int extendedConditionalExit = 0; // 조건문의 탈출 플래그 (마지막 else나 elif 문의 끝)

            int i = -1;

            // 라인 단위로 파싱한다.
            while (++i < block.branch.Count)
            {

                Lextree line = block.branch[i];
                int lineNumber = line.lineNumber;

                // 다른 제어문의 도움 없이 코드 블록이 단독으로 사용될 수 없다.
                if (line.hasBranch)
                {
                    Debug.reportError("Syntax error 1", "Unexpected code block", lineNumber);
                    continue;
                }
                // 라인의 토큰열을 취득한다.
                List<Token> tokens = line.lexData;

                if (tokens.Count < 1)
                    continue;

                // 변수 선언문
                if (VariableDeclarationSyntax.match(tokens))
                {

                    // 구조체에서는 변수를 파싱하지 않는다. (이미 스캐닝에서 캐시 완료)
                    if (option.inStructure)
                        continue;

                    VariableDeclarationSyntax syntax = VariableDeclarationSyntax.analyze(tokens, lineNumber);

                    // 만약 구문 분석 중 오류가 발생했다면 다음 구문으로 건너 뛴다.
                    if (syntax == null)
                        continue;

                    // 변수 타입이 유효한지 확인한다.
                    if (symbolTable.getClass(syntax.variableType.value) == null)
                    {
                        Debug.reportError("Type error 6", "유효하지 않은 변수 타입입니다.", lineNumber);
                        continue;
                    }

                    // 변수 정의가 유효한지 확인한다.
                    if (symbolTable.getVariable(syntax.variableName.value) != null)
                    {
                        Debug.reportError("Duplication error 7", "변수 정의가 중복되었습니다.", lineNumber);
                        continue;
                    }

                    VariableSymbol variable = new VariableSymbol(syntax.variableName.value, syntax.variableType.value);
                    symbolTable.add(variable);

                    // 토큰에 심볼을 태그한다.
                    syntax.variableName.setTag(variable);

                    // 정의된 심볼 목록에 추가한다.
                    definedSymbols.Add(variable);

                    // 어셈블리에 변수의 메모리 어드레스 할당 명령을 추가한다.				
                    if (variable.isNumber() || variable.isString())
                        assembly.writeCode("SAL " + variable.address);
                    else
                        assembly.writeCode("SAA " + variable.address);

                    // 초기화 데이터가 존재할 경우
                    if (syntax.initializer != null)
                    {
                        variable.initialized = true;

                        // 초기화문을 파싱한 후 어셈블리에 쓴다.
                        ParsedPair parsedInitializer = parseLine(syntax.initializer, lineNumber);

                        if (parsedInitializer == null) continue;
                        assembly.writeLine(parsedInitializer.data);
                        assembly.writeCode("POP 0");
                    }
                }
                else if (FunctionDeclarationSyntax.match(tokens))
                {

                    // 함수 구문을 분석한다.
                    FunctionDeclarationSyntax syntax = FunctionDeclarationSyntax.analyze(tokens, lineNumber);

                    // 만약 구문 분석 중 오류가 발생했다면 다음 구문으로 건너 뛴다.
                    if (syntax == null)
                        continue;

                    List<string> parametersTypeList = new List<string>();

                    // 매개변수 각각의 유효성을 검증하고 심볼 형태로 가공한다.
                    foreach (int k in Enumerable.Range(0, syntax.parameters.Count))
                    {

                        if (!ParameterDeclarationSyntax.match(syntax.parameters[k]))
                        {
                            continue;
                        }
                        // 매개 변수의 구문을 분석한다.
                        ParameterDeclarationSyntax parameterSyntax = ParameterDeclarationSyntax.analyze(syntax.parameters[k], lineNumber);

                        // 매개 변수 선언문에 Syntax error가 있을 경우 건너 뛴다.
                        if (parameterSyntax == null)
                            continue;

                        parametersTypeList.Add(parameterSyntax.parameterType.value);
                    }

                    // 테이블에서 함수 심볼을 가져온다. (이미 스캐닝 과정에서 함수가 테이블에 등록되었으므로)
                    FunctionSymbol functn = symbolTable.getFunction(syntax.functionName.value, parametersTypeList);

                    // 함수 심볼을 정의된 심볼 목록에 추가한다.
                    definedSymbols.Add(functn);

                    // 다음 라인이 블록 형태인지 확인한다.
                    if (!hasNextBlock(block, i))
                    {
                        Debug.reportError("Syntax error 8", "함수 구현부가 존재하지 않습니다.", lineNumber);
                        continue;
                    }

                    // 프로시져가 임의로 실행되는 것을 막기 위해 프로시저의 끝 부분으로 점프한다.
                    assembly.writeCode("PSH %" + functn.functionExit);
                    assembly.writeCode("JMP");

                    // 프로시져의 시작 부분을 알려주는 코드
                    assembly.flag(functn.functionEntry);

                    // 프로시져 구현부를 파싱한다. 옵션: 함수
                    ParseOption functionOption = option.copy();

                    functionOption.inStructure = false;
                    functionOption.inFunction = true;
                    functionOption.inIterator = false;
                    functionOption.parentFunction = functn;

                    // 파라미터 변수를 추가/할당한다.
                    foreach (int j in Enumerable.Range(0, functn.parameters.Count))
                    {
                        // 심볼 테이블에 추가한다.
                        symbolTable.add(functn.parameters[j]);
                    }


                    parseBlock(block.branch[++i], functionOption);

                    // 파라미터 변수를 제거한다.				
                    foreach (int j in Enumerable.Range(0, functn.parameters.Count))
                    {
                        symbolTable.remove(functn.parameters[j]);
                    }

                    /*
                     * 프로시져 호출 매커니즘은 다음과 같다.
                     * 
                     * 호출 스택에 기반하여 프로시져의 끝에서 마지막 스택 플래그로 이동.(pop)
                     */
                    // 마지막 호출 위치를 가져온다.
                    assembly.writeCode("MOC");

                    // 마지막 호출 위치로 이동한다. (이 명령은 함수가 void형이고, 리턴 명령을 결국 만나지 못했을 때 실행되게 된다.)
                    assembly.writeCode("JMP");

                    // 프로시져의 끝 부분을 표시한다.
                    assembly.flag(functn.functionExit);
                }
                else if (ClassDeclarationSyntax.match(tokens))
                {

                    // 오브젝트 구문을 분석한다.
                    ClassDeclarationSyntax syntax = ClassDeclarationSyntax.analyze(tokens, lineNumber);

                    // 만약 구문 분석 중 오류가 발생했다면 다음 구문으로 건너 뛴다.
                    if (syntax == null)
                        continue;

                    // 다음 라인이 블록 형태인지 확인한다.
                    if (!hasNextBlock(block, i))
                    {
                        Debug.reportError("Syntax error  9", "클래스의 구현부가 존재하지 않습니다.", lineNumber);
                        continue;
                    }

                    // 클래스 정의를 취득한다.
                    ClassSymbol klass = symbolTable.getClass(syntax.className.value) as ClassSymbol;

                    // 클래스 내부의 클래스일 경우 구현부를 스캔한다.
                    if (option.inStructure)
                    {

                        ScanOption innerScanOption = new ScanOption();
                        innerScanOption.inStructure = true;
                        innerScanOption.parentClass = klass;


                        scan(block.branch[i + 1], innerScanOption);
                    }

                    // 오브젝트 역시 스캔이 끝난 상태로, 하위 항목의 기본 선언 정보는 기록되어 있으나, 실제 명령은 파싱되지 않은
                    // 상태이다. 하위 종속 항목들에 대해 파싱한다.
                    ParseOption classOption = option.copy();
                    classOption.inStructure = true;
                    classOption.inIterator = false;
                    classOption.inFunction = false;


                    parseBlock(block.branch[++i], classOption);

                    // 정의된 심볼 목록에 추가한다.
                    definedSymbols.Add(klass);
                }
                else if (IfSyntax.match(tokens))
                {

                    if (option.inStructure)
                    {
                        Debug.reportError("Syntax error 2", "conditional/iteration statements couldnt be used in class structure", lineNumber);
                        continue;
                    }

                    IfSyntax syntax = IfSyntax.analyze(tokens, lineNumber);

                    if (syntax == null)
                        continue;

                    // if문은 확장된 조건문의 시작이므로 초기화한다.				
                    extendedConditional = false;

                    // 뒤에 else if 나 else를 가지고 있으면 확장된 조건문을 사용한다.
                    if (hasNextConditional(block, i))
                    {
                        extendedConditional = true;
                        extendedConditionalExit = assignFlag();
                    }
                    else
                    {
                        extendedConditional = false;
                    }

                    // 조건문을 취득한 후 파싱한다.
                    ParsedPair parsedCondition = parseLine(syntax.condition, lineNumber);

                    if (parsedCondition == null)
                        continue;

                    // 조건문 결과 타입이 정수형이 아니라면 (True:1, False:0) 에러를 출력한다.
                    if (parsedCondition.type != "number" && parsedCondition.type != "bool")
                    {

                        Debug.reportError("Syntax error 10", "참, 거짓 여부를 판별할 수 없는 조건식입니다.", lineNumber);
                        continue;
                    }

                    // if문의 구현부가 존재하는지 확인한다.
                    if (!hasNextBlock(block, i))
                    {
                        Debug.reportError("Syntax error 11", "if문의 구현부가 존재하지 않습니다.", lineNumber);
                        continue;
                    }

                    // 조건문 탈출 플래그
                    int ifExit = assignFlag();

                    // 어셈블리에 조건식을 쓴다.
                    assembly.writeLine(parsedCondition.data);
                    assembly.writeCode("PSH %" + ifExit);

                    // 조건이 거짓일 경우 -> if절을 건너 뛴다.
                    assembly.writeCode("JMF");

                    // 구현부를 파싱한다.
                    ParseOption ifOption = option.copy();
                    ifOption.inStructure = false;


                    parseBlock(block.branch[++i], ifOption);

                    // 만약 참이라서 여기까지 실행되면, 확장 조건문인 경우 끝으로 이동
                    if (extendedConditional)
                    {
                        assembly.writeCode("PSH %" + extendedConditionalExit);
                        assembly.writeCode("JMP");
                    }

                    assembly.flag(ifExit);
                }
                else if (ElseIfSyntax.match(tokens))
                {

                    if (option.inStructure)
                    {
                        Debug.reportError("Syntax error 2", "conditional/iteration statements couldnt be used in class structure", lineNumber);
                        continue;
                    }

                    // 확장된 조건문을 사용하지 않는 상태에서 else if문이 등장하면 에러를 출력한다
                    if (!extendedConditional)
                    {
                        Debug.reportError("Syntax error 12", "else-if문은 단독으로 쓰일 수 없습니다.", lineNumber);
                        continue;
                    }

                    ElseIfSyntax syntax = ElseIfSyntax.analyze(tokens, lineNumber);

                    if (syntax == null)
                        continue;

                    // 뒤에 else if 나 else를 가지고 있으면 확장된 조건문을 사용한다.
                    if (hasNextConditional(block, i))
                        extendedConditional = true;
                    else
                        extendedConditional = false;

                    // 조건문을 취득한 후 파싱한다.
                    ParsedPair parsedCondition = parseLine(syntax.condition, lineNumber);

                    if (parsedCondition == null)
                        continue;

                    // 조건문 결과 타입이 정수형이 아니라면 (True:1, False:0) 에러를 출력한다.
                    if (parsedCondition.type != "number" && parsedCondition.type != "bool")
                    {
                        Debug.reportError("Syntax error 13", "참, 거짓 여부를 판별할 수 없는 조건식입니다.", lineNumber);
                        continue;
                    }

                    // if문의 구현부가 존재하는지 확인한다.
                    if (!hasNextBlock(block, i))
                    {
                        Debug.reportError("Syntax error 14", "if문의 구현부가 존재하지 않습니다.", lineNumber);
                        continue;
                    }

                    // 조건문 탈출 플래그
                    int elseIfExit = 0;
                    if (!extendedConditional)
                        elseIfExit = extendedConditionalExit;
                    else
                        elseIfExit = flagCount++;

                    // 어셈블리에 조건식을 쓴다.
                    assembly.writeLine(parsedCondition.data);
                    assembly.writeCode("PSH %" + elseIfExit);

                    // 조건이 거짓일 경우 구현부를 건너 뛴다.
                    assembly.writeCode("JMF");

                    // 구현부를 파싱한다.
                    ParseOption ifOption = option.copy();
                    ifOption.inStructure = false;


                    parseBlock(block.branch[++i], ifOption);

                    // 만약 참이라서 구현부가 실행되었을 경우, 조건 블록의 가장 끝으로 이동한다.
                    if (extendedConditional)
                    {
                        assembly.writeCode("PSH %" + extendedConditionalExit);
                        assembly.writeCode("JMP");
                        assembly.flag(elseIfExit);
                    }

                    // 만약 확장 조건문이 elseIf를 마지막으로 끝나는 경우라면
                    else
                    {
                        assembly.flag(extendedConditionalExit);
                    }

                }
                else if (ElseSyntax.match(tokens))
                {

                    if (option.inStructure)
                    {
                        Debug.reportError("Syntax error 2", "conditional/iteration statements couldnt be used in class structure", lineNumber);
                        continue;
                    }

                    ElseSyntax syntax = ElseSyntax.analyze(tokens, lineNumber);

                    // 만약 else 문이 유효하지 않을 경우 다음으로 건너 뛴다.
                    if (syntax == null)
                        continue;


                    // 확장된 조건문을 사용하지 않는 상태에서 else문이 등장하면 에러를 출력한다
                    if (!extendedConditional)
                    {
                        Debug.reportError("Syntax error 15", "else문은 단독으로 쓰일 수 없습니다.", lineNumber);
                        continue;
                    }

                    // 확장 조건문을 종료한다.
                    extendedConditional = false;

                    // else문의 구현부가 존재하는지 확인한다.
                    if (!hasNextBlock(block, i))
                    {
                        Debug.reportError("Syntax error 16", "else문의 구현부가 존재하지 않습니다.", lineNumber);
                        continue;
                    }

                    // 구현부를 파싱한다. 만약 이터레이션 플래그가 있는 옵션일 경우 그대로 넘긴다.
                    ParseOption elseOption = option.copy();
                    elseOption.inStructure = false;


                    parseBlock(block.branch[++i], elseOption);

                    // 확장 조건문 종료 플래그를 쓴다.
                    assembly.flag(extendedConditionalExit);
                }
                else if (ForSyntax.match(tokens))
                {

                    if (option.inStructure)
                    {
                        Debug.reportError("Syntax error 2", "conditional/iteration statements couldnt be used in class structure", lineNumber);
                        continue;
                    }

                    ForSyntax syntax = ForSyntax.analyze(tokens, lineNumber);

                    if (syntax == null)
                        continue;

                    // 증감 변수가 유효한지 확인한다.
                    if (symbolTable.getVariable(syntax.counter.value) != null)
                    {
                        Debug.reportError("Duplication error 17", "증감 변수 정의가 충돌합니다.", lineNumber);
                        continue;
                    }

                    // 증감 변수를 생성한다.
                    VariableSymbol counter = new VariableSymbol(syntax.counter.value, "number");
                    counter.initialized = true;

                    // 증감 변수를 태그한다.
                    syntax.counter.setTag(counter);

                    // 테이블에 증감 변수를 등록한다.
                    symbolTable.add(counter);

                    // 초기값 파싱
                    ParsedPair parsedInitialValue = parseLine(syntax.start, lineNumber);

                    if (parsedInitialValue == null)
                        continue;

                    if (parsedInitialValue.type != "number")
                    {
                        Debug.reportError("Type error 17", "초기 값의 타입이 실수형이 아닙니다.", lineNumber);
                        continue;
                    }

                    // for문의 기본 구성인 n -> k 에서 이는 기본적으로 while(n <= k)를 의미하므로 동치인 명령을
                    // 생성한다.
                    List<Token> temp = new List<Token>();
                    temp.Add(syntax.counter);
                    temp.Add(Token.findByType(Type.LessThanOrEqualTo));
                    List<List<Token>> temp2 = new List<List<Token>>();
                    temp2.Add(temp);
                    temp2.Add(syntax.end);
                    List<Token> condition = TokenTools.merge(temp2);
                    ParsedPair parsedCondition = parseLine(condition, lineNumber);

                    if (parsedCondition == null)
                        continue;

                    if (parsedCondition == null || parsedInitialValue == null)
                        continue;

                    // for문의 구현부가 존재하는지 확인한다.
                    if (!hasNextBlock(block, i))
                    {
                        Debug.reportError("Syntax error 18", "for문의 구현부가 존재하지 않습니다.", lineNumber);
                        continue;
                    }

                    /*
                     * for의 어셈블리 구조는, 증감자 초기화(0) -> 귀환 플래그 -> 증감자 증감 -> 조건문(jump if
                     * false) -> 내용 ->귀환 플래그로 점프 -> 탈출 플래그 로 되어 있다.
                     */

                    // 귀환/탈출 플래그 생성
                    int forEntry = assignFlag();
                    int forExit = assignFlag();

                    // 증감자 초기화 (-1)

                    assembly.writeCode("SAL " + counter.address);
                    assembly.writeLine(parsedInitialValue.data);
                    assembly.writeCode("PSH -1");
                    assembly.writeCode("OPR 1");
                    assembly.writeCode("PSH " + counter.address);
                    assembly.writeCode("STO");

                    // 귀환 플래그를 심는다.
                    assembly.flag(forEntry);

                    // 증감자 증감 (+1)
                    assembly.writeCode("PSH " + counter.address);
                    assembly.writeCode("IVK 27");

                    // 조건문이 거짓이면 탈출 플래그로 이동
                    assembly.writeLine(parsedCondition.data);
                    assembly.writeCode("PSH %" + forExit);
                    assembly.writeCode("JMF");

                    // for문의 구현부를 파싱한다. 이 때, 기존의 옵션은 뒤의 과정에서도 동일한 내용으로 사용되므로 새로운 옵션을
                    // 생성한다.
                    ParseOption forOption = option.copy();
                    forOption.inStructure = false;
                    forOption.inIterator = true;
                    forOption.blockEntry = forEntry;
                    forOption.blockExit = forExit;


                    parseBlock(block.branch[++i], forOption);

                    // 귀환 플래그로 점프
                    assembly.writeCode("PSH %" + forEntry);
                    assembly.writeCode("JMP");

                    // 탈출 플래그를 심는다.
                    assembly.flag(forExit);

                    // 증감 변수를 제거한다.
                    symbolTable.remove(counter);
                }
                else if (WhileSyntax.match(tokens))
                {

                    if (option.inStructure)
                    {
                        Debug.reportError("Syntax error 2", "conditional/iteration statements couldnt be used in class structure", lineNumber);
                        continue;
                    }

                    WhileSyntax syntax = WhileSyntax.analyze(tokens, lineNumber);

                    if (syntax == null)
                        continue;

                    /*
                     * while의 어셈블리 구조는, 귀환 플래그 -> 조건문(jump if false) -> 내용 -> 귀환
                     * 플래그로 이동 -> 탈출 플래그로 되어 있다.
                     */

                    // 조건문을 파싱한다.
                    ParsedPair parsedCondition = parseLine(syntax.condition, lineNumber);

                    if (parsedCondition == null)
                        continue;

                    // while문의 구현부가 존재하는지 확인한다.
                    if (!hasNextBlock(block, i))
                    {
                        Debug.reportError("Syntax error 19", "while문의 구현부가 존재하지 않습니다.", lineNumber);
                        continue;
                    }

                    // 귀환/탈출 플래그 생성
                    int whileEntry = assignFlag();
                    int whileExit = assignFlag();

                    // 귀환 플래그를 심는다.
                    assembly.flag(whileEntry);

                    // 조건문을 체크하여 거짓일 경우 탈출 플래그로 이동한다.
                    assembly.writeLine(parsedCondition.data);
                    assembly.writeCode("PSH %" + whileExit);
                    assembly.writeCode("JMF");

                    // while문의 구현부를 파싱한다.
                    ParseOption whileOption = option.copy();
                    whileOption.inStructure = false;
                    whileOption.inIterator = true;
                    whileOption.blockEntry = whileEntry;
                    whileOption.blockExit = whileExit;


                    parseBlock(block.branch[++i], whileOption);

                    // 귀환 플래그로 점프한다.
                    assembly.writeCode("PSH %" + whileEntry);
                    assembly.writeCode("JMP");

                    // 탈출 플래그를 심는다.
                    assembly.flag(whileExit);
                }
                else if (ContinueSyntax.match(tokens))
                {

                    if (!option.inIterator)
                    {
                        Debug.reportError("Syntax error 3", "제어 명령은 반복문 내에서만 사용할 수 있습니다.", lineNumber);
                        continue;
                    }

                    // 귀환 플래그로 점프한다.
                    assembly.writeCode("PSH %" + option.blockEntry);
                    assembly.writeCode("JMP");
                }
                else if (BreakSyntax.match(tokens))
                {

                    if (!option.inIterator)
                    {
                        Debug.reportError("Syntax error 3", "제어 명령은 반복문 내에서만 사용할 수 있습니다.", lineNumber);
                        continue;
                    }

                    // 탈출 플래그로 점프한다.
                    assembly.writeCode("PSH %" + option.blockExit);
                    assembly.writeCode("JMP");
                }
                else if (ReturnSyntax.match(tokens))
                {

                    if (!option.inFunction)
                    {
                        Debug.reportError("Syntax error 4", "리턴 명령은 함수 정의 내에서만 사용할 수 있습니다.", lineNumber);
                        continue;
                    }

                    ReturnSyntax syntax = ReturnSyntax.analyze(tokens, lineNumber);

                    if (syntax == null)
                        continue;

                    if (!option.inFunction)
                    {
                        Debug.reportError("Syntax error 20", "return 명령은 함수 내에서만 사용할 수 있습니다.", lineNumber);
                        continue;
                    }

                    // 만약 함수 타입이 Void일 경우 그냥 탈출 플래그로 이동한다.
                    if (option.parentFunction.isVoid())
                    {

                        // 반환값이 존재하면 에러를 출력한다.
                        if (syntax.returnValue.Count > 0)
                        {
                            Debug.reportError("Syntax error 20", "void형 함수는 값을 반환할 수 없습니다.", lineNumber);
                            continue;
                        }

                        // 마지막 호출 지점을 가져온다.
                        assembly.writeCode("MOC");

                        // 마지막 호출 지점으로 이동한다.
                        assembly.writeCode("JMP");
                    }

                    // 반환 타입이 있을 경우
                    else
                    {

                        // 반환값이 없다면 에러를 출력한다.
                        if (tokens.Count < 1)
                        {
                            Debug.reportError("Syntax error 21", "return문이 값을 반환하지 않습니다.", lineNumber);
                            continue;
                        }

                        // 반환값을 파싱한다. 파싱된 결과는 스택에 저장된다.
                        ParsedPair parsedReturnValue = parseLine(syntax.returnValue, lineNumber);

                        if (parsedReturnValue == null)
                            continue;

                        if (parsedReturnValue.type != option.parentFunction.type && parsedReturnValue.type != "*")
                        {
                            TokenTools.view1D(parsedReturnValue.data);
                            Debug.reportError("Syntax error 22", "리턴된 데이터의 타입(" + parsedReturnValue.type + ")이 함수 리턴 타입(" + option.parentFunction.type + ")과 일치하지 않습니다.", lineNumber);
                            continue;
                        }

                        // 리턴 값을 쓴다.
                        assembly.writeLine(parsedReturnValue.data);

                        // 마지막 호출 지점을 가져온다.
                        assembly.writeCode("MOC");

                        // 마지막 호출 지점으로 이동한다. (레지스터 값으로 점프 명령)
                        assembly.writeCode("JMP");
                    }
                }
                /*
                // 인클루드 문			
                else if (IncludeSyntax.match(tokens))
                {

                    IncludeSyntax syntax = IncludeSyntax.analyze(tokens, lineNumber);

                    // 인클루드 대상 파일을 로드한다.
                    string targetCode = null;
                    using (StreamReader reader = new StreamReader()
                    {

                    }
                        try targetCode = PCLStorage.FileSystem(buildPath + syntax.targetFile) //TODO :: fin way to read
                    catch (Exception error) {
                            Debug.reportError("File Not Found Error", "Cannot find including orca file.", lineNumber);
                            return null;
                        }

                    // 어휘 분석한다.
                    var lextree: Lextree = lexer.analyze(targetCode);

                    // 스캔한다.
                    scan(lextree, new ScanOption());

                    for (j in 0...lextree.branch.length)
                    {
                        block.branch.insert(i + j + 1, lextree.branch[j]);
                    }
                    }*/

                // 일반 대입문을 파싱한다.
                else
                {

                    if (option.inStructure)
                    {
                        Debug.reportError("Syntax error 5", "구조체 정의에서 연산 처리를 할 수 없습니다.", lineNumber);
                        continue;
                    }

                    ParsedPair parsedLine = parseLine(tokens, lineNumber);
                    if (parsedLine == null)
                        continue;

                    assembly.writeLine(parsedLine.data);

                    // 스택이 쌓이는 것을 방지하기 위해 pop 명령 추가.
                    // 예외는 함수.
                    if (!FunctionCallSyntax.match(tokens))
                    {
                        assembly.writeCode("POP 0");
                    }

                }
            }

            // definition에 있던 심볼을 테이블에서 모두 제거한다.
            foreach (int s in Enumerable.Range(0, definedSymbols.Count))
            {
                symbolTable.remove(definedSymbols[s]);
            }
        }


        /**
         * 토큰열을 파싱한다.
         * 
         * @param	tokens
         * @param	lineNumber
         * @return
         */
        ParsedPair parseLine(List<Token> tokens, int lineNumber)
        {

            // 토큰이 비었을 경우
            if (tokens.Count < 1)
            {
                Debug.reportError("Syntax error 23", "계산식에 피연산자가 존재하지 않습니다.", lineNumber);
                return null;
            }

            // 의미 없는 껍데기가 있다면 벗긴다.
            tokens = TokenTools.pill(tokens);

            // 토큰열이 하나일 경우 (파싱 트리의 최하단에 도달했을 경우)
            if (tokens.Count == 1)
            {

                // 변수일 경우 토큰의 유효성 검사를 한다.
                if (tokens[0].type == Type.ID)
                {

                    VariableSymbol variable = symbolTable.getVariable(tokens[0].value);

                    // 태그되지 않은 변수일 경우 유효성을 검증한 후 태그한다.
                    if (!tokens[0].tagged)
                    {
                        if (variable == null)
                        {
                            Debug.reportError("Undefined Error 24", tokens[0].value + "는 정의되지 않은 변수입니다.", lineNumber);
                            return null;
                        }

                        // 토큰에 변수를 태그한다.
                        tokens[0].setTag(variable);
                    }

                    return new ParsedPair(tokens, variable.type);
                }

                LiteralSymbol literal = null;

                switch (tokens[0].type)
                {

                    // true/false 토큰은 각각 1/0으로 처리한다.
                    case Type.True:
                        literal = symbolTable.getLiteral("1", LiteralSymbol.NUMBER);
                        break;
                    case Type.False:
                        literal = symbolTable.getLiteral("0", LiteralSymbol.NUMBER);
                        break;
                    case Type.Number:
                        literal = symbolTable.getLiteral(tokens[0].value, LiteralSymbol.NUMBER);
                        break;
                    case Type.String:
                        literal = symbolTable.getLiteral(tokens[0].value, LiteralSymbol.STRING);
                        break;
                    default:
                        Debug.reportError("Syntax error 25", "심볼의 타입을 찾을 수 없습니다.", lineNumber);
                        return null;
                }

                // 토큰에 리터럴 태그하기
                tokens[0].setTag(literal);

                return new ParsedPair(tokens, literal.type);
            }//여기까지

            /**
             * 함수 호출: F(X,Y,Z)
             */
            if (FunctionCallSyntax.match(tokens))
            {

                // 프로시저 호출 구문을 분석한다.
                FunctionCallSyntax syntax = FunctionCallSyntax.analyze(tokens, lineNumber);

                if (syntax == null)
                    return null;

                List<List<Token>> arguments = new List<List<Token>>();
                List<string> argumentsTypeList = new List<string>();

                // 각각의 파라미터를 파싱한다.
                foreach (int i in Enumerable.Range(0, syntax.functionArguments.Count))
                {

                    // 파라미터가 비었을 경우
                    if (syntax.functionArguments[syntax.functionArguments.Count - 1 - i].Count < 1)
                    {
                        Debug.reportError("Syntax error 28", "파라미터가 비었습니다.", lineNumber);
                        return null;
                    }

                    // 파라미터를 파싱한다.
                    ParsedPair parsedArgument = parseLine(syntax.functionArguments[syntax.functionArguments.Count - 1 - i], lineNumber);

                    if (parsedArgument == null)
                        return null;

                    // 파라미터를 쌓는다.
                    arguments.Add(parsedArgument.data);
                    argumentsTypeList.Insert(0, parsedArgument.type);
                }
                List<Token> temp = new List<Token>();
                temp.Add(syntax.functionName);
                arguments.Add(temp);

                // 함수 심볼을 취득한다.
                FunctionSymbol functn = symbolTable.getFunction(syntax.functionName.value, argumentsTypeList);

                if (functn == null)
                {
                    Debug.reportError("Undefined Error 26", syntax.functionName.value + "(" + argumentsTypeList + ") 는 정의되지 않은 프로시져입니다.", lineNumber);
                    return null;
                }
                syntax.functionName.setTag(functn);
                return new ParsedPair(TokenTools.merge(arguments), functn.type);
            }

            /**
             * 배열 생성 : [A, B, C, D, ... , ZZZ]
             */
            else if (ArraySyntax.match(tokens))
            {

                ArraySyntax syntax = ArraySyntax.analyze(tokens, lineNumber);

                if (syntax == null)
                    return null;

                List<List<Token>> parsedElements = new List<List<Token>>();

                // 배열 리터럴의 각 원소를 파싱한 후 스택에 쌓는다.
                foreach (int i in Enumerable.Range(0, syntax.elements.Count))
                {

                    // 배열의 원소가 유효한지 체크한다.
                    if (syntax.elements[syntax.elements.Count - 1 - i].Count < 1)
                    {
                        continue;
                    }

                    // 배열의 원소를 파싱한다.
                    ParsedPair parsedElement = parseLine(syntax.elements[syntax.elements.Count - 1 - i], lineNumber);

                    if (parsedElement == null)
                        return null;

                    parsedElements.Add(parsedElement.data);
                    List<Token> temp = new List<Token>();
                    temp.Add(new Token(Type.Number, (syntax.elements.Count - 1 - i).ToString()));
                    parsedElements.Add(temp);
                }

                /*
                 * 배열 리터럴의 토큰 구조는
                 * 
                 * A1, A2, A3, ... An, ARRAY_LITERAL(n)
                 */
                List<Token> mergedElements = TokenTools.merge(parsedElements);
                mergedElements.Add(new Token(Type.Array, parsedElements.Count.ToString()));//weird


                return new ParsedPair(mergedElements, "array");
            }

            /**
             * 객체 생성 : new A
             */
            else if (InstanceCreationSyntax.match(tokens))
            {

                // 객체 정보 취득
                InstanceCreationSyntax syntax = InstanceCreationSyntax.analyze(tokens, lineNumber);

                if (syntax == null)
                    return null;

                ClassSymbol targetClass = symbolTable.getClass(syntax.instanceType.value);

                if (targetClass == null)
                {
                    Debug.reportError("Undefined error 31", "정의되지 않은 클래스입니다.", lineNumber);
                    return null;
                }

                // 토큰에 오브젝트 태그
                syntax.instanceType.setTag(targetClass);
                List<Token> temp = new List<Token>();
                temp.Add(syntax.instanceType);
                temp.Add(Token.findByType(Type.Instance));
                return new ParsedPair(temp, targetClass.id);
            }

            /**
             * 속성 선택문 : A.B.C -> 배열 취급하여 파싱한다.
             */
            else if (AttributeSyntax.match(tokens))
            {

                AttributeSyntax syntax = AttributeSyntax.analyze(tokens, lineNumber);

                if (syntax == null)
                    return null;

                List<Token> target = syntax.attributes[0];
                ClassSymbol parentClass = null;

                // 타겟을 파싱한다. 타입은 무조건 array 계열
                ParsedPair parsedTarget = parseLine(target, lineNumber);
                parentClass = symbolTable.getClass(parsedTarget.type);

                List<List<Token>> parsedAttributes = new List<List<Token>>();
                parsedAttributes.Add(parsedTarget.data);

                // 각각의 속성을 파싱한다.
                foreach (int j in Enumerable.Range(1, syntax.attributes.Count))
                {
                    List<Token> attribute = syntax.attributes[j];

                    // 함수형일 경우
                    if (FunctionCallSyntax.match(attribute))
                    {

                        FunctionCallSyntax functionSyntax = FunctionCallSyntax.analyze(attribute, lineNumber);

                        if (functionSyntax == null)
                            continue;

                        // 맨 앞의 파라미터는 앞쪽과 자연스럽게 연결됨.
                        List<List<Token>> parsedFunction = new List<List<Token>>();
                        List<string> typeList = new List<string>();
                        typeList.Add(parentClass.id);

                        // 매개 변수를 파싱하면서, 함수를 찾기 위한 타입 리스트를 만든다.
                        foreach (int i in Enumerable.Range(0, functionSyntax.functionArguments.Count))
                        {
                            ParsedPair parsedArgument = parseLine(functionSyntax.functionArguments[i], lineNumber);
                            if (parsedArgument == null) continue;

                            parsedFunction.Add(parsedArgument.data);
                            typeList.Add(parsedArgument.type);
                        }

                        // 함수 심볼을 취득한다.
                        FunctionSymbol functionSymbol = symbolTable.getFunction(functionSyntax.functionName.value, typeList);

                        if (functionSymbol == null)
                        {
                            //trace(typeList);
                            Debug.reportError("Undefined error 351", "속성을 찾을 수 없습니다.", lineNumber);
                            return null;
                        }
                        functionSyntax.functionName.setTag(functionSymbol);
                        List<Token> temp = new List<Token>();
                        temp.Add(functionSyntax.functionName);
                        parsedFunction.Add(temp);

                        // 일렬로 줄 세운 후 파싱된 속성 목록에 추가한다.
                        parsedAttributes.Add(TokenTools.merge(parsedFunction));

                        // 다음 속성을 위해 parentClass를 지정해 준다.
                        parentClass = symbolTable.getClass(functionSymbol.type);
                    }

                    // 데이터 참조(변수) 일 경우 맴버 인덱스를 검색해서 배열 참조 형태로 리턴
                    else
                    {

                        // 두개 이상으로 이루어져 있을 경우, (괄호형이나 배열은 첫 번째에서만 허용함)
                        if (attribute.Count > 1)
                        {
                            Debug.reportError("Syntax Error 3333", "Unexpected Token", lineNumber);
                            return null;
                        }
                        // 속성을 찾는다.	
                        VariableSymbol memberSymbol = parentClass.findMemberByID(attribute[0].value);

                        if (memberSymbol == null)
                        {
                            Debug.reportError("Undefined error 424", "속성을 찾을 수 없습니다.", lineNumber);
                            return null;
                        }
                        attribute[0].setTag(memberSymbol);

                        int memberIndex = 0;
                        foreach (int k in Enumerable.Range(0, parentClass.members.Count))
                        {
                            if (parentClass.members[k] == memberSymbol)
                            {
                                memberIndex = k;
                                break;
                            }
                        }

                        // 맴버 인덱스 토큰
                        Token memberIndexToken = new Token(Type.Number, memberIndex.ToString());

                        // A[a][b][c] 를 c b a A Array_reference(3) 로 배열한다. 즉					

                        // 파싱된 속성 목록의 가장 앞에 추가한다.
                        List<Token> temp = new List<Token>();
                        temp.Add(memberIndexToken);
                        parsedAttributes.Insert(0, temp);
                        temp = new List<Token>();
                        temp.Add(new Token(Type.ArrayReference, "1"));
                        parsedAttributes.Add(temp);

                        // 다음 속성을 위해 parentClass를 지정해 준다.
                        parentClass = symbolTable.getClass(memberSymbol.type);
                    }
                }

                // 속성을 일렬로 줄 세운 후, 리턴한다.
                List<Token> mergedAttributes = TokenTools.merge(parsedAttributes);

                return new ParsedPair(mergedAttributes, parentClass.id);
            }

            /**
             * 배열 참조: a[1][2]
             */
            else if (ArrayReferenceSyntax.match(tokens))
            {

                /*
                 * 배열 인덱스 연산자는 우선순위가 가장 높고, 도트보다 뒤에 처리되므로 배열 인덱스 열기 문자 ('[')로 구분되는
                 * 토큰은 단일 변수의 n차 접근으로만 표시될 수 있다. 즉 이 프로시져에서 걸리는 토큰열은 모두 A[N]...[N]의
                 * 형태를 하고 있다. (단 A는 변수거나 프로시져)
                 * 
                 * A[1][2] -> GET 0, A 1 -> GET 1, 0, 2 -> PSH 1
                 */

                // 배열 참조 구문을 분석한다.
                ArrayReferenceSyntax syntax = ArrayReferenceSyntax.analyze(tokens, lineNumber);

                if (syntax == null)
                    return null;

                VariableSymbol array = symbolTable.getVariable(syntax.array.value);

                if (array == null)
                {
                    Debug.reportError("Undefined Error 34", "정의되지 않은 배열입니다.", lineNumber);
                    return null;
                }
                syntax.array.setTag(array);

                // 변수가 배열이 아닐 경우, 문자열 인덱스값 읽기로 처리
                if (array.type != "array")
                {

                    // 변수가 문자열도 아니면, 에러
                    if (array.type != "string")
                    {
                        Debug.reportError("Type error 35", "인덱스 참조는 배열에서만 가능합니다.", lineNumber);
                        return null;
                    }

                    // 문자열 인덱스 참조 명령을 처리한다.
                    if (syntax.references.Count != 1)
                    {
                        Debug.reportError("Type error 36", "문자열을 n차원 배열처럼 취급할 수 없습니다.", lineNumber);
                        return null;
                    }

                    // index A CharAt 의 순서로 배열한다.
                    ParsedPair parsedIndex = parseLine(syntax.references[0], lineNumber);

                    // 인덱스 파싱 중 에러가 발생했다면 건너 뛴다.
                    if (parsedIndex == null)
                        return null;

                    // 인덱스가 정수가 아닐 경우
                    if (parsedIndex.type != "number")
                    {
                        Debug.reportError("Type error 37", "문자열의 인덱스가 정수가 아닙니다.", lineNumber);
                        return null;
                    }

                    List<Token> _result = new List<Token>();
                    _result.Add(syntax.array);
                    _result.Concat(parsedIndex.data);
                    _result.Add(Token.findByType(Type.CharAt));

                    // 결과를 리턴한다.
                    return new ParsedPair(_result, "string");
                }

                // 파싱된 인덱스들
                List<List<Token>> parsedReferences = new List<List<Token>>();

                // 가장 낮은 인덱스부터 차례로 파싱한다.
                foreach (int i in Enumerable.Range(0, syntax.references.Count))
                {

                    List<Token> reference = syntax.references[syntax.references.Count - 1 - i];

                    ParsedPair parsedReference = parseLine(reference, lineNumber);

                    if (parsedReference == null)
                        continue;

                    // 인덱스가 정수가 아닐 경우
                    if (parsedReference.type != "number")
                    {
                        Debug.reportError("Type error 38", "배열의 인덱스가 정수가 아닙니다.", lineNumber);
                        continue;
                    }

                    // 할당
                    parsedReferences.Add(parsedReference.data);
                }

                // A[a][b][c] 를 c b a A Array_reference(3) 로 배열한다.
                List<Token> result = TokenTools.merge(parsedReferences);
                result.Add(syntax.array);
                result.Add(new Token(Type.ArrayReference, parsedReferences.Count.ToString()));

                // 리턴 타입은 어떤 타입이라도 될 수 있다.
                return new ParsedPair(result, "*");
            }

            /**
             * 캐스팅: stuff as number
             */
            else if (CastingSyntax.match(tokens))
            {

                CastingSyntax syntax = CastingSyntax.analyze(tokens, lineNumber);

                if (syntax == null)
                    return null;

                // 캐스팅 대상을 파싱한 후 끝에 캐스팅 명령을 추가한다.
                ParsedPair parsedTarget = parseLine(syntax.target, lineNumber);



                if (parsedTarget == null)
                    return null;

                // 문자형으로 캐스팅
                if (syntax.castingType == "string")
                {

                    // 아직은 숫자 -> 문자만 가능하다.
                    if (parsedTarget.type != "number" && parsedTarget.type != "bool" && parsedTarget.type != "*")
                    {
                        Debug.reportError("Type error 39", "이 타입을 문자형으로 캐스팅할 수 없습니다.", lineNumber);
                        return null;
                    }

                    List<Token> result = parsedTarget.data;
                    result.Add(Token.findByType(Type.CastToString));

                    // 캐스팅된 문자열을 출력
                    return new ParsedPair(result, "string");
                }

                // 실수형으로 캐스팅
                else if (syntax.castingType == "number")
                {

                    // 아직은 문자 -> 숫자만 가능하다.
                    if (parsedTarget.type != "string" && parsedTarget.type != "bool" && parsedTarget.type != "*")
                    {
                        Debug.reportError("Type error 40", "이 타입을 실수형으로 캐스팅할 수 없습니다.", lineNumber);
                        return null;
                    }

                    List<Token> result = parsedTarget.data;
                    result.Add(Token.findByType(Type.CastToNumber));

                    // 캐스팅된 문자열을 출력
                    return new ParsedPair(result, "number");
                }

                // 그 외의 경우
                else
                {

                    // 캐스팅 타입이 적절한지 체크한다.
                    if (symbolTable.getClass(syntax.castingType) == null)
                    {
                        Debug.reportError("Undefined Error 41", "올바르지 않은 타입입니다.", lineNumber);
                        return null;
                    }

                    // 표면적으로만 캐스팅한다. -> [경고] 실질적인 형 검사가 되지 않기 때문에 VM이 죽을 수도 있다.
                    return new ParsedPair(parsedTarget.data, syntax.castingType);
                }

            }

            /**
             * 접두형 단항 연산자: !(true) , ++a
             */
            else if (PrefixSyntax.match(tokens))
            {

                PrefixSyntax syntax = PrefixSyntax.analyze(tokens, lineNumber);

                if (syntax == null)
                    return null;

                // 피연산자를 파싱한다.
                ParsedPair parsedOperand = parseLine(syntax.operand, lineNumber);

                if (parsedOperand == null)
                    return null;

                // 어드레스 취급하는 경우	
                if (syntax._operator.type == Type.PrefixIncrement || syntax._operator.type == Type.PrefixDecrement)
                {

                    // 배열이나 맴버 변수 대입이면
                    if (parsedOperand.data[parsedOperand.data.Count - 1].type == Type.ArrayReference)
                    {
                        parsedOperand.data[parsedOperand.data.Count - 1].useAsAddress = true;
                        syntax._operator.useAsArrayReference = true;
                    }

                    // 전역/로컬 변수 대입이면
                    else if (parsedOperand.data.Count == 1)
                    {
                        parsedOperand.data[parsedOperand.data.Count - 1].useAsAddress = true;
                    }

                    // 그 외의 경우
                    else
                    {
                        Debug.reportError("Type error 44", "증감 연산자 사용이 잘못되었습니다.", lineNumber);
                        return null;
                    }
                }

                // 접두형 연산자의 경우 숫자만 올 수 있다.
                if (parsedOperand.type != "number" && parsedOperand.type != "*")
                {
                    TokenTools.view1D(tokens);
                    Debug.reportError("Type error 43", "접두형 연산자 뒤에는 실수형 데이터만 올 수 있습니다.", lineNumber);
                    return null;
                }

                List<Token> result = parsedOperand.data;
                result.Add(syntax._operator);

                // 결과를 리턴한다.
                return new ParsedPair(result, parsedOperand.type);
            }

            /**
             * 접미형 단항 연산자: a++
             */
            else if (SuffixSyntax.match(tokens))
            {

                SuffixSyntax syntax = SuffixSyntax.analyze(tokens, lineNumber);

                if (syntax == null)
                    return null;

                // 피연산자를 파싱한다.
                ParsedPair parsedOperand = parseLine(syntax.operand, lineNumber);

                if (parsedOperand == null)
                    return null;

                // 어드레스 취급하는 경우	
                if (syntax._operator.type == Type.SuffixIncrement || syntax._operator.type == Type.SuffixDecrement)
                {

                    // 배열이나 맴버 변수 대입이면
                    if (parsedOperand.data[parsedOperand.data.Count - 1].type == Type.ArrayReference)
                    {

                        parsedOperand.data[parsedOperand.data.Count - 1].useAsAddress = true;
                        syntax._operator.useAsArrayReference = true;
                    }

                    // 전역/로컬 변수 대입이면
                    else if (parsedOperand.data.Count == 1)
                    {
                        parsedOperand.data[parsedOperand.data.Count - 1].useAsAddress = true;
                    }

                    // 그 외의 경우
                    else
                    {
                        Debug.reportError("Type error 44", "증감 연산자 사용이 잘못되었습니다.", lineNumber);
                        return null;
                    }

                }

                // 접두형 연산자의 경우 숫자만 올 수 있다.
                if (parsedOperand.type != "number" && parsedOperand.type != "*")
                {
                    Debug.reportError("Type error 45", "접미형 연산자 앞에는 실수형 데이터만 올 수 있습니다.", lineNumber);
                    return null;
                }

                List<Token> result = parsedOperand.data;
                result.Add(syntax._operator);

                // 결과를 리턴한다.
                return new ParsedPair(result, parsedOperand.type);
            }

            /**
             * 이항 연산자: a+b
             */
            else if (InfixSyntax.match(tokens))
            {

                InfixSyntax syntax = InfixSyntax.analyze(tokens, lineNumber);

                if (syntax == null)
                    return null;

                // 양 항을 모두 파싱한다.
                ParsedPair left = parseLine(syntax.left, lineNumber);
                ParsedPair right = parseLine(syntax.right, lineNumber);

                if (left == null || right == null)
                {
                    return null;
                }

                // 시스템 값 참조 연산자일 경우
                if (syntax._operator.type == Type.RuntimeValueAccess)
                {

                    // 에러를 막기 위해 타입을 임의로 지정한다.
                    left.type = right.type = "number";
                }

                // 와일드카드 처리, 와일드카드가 양 변에 한 쪽이라도 있으면
                if (left.type == "*" || right.type == "*")
                {

                    // 와일드카드가 없는 쪽으로 통일한다.
                    if (left.type != "*")
                        right.type = left.type;

                    else if (right.type != "*")
                        left.type = right.type;

                    // 모두 와일드카드라면. (배열 원소와 배열 원소끼리 연산)
                    else
                    {
                        // 양 쪽 모두 숫자 처리
                        left.type = right.type = "number";
                    }
                }

                if (right.type == "bool") right.type = "number";
                if (left.type == "bool") left.type = "number";

                // 형 체크 프로세스: 두 항 타입이 같을 경우
                if (left.type == right.type)
                {

                    // 만약 문자열에 대한 이항 연산이라면, 대입/더하기만 허용한다.
                    if (left.type == "string")
                    {
                        // 산술 연산자를 문자열 연산자로 수정한다.
                        switch (syntax._operator.type)
                        {
                            case Type.AdditionAssignment:
                                syntax._operator = Token.findByType(Type.AppendAssignment);
                                break;
                            case Type.Addition:
                                syntax._operator = Token.findByType(Type.Append);
                                break;
                            case Type.EqualTo:
                            case Type.NotEqualTo:
                                left.type = right.type = "number";
                                break;
                            // 문자열 - 문자열 대입이면 SDW명령을 활성화시킨다.
                            case Type.Assignment:
                                syntax._operator.value = "string";
                                break;
                            default:
                                Debug.reportError("Syntax error 47", "이 연산자로 문자열 연산을 수행할 수 없습니다.", lineNumber);
                                return null;
                        }

                    }

                    // 숫자에 대한 이항 연산일 경우
                    else if (left.type == "number")
                    {

                        switch (syntax._operator.type)
                        {
                            // 실수형 - 실수형 대입이면 NDW명령을 활성화시킨다.
                            case Type.Assignment:
                                syntax._operator.value = "number";
                                break;
                            default:
                                break;
                        }

                    }

                    // 그 외의 배열이나 인스턴스의 경우
                    else
                    {
                        switch (syntax._operator.type)
                        {
                            // 인스턴스 - 인스턴스 대입이면 NDW명령을 활성화시킨다.
                            case Type.Assignment:
                                syntax._operator.value = "instance";
                                break;
                            default:
                                Debug.reportError("Syntax error 48", "대입 명령을 제외한 이항 연산자는 문자/숫자 이외의 처리를 할 수 없습니다.", lineNumber);
                                return null;
                        }
                    }

                }

                // 형 체크 프로세스: 두 항의 타입이 다를 경우
                else
                {

                    // 자동 캐스팅을 시도한다.
                    switch (syntax._operator.type)
                    {
                        case Type.Addition:

                            // 문자 + 숫자
                            if (left.type == "string" && right.type == "number")
                            {

                                right.data.Add(Token.findByType(Type.CastToString));
                                right.type = "string";

                                // 연산자를 APPEND로 수정한다.
                                syntax._operator = Token.findByType(Type.Append);

                            }

                            // 숫자 + 문자
                            else if (left.type == "number" && right.type == "string")
                            {

                                left.data.Add(Token.findByType(Type.CastToString));
                                left.type = "string";

                                // 연산자를 APPEND로 수정한다.
                                syntax._operator = Token.findByType(Type.Append);

                            }
                            else
                            {
                                Debug.reportError("Syntax error 49", "다른 두 타입 간 연산을 실행할 수 없습니다.", lineNumber);
                                return null;
                            }
                            break;
                        case Type.AdditionAssignment:

                            // 문자 + 숫자
                            if (left.type == "string" && right.type == "number")
                            {
                                right.data.Add(Token.findByType(Type.CastToString));
                                right.type = "string";

                                // 연산자를 APPEND로 수정한다.
                                syntax._operator = Token.findByType(Type.AppendAssignment);

                            }
                            else
                            {
                                Debug.reportError("Syntax error 49", "다른 두 타입 간 연산을 실행할 수 없습니다.", lineNumber);
                                return null;
                            }
                            break;

                        default:
                            TokenTools.view1D(tokens);
                            Debug.reportError("Syntax error 50", "다른 두 타입(" + left.type + "," + right.type + ") 간 연산을 실행할 수 없습니다.", lineNumber);
                            return null;
                    }
                }

                // 대입 명령이면
                if (syntax._operator.getPrecedence() > 15)
                {

                    // 배열이나 맴버 변수 대입이면
                    if (left.data[left.data.Count - 1].type == Type.ArrayReference)
                    {
                        left.data[left.data.Count - 1].useAsAddress = true;
                        syntax._operator.useAsArrayReference = true;
                    }

                    // 전역/로컬 변수 대입이면
                    else
                    {
                        left.data[left.data.Count - 1].useAsAddress = true;
                        syntax._operator.useAsArrayReference = false;
                    }
                }

                // 시스템 값 참조 연산자일 경우
                if (syntax._operator.type == Type.RuntimeValueAccess)
                {

                    // 에러를 막기 위해 타입을 임의로 지정한다.
                    left.type = right.type = "*";
                }

                // 형 체크가 끝나면 좌, 우 변을 잇고 리턴한다.
                List<Token> result = left.data.Concat(right.data).ToList<Token>();
                result.Add(syntax._operator);

                return new ParsedPair(result, right.type);
            }
            TokenTools.view1D(tokens);
            Debug.reportError("Syntax error 51", "연산자가 없는 식입니다.", lineNumber);
            return null;
        }


        /**
         * 스코프 내의 프로시져와 오브젝트 정의를 읽어서 테이블에 기록한다.
         * 
         * @param	block
         * @param	scanOption
         */
        void scan(Lextree block, ScanOption option)
        {

            // 구조체 스캔일 경우 맴버변수를 저장할 공간 생성
            List<VariableSymbol> members = null;

            // 임시 스코프용
            List<Symbol> definedSymbols = new List<Symbol>();

            if (option.inStructure)
            {
                members = new List<VariableSymbol>();
            }

            int i = -1;
            while (++i < block.branch.Count)
            {

                Lextree line = block.branch[i];
                int lineNumber = line.lineNumber;

                // 만약 유닛에 가지가 있다면 넘어감
                if (line.hasBranch)
                    continue;

                List<Token> tokens = line.lexData;

                if (tokens.Count < 1)
                    continue;

                if (VariableDeclarationSyntax.match(tokens))
                {

                    // 변수(맴버 변수)는 구조체에서만 스캔한다.
                    if (!option.inStructure)
                        continue;

                    // 스캔시에는 에러를 표시하지 않는다. (파싱 단계에서 표시)
                    Debug.supressError(true);

                    VariableDeclarationSyntax syntax = VariableDeclarationSyntax.analyze(tokens, lineNumber);

                    Debug.supressError(false);

                    if (syntax == null)
                        continue;

                    // 변수 심볼을 생성한다.
                    VariableSymbol variable = new VariableSymbol(syntax.variableName.value, syntax.variableType.value);

                    // 이미 사용되고 있는 변수인지 체크
                    if (symbolTable.getVariable(variable.id) != null)
                    {
                        Debug.reportError("Duplication error 52", "변수 정의가 충돌합니다.", lineNumber);
                        continue;
                    }

                    // 심볼 테이블에 추가
                    definedSymbols.Add(variable);
                    symbolTable.add(variable);

                    // 메모리에 할당
                    if (variable.type == "number" || variable.type == "string" || variable.type == "bool")
                        assembly.writeCode("SAL " + variable.address);
                    else
                        assembly.writeCode("SAA " + variable.address);

                    // 초기화 데이터가 존재할 경우
                    if (syntax.initializer != null)
                    {
                        variable.initialized = true;

                        // 초기화문을 파싱한 후 어셈블리에 쓴다.
                        ParsedPair parsedInitializer = parseLine(syntax.initializer, lineNumber);

                        if (parsedInitializer == null) continue;
                        assembly.writeLine(parsedInitializer.data);
                    }



                    members.Add(variable);
                }
                else if (FunctionDeclarationSyntax.match(tokens))
                {

                    // 올바르지 않은 선언문일 경우 건너 뛴다.
                    FunctionDeclarationSyntax syntax = FunctionDeclarationSyntax.analyze(tokens, lineNumber);

                    // 스캔시에는 에러를 표시하지 않는다. (파싱 단계에서 표시)
                    Debug.supressError(true);

                    if (syntax == null)
                        continue;

                    Debug.supressError(false);

                    List<VariableSymbol> parameters = new List<VariableSymbol>();
                    List<String> parametersTypeList = new List<String>();

                    // 매개변수 각각의 유효성을 검증하고 심볼 형태로 가공한다.
                    foreach (int k in Enumerable.Range(0, syntax.parameters.Count))
                    {

                        if (!ParameterDeclarationSyntax.match(syntax.parameters[k]))
                        {
                            TokenTools.view2D(syntax.parameters);
                            Debug.reportError("Syntax error 53", "파라미터 정의가 올바르지 않습니다.", lineNumber);
                            continue;
                        }
                        // 매개 변수의 구문을 분석한다.
                        ParameterDeclarationSyntax parameterSyntax = ParameterDeclarationSyntax.analyze(syntax.parameters[k], lineNumber);

                        // 매개 변수 선언문에 Syntax error가 있을 경우 건너 뛴다.
                        if (parameterSyntax == null)
                            continue;

                        // 매개 변수 이름의 유효성을 검증한다.
                        if (symbolTable.getVariable(parameterSyntax.parameterName.value) != null)
                        {
                            Debug.reportError("Duplication error 54", parameterSyntax.parameterName.value + " 변수 정의가 충돌합니다.", lineNumber);
                            continue;
                        }

                        // 매개 변수 타입의 유효성을 검증한다.
                        if (symbolTable.getClass(parameterSyntax.parameterType.value) == null)
                        {
                            Debug.reportError("Duplication error 55", "매개 변수 타입이 유효하지 않습니다.", lineNumber);
                            continue;
                        }

                        // 매개 변수 심볼을 생성한다
                        VariableSymbol parameter = new VariableSymbol(parameterSyntax.parameterName.value, parameterSyntax.parameterType.value);
                        parameterSyntax.parameterName.setTag(parameter);
                        parameters[k] = parameter;
                    }

                    // 함수 정의 충돌을 검사한다.
                    if (symbolTable.getFunction(syntax.functionName.value, parametersTypeList) != null)
                    {
                        Debug.reportError("Duplication error 56", "함수 정의가 충돌합니다.", lineNumber);
                        continue;
                    }

                    FunctionSymbol functn = new FunctionSymbol(syntax.functionName.value, syntax.returnType.value, parameters);

                    // 프로시져 시작 부분과 종결 부분을 나타내는 플래그를 생성한다.
                    functn.functionEntry = assignFlag();
                    functn.functionExit = assignFlag();

                    // 함수 토큰을 태그한다.
                    syntax.functionName.setTag(functn);

                    // 프로시져를 심볼 테이블에 추가한다.
                    symbolTable.add(functn);
                }
                else if (ClassDeclarationSyntax.match(tokens))
                {

                    // 오브젝트 선언 구문을 분석한다.
                    ClassDeclarationSyntax syntax = ClassDeclarationSyntax.analyze(tokens, lineNumber);

                    // 오브젝트 선언 구문에 에러가 있을 경우 건너 뛴다.
                    if (syntax == null)
                        continue;

                    // 오브젝트 이름의 유효성을 검증한다.
                    if (symbolTable.getClass(syntax.className.value) != null)
                    {
                        Debug.reportError("Syntax error 56", "오브젝트 정의가 중복되었습니다.", lineNumber);
                        continue;
                    }

                    // 오브젝트 구현부가 존재하는지 확인한다.
                    if (!hasNextBlock(block, i))
                    {
                        Debug.reportError("Syntax error 57", "구조체의 구현부가 존재하지 않습니다.", lineNumber);
                        continue;
                    }

                    // 오브젝트를 심볼 테이블에 추가한다.
                    ClassSymbol klass = new ClassSymbol(syntax.className.value);

                    symbolTable.add(klass);

                    // 클래스 내부의 클래스는 지금 스캔하지 않는다.
                    if (option.inStructure)
                        continue;

                    // 오브젝트의 하위 항목을 스캔한다.
                    ScanOption objectOption = option.copy();
                    objectOption.inStructure = true;
                    objectOption.parentClass = klass;


                    scan(block.branch[++i], objectOption);
                }
            }

            // 만약 구조체 스캔일 경우 맴버 변수와 프로시져 정의를 오브젝트 심볼에 쓴다.
            if (option.inStructure)
            {
                option.parentClass.members = members;
            }

            foreach (int j in Enumerable.Range(0, definedSymbols.Count))
            {
                symbolTable.remove(definedSymbols[j]);
            }
        }

        /**
         * 다다음 인덱스에 이어지는 조건문이 존재하는지 확인한다.
         * 
         * 
         * @param tree
         * @param index
         * @return
         */
        private bool hasNextConditional(Lextree tree, int index)
        {

            // 다다음 인덱스가 존재하고,
            if (index + 2 < tree.branch.Count)
            {

                Lextree possibleBranch = tree.branch[index + 2];
                if (!possibleBranch.hasBranch && possibleBranch.lexData.Count > 0)
                {
                    Token firstToken = possibleBranch.lexData[0];

                    // 이어지는 조건문이 있을 경우
                    if (firstToken.type == Type.Else)
                        return true;
                }
            }
            return false;
        }

        /** 
         * 다음 코드 블록이 존재하는지의 여부를 리턴한다.
         * 
         * @param tree
         * @param index
         * @return
         */
        private bool hasNextBlock(Lextree tree, int index)
        {
            if ((!(index < tree.branch.Count)) || !tree.branch[index + 1].hasBranch)
                return false;
            return true;
        }
    }
}

/**
 * 파싱된 페어
 */
class ParsedPair
{
    public List<Token> data;
    public string type;

    public ParsedPair(List<Token> data, string type)
    {
        this.data = data;
        this.type = type;
    }
}

/**
 * 파싱 옵션 클래스
 */
class ParseOption
{

    /**
     * 파싱 옵션
     */
    public bool inStructure = false;
    public bool inFunction = false;
    public bool inIterator = false;

    /**
     * 블록의 시작과 끝 옵션
     */
    public int blockEntry = 0;
    public int blockExit = 0;

    /**
     * 함수 내부일 경우의 함수 참조
     */
    public FunctionSymbol parentFunction;

    /**
     * 파싱 옵션 복사
     * 
     * @return
     */
    public

    ParseOption copy()
    {

        ParseOption option = new ParseOption();

        option.inStructure = inStructure;
        option.inFunction = inFunction;
        option.inIterator = inIterator;
        option.blockEntry = blockEntry;
        option.blockExit = blockExit;
        option.parentFunction = parentFunction;

        return option;
    }

}

/**
 * 스캔 옵션 클래스
 */
class ScanOption
{

    /**
     * 스캔 옵션
     */
    public bool inStructure = false;

    /**
     * 함수 내부일 경우의 함수 참조
     */
    public ClassSymbol parentClass;

    /**
     * 스캔 옵션 복사
     * 
     * @return
     */
    public ScanOption copy()
    {

        ScanOption option = new ScanOption();

        option.inStructure = inStructure;
        option.parentClass = parentClass;

        return option;
    }
}
