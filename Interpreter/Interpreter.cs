using Interpreter.Enum;
using System.Reflection;

namespace Interpreter
{
    public class Interpreter
    {

        private readonly Lexer _lexer;
        private Token _preToken;
        private Token _currentToken;
        private readonly Dictionary<string, object> _variables = new();
        private readonly Dictionary<string, Interpreter> _functions = new();
        private readonly List<Interpreter> _imports = new();
        private bool _excutedImports;

        private Dictionary<string, object> _globalVariables;
        private Dictionary<string, Interpreter> _globalFunctions;
        private List<string> _argNames;
        private bool _asyncCall;

        public bool AsyncCall => _asyncCall;
        public object Result { get; set; }
        public bool Break { get; set; }
        public Interpreter(Lexer lexer)
        {
            _lexer = lexer;
            //_preToken = _currentToken = _lexer.GetNextToken();

            Preprocess();
        }

        private void Preprocess()
        {
            var imports = _lexer.GetImports();
            foreach (var im in imports)
            {
                var lexer = new Lexer(_lexer.WorkDirectory, im);
                var interpreter = new Interpreter(lexer);
                _imports.Add(interpreter);
            }
        }

        private async Task ExcutePreprocess()
        {
            if (!_excutedImports)
            {
                foreach (var im in _imports)
                {
                    await im.Parse();
                }
                _excutedImports = true;
            }
        }

        private Dictionary<string, object> GetVariables()
        {
            if (_globalVariables == null)
                return _variables;

            var newVariables = _globalVariables.ToDictionary(x => x.Key, x => x.Value);
            foreach (var v in _variables)
                newVariables[v.Key] = v.Value;
            return newVariables;
        }

        private Dictionary<string, Interpreter> GetFunctions()
        {
            if (_globalFunctions == null)
                return _functions;
            var newFunctions = _globalFunctions.ToDictionary(x => x.Key, x => x.Value);
            foreach (var v in _functions)
                newFunctions[v.Key] = v.Value;
            return newFunctions;
        }

        public void SetImports(List<Interpreter> imports)
        {
            _imports.Clear();
            _imports.AddRange(imports);
            _excutedImports = true;
        }

        public void SetAsyncCall(bool async)
        {
            _asyncCall = async;
        }

        public void SetGlobal(Dictionary<string, object> globalVariables, Dictionary<string, Interpreter> globalFunctions)
        {
            _globalVariables = globalVariables;
            _globalFunctions = globalFunctions;
        }

        public void SetArgNames(List<string> argNames)
        {
            _argNames = argNames;
        }
        public void SetArgs(List<object> args)
        {
            foreach (var pair in _argNames.Zip(args, (a, b) => (a, b)))
                SetVariable(pair.a, pair.b);
        }
        public void SetVariable(string name, object value)
        {
            _variables[name] = value;
        }

        private void ResetLocal()
        {
            _variables.Clear();
            _functions.Clear();
        }

        private void ResetLexer()
        {
            Result = null;
            _lexer.Reset();
            _preToken = _currentToken = _lexer.GetNextToken();
        }

        private void Error()
        {
            throw new InvalidSyntaxException(_lexer, $"Invalid syntax {_currentToken.Type} {_currentToken.Value}");
        }

        private void EatNewline()
        {
            while (_currentToken.Type == TokenType.Newline)
                Eat(TokenType.Newline);
        }

        private void Eat(TokenType tokenType)
        {
            if (tokenType != TokenType.Newline && _currentToken.Type == TokenType.Newline)
            {
                _preToken = _currentToken;
                _currentToken = _lexer.GetNextToken();
                Eat(tokenType);
                return;
            }

            if (tokenType == TokenType.Eof)
                Error();
            else if (_currentToken.Type == tokenType)
            {
                _preToken = _currentToken;
                _currentToken = _lexer.GetNextToken();
            }
            else
                Error();
        }

