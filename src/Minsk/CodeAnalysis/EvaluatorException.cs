namespace Minsk.CodeAnalysis
{
    [System.Serializable]
    public class EvaluatorException : System.Exception
    {
        public EvaluatorException() { }
        public EvaluatorException(string message) : base(message) { }
        public EvaluatorException(string message, System.Exception inner) : base(message, inner) { }
        protected EvaluatorException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}