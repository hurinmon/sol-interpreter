﻿# Interpreter

이 프로젝트는 간단한 인터프리터를 구현한 것입니다. 이 인터프리터는 주어진 소스 파일을 해석하고 실행합니다.

## 요구 사항

- .NET 8.0
- C# 12.0

## 프로젝트 구조

- `Program.cs`: 프로그램의 진입점입니다.
- `Lexer.cs`: 소스 파일을 토큰으로 분리하는 어휘 분석기입니다.
- `Interpreter.csproj`: 프로젝트 파일로, .NET 8.0을 타겟으로 합니다.

## 사용법

1. 이 저장소를 클론합니다.
2. 필요한 .NET SDK를 설치합니다.

3. 프로젝트를 빌드하고 실행합니다.
## 주요 클래스 및 메서드

### Program

- `Main(string[] args)`: 프로그램의 진입점으로, 어휘 분석기와 인터프리터를 초기화하고 실행합니다.

### Lexer

- `Lexer(string workDirectory, string path)`: 주어진 작업 디렉토리와 파일 경로를 사용하여 어휘 분석기를 초기화합니다.
- `GetNextToken()`: 다음 토큰을 반환합니다.
- `GetImports()`: 소스 파일에서 import 구문을 추출합니다.

### 예제 스크립트
```javascript
function logTest() 
{
	Log('로그 테스트');
	return '리턴';
}

function logTest2(msg, a, b) 
{
	Log( '인자로 받은 mgs(인자1) : ' + msg);
	sum = a + b;
	Log( '인자로 받은 값 Sum(21) : ' + sum + ' ' + (a + b));
}

mailIds = { '300', '301', 10};

test = logTest();
for(i = 0; i < 2; i++)
{
	Log('for문 테스트 이 문장은 두번 ' + '출력됩니다.');
}
Log('함수 리턴값은?(리턴) : '+ test);
Log('1~10 사이 랜덤 테스트 : ' + RandomRange(1,10));
if(RandomRange(1,10) > 5)
{
	Log('여긴 50%의 확률로 출력됩니다.');
}

boolTest = !false;
Log('!bool 테스트(true) : ' +  boolTest);

foreach(id in mailIds)
{
	Log('foreach 테스트 (300, 301, 10): ' + id);
}

logTest2('인자1', 10, 11);
for (y = 0; y < 2; y++) {
	for (i = 0; i < 2; i++) {
		if (i == 0) {
			Log('test sfs' + '  add ' + i + y);
		}
	}
}

Log('% 테스트 입니다.(1) : ' + 10 % 3);

loopCount = 0;
loop = true;
while (loop)
{
	if (loopCount == 2)
		break;

	Log('while문 테스트(2) : ' + loopCount);
	loopCount = loopCount + 1;
}

if (loop == false)
{
	Log('if 오류');
}
else
{
	Log('else 문 테스트 입니다.');
}