        private async Task<object> Factor()
        {
            var token = _currentToken;
            if (token.Type == TokenType.Decimal)
            {
                Eat(TokenType.Decimal);
                return decimal.Parse(token.Value);
            }
            else if (token.Type == TokenType.Minus)
            {
                Eat(TokenType.Minus);
                var decimalString = _currentToken.Value;
                Eat(TokenType.Decimal);
                return -decimal.Parse(decimalString);
            }
            else if (token.Type == TokenType.True)
            {
                Eat(TokenType.True);
                return bool.Parse(token.Value);
            }
            else if (token.Type == TokenType.False)
            {
                Eat(TokenType.False);
                return bool.Parse(token.Value);
            }
            else if (token.Type == TokenType.Not)
            {
                Eat(TokenType.Not);
                var result = await Expr();
                return !(bool)result;
            }
            else if (token.Type == TokenType.Lparen)
            {
                Eat(TokenType.Lparen);
                var result = await Expr();
                Eat(TokenType.Rparen);
                return result;
            }
            else if (token.Type == TokenType.Id)
            {
                if (_lexer.NextToken('('))
                {
                    return await CallFunction();
                }
                else if (_lexer.NextToken('.'))
                {
                    return await CallClassFunction();
                }
                else
                {
                    string variableName = token.Value;
                    Eat(TokenType.Id);
                    return GetVariable(variableName);
                }
            }
            else if (token.Type == TokenType.SingleQuote)
            {
                return ParseString();
            }
            else if (token.Type == TokenType.Lbrace) //배열지원
            {
                return await ParseArray();
            }
            else if (token.Type == TokenType.Await)
            {
                return await ParseAwait();
            }
            else if (token.Type == TokenType.New)
            {
                return await ParseNew();
            }
            else
            {
                Error();
                return null;
            }
        }

        private async Task<object> Term()
        {
            var result = await Factor();

            while (_currentToken.Type == TokenType.Mul ||
                _currentToken.Type == TokenType.Div ||
                _currentToken.Type == TokenType.Modulo)
            {
                Token token = _currentToken;
                if (token.Type == TokenType.Mul)
                {
                    Eat(TokenType.Mul);
                    result = (decimal)result * (decimal)await Factor();
                }
                else if (token.Type == TokenType.Div)
                {
                    Eat(TokenType.Div);
                    result = (decimal)result / (decimal)await Factor();
                }
                else if (token.Type == TokenType.Modulo)
                {
                    Eat(TokenType.Modulo);
                    result = (decimal)result % (decimal)await Factor();
                }
            }

            return result;
        }

        private async Task<object> Expr()
        {
            var result = await Term();

            while (_currentToken.Type == TokenType.Plus || _currentToken.Type == TokenType.Minus)
            {
                Token token = _currentToken;
                if (token.Type == TokenType.Plus)
                {
                    Eat(TokenType.Plus);
                    result = Add(result, await Term());
                }
                else if (token.Type == TokenType.Minus)
                {
                    Eat(TokenType.Minus);
                    result = Minus(result, await Term());
                }
            }

            return result;
        }

        private object Add(object a, object b)
        {
            if (a is string)
            {
                return a.ToString() + b.ToString();
            }
            else if (a is decimal && b is decimal)
            {
                return (decimal)a + (decimal)b;
            }
            else
            {
                return a.ToString() + b.ToString();
            }
        }

        private object Minus(object a, object b)
        {
            if (a is decimal && b is decimal)
            {
                return (decimal)a - (decimal)b;
            }
            throw new InvalidSyntaxException(_lexer, "Invalid Minus");
        }


