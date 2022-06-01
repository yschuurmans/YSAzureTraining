using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace YSTrainingFunc
{
    public class Redis
    {
        private static long _lastReconnectTicks = DateTimeOffset.MinValue.UtcTicks;
        private static DateTimeOffset _firstErrorTime = DateTimeOffset.MinValue;
        private static DateTimeOffset _previousErrorTime = DateTimeOffset.MinValue;
        private static SemaphoreSlim _reconnectSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private static SemaphoreSlim _initSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private static ConnectionMultiplexer _connection;
        private static bool _didInitialize = false;
        // In general, let StackExchange.Redis handle most reconnects,
        // so limit the frequency of how often ForceReconnect() will
        // actually reconnect.
        public static TimeSpan ReconnectMinInterval => TimeSpan.FromSeconds(60);
        // If errors continue for longer than the below threshold, then the
        // multiplexer seems to not be reconnecting, so ForceReconnect() will
        // re-create the multiplexer.
        public static TimeSpan ReconnectErrorThreshold => TimeSpan.FromSeconds(30);
        public static TimeSpan RestartConnectionTimeout => TimeSpan.FromSeconds(15);
        public static int RetryMaxAttempts => 5;

        public static ConnectionMultiplexer Connection { get { return _connection; } }
        public static async Task InitializeAsync()
        {
            if (_didInitialize)
            {
                throw new InvalidOperationException("Cannot initialize more than once.");
            }

            _connection = await CreateConnectionAsync();
            _didInitialize = true;
        }
        // This method may return null if it fails to acquire the semaphore in time.
        // Use the return value to update the "connection" field
        private static async Task<ConnectionMultiplexer> CreateConnectionAsync()
        {
            if (_connection != null)
            {
                // If we already have a good connection, let's re-use it
                return _connection;
            }
            try
            {
                await _initSemaphore.WaitAsync(RestartConnectionTimeout);
            }
            catch
            {
                // We failed to enter the semaphore in the given amount of time. Connection will either be null, or have a value that was created by another thread.
                return _connection;
            }
            // We entered the semaphore successfully.
            try
            {
                if (_connection != null)
                {
                    // Another thread must have finished creating a new connection while we were waiting to enter the semaphore. Let's use it
                    return _connection;
                }
                // Otherwise, we really need to create a new connection.
                string cacheConnection = "";// Configuration["CacheConnection"].ToString();
                return await ConnectionMultiplexer.ConnectAsync(cacheConnection);
            }
            finally
            {
                _initSemaphore.Release();
            }
        }

        public static async Task CloseConnectionAsync(ConnectionMultiplexer oldConnection)
        {
            if (oldConnection == null)
            {
                return;
            }
            try
            {
                await oldConnection.CloseAsync();
            }
            catch (Exception)
            {
                // Ignore any errors from the oldConnection
            }
        }
        /// <summary>
        /// Force a new ConnectionMultiplexer to be created.
        /// NOTES:
        ///     1. Users of the ConnectionMultiplexer MUST handle ObjectDisposedExceptions, which can now happen as a result of calling ForceReconnectAsync().
        ///     2. Call ForceReconnectAsync() for RedisConnectionExceptions and RedisSocketExceptions. You can also call it for RedisTimeoutExceptions,
        ///         but only if you're using generous ReconnectMinInterval and ReconnectErrorThreshold. Otherwise, establishing new connections can cause
        ///         a cascade failure on a server that's timing out because it's already overloaded.
        ///     3. The code will:
        ///         a. wait to reconnect for at least the "ReconnectErrorThreshold" time of repeated errors before actually reconnecting
        ///         b. not reconnect more frequently than configured in "ReconnectMinInterval"
        /// </summary>
        public static async Task ForceReconnectAsync()
        {
            var utcNow = DateTimeOffset.UtcNow;
            long previousTicks = Interlocked.Read(ref _lastReconnectTicks);
            var previousReconnectTime = new DateTimeOffset(previousTicks, TimeSpan.Zero);
            TimeSpan elapsedSinceLastReconnect = utcNow - previousReconnectTime;
            // If multiple threads call ForceReconnectAsync at the same time, we only want to honor one of them.
            if (elapsedSinceLastReconnect < ReconnectMinInterval)
            {
                return;
            }
            try
            {
                await _reconnectSemaphore.WaitAsync(RestartConnectionTimeout);
            }
            catch
            {
                // If we fail to enter the semaphore, then it is possible that another thread has already done so.
                // ForceReconnectAsync() can be retried while connectivity problems persist.
                return;
            }
            try
            {
                utcNow = DateTimeOffset.UtcNow;
                elapsedSinceLastReconnect = utcNow - previousReconnectTime;
                if (_firstErrorTime == DateTimeOffset.MinValue)
                {
                    // We haven't seen an error since last reconnect, so set initial values.
                    _firstErrorTime = utcNow;
                    _previousErrorTime = utcNow;
                    return;
                }
                if (elapsedSinceLastReconnect < ReconnectMinInterval)
                {
                    return; // Some other thread made it through the check and the lock, so nothing to do.
                }
                TimeSpan elapsedSinceFirstError = utcNow - _firstErrorTime;
                TimeSpan elapsedSinceMostRecentError = utcNow - _previousErrorTime;
                bool shouldReconnect =
                    elapsedSinceFirstError >= ReconnectErrorThreshold // Make sure we gave the multiplexer enough time to reconnect on its own if it could.
                    && elapsedSinceMostRecentError <= ReconnectErrorThreshold; // Make sure we aren't working on stale data (e.g. if there was a gap in errors, don't reconnect yet).
                                                                               // Update the previousErrorTime timestamp to be now (e.g. this reconnect request).
                _previousErrorTime = utcNow;
                if (!shouldReconnect)
                {
                    return;
                }
                _firstErrorTime = DateTimeOffset.MinValue;
                _previousErrorTime = DateTimeOffset.MinValue;
                ConnectionMultiplexer oldConnection = _connection;
                await CloseConnectionAsync(oldConnection);
                _connection = null;
                _connection = await CreateConnectionAsync();
                Interlocked.Exchange(ref _lastReconnectTicks, utcNow.UtcTicks);
            }
            finally
            {
                _reconnectSemaphore.Release();
            }
        }
        // In real applications, consider using a framework such as
        // Polly to make it easier to customize the retry approach.
        private static async Task<T> BasicRetryAsync<T>(Func<T> func)
        {
            int reconnectRetry = 0;
            int disposedRetry = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (Exception ex) when (ex is RedisConnectionException || ex is SocketException)
                {
                    reconnectRetry++;
                    if (reconnectRetry > RetryMaxAttempts)
                        throw;
                    await ForceReconnectAsync();
                }
                catch (ObjectDisposedException)
                {
                    disposedRetry++;
                    if (disposedRetry > RetryMaxAttempts)
                        throw;
                }
            }
        }
        public static Task<IDatabase> GetDatabaseAsync()
        {
            return BasicRetryAsync(() => Connection.GetDatabase());
        }
        public static Task<System.Net.EndPoint[]> GetEndPointsAsync()
        {
            return BasicRetryAsync(() => Connection.GetEndPoints());
        }
        public static Task<IServer> GetServerAsync(string host, int port)
        {
            return BasicRetryAsync(() => Connection.GetServer(host, port));
        }
    }
}
