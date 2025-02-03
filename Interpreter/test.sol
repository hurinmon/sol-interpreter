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