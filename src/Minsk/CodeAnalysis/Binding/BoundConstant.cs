namespace Minsk.CodeAnalysis.Binding
{
    internal sealed class BoundConstant
    {
        public BoundConstant(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}
