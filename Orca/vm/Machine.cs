using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca.vm
{
    /**
     * Orca Advanced Orca Virtual Machine
     * 
     * @author 김 현준
     */
    class Machine
    {

        private static Dictionary<string, int> opcode = new Dictionary<string, int>();



        private static string undefined = "undefined";

        /**
         * VM 환경 변수
         */
        public int maximumStackSize = 1024 * 20;
        public int dynamicMemoryIndex;

        /**
         * 시스템 레지스터, 스택
         */
        public Memory memory;
        public List<object> register;
        public List<object> mainStack;
        public List<int> callStack;
        public List<List<int>> scope;

        /**
         * 프로그램
         */
        public List<Instruction> program;
        public int pointer;

        /**
         * 벨루가 머신 초기화
         */
        public Machine(int maximumStackSize = 1024 * 20)
        {
            this.maximumStackSize = maximumStackSize;
            opcode.Add("PSH", 0x1);
            opcode.Add("PSR", 0x2);
            opcode.Add("PSM", 0x3);
            opcode.Add("POP", 0x4);
            opcode.Add("OPR", 0x5);
            opcode.Add("JMP", 0x6);
            opcode.Add("JMF", 0x7);
            opcode.Add("IVK", 0x8);
            opcode.Add("SAL", 0x9);
            opcode.Add("SAA", 0xA);
            opcode.Add("DAL", 0xB);
            opcode.Add("DAA", 0xC);
            opcode.Add("STO", 0xD);
            opcode.Add("STA", 0xE);
            opcode.Add("OSC", 0xF);
            opcode.Add("CSC", 0x10);
            opcode.Add("FRE", 0x11);
            opcode.Add("RDA", 0x12);
            opcode.Add("PSC", 0x13);
            opcode.Add("MOC", 0x14);
            opcode.Add("END", 0x15);
        }
        /**
         * 벨루가 머신에 어셈블리를 로드한다.
         */
        public void load(string assembly)
        {

            // 어셈블리를 캐시한다.
            program = parseAssembly(assembly);

            // 시스템 초기화
            memory = new Memory(dynamicMemoryIndex);
            register = new List<object>();
            mainStack = new List<object>();
            callStack = new List<int>();
            scope = new List<List<int>>();

            // 최상위 스코프
            scope.Add(new List<int>());

            pointer = 0;
        }

        public void run()
        {

            while (true)
            {
                Instruction inst = program[pointer];

                switch (inst.opcode)
                {
                    // PSH
                    case 1: mainStack.Add(inst.arg);
                    // PSR	
                    case 2: mainStack.Add(register[inst.intArg]);
                    // PSM	
                    case 3: mainStack.Add(memory.read(inst.intArg));
                    // POP	
                    case 4: register[inst.intArg] = mainStack.pop();
                    // OPR	
                    case 5: mainStack.Add(operate(inst.intArg));
                    // JMP	
                    case 6: pointer = cast(mainStack.pop(), Int); continue;
                    // JMF	
                    case 7:
                        var condition:Dynamic = mainStack.pop();
                        if (mainStack.pop() <= 0)
                        {
                            pointer = cast(condition, Int);
                            continue;
                        }
                    // IVK	
                    case 8: invoke(inst.intArg);
                    // SAL	
                    case 9: memory.allocate(undefined, inst.intArg); scope[scope.length - 1].Add(inst.intArg);
                    // SAA	
                    case 10: memory.allocate(new Array<Dynamic>(), inst.intArg); scope[scope.length - 1].Add(inst.intArg);
                    // DAL
                    case 11: mainStack.Add(memory.allocate(undefined));
                    // DAA	
                    case 12: mainStack.Add(memory.read(memory.allocate(new Array<Dynamic>())));
                    // STO	
                    case 13: memory.write(cast(mainStack.pop(), Int), mainStack.pop());
                    // STA	
                    case 14:
                        var targetArray:Array < Dynamic > = cast(mainStack.pop(), Array<Dynamic>);
                        var targetIndex:Int = cast(mainStack.pop(), Int);
                        targetArray[targetIndex] = mainStack.pop();
                    // OSC
                    case 15: scope.Add(new Array<Int>());
                    // CSC
                    case 16:
                        var currentScope:Array < Int > = scope.pop();
                        for (j in 0...currentScope.length) memory.free(currentScope[j]);
                    // FRE
                    case 17: memory.free(inst.intArg);
                    // RDA	
                    case 18:
                        List<object> targetArray = mainStack.pop() as List<object>;
                        int targetIndex = mainStack.pop() as int;
                        mainStack.Add(targetArray[targetIndex]);
                    // PSC	
                    case 19: callStack.Add(pointer + 3);
                    // MOC	
                    case 20: mainStack.Add(callStack.pop());
                    // END	
                    case 21: break;
                    // 정의되지 않은 명령	
                    default: Sys.println("Undefined opcode error."); break;
                }
                pointer++;
            }
        }


        private object operate(int oprcode)
        {

            object n1 = "";
            object n2 = "";
            object n3 = "";
            int n1Int = 0;
            List<object> n2Array = null;

            switch (oprcode)
            {
                case 9, 10, 46, 47, 48:
				n1 = mainStack.pop();
                case 1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 13, 38, 39, 40, 41, 42, 43, 44, 45, 49, 50:
				n2 = mainStack.pop(); n1 = mainStack.pop();
                case 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25:
				n2 = mainStack.pop(); n1Int = cast(mainStack.pop(), Int);
                case 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37:
				n3 = mainStack.pop();
                    n2Array = cast(mainStack.pop(), Array<Dynamic>);
                    n1Int = cast(mainStack.pop(), Int);
            }
            switch (oprcode)
            {
                case 1: return Convert.ToDecimal(n1) + Convert.ToDecimal(n2);
                case 2: return Convert.ToDecimal(n1) - Convert.ToDecimal(n2);
                case 3: return Convert.ToDecimal(n1) / Convert.ToDecimal(n2);
                case 4: return Convert.ToDecimal(n1) * Convert.ToDecimal(n2);
                case 5: return Convert.ToDecimal(n1) % Convert.ToDecimal(n2);
                case 6: return Convert.ToInt32(n1) & Convert.ToInt32(n2);
                case 7: return Convert.ToInt32(n1) | Convert.ToInt32(n2);
                case 8: return Convert.ToInt32(n1) ^ Convert.ToInt32(n2);
                case 9: return ~Convert.ToInt32(n1);
                case 10: return -Convert.ToDecimal(n1);
                case 11: return Convert.ToInt32(n1) << Convert.ToInt32(n2);
                case 12: return Convert.ToInt32(n1) >> Convert.ToInt32(n2);
                case 13: return n1.ToString() + n2.ToString();
                case 14: return memory.write(n1Int, n2);
                case 15: return memory.write(n1Int, memory.read(n1Int) + n2);
                case 16: return memory.write(n1Int, memory.read(n1Int) - n2);
                case 17: return memory.write(n1Int, memory.read(n1Int) / n2);
                case 18: return memory.write(n1Int, memory.read(n1Int) * n2);
                case 19: return memory.write(n1Int, memory.read(n1Int) % n2);
                case 20: return memory.write(n1Int, memory.read(n1Int) & n2);
                case 21: return memory.write(n1Int, memory.read(n1Int) | n2);
                case 22: return memory.write(n1Int, memory.read(n1Int) ^ n2);
                case 23: return memory.write(n1Int, memory.read(n1Int) << n2);
                case 24: return memory.write(n1Int, memory.read(n1Int) >> n2);
                case 25: return memory.write(n1Int, Std.string(memory.read(n1Int)) + Std.string(n2));
                case 26: return n2Array[n1Int] = n3;
                case 27: return n2Array[n1Int] = n2Array[n1Int] + n3;
                case 28: return n2Array[n1Int] = n2Array[n1Int] - n3;
                case 29: return n2Array[n1Int] = n2Array[n1Int] / n3;
                case 30: return n2Array[n1Int] = n2Array[n1Int] * n3;
                case 31: return n2Array[n1Int] = n2Array[n1Int] % n3;
                case 32: return n2Array[n1Int] = n2Array[n1Int] & n3;
                case 33: return n2Array[n1Int] = n2Array[n1Int] | n3;
                case 34: return n2Array[n1Int] = n2Array[n1Int] ^ n3;
                case 35: return n2Array[n1Int] = n2Array[n1Int] << n3;
                case 36: return n2Array[n1Int] = n2Array[n1Int] >> n3;
                case 37: return n2Array[n1Int] = Std.string(n2Array[n1Int]) + Std.string(n3);
                case 38: return (n1 == n2 ? 1 : 0);
                case 39: return (n1 != n2 ? 1 : 0);
                case 40: return (n1 > n2 ? 1 : 0);
                case 41: return (n1 >= n2 ? 1 : 0);
                case 42: return (n1 < n2 ? 1 : 0);
                case 43: return (n1 <= n2 ? 1 : 0);
                case 44: return (n1 + n2 > 1 ? 1 : 0);
                case 45: return (n1 + n2 > 0 ? 1 : 0);
                case 46: return (n1 < 1 ? 1 : 0);
                case 47: return (cast(n1, String).indexOf(".") > 0) ? Std.parseFloat(n1) : Std.parseInt(n1);
                case 48: return Std.string(n1);
                case 49: return Std.string(n1).charAt(cast(n2, Int));
                case 50: return getRuntimeValue(n1, cast(n2, Int));
            }

            Sys.println("Undefined oprcode error.");
            return null;
        }

        private void invoke(int inkcode)
        {
            switch (inkcode)
            {
                case 1: Sys.println(mainStack.pop());
                case 2: mainStack.push(Sys.stdin().readLine());
                case 3: Sys.println("ORCA VM(BELUGA) UNSTABLE");
                case 4: mainStack.push(Api.abs(cast(mainStack.pop(), Float)));
                case 5: mainStack.push(Api.acos(cast(mainStack.pop(), Float)));
                case 6: mainStack.push(Api.asin(cast(mainStack.pop(), Float)));
                case 7: mainStack.push(Api.atan(cast(mainStack.pop(), Float)));
                case 8: mainStack.push(Api.atan2(cast(mainStack.pop(), Float), cast(mainStack.pop(), Float)));
                case 9: mainStack.push(Api.ceil(cast(mainStack.pop(), Float)));
                case 10: mainStack.push(Api.floor(cast(mainStack.pop(), Float)));
                case 11: mainStack.push(Api.round(cast(mainStack.pop(), Float)));
                case 12: mainStack.push(Api.cos(cast(mainStack.pop(), Float)));
                case 13: mainStack.push(Api.sin(cast(mainStack.pop(), Float)));
                case 14: mainStack.push(Api.tan(cast(mainStack.pop(), Float)));
                case 15: mainStack.push(Api.log(cast(mainStack.pop(), Float)));
                case 16: mainStack.push(Api.sqrt(cast(mainStack.pop(), Float)));
                case 17: mainStack.push(Api.pow(cast(mainStack.pop(), Float), cast(mainStack.pop(), Float)));
                case 18: mainStack.push(Api.random());
                case 27:
                    var counterAddr:Int = cast(mainStack.pop(), Int);
                    var counterMem:Array < Dynamic > = memory.storage[counterAddr];
                    counterMem[counterMem.length - 1]++;
                default: Sys.println("Undefined inkcode error.");
            }
        }

        private object getRuntimeValue(object target, int valueType = 0) {
		switch(valueType) {
			// 배열 길이
			case 0:
				return cast(target, Array<Dynamic>).length;
			// 문자열 길이	
			case 1:
				return cast(target, String).length;
			case 2:
				return cast(target, String).charCodeAt(0);
			case 3:
				var index:Int = cast(target, Int);	
				return String.fromCharCode(index);
		}
		return null;
	}


/**
 * 어셈블리를 인스트럭션 배열로 파싱한다.
 * 
 * @param	assembly
 * @return
 */
private List<Instruction> parseAssembly(string assembly) {

    // 줄바꿈 문자로 어셈블리 코드를 구분한다.
    string[] lines = assembly.Split('\n');

    List<Instruction> instructions = new List<Instruction>();

		// 메타데이터를 읽는다.
		dynamicMemoryIndex = int.Parse(lines[0]);
		
		for (int i in 1...lines.length) { 

			var line = lines[i];
			
			// 명령 식별자를 읽어 온다.
			var mnemonic = line.substring(0, 3);

			// 단문형 명령이라면 추가 바이트를 파싱하지 않는다.
			if (line.length< 4) {
				instructions.push(new Instruction(opcode.get(mnemonic)));
				continue;
			}
			
			var arg:Dynamic = null;
			
			// 명령의 종결 문자로 데이터 타입을 판단한다.
			switch(line.charAt(line.length - 1)) {				
				case "s":
					arg = line.substring(4, line.length - 1);
				default:
					var rawnum:String = StringTools.trim(line.substring(4));					
					if (rawnum.indexOf(".") > 0)
						arg = Std.parseFloat(rawnum);
					else					
						arg = Std.parseInt(rawnum);
			}
			
			// 명령 객체를 생성한다.
			instructions.push(new Instruction(opcode.get(mnemonic), arg));
		}

		return instructions;
	}
}

/**
 * 어셈블리 인스트럭션
 */
class Instruction
{

    public int opcode;

    // 빠른 실행을 위해 미리 캐스팅
    public int intArg = 0;
    public object arg;

    public Instruction(int opcode, object arg = null)
    {
        this.opcode = opcode;
        this.arg = arg;

        if (arg is int)
        {
            intArg = Convert.ToInt32(arg);
        }
    }
}


/**
 * 가상 메모리 스토리지
 */
class Memory
{

    public int dynamicMemoryIndex;
    public List<List<object>> storage;

    public Memory(int dynamicMemoryIndex)
    {
        storage = new List<List<object>>();

        this.dynamicMemoryIndex = dynamicMemoryIndex;
        storage[dynamicMemoryIndex] = new List<object>();
    }

    /**
     * 메모리에 새 데이터를 할당한다.
     * 
     * @param	initValue
     * @param	address
     * @return
     */
    public int allocate(object initValue, int address = -1)
    {

        // 동적 할당이라면 스토리지의 끝에 메모리를 할당한다.
        if (address < 0)
        {
            storage.Add(new List<object>(new object[] { initValue }));
            return storage.Count - 1;
        }
        // 스토리지가 없다면 생성해 준다.
        if (storage[address] == null)
            storage[address] = new List<object>();

        List<object> memory = storage[address];

        memory.Add(initValue);

        return address;
    }

    /**
	 * 데이터를 할당 해제한다.
	 * 
	 * @param	address
	 */
    public void free(int address)
    {
        List<object> memory = storage[address];

        if (memory != null && memory.Count > 0)
        {
            memory.RemoveAt(memory.Count - 1); ;
        }
    }

    /**
	 * 메모리에 데이터를 쓴다.
	 * 
	 * @param	address
	 * @param	data
	 */
    public object write(int address, object data)
    {
        List<object> memory = storage[address];
        memory[memory.Count - 1] = data;
        return data;
    }

    /**
	 * 메모리에 배열 데이터를 쓴다.
	 * 
	 * @param	address
	 * @param	index
	 * @param	value
	 */
    public object writeArray(int address, int index, object data)
    {
        List<object> array = read(address) as List<object>;
        array[index] = data;
        return data;
    }

    /**
	 * 메모리로부터 데이터를 읽어 온다.
	 * 
	 * @param	address
	 * @return
	 */
    public object read(int address)
    {
        List<object> memory = storage[address];
        return memory[memory.Count - 1];
    }

    /**
	 * 메모리로부터 배열 데이터를 읽어 온다.
	 * 
	 * @param	address
	 * @param	index
	 * @return
	 */
    public object readArray(int address, int index)
    {
        List<object> array = read(address) as List<object>;
        return array[index];
    }
}
