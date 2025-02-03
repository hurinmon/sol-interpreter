using System.Reflection;

namespace Interpreter
{
    internal class CshapHelper
    {
        public static readonly Dictionary<string, MethodInfo> _methodInfos;

        static CshapHelper()
        {
            _methodInfos = typeof(CshapHelper).GetMethods(BindingFlags.Public | BindingFlags.Static).ToDictionary(x => x.Name, x => x);
        }

        public static Task<(object, bool)> Call(string functionName, List<object> arguments)
        {
            if (_methodInfos.TryGetValue(functionName, out var method))
            {
                var taskCompletion = new TaskCompletionSource<(object, bool)>();
                var parameters = method.GetParameters();
                try
                {
                    var result = method.Invoke(null, parameters.
                    Zip(arguments, (a, b) => (a, b)).
                    Select(x => BuiltInFunctions.Convert(x.a.ParameterType, x.b)).
                    ToArray());
                    taskCompletion.SetResult((result, true));
                }
                catch (Exception e)
                {
                    taskCompletion.TrySetException(e);
                }
                return taskCompletion.Task;
            }
            return Task.FromResult<(object, bool)>((null, false));
        }

        public static void SendCheatCode(string cheat)
        {
            Console.WriteLine(cheat);
        }
        public static void Log(object msg)
        {
            Console.WriteLine(msg);
        }
        public static int RandomRange(decimal minValue, decimal maxValue)
        {
            return System.Random.Shared.Next(Convert.ToInt32(minValue), Convert.ToInt32(maxValue));
        }

        public static object Random(List<object> rands)
        {
            return rands[System.Random.Shared.Next(0, rands.Count)];
        }
    }
}
