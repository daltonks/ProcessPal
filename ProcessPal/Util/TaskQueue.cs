using System.Diagnostics;

namespace ProcessPal.Util
{
    public class TaskQueue
    {
        private readonly object _locker = new();
        private volatile Task _lastTask = Task.CompletedTask;

        public async Task<T> QueueAsync<T>(Func<T> function)
        {
            T result = default;

            await QueueAsync(() => {
                result = function.Invoke();
            }).ConfigureAwait(false);

            return result;
        }

        public async Task<T> QueueAsync<T>(Func<Task<T>> asyncFunction)
        {
            T result = default;

            await QueueAsync(async () => {
                result = await asyncFunction.Invoke().ConfigureAwait(false);
            }).ConfigureAwait(false);

            return result;
        }

        public Task QueueAsync(Action action)
        {
            return QueueAsync(() => {
                action();
                return Task.CompletedTask;
            });
        }
        
        public Task QueueAsync(Func<Task> asyncAction)
        {
            lock(_locker)
            {
                _lastTask = _lastTask.ContinueWith(
                    async _ =>
                    {
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
    }
}