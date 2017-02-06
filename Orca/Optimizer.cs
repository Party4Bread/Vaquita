using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orca
{
    /**
     * Orca Assembly Optimizer
     * 
     * @author 김 현준
     */
    class Optimizer
    {

        /**
         * 플래그 맵
         */
        public Dictionary<int, int> flags;	
	
	/**
	 * 어셈블리 코드에 대해 최적화 작업을 수행한다.
	 * 
	 * @param code
	 * @return
	 */
	public string optimize(string code) {

		flags = new Dictionary<int, int>();//Map

		// 줄 바꿈 문자로 코드를 분할한다.
		List<string> lines = code.Split('\n').ToList();

		// 점프문의 위치
		List<int> jumps = new List<int>();

		int totalLines = 0;

		// 플래그를 쭉 스캔한다.
		foreach (int i in Enumerable.Range(0,lines.Count)) {
			// 빈 라인이라면 넘어가기
			if (lines[i].Length < 1)
				continue;
			if (lines[i].Length < 5) {
				totalLines++;
				continue;
			}
			// 라벨 플래그 생성이라면
			if (lines[i].Substring(0, 5) == "FLG %")
				flags.Add(int.Parse(lines[i].Substring(5)), totalLines);
			else
				totalLines++;
			// PUSH 문이라면
			if (lines[i].Substring(0, 3) == "PSH")
				jumps.Add(i);
		}

		// JUMP 명령에 있는 플래그를 모두 치환한다.
		foreach (int i in Enumerable.Range(0,jumps.Count)) { 

			string jump = lines[jumps[i]];

			// 플래그가 없는 JUMP 명령은 무시한다.
			if (jump.IndexOf("%") < 0)
				continue;

                // 플래그 라인 넘버를 취득한다.
                int lineNumber = 0; 
                    flags.TryGetValue(int.Parse(jump.Substring(jump.IndexOf("%") + 1)),out lineNumber);

			// 플래그를 라인 넘버로 치환한다.
			lines[jumps[i]] = jump.Substring(0, jump.IndexOf("%")) + lineNumber.ToString();

		}

		string buffer = "";

		// 새 명령을 반환한다.
		foreach (int i in Enumerable.Range(0,lines.Count)) { 
			if (lines[i].Length< 1)
				continue;
			if (lines[i].Substring(0, 3) != "FLG")
				buffer += lines[i] + "\n";
		}

		return buffer;
	}
	
}
}
