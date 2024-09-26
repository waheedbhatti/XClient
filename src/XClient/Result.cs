using XClient.Errors;

namespace XClient
{
    public readonly struct Result<TValue>
    {
        private readonly ResultState state;
        private readonly TValue? value;
        private readonly Error? error;

        public bool Success => state == ResultState.Success;
        public TValue Value => state == ResultState.Success && value != null ? value : throw new InvalidOperationException("Invalid state or no value available");
        public Error Error => state == ResultState.Fail && error != null ? error : throw new InvalidOperationException("Invalid state or error not available");

        public Result(TValue value)
        {
            this.value = value;
            this.state = ResultState.Success;
        }

        public Result(Error error)
        {
            this.error = error;
            this.state = ResultState.Fail;
        }


        public TValue? GetNullableValue()
        {
            return value;
        }


        public static implicit operator TValue(Result<TValue> result)
        {
            return result.Value;
        }

        public static implicit operator Result<TValue>(TValue value)
        {
            return new Result<TValue>(value);
        }

        public static implicit operator Result<TValue>(Error error)
        {
            return new Result<TValue>(error);
        }
    }

    public enum ResultState
    {
        Success,
        Fail
    }
}