        private string FuncCondition()
        {
            var result = "";
            var lParenCount = 0;
            while (lParenCount != 0 || (_currentToken.Type != TokenType.Rparen && _currentToken.Type != TokenType.Semi))
            {
                if (_currentToken.Type == TokenType.Lparen)
                    lParenCount++;
                if (_currentToken.Type == TokenType.Rparen)
                    lParenCount--;
                if (_currentToken.Type == TokenType.Await)
                    result += _currentToken.Value + " ";
                else
                    result += _currentToken.Value;

                Eat(_currentToken.Type);
            }
            return result;
        }
        private Func<Task<bool>> ConditionFunc()
        {
            var line = _lexer.Line - 1;

            var conditionBlock = FuncCondition();

            var lexer = new Lexer($"condition_internal_xxxxxx {conditionBlock}");
            lexer.SetLine(line);
            var interpreter = new Interpreter(lexer);
            interpreter.SetImports(_imports);
            interpreter.SetGlobal(GetVariables(), GetFunctions());
            Func<Task<bool>> func = async () =>
            {
                interpreter.SetGlobal(GetVariables(), GetFunctions());
                await interpreter.Parse();
                return (bool)interpreter.Result;
            };
            return func;
        }

        private CompOp GetCompOp()
        {
            CompOp compOp = CompOp.None;
            while (_currentToken.Type == TokenType.Assign ||
                _currentToken.Type == TokenType.Not ||
                _currentToken.Type == TokenType.LessThan ||
                _currentToken.Type == TokenType.GeaterThan)
            {
                if (_currentToken.Type == TokenType.Assign)
                    compOp |= CompOp.Equal;
                else if (_currentToken.Type == TokenType.Not)
                    compOp |= CompOp.Not;
                else if (_currentToken.Type == TokenType.LessThan)
                    compOp |= CompOp.Less;
                else if (_currentToken.Type == TokenType.GeaterThan)
                    compOp |= CompOp.Greater;
                Eat(_currentToken.Type);
            }

            return compOp;
        }

        private async Task<bool> Condition()
        {
            var lValue = await Expr();
            CompOp compOp = GetCompOp();
            if (_currentToken.Type == TokenType.Rparen || _currentToken.Type == TokenType.Eof)
            {
                return Evaluate(CompOp.Equal, lValue, true);
            }
            else
            {
                var rValue = await Expr();
                return Evaluate(compOp, lValue, rValue);
            }
        }

        private async Task ConditionInternal()
        {
            Eat(TokenType.Condition);
            var result = await Condition();
            Result = result;
        }

        private bool Evaluate(CompOp op, object lValue, object rValue)
        {
            if (lValue is decimal && rValue is decimal)
            {
                var l = Convert.ToDecimal(lValue);
                var r = Convert.ToDecimal(rValue);
                if (IsSet(op, CompOp.Equal | CompOp.Less))
                    return l <= r;
                else if (IsSet(op, CompOp.Equal | CompOp.Greater))
                    return l >= r;
                else if (IsSet(op, CompOp.Equal | CompOp.Not))
                    return l != r;
                else if (IsSet(op, CompOp.Equal))
                    return l == r;
                else if (IsSet(op, CompOp.Less))
                    return l < r;
                else if (IsSet(op, CompOp.Greater))
                    return l > r;
                else
                    throw new InvalidSyntaxException(_lexer, "Not allowed operator");
            }
            else
            {
                if (op == CompOp.Equal)
                    return lValue.Equals(rValue);
                else if (IsSet(op, CompOp.Equal | CompOp.Not))
                    return !lValue.Equals(rValue);
                throw new InvalidSyntaxException(_lexer, "Not allowed operator");
            }
        }

        private bool IsSet(CompOp op, CompOp check)
        {
            return (op & check) == check;
        }

        private Action IncreaseEvaluate()
        {
            var variableName = _currentToken.Value;
            Eat(TokenType.Id);

            if (_currentToken.Type == TokenType.Plus)
            {
                Eat(TokenType.Plus);
                Eat(TokenType.Plus);
                return () =>
                {
                    if (_globalVariables?.ContainsKey(variableName) ?? false)
                        _globalVariables[variableName] = (decimal)_globalVariables[variableName] + 1;
                    else
                        _variables[variableName] = (decimal)_variables[variableName] + 1;
                };
            }
            else if (_currentToken.Type == TokenType.Minus)
            {
                Eat(TokenType.Minus);
                Eat(TokenType.Minus);
                return () =>
                {
                    if (_globalVariables?.ContainsKey(variableName) ?? false)
                        _globalVariables[variableName] = (decimal)_globalVariables[variableName] - 1;
                    else
                        _variables[variableName] = (decimal)_variables[variableName] - 1;
                };
            }

            return () =>
            {
            };
        }

