using System.Text.Json;

namespace HaiyaBox.Settings
{
    /// <summary>
    /// 事件记录条目，存储单个事件的完整信息
    /// </summary>
    public class EventRecordEntry
    {
        /// <summary>
        /// 事件时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 事件类型名称（如：EnemyCastSpell、MapEffect等）
        /// </summary>
        public string EventType { get; set; } = "";

        /// <summary>
        /// 原始事件数据（object类型，避免类型转换丢失信息）
        /// </summary>
        public object? RawData { get; set; }

        /// <summary>
        /// 事件数据字典，用于存储从事件对象中提取的键值对信息
        /// </summary>
        public Dictionary<string, object?> EventData { get; set; } = new();

        /// <summary>
        /// 构造事件记录条目
        /// </summary>
        /// <param name="eventType">事件类型名称</param>
        /// <param name="rawData">原始事件数据</param>
        public EventRecordEntry(string eventType, object? rawData)
        {
            Timestamp = DateTime.Now;
            EventType = eventType;
            RawData = rawData;
        }

        /// <summary>
        /// 添加事件数据
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void AddData(string key, object? value)
        {
            EventData[key] = value;
        }

        /// <summary>
        /// 格式化输出事件信息
        /// </summary>
        public override string ToString()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine($"[事件类型] {EventType}");
            result.AppendLine($"[时间] {Timestamp:yyyy-MM-dd HH:mm:ss.fff}");

            // 输出事件数据
            if (EventData.Count > 0)
            {
                result.AppendLine("[数据]");
                foreach (var kvp in EventData)
                {
                    if (kvp.Value == null)
                        result.AppendLine($"  {kvp.Key}: null");
                    else if (kvp.Value.GetType().IsArray)
                    {
                        var array = (Array)kvp.Value;
                        result.AppendLine($"  {kvp.Key}: [{string.Join(", ", array.Cast<object>().Select(o => o?.ToString() ?? "null"))}]");
                    }
                    else
                    {
                        result.AppendLine($"  {kvp.Key}: {kvp.Value}");
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 获取简化的事件信息（用于列表显示）
        /// </summary>
        public string GetSummary()
        {
            if (EventData.Count == 0)
                return $"{EventType} - {Timestamp:HH:mm:ss.fff}";

            var result = new System.Text.StringBuilder();
            result.Append($"{EventType} - {Timestamp:HH:mm:ss.fff}");

            // 显示前3个关键信息
            int count = 0;
            foreach (var kvp in EventData)
            {
                if (count >= 3)
                    break;

                if (kvp.Key.Contains("Id", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains("Name", StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains("Target", StringComparison.OrdinalIgnoreCase))
                {
                    result.Append($" | {kvp.Key}: {kvp.Value}");
                    count++;
                }
            }

            return result.ToString();
        }
    }
}
