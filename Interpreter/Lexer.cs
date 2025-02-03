using Interpreter.Enum;
using System.Text;
using System.Text.RegularExpressions;

namespace Interpreter
{
    public class Lexer
    {
        public int Line => _line;
        public string FullPath => Path.Combine(_workDirectory, _filePath);
        public string FileName => Path.GetFileName(_filePath);
        public string WorkDirectory => _workDirectory;
        private readonly string _text;
        private readonly string _filePath = "";
        private string _workDirectory = "";
        private int _pos;
        private int _line;
        private int _initLine;
        private char _currentChar;
        private bool _startSingleQuote;
        private readonly StringBuilder _sb = new StringBuilder();

        public Lexer(string text)
        {
            _text = text;
            _initLine = 1;
            Reset();
        }

        public Lexer(string workDirectory, string path)
        {
            _workDirectory = workDirectory;
            _filePath = path;
            _text = File.ReadAllText(FullPath).Replace("\\n", "\n").Replace("\\r", "\r");
            _initLine = 1;
            Reset();
        }

        public List<string> GetImports()
        {
            string pattern = @"^(?!\/\/|\/\*).*?import '(.*?)';";
            MatchCollection matches = Regex.Matches(_text, pattern, RegexOptions.Multiline);
            List<string> result = new List<string>(matches.Count);
            foreach (Match match in matches)
            {
                string extractedValue = match.Groups[1].Value;
                result.Add(extractedValue);
            }
            return result;
        }
        public void SetWorkDirectory(string workDirectory)
        {
            _workDirectory = workDirectory;
        }

        public void SetLine(int line)
        {
            _initLine = _line = line;
        }

        public void Reset()
        {
            _pos = 0;
            _line = _initLine;
            if (string.IsNullOrEmpty(_text))
                _currentChar = '\0';
            else
                _currentChar = _text[_pos];
        }

        private void Error()
        {
            throw new InvalidSyntaxException(this, $"Invalid character {_currentChar}");
        }

        public string ReadBody(string currentToken, bool existLbrace)
        {
            _sb.Length = 0;
            _sb.Append(currentToken);
            if (existLbrace)
            {
                var lbraceCount = 1;
                for (int i = _pos; i < _text.Length; i++)
                {
                    if (_text[i] == '{')
                        lbraceCount++;
                    else if (_text[i] == '}')
                        lbraceCount--;
                    if (lbraceCount == 0)
                        break;

                    _pos++;
                    if (_text[i] == '\n')
                        _line++;

                    _sb.Append(_text[i]);
                }
            }
            else
            {
                for (int i = _pos; i < _text.Length; i++)
                {
                    if (_text[i] == '\n')
                        break;
                    _pos++;
                    _sb.Append(_text[i]);
                }
            }
            _currentChar = _text[_pos];
            return _sb.ToString();
        }
        public bool NextToken(char t)
        {
            var nextPos = _pos;
            while (nextPos <= _text.Length - 1 && char.IsWhiteSpace(_text[nextPos]))
            {
                nextPos += 1;
            }
            if (nextPos > _text.Length - 1)
                return false;
            return _text[nextPos] == t;
        }

        private void Advance()
        {
            _pos++;
            if (_pos > _text.Length - 1)
                _currentChar = '\0';
            else
            {
                _currentChar = _text[_pos];

                if (_currentChar == '\n')
                    _line++;
            }
        }

        private void SkipWhitespace()
        {
            while (_currentChar != '\0' && _currentChar != '\n' && char.IsWhiteSpace(_currentChar))
                Advance();
        }

        private string Decimal()
        {
            string result = "";
            while (_currentChar != '\0' && (char.IsDigit(_currentChar) || _currentChar == '.'))
            {
                result += _currentChar;
                Advance();
            }

            return result;
        }

        private bool IsLetter(char c)
        {
            return char.IsLetter(_currentChar) || c == '_';
        }

        private string Identifier()
        {
            string result = "";
            if (_startSingleQuote)
            {
                while (_currentChar != '\0')
                {
                    if (_currentChar == '\'')
                        break;
                    result += _currentChar;
                    Advance();
                }
            }
            else
            {
                while (_currentChar != '\0' && (IsLetter(_currentChar) || char.IsDigit(_currentChar)))
                {
                    result += _currentChar;
                    Advance();
                }
            }

            return result;
        }