        private string FuncBody(bool existLbrace)
        {
            var funcBody = _lexer.ReadBody(_currentToken.Value, existLbrace);
            _currentToken = _lexer.GetNextToken();
            return funcBody;
        }

        private async Task<List<object>> Args()
        {
            var result = new List<object>();
            while (_currentToken.Type != TokenType.Rparen)
            {
                if (_currentToken.Type == TokenType.Comma)
                {
                    Eat(TokenType.Comma);
                }
                else
                {
                    result.Add(await Expr());
                }
            }
            return result;
        }

        private string ParseString()
        {
            Eat(TokenType.SingleQuote);
            var result = _currentToken.Value;
            if (_currentToken.Type == TokenType.Id)
                Eat(TokenType.Id);
            else if (_currentToken.Type == TokenType.Decimal)
                Eat(TokenType.Decimal);
            else
                throw new InvalidSyntaxException(_lexer, $"Not a string value {result} {_currentToken.Type}");

            Eat(TokenType.SingleQuote);
            return result;
        }

        private async Task<object> ParseArray()
        {
            var objs = new List<object>();
            Eat(TokenType.Lbrace);

            while (_currentToken.Type != TokenType.Rbrace)
            {
                if (_currentToken.Type == TokenType.Comma)
                {
                    Eat(TokenType.Comma);
                }
                objs.Add(await Expr());
            }

            Eat(TokenType.Rbrace);
            return objs;
        }

        private async Task<object> ParseNew()
        {
            Eat(TokenType.New);

            var className = _currentToken.Value;

            Eat(TokenType.Id);
            Eat(TokenType.Lparen);
            var args = await Args();
            Eat(TokenType.Rparen);
            var entry = Assembly.GetEntryAssembly();
            var exist = entry.DefinedTypes.FirstOrDefault(x => x.Name == className);
            if (exist == null)
                throw new InvalidSyntaxException(_lexer, $"Not found class {className}");
            if (!exist.DeclaredConstructors.Any(x => x.GetParameters().Length == args.Count))
                throw new InvalidSyntaxException(_lexer, $"Not found constructor {className}");

            var constructor = exist.GetConstructors().Where(x => x.GetParameters().Length == args.Count).FirstOrDefault();

            var newInstance = Activator.CreateInstance(exist.AsType(), constructor.GetParameters().
                    Zip(args, (a, b) => (a, b)).
                    Select(x => BuiltInFunctions.Convert(x.a.ParameterType, x.b)).
                    ToArray());
            return newInstance;
        }

        private async Task AssignVariable()
        {
            string variableName = _currentToken.Value;
            Eat(TokenType.Id);
            Eat(TokenType.Assign);
            if (_globalVariables?.ContainsKey(variableName) ?? false)
                _globalVariables[variableName] = await Expr();
            else
                _variables[variableName] = await Expr();
            Eat(TokenType.Semi);
        }

        private void DefineFunction()
        {
            var asyncCall = false;
            if (_currentToken.Type == TokenType.Async)
            {
                asyncCall = true;
                Eat(TokenType.Async);
            }
            Eat(TokenType.Function);

            string functionName = _currentToken.Value;
            Eat(TokenType.Id);


            Eat(TokenType.Lparen);
            List<string> argNames = new();
            while (_currentToken.Type != TokenType.Rparen)
            {
                if (_currentToken.Type == TokenType.Comma)
                    Eat(TokenType.Comma);

                argNames.Add(_currentToken.Value);
                Eat(TokenType.Id);
            }
            Eat(TokenType.Rparen);
            Eat(TokenType.Lbrace);
            var functionNameWithArgs = $"{functionName}{argNames.Count}";
            if (_functions.ContainsKey(functionNameWithArgs))
                throw new InvalidSyntaxException(_lexer, $"duplicated function {functionName}");

            var line = _lexer.Line - 1;
            var funcBody = FuncBody(true);
            var lexer = new Lexer(funcBody);
            lexer.SetLine(line);
            var interpreter = new Interpreter(lexer);
            interpreter.SetAsyncCall(asyncCall);
            interpreter.SetArgNames(argNames);
            _functions[functionNameWithArgs] = interpreter;
            Eat(TokenType.Rbrace);
        }

