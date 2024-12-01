using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Phos.Connections.AssettoCorsa.SharedMemory
{
    public delegate void PhysicsUpdatedHandler(object sender, PhysicsEventArgs e);
    public delegate void GraphicsUpdatedHandler(object sender, GraphicsEventArgs e);
    public delegate void StaticInfoUpdatedHandler(object sender, StaticInfoEventArgs e);
    public delegate void GameStatusChangedHandler(object sender, GameStatusEventArgs e);

    public class AssettoCorsaNotStartedException : Exception
    {
        public AssettoCorsaNotStartedException() : base("Shared Memory not connected, is Assetto Corsa running and have you run assettoCorsa.Start()?")
        {
        }
    }

    internal enum AcMemoryStatus { DISCONNECTED, CONNECTING, CONNECTED }

    public sealed class AcSharedMemory
    {
        private Timer sharedMemoryRetryTimer;
        private AcMemoryStatus memoryStatus = AcMemoryStatus.DISCONNECTED;
        public bool IsRunning { get { return (memoryStatus == AcMemoryStatus.CONNECTED); } }

        private AC_STATUS gameStatus = AC_STATUS.AC_OFF;

        public event GameStatusChangedHandler? GameStatusChanged;
        public void OnGameStatusChanged(GameStatusEventArgs e)
        {
            GameStatusChanged?.Invoke(this, e);
        }

        public static readonly Dictionary<AC_STATUS, string> StatusNameLookup = new Dictionary<AC_STATUS, string>
        {
            { AC_STATUS.AC_OFF, "Off" },
            { AC_STATUS.AC_LIVE, "Live" },
            { AC_STATUS.AC_PAUSE, "Pause" },
            { AC_STATUS.AC_REPLAY, "Replay" },
        };

        public AcSharedMemory()
        {
            sharedMemoryRetryTimer = new Timer(2000);
            sharedMemoryRetryTimer.AutoReset = true;
            sharedMemoryRetryTimer.Elapsed += sharedMemoryRetryTimer_Elapsed;

            _physicsTimer = new Timer();
            _physicsTimer.AutoReset = true;
            _physicsTimer.Elapsed += physicsTimer_Elapsed;
            PhysicsInterval = 10;

            _graphicsTimer = new Timer();
            _graphicsTimer.AutoReset = true;
            _graphicsTimer.Elapsed += graphicsTimer_Elapsed;
            GraphicsInterval = 1000;

            _staticInfoTimer = new Timer();
            _staticInfoTimer.AutoReset = true;
            _staticInfoTimer.Elapsed += staticInfoTimer_Elapsed;
            StaticInfoInterval = 1000;

            Stop();
        }

        /// <summary>
        /// Connect to the shared memory and start the update timers
        /// </summary>
        public void Start()
        {
            sharedMemoryRetryTimer.Start();
        }

        void sharedMemoryRetryTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ConnectToSharedMemory();
        }

        private bool ConnectToSharedMemory()
        {
            try
            {
                memoryStatus = AcMemoryStatus.CONNECTING;
                // Connect to shared memory
                _physicsMmf = MemoryMappedFile.OpenExisting("Local\\acpmf_physics");
                _graphicsMmf = MemoryMappedFile.OpenExisting("Local\\acpmf_graphics");
                _staticInfoMmf = MemoryMappedFile.OpenExisting("Local\\acpmf_static");

                // Start the timers
                _staticInfoTimer.Start();
                ProcessStaticInfo();

                _graphicsTimer.Start();
                ProcessGraphics();

                _physicsTimer.Start();
                ProcessPhysics();

                // Stop retry timer
                sharedMemoryRetryTimer.Stop();
                memoryStatus = AcMemoryStatus.CONNECTED;
                return true;
            }
            catch (FileNotFoundException)
            {
                _staticInfoTimer.Stop();
                _graphicsTimer.Stop();
                _physicsTimer.Stop();
                return false;
            }
        }

        /// <summary>
        /// Stop the timers and dispose of the shared memory handles
        /// </summary>
        public void Stop()
        {
            memoryStatus = AcMemoryStatus.DISCONNECTED;
            sharedMemoryRetryTimer.Stop();

            // Stop the timers
            _physicsTimer.Stop();
            _graphicsTimer.Stop();
            _staticInfoTimer.Stop();
        }

        /// <summary>
        /// Interval for physics updates in milliseconds
        /// </summary>
        public double PhysicsInterval
        {
            get => _physicsTimer.Interval;
            set
            {
                _physicsTimer.Interval = value;
            }
        }

        /// <summary>
        /// Interval for graphics updates in milliseconds
        /// </summary>
        public double GraphicsInterval
        {
            get => _graphicsTimer.Interval;
            set
            {
                _graphicsTimer.Interval = value;
            }
        }

        /// <summary>
        /// Interval for static info updates in milliseconds
        /// </summary>
        public double StaticInfoInterval
        {
            get => _staticInfoTimer.Interval;
            set
            {
                _staticInfoTimer.Interval = value;
            }
        }

        private MemoryMappedFile? _physicsMmf;
        private MemoryMappedFile? _graphicsMmf;
        private MemoryMappedFile? _staticInfoMmf;

        private readonly Timer _physicsTimer;
        private readonly Timer _graphicsTimer;
        private readonly Timer _staticInfoTimer;

        /// <summary>
        /// Represents the method that will handle the physics update events
        /// </summary>
        public event PhysicsUpdatedHandler? PhysicsUpdated;

        /// <summary>
        /// Represents the method that will handle the graphics update events
        /// </summary>
        public event GraphicsUpdatedHandler? GraphicsUpdated;

        /// <summary>
        /// Represents the method that will handle the static info update events
        /// </summary>
        public event StaticInfoUpdatedHandler? StaticInfoUpdated;

        public void OnPhysicsUpdated(PhysicsEventArgs e)
        {
            PhysicsUpdated?.Invoke(this, e);
        }

        public void OnGraphicsUpdated(GraphicsEventArgs e)
        {
            if (GraphicsUpdated == null)
            {
                return;
            }
            
            GraphicsUpdated(this, e);
            
            if (gameStatus == e.Graphics.Status)
            {
                return;
            }
            
            gameStatus = e.Graphics.Status;
            GameStatusChanged?.Invoke(this, new GameStatusEventArgs(gameStatus));
        }

        public void OnStaticInfoUpdated(StaticInfoEventArgs e)
        {
            StaticInfoUpdated?.Invoke(this, e);
        }

        private void physicsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ProcessPhysics();
        }

        private void graphicsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ProcessGraphics();
        }

        private void staticInfoTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ProcessStaticInfo();
        }

        private void ProcessPhysics()
        {
            if (memoryStatus == AcMemoryStatus.DISCONNECTED)
            {
                return;
            }   

            try
            {
                var physics = ReadPhysics();
                OnPhysicsUpdated(new PhysicsEventArgs(physics));
            }
            catch (AssettoCorsaNotStartedException)
            { }
        }

        private void ProcessGraphics()
        {
            if (memoryStatus == AcMemoryStatus.DISCONNECTED)
            {
                return;
            }

            try
            {
                var graphics = ReadGraphics();
                OnGraphicsUpdated(new GraphicsEventArgs(graphics));
            }
            catch (AssettoCorsaNotStartedException)
            { }
        }

        private void ProcessStaticInfo()
        {
            if (memoryStatus == AcMemoryStatus.DISCONNECTED)
            {
                return;
            }   

            try
            {
                var staticInfo = ReadStaticInfo();
                OnStaticInfoUpdated(new StaticInfoEventArgs(staticInfo));
            }
            catch (AssettoCorsaNotStartedException)
            { }
        }

        /// <summary>
        /// Read the current physics data from shared memory
        /// </summary>
        /// <returns>A Physics object representing the current status, or null if not available</returns>
        private Physics ReadPhysics()
        {
            if (memoryStatus == AcMemoryStatus.DISCONNECTED || _physicsMmf == null)
                throw new AssettoCorsaNotStartedException();

            using (var stream = _physicsMmf.CreateViewStream())
            {
                using (var reader = new BinaryReader(stream))
                {
                    var size = Marshal.SizeOf(typeof(Physics));
                    var bytes = reader.ReadBytes(size);
                    var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    var data = (Physics)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Physics));
                    handle.Free();
                    return data;
                }
            }
        }

        private Graphics ReadGraphics()
        {
            if (memoryStatus == AcMemoryStatus.DISCONNECTED || _graphicsMmf == null)
            {
                throw new AssettoCorsaNotStartedException();
            }

            using (var stream = _graphicsMmf.CreateViewStream())
            {
                using (var reader = new BinaryReader(stream))
                {
                    var size = Marshal.SizeOf(typeof(Graphics));
                    var bytes = reader.ReadBytes(size);
                    var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    var data = (Graphics)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Graphics));
                    handle.Free();
                    return data;
                }
            }
        }

        private StaticInfo ReadStaticInfo()
        {
            if (memoryStatus == AcMemoryStatus.DISCONNECTED || _staticInfoMmf == null)
            {
                throw new AssettoCorsaNotStartedException();
            }

            using (var stream = _staticInfoMmf.CreateViewStream())
            {
                using (var reader = new BinaryReader(stream))
                {
                    var size = Marshal.SizeOf(typeof(StaticInfo));
                    var bytes = reader.ReadBytes(size);
                    var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    var data = (StaticInfo)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(StaticInfo));
                    handle.Free();
                    return data;
                }
            }
        }
    }
}
