using System.Diagnostics;

namespace ProcessPal.Util
{
    public class TaskQueue
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
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
        
        public async Task QueueAsync(Func<Task> asyncAction)
        {
            await _semaphore.WaitAsync();
            try
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
                
                await _lastTask;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}