        private async Task<object> CallClassFunction()
        {
            var variableName = _currentToken.Value;
            Eat(TokenType.Id);
            Eat(TokenType.Dot);
            var funcName = _currentToken.Value;
            Eat(TokenType.Id);

            Eat(TokenType.Lparen);
            List<object> args = new();
            if (_currentToken.Type != TokenType.Rparen)
                args = await Args();

            Eat(TokenType.Rparen);
            var instance = GetVariable(variableName);
            var methods = instance.GetType().GetMethods().Where(x => x.Name == funcName);

            if (methods.Count() == 0)
                throw new InvalidSyntaxException(_lexer, $"not found method {funcName}");

            foreach (var method in methods)
            {
                try
                {
                    var repeat = Math.Max(0, method.GetParameters().Length - args.Count);
                    var result = method.Invoke(instance, method.GetParameters()
                        .Zip(args, (a, b) => (a, b))
                        .Select(x => BuiltInFunctions.Convert(x.a.ParameterType, x.b))
                        .Concat(Enumerable.Repeat(Type.Missing, repeat))
                        .ToArray());
                    return result;
                }
                catch (Exception) { }
            }
            throw new InvalidSyntaxException(_lexer, $"not found method {funcName}");
        }

        private async Task<object> CallFunction()
        {
            var preTokenType = _preToken.Type;
            var functionName = _currentToken.Value;
            Eat(TokenType.Id);
            Eat(TokenType.Lparen);
            List<object> args = new();
            if (_currentToken.Type != TokenType.Rparen)
                args = await Args();

            Eat(TokenType.Rparen);

            var functionNameMangling = $"{functionName}{args.Count}";

            if (BuiltInFunctions.IsBuiltInFunc(functionNameMangling))
            {
                return BuiltInFunctions.Call(functionNameMangling, args);
            }
            else
            {
                var func = GetFunction(functionNameMangling);
                if (func != null)
                {
                    func.ResetLocal();
                    func.SetImports(_imports);
                    func.SetArgs(args);
                    func.SetGlobal(GetVariables(), GetFunctions());
                    if (func.AsyncCall)
                    {
                        if (preTokenType == TokenType.Await)
                            await func.Parse();
                        else
                            func.Parse().GetAwaiter();
                    }
                    else
                    {
                        if (preTokenType == TokenType.Await)
                            throw new InvalidSyntaxException(_lexer, $"not awaitable function {functionName}");

                        func.Parse().GetAwaiter();
                    }
                    return func.Result;
                }
                else
                {
                    return await CallCSharpFunction(functionName, args);
                }
            }
        }

        private async Task ExcuteFor()
        {
            Eat(TokenType.For);
            Eat(TokenType.Lparen);
            await AssignVariable();
            var eval = ConditionFunc();
            Eat(TokenType.Semi);
            var increaseEval = IncreaseEvaluate();
            Eat(TokenType.Rparen);
            Eat(TokenType.Lbrace);

            var line = _lexer.Line - 1;
            var funcBody = FuncBody(true);

            var lexer = new Lexer(funcBody);
            lexer.SetLine(line);
            var interpreter = new Interpreter(lexer);
            interpreter.SetImports(_imports);
            while (true)
            {
                if (!await eval())
                    break;
                interpreter.SetGlobal(GetVariables(), GetFunctions());
                await interpreter.Parse();
                if (interpreter.Break)
                    break;
                increaseEval();

            }
            Eat(TokenType.Rbrace);
        }

