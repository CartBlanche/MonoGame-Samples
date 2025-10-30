using BepuUtilities;
using BepuUtilities.Memory;

namespace Shooter.Physics;

/// <summary>
/// Simple thread dispatcher for BepuPhysics multi-threading.
/// 
/// EDUCATIONAL NOTE - MULTI-THREADING IN PHYSICS:
/// 
/// Modern physics engines use multiple CPU cores for performance.
/// Bepu can distribute work like:
/// - Collision detection across body pairs
/// - Constraint solving across islands
/// - Narrow phase processing
/// 
/// This gives significant performance improvements on multi-core CPUs.
/// On a 4-core CPU, you might see 3x speed improvement.
/// On an 8-core CPU, you might see 6-7x improvement.
/// 
/// The ThreadDispatcher manages a pool of worker threads that
/// Bepu can use for parallel processing during Simulation.Timestep().
/// </summary>
public class SimpleThreadDispatcher : IThreadDispatcher, IDisposable
{
    private int _threadCount;
    private Worker[] _workers;
    private AutoResetEvent _signal = new(false);
    private int _completedWorkers;
    
    public int ThreadCount => _threadCount;
    
    public SimpleThreadDispatcher(int threadCount)
    {
        _threadCount = threadCount;
        _workers = new Worker[threadCount];
        
        for (int i = 0; i < threadCount; i++)
        {
            _workers[i] = new Worker(this);
        }
    }
    
    public void DispatchWorkers(Action<int> workerBody, int maximumWorkerCount)
    {
        // Use up to the requested number of workers
        var workerCount = Math.Min(maximumWorkerCount, _threadCount);
        
        _completedWorkers = 0;
        
        // Start all workers
        for (int i = 0; i < workerCount; i++)
        {
            _workers[i].Start(workerBody, i);
        }
        
        // Wait for all workers to complete
        while (Volatile.Read(ref _completedWorkers) < workerCount)
        {
            _signal.WaitOne();
        }
    }
    
    public BufferPool GetThreadMemoryPool(int workerIndex)
    {
        // For simplicity, return null - Bepu will use shared pool
        return null!;
    }
    
    private void OnWorkerCompleted()
    {
        Interlocked.Increment(ref _completedWorkers);
        _signal.Set();
    }
    
    public void Dispose()
    {
        foreach (var worker in _workers)
        {
            worker.Dispose();
        }
        _signal.Dispose();
    }
    
    /// <summary>
    /// Individual worker thread that executes physics work.
    /// </summary>
    private class Worker : IDisposable
    {
        private SimpleThreadDispatcher _dispatcher;
        private Thread _thread;
        private AutoResetEvent _startSignal = new(false);
        private volatile bool _running = true;
        private Action<int>? _currentWork;
        private int _workerIndex;
        
        public Worker(SimpleThreadDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _thread = new Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"BepuPhysics Worker"
            };
            _thread.Start();
        }
        
        public void Start(Action<int> work, int workerIndex)
        {
            _currentWork = work;
            _workerIndex = workerIndex;
            _startSignal.Set();
        }
        
        private void WorkerLoop()
        {
            while (_running)
            {
                _startSignal.WaitOne();
                
                if (!_running)
                    break;
                    
                _currentWork?.Invoke(_workerIndex);
                _dispatcher.OnWorkerCompleted();
            }
        }
        
        public void Dispose()
        {
            _running = false;
            _startSignal.Set();
            _thread.Join();
            _startSignal.Dispose();
        }
    }
}