        public Token GetNextToken()
        {
            while (_currentChar != '\0')
            {
                if (!_startSingleQuote && _currentChar != '\n' && char.IsWhiteSpace(_currentChar))
                {
                    SkipWhitespace();
                    continue;
                }

                if (!_startSingleQuote && char.IsDigit(_currentChar))
                {
                    return new Token(TokenType.Decimal, Decimal());
                }

                if ((_startSingleQuote && _currentChar != '\'') || IsLetter(_currentChar) || (_currentChar != '\n' && char.IsWhiteSpace(_currentChar)))
                {
                    string id = Identifier();
                    if (!_startSingleQuote)
                    {
                        switch (id)
                        {
                            case "function":
                                return new Token(TokenType.Function, id);
                            case "for":
                                return new Token(TokenType.For, id);
                            case "foreach":
                                return new Token(TokenType.Foreach, id);
                            case "in":
                                return new Token(TokenType.In, id);
                            case "return":
                                return new Token(TokenType.Return, id);
                            case "if":
                                return new Token(TokenType.If, id);
                            case "true":
                                return new Token(TokenType.True, id);
                            case "false":
                                return new Token(TokenType.False, id);
                            case "async":
                                return new Token(TokenType.Async, id);
                            case "await":
                                return new Token(TokenType.Await, id);
                            case "import":
                                return new Token(TokenType.Import, id);
                            case "new":
                                return new Token(TokenType.New, id);
                            case "break":
                                return new Token(TokenType.Break, id);
                            case "while":
                                return new Token(TokenType.While, id);
                            case "else":
                                return new Token(TokenType.Else, id);
                            case "condition_internal_xxxxxx":
                                return new Token(TokenType.Condition, id);
                            default:
                                return new Token(TokenType.Id, id);
                        }
                    }
                    else
                    {
                        return new Token(TokenType.Id, id);
                    }
                }

                if (_currentChar == '\n')
                {
                    Advance();
                    return new Token(TokenType.Newline, "\n");
                }
                else if (_currentChar == '!')
                {
                    Advance();
                    return new Token(TokenType.Not, "!");
                }
                else if (_currentChar == '.')
                {
                    Advance();
                    return new Token(TokenType.Dot, ".");
                }
                else if (_currentChar == '/')
                {
                    Advance();
                    if (_currentChar == '/')
                    {
                        Advance();
                        return new Token(TokenType.DoubleSlash, "//");
                    }
                    else if (_currentChar == '*')
                    {
                        Advance();
                        return new Token(TokenType.LBlockComment, "/*");
                    }
                    return new Token(TokenType.Div, "/");
                }
                else if (_currentChar == '%')
                {
                    Advance();
                    return new Token(TokenType.Modulo, "%");
                }
                else if (_currentChar == '>')
                {
                    Advance();
                    return new Token(TokenType.GeaterThan, ">");
                }
                else if (_currentChar == '\'')
                {
                    _startSingleQuote = !_startSingleQuote;
                    Advance();
                    return new Token(TokenType.SingleQuote, "'");
                }
                else if (_currentChar == '<')
                {
                    Advance();
                    return new Token(TokenType.LessThan, "<");
                }
                else if (_currentChar == '=')
                {
                    Advance();
                    return new Token(TokenType.Assign, "=");
                }
                else if (_currentChar == ';')
                {
                    Advance();
                    return new Token(TokenType.Semi, ";");
                }
                else if (_currentChar == '+')
                {
                    Advance();
                    return new Token(TokenType.Plus, "+");
                }
                else if (_currentChar == '-')
                {
                    Advance();
                    return new Token(TokenType.Minus, "-");
                }
                else if (_currentChar == '*')
                {
                    Advance();

                    if (_currentChar == '/')
                    {
                        Advance();
                        return new Token(TokenType.RBlockComment, "*/");
                    }
                    return new Token(TokenType.Mul, "*");
                }
                else if (_currentChar == '(')
                {
                    Advance();
                    return new Token(TokenType.Lparen, "(");
                }
                else if (_currentChar == ')')
                {
                    Advance();
                    return new Token(TokenType.Rparen, ")");
                }
                else if (_currentChar == ',')
                {
                    Advance();
                    return new Token(TokenType.Comma, ",");
                }
                else if (_currentChar == '{')
                {
                    Advance();
                    return new Token(TokenType.Lbrace, "{");
                }
                else if (_currentChar == '}')
                {
                    Advance();
                    return new Token(TokenType.Rbrace, "}");
                }

                Error();
            }

            return new Token(TokenType.Eof, null);
        }
    }
}