        private async Task ExcuteWhile()
        {
            Eat(TokenType.While);
            Eat(TokenType.Lparen);
            var eval = ConditionFunc();
            Eat(TokenType.Rparen);
            Eat(TokenType.Lbrace);

            var line = _lexer.Line - 1;
            var funcBody = FuncBody(true);

            var lexer = new Lexer(funcBody);
            lexer.SetLine(line);
            var interpreter = new Interpreter(lexer);
            interpreter.SetImports(_imports);

            while (true)
            {
                if (!await eval())
                    break;
                interpreter.SetGlobal(GetVariables(), GetFunctions());
                await interpreter.Parse();
                if (interpreter.Break)
                    break;
            }
            Eat(TokenType.Rbrace);
        }

        private async Task ExcuteForeach()
        {
            Eat(TokenType.Foreach);
            Eat(TokenType.Lparen);

            var variableName = _currentToken.Value;
            Eat(TokenType.Id);
            Eat(TokenType.In);

            var iterableName = _currentToken.Value;
            Eat(TokenType.Id);
            Eat(TokenType.Rparen);
            Eat(TokenType.Lbrace);
            var line = _lexer.Line - 1;
            var funcBody = FuncBody(true);
            var lexer = new Lexer(funcBody);
            lexer.SetLine(line);
            var interpreter = new Interpreter(lexer);
            interpreter.SetImports(_imports);
            interpreter.SetGlobal(GetVariables(), GetFunctions());

            dynamic getVariable = GetVariable(iterableName);
            foreach (var v in getVariable)
            {
                interpreter.SetVariable(variableName, v);
                await interpreter.Parse();
            }
            Eat(TokenType.Rbrace);
        }

        private async Task ExcuteIf()
        {

            Eat(TokenType.If);
            Eat(TokenType.Lparen);
            var condition = await Condition();
            Eat(TokenType.Rparen);

            var lbrace = false;
            EatNewline();
            if (_currentToken.Type == TokenType.Lbrace)
            {
                lbrace = true;
                Eat(TokenType.Lbrace);
            }
            EatNewline();

            var line = _lexer.Line - 1;
            var funcBody = FuncBody(lbrace);
            if (condition)
            {
                var lexer = new Lexer(funcBody);
                lexer.SetLine(line);
                var interpreter = new Interpreter(lexer);
                interpreter.SetImports(_imports);
                interpreter.SetGlobal(GetVariables(), GetFunctions());
                await interpreter.Parse();
                Break = interpreter.Break;
            }
            if (lbrace)
                Eat(TokenType.Rbrace);

            EatNewline();

            if (_currentToken.Type == TokenType.Else)
            {
                Eat(TokenType.Else);

                EatNewline();
                lbrace = false;
                if (_currentToken.Type == TokenType.Lbrace)
                {
                    lbrace = true;
                    Eat(TokenType.Lbrace);
                }
                EatNewline();
                line = _lexer.Line - 1;
                funcBody = FuncBody(lbrace);

                if (!condition)
                {
                    var lexer = new Lexer(funcBody);
                    lexer.SetLine(line);
                    var interpreter = new Interpreter(lexer);
                    interpreter.SetImports(_imports);
                    interpreter.SetGlobal(GetVariables(), GetFunctions());
                    await interpreter.Parse();
                    Break = interpreter.Break;
                }

                if (lbrace)
                    Eat(TokenType.Rbrace);

            }
        }

        private async Task<object> ParseAwait()
        {
            Eat(TokenType.Await);
            dynamic r = await Expr();
            return await r;
        }

        private async Task ExcuteAwait()
        {
            Eat(TokenType.Await);
            var result = await Expr() as Task;
            if (result != null)
                await result;
        }

        private void SkipComment()
        {
            Eat(TokenType.DoubleSlash);
            while (_currentToken.Type != TokenType.Eof && _currentToken.Type != TokenType.Newline)
            {
                Eat(_currentToken.Type);
            }
        }

