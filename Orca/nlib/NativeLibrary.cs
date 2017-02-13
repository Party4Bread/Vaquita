using Orca.symbol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.nlib
{
    /**
     * ...
     * @author 김 현준
     */
    class NativeLibrary
    {

        public static bool initialized = false;

        public List<NativeClass> classes;
        public List<NativeFunction> functions;

        public NativeLibrary()
        {
            initialize();
        }

        public void initialize()
        {
            if (initialized) return;
            initialized = true;

            classes = new List<NativeClass>();
            functions = new List<NativeFunction>();
            List<NativeFunction> temp = new List<NativeFunction>();
            NativeClass number = new NativeClass("number", temp);
            NativeClass _string = new NativeClass("string", temp);
            NativeClass boolean = new NativeClass("bool", temp);
            NativeClass array = new NativeClass("array", temp);
            NativeClass _void = new NativeClass("void", temp);
            List<string> None=new List<string>(), One = new List<string>(), Two= new List<string>();
            One.Add("number");
            Two.Add("number");
            Two.Add("number");
            List<string> temp2 = new List<string>();
            temp2.Add("*");

            NativeFunction print = new NativeFunction("print", temp2, "void");
            print.write("IVK 1");

            NativeFunction read = new NativeFunction("read", None, "string");
            read.write("IVK 2");

            NativeFunction exit = new NativeFunction("exit", None, "void");
            exit.write("END");

            NativeFunction info = new NativeFunction("info", None, "void");
            info.write("IVK 3");

            NativeFunction abs = new NativeFunction("abs", One, "number");
            abs.write("IVK 4");

            NativeFunction acos = new NativeFunction("acos", One, "number");
            acos.write("IVK 5");

            NativeFunction asin = new NativeFunction("asin", One, "number");
            asin.write("IVK 6");

            NativeFunction atan = new NativeFunction("atan", One, "number");
            atan.write("IVK 7");

            NativeFunction atan2 = new NativeFunction("atan2", Two, "number");
            atan2.write("IVK 8");

            NativeFunction ceil = new NativeFunction("ceil", One, "number");
            ceil.write("IVK 9");

            NativeFunction floor = new NativeFunction("floor", One, "number");
            floor.write("IVK 10");

            NativeFunction round = new NativeFunction("round", One, "number");
            round.write("IVK 11");

            NativeFunction cos = new NativeFunction("cos", One, "number");
            cos.write("IVK 12");

            NativeFunction sin = new NativeFunction("sin", One, "number");
            sin.write("IVK 13");

            NativeFunction tan = new NativeFunction("tan", One, "number");
            tan.write("IVK 14");

            NativeFunction log = new NativeFunction("log", One, "number");
            log.write("IVK 15");

            NativeFunction sqrt = new NativeFunction("sqrt", One, "number");
            sqrt.write("IVK 16");

            NativeFunction pow = new NativeFunction("pow", Two, "number");
            pow.write("IVK 17");

            NativeFunction random = new NativeFunction("random", None, "number");
            random.write("IVK 18");

            addClass(number);
            addClass(_string);
            addClass(array);
            addClass(boolean);
            addClass(_void);

            addFunction(print);
            addFunction(read);
            addFunction(info);
            addFunction(exit);
            addFunction(abs);
            addFunction(asin);
            addFunction(acos);
            addFunction(atan);
            addFunction(atan2);
            addFunction(ceil);
            addFunction(floor);
            addFunction(round);
            addFunction(cos);
            addFunction(sin);
            addFunction(tan);
            addFunction(log);
            addFunction(sqrt);
            addFunction(pow);
            addFunction(random);

        }

        /**
         * 네이티브 라이브러리에 클래스를 추가한다.
         * 
         * @param	nativeClass
         */
        public void addClass(NativeClass nativeClass)
        {
            classes.Add(nativeClass);
        }

        /**
         * 네이티브 라이브러리에 함수를 추가한다.
         * 
         * @param	nativeFunction
         */
        public void addFunction(NativeFunction nativeFunction)
        {
            functions.Add(nativeFunction);
        }

        /**
         * 네이티브 라이브러리를 심볼 테이블에 로드한다.
         * 
         * @param symbolTable
         */
        public void load(SymbolTable symbolTable)
        {

            // 클래스 입력
            foreach (int i in Enumerable.Range(0, classes.Count))
            {
                symbolTable.classes.Add(new ClassSymbol(classes[i].className));
            }

            // 함수 입력
            foreach (int i in Enumerable.Range(0, functions.Count))
            {
                List<VariableSymbol> parameters = new List<VariableSymbol>();

                // 파라미터 처리
                foreach (int j in Enumerable.Range(0, functions[i].parameters.Count))
                    parameters.Add(new VariableSymbol("native_arg_" + j.ToString(), functions[i].parameters[j]));

                // 함수 심볼 객체 생성
                FunctionSymbol functn = new FunctionSymbol(functions[i].functionName, functions[i].returnType, parameters);

                functn.isNative = true;
                functn.nativeFunction = functions[i];

                symbolTable.functions.Add(functn);
            }
        }
    }
}