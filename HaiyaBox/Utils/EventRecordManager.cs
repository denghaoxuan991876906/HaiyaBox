using System.Collections.Concurrent;
using AEAssist.CombatRoutine.Trigger;
using HaiyaBox.Settings;

namespace HaiyaBox.Utils
{
    /// <summary>
    /// 事件记录管理器
    /// 负责管理4种核心事件类型的记录存储，每种事件最多保存15条
    /// </summary>

    /// <summary>
    /// 事件记录条目结构体，包含事件参数和时间戳
    /// </summary>
    public class TimedRecord
    {
        public ITriggerCondParams Record { get; }
        public DateTime Timestamp { get; }

        public TimedRecord(ITriggerCondParams record)
        {
            Record = record;
            Timestamp = DateTime.Now;
        }
    }

    public class EventRecordManager
    {
        private static EventRecordManager? _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static EventRecordManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new EventRecordManager();
                    }
                }
                return _instance;
            }
        }

        // 事件记录存储：每个事件类型一个队列，最多保存15条
        private readonly ConcurrentDictionary<string, ConcurrentQueue<ITriggerCondParams>> _eventRecords;

        // 带时间戳的事件记录存储
        private readonly ConcurrentDictionary<string, ConcurrentQueue<TimedRecord>> _timedEventRecords;

        // 每种事件类型最大记录条数
        private const int MAX_RECORDS_PER_TYPE = 15;

        private EventRecordManager()
        {
            _eventRecords = new ConcurrentDictionary<string, ConcurrentQueue<ITriggerCondParams>>();
            _timedEventRecords = new ConcurrentDictionary<string, ConcurrentQueue<TimedRecord>>();
            // 初始化4种核心事件类型的队列
            InitializeEventTypes();
        }

        /// <summary>
        /// 初始化4种核心事件类型
        /// </summary>
        private void InitializeEventTypes()
        {
            var eventTypes = new[]
            {
                "EnemyCastSpell",       // 敌对咏唱事件
                "Tether",               // 连线/系绳事件
                "TargetIconEffect",     // 目标标记事件
                "UnitCreate"            // 创建单位事件
            };

            foreach (var eventType in eventTypes)
            {
                _eventRecords.TryAdd(eventType, new ConcurrentQueue<ITriggerCondParams>());
                _timedEventRecords.TryAdd(eventType, new ConcurrentQueue<TimedRecord>());
            }
        }

        /// <summary>
        /// 添加事件记录
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="entry">事件记录条目</param>
        public void AddRecord(string eventType, ITriggerCondParams entry)
        {
            // 存储到原始记录队列（向后兼容）
            if (!_eventRecords.TryGetValue(eventType, out var queue))
            {
                queue = new ConcurrentQueue<ITriggerCondParams>();
                _eventRecords.TryAdd(eventType, queue);
            }
            queue.Enqueue(entry);

            // 存储到带时间戳的记录队列
            var timedRecord = new TimedRecord(entry);
            if (!_timedEventRecords.TryGetValue(eventType, out var timedQueue))
            {
                timedQueue = new ConcurrentQueue<TimedRecord>();
                _timedEventRecords.TryAdd(eventType, timedQueue);
            }
            timedQueue.Enqueue(timedRecord);

            // 限制每种事件类型的最大记录数
            while (queue.Count > MAX_RECORDS_PER_TYPE)
            {
                queue.TryDequeue(out _);
            }
            while (timedQueue.Count > MAX_RECORDS_PER_TYPE)
            {
                timedQueue.TryDequeue(out _);
            }
        }

        /// <summary>
        /// 获取指定事件类型的记录列表
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <returns>事件记录列表（从新到旧）</returns>
        public List<ITriggerCondParams> GetRecords(string eventType)
        {
            if (!_eventRecords.TryGetValue(eventType, out var queue))
            {
                return new List<ITriggerCondParams>();
            }

            return queue.ToList();
        }

        /// <summary>
        /// 获取指定事件类型的带时间戳记录列表
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <returns>带时间戳的记录列表（从新到旧）</returns>
        public List<TimedRecord> GetTimedRecords(string eventType)
        {
            if (!_timedEventRecords.TryGetValue(eventType, out var queue))
            {
                return new List<TimedRecord>();
            }

            return queue.ToList();
        }

        /// <summary>
        /// 获取所有事件类型的记录数量
        /// </summary>
        /// <returns>各事件类型的记录数量字典</returns>
        public Dictionary<string, int> GetRecordCounts()
        {
            var counts = new Dictionary<string, int>();
            foreach (var kvp in _eventRecords)
            {
                counts[kvp.Key] = kvp.Value.Count;
            }
            return counts;
        }

        /// <summary>
        /// 清空指定事件类型的记录
        /// </summary>
        /// <param name="eventType">事件类型</param>
        public void ClearRecords(string eventType)
        {
            // 清空原始记录队列
            if (_eventRecords.TryGetValue(eventType, out var queue))
            {
                while (queue.TryDequeue(out _))
                {
                    // 循环清空队列
                }
            }

            // 清空带时间戳的记录队列
            if (_timedEventRecords.TryGetValue(eventType, out var timedQueue))
            {
                while (timedQueue.TryDequeue(out _))
                {
                    // 循环清空队列
                }
            }
        }

        /// <summary>
        /// 清空所有记录
        /// </summary>
        public void ClearAllRecords()
        {
            foreach (var kvp in _eventRecords)
            {
                while (kvp.Value.TryDequeue(out _))
                {
                    // 循环清空队列
                }
            }

            foreach (var kvp in _timedEventRecords)
            {
                while (kvp.Value.TryDequeue(out _))
                {
                    // 循环清空队列
                }
            }
        }

        /// <summary>
        /// 获取所有事件类型的记录
        /// </summary>
        /// <returns>所有事件记录</returns>
        public Dictionary<string, List<ITriggerCondParams>> GetAllRecords()
        {
            var result = new Dictionary<string, List<ITriggerCondParams>>();
            foreach (var kvp in _eventRecords)
            {
                result[kvp.Key] = kvp.Value.ToList();
            }
            return result;
        }
    }
}