        private void SkipImport()
        {
            Eat(TokenType.Import);
            while (_currentToken.Type != TokenType.Eof && _currentToken.Type != TokenType.Newline)
            {
                Eat(_currentToken.Type);
            }
        }

        private void SkipBlockComment()
        {
            Eat(TokenType.LBlockComment);
            while (_currentToken.Type != TokenType.RBlockComment)
            {
                Eat(_currentToken.Type);
            }
            Eat(TokenType.RBlockComment);
        }

        private async Task<object> CallCSharpFunction(string functionName, List<object> arguments)
        {
            var (result, success) = await CshapHelper.Call(functionName, arguments);
            if (!success)
                throw new InvalidSyntaxException(_lexer, "Function not found: " + functionName);

            if (result != null)
            {
                try
                {
                    result = Convert.ToDecimal(result);
                }
                catch (Exception) { }
            }
            return result;
        }

        private object GetVariable(string name)
        {
            if (_variables != null)
            {
                if (_variables.TryGetValue(name, out var variable))
                    return variable;
            }

            if (_globalVariables != null)
            {
                if (_globalVariables.TryGetValue(name, out var variable))
                    return variable;
            }

            foreach (var im in _imports)
            {
                if (im.GetVariables().TryGetValue(name, out var variable))
                    return variable;
            }
            return null;
        }

        private Interpreter GetFunction(string name)
        {
            if (_functions != null)
            {
                if (_functions.TryGetValue(name, out var func))
                    return func;
            }

            if (_globalFunctions != null)
            {
                if (_globalFunctions.TryGetValue(name, out var func))
                    return func;
            }

            foreach (var im in _imports)
            {
                if (im.GetFunctions().TryGetValue(name, out var func))
                    return func;
            }
            return null;
        }

        public async Task Parse()
        {
            ResetLexer();
            await ExcutePreprocess();

            while (_currentToken.Type != TokenType.Eof)
            {
                if (_currentToken.Type == TokenType.Id && _lexer.NextToken('='))
                {
                    await AssignVariable();
                }
                else if (_currentToken.Type == TokenType.Id && _lexer.NextToken('('))
                {
                    await CallFunction();
                }
                else if (_currentToken.Type == TokenType.Id && _lexer.NextToken('.'))
                {
                    await CallClassFunction();
                }
                else if (_currentToken.Type == TokenType.Function || _currentToken.Type == TokenType.Async)
                {
                    DefineFunction();
                }
                else if (_currentToken.Type == TokenType.Return)
                {
                    Eat(TokenType.Return);
                    Result = await Expr();
                }
                else if (_currentToken.Type == TokenType.For)
                {
                    await ExcuteFor();
                }
                else if (_currentToken.Type == TokenType.While)
                {
                    await ExcuteWhile();
                }
                else if (_currentToken.Type == TokenType.Foreach)
                {
                    await ExcuteForeach();
                }
                else if (_currentToken.Type == TokenType.If)
                {
                    await ExcuteIf();
                }
                else if (_currentToken.Type == TokenType.Await)
                {
                    await ExcuteAwait();
                }
                else if (_currentToken.Type == TokenType.DoubleSlash)
                {
                    SkipComment();
                }
                else if (_currentToken.Type == TokenType.LBlockComment)
                {
                    SkipBlockComment();
                }
                else if (_currentToken.Type == TokenType.Semi)
                {
                    Eat(TokenType.Semi);
                }
                else if (_currentToken.Type == TokenType.Newline)
                {
                    Eat(TokenType.Newline);
                }
                else if (_currentToken.Type == TokenType.Import)
                {
                    SkipImport();
                }
                else if (_currentToken.Type == TokenType.Break)
                {
                    Break = true;
                }
                else if (_currentToken.Type == TokenType.Condition)
                {
                    await ConditionInternal();
                }
                else
                {
                    Error();
                }

                if (Break)
                    return;
            }
        }
    }
}
