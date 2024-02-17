using System.Diagnostics;

namespace ProcessPal.Server.Util
{
    public class TaskQueue
    {
        private readonly object _locker = new();
        private volatile Task _lastTask = Task.CompletedTask;
        private volatile bool _isShutdown;

        public async Task<T> RunAsync<T>(Func<T> function)
        {
            T result = default;

            await RunAsync(() => {
                result = function.Invoke();
            }).ConfigureAwait(false);

            return result;
        }

        public async Task<T> RunAsync<T>(Func<Task<T>> asyncFunction)
        {
            T result = default;

            await RunAsync(async () => {
                result = await asyncFunction.Invoke().ConfigureAwait(false);
            }).ConfigureAwait(false);

            return result;
        }

        public Task RunAsync(Action action)
        {
            return RunAsync(() => {
                action();
                return Task.CompletedTask;
            });
        }
        
        public Task RunAsync(Func<Task> asyncAction)
        {
            lock (_locker)
            {
                _lastTask = _lastTask.ContinueWith(
                    async _ =>
                    {
                        if (_isShutdown)
                        {
                            throw new ObjectDisposedException(nameof(TaskQueue));
                        }

                        try
                        {
                            await asyncAction().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }, 
                    TaskContinuationOptions.RunContinuationsAsynchronously
                ).Unwrap();
                return _lastTask;
            }
        }

        public Task ShutdownAsync()
        {
            return RunAsync(() => {
                _isShutdown = true;
            });
        }
    }
}