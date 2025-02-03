using System.Reflection;

namespace Interpreter
{
    internal class BuiltInFunctions
    {
        public static readonly Dictionary<string, MethodInfo> _methodInfos;
        static BuiltInFunctions()
        {
            _methodInfos = typeof(BuiltInFunctions).GetMethods(BindingFlags.Public | BindingFlags.Static).ToDictionary(x => $"{x.Name}{x.GetParameters().Length}", x => x);
        }

        public static bool IsBuiltInFunc(string functionName)
        {
            return _methodInfos.ContainsKey(functionName);
        }

        public static object Call(string functionName, List<object> arguments)
        {
            var method = _methodInfos[functionName];
            var parameters = method.GetParameters();
            return method.Invoke(null, parameters.
                Zip(arguments, (a, b) => (a, b)).
                Select(x => Convert(x.a.ParameterType, x.b)).
                ToArray());
        }

        public static object Convert(Type desire, object target)
        {
            if (desire == typeof(int))
            {
                return System.Convert.ToInt32(target);
            }
            else if (desire == typeof(float))
            {
                return System.Convert.ToSingle(target);
            }
            else if (desire == typeof(bool))
            {
                return System.Convert.ToBoolean(target);
            }
            else if (desire == typeof(char))
            {
                return System.Convert.ToChar(target);
            }
            else if (desire == typeof(char[]))
            {
                return new char[] { System.Convert.ToChar(target) };
            }
            else if (desire == typeof(string))
            {
                return System.Convert.ToString(target);
            }

            return target;
        }

        public static Task Delay(int millisecondsDelay)
        {
            return Task.Delay(millisecondsDelay);
        }
    }
}
