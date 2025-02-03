namespace Interpreter.Enum
{
    [Flags]
    public enum CompOp
    {
        None = 0,
        Less = 1 << 0,
        Greater = 1 << 1,
        Not = 1 << 2,
        Equal = 1 << 3,
    }
}
