namespace Cila.OmniChain
{
    internal class EthLogger : ILogger
    {

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return new Scope<TState>(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            //Console.WriteLine("LogLevel {0}, EventId: {1} , State: {2}, Execption, {3} ", logLevel, eventId, state, exception);
            Console.WriteLine(formatter(state,exception));
        }
    }

    public class Scope<T> : IDisposable where T : notnull
    {

        public Scope(T state)
        {
            State = state;
        }

        public T State { get; }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}