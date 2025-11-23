using System.Numerics;
using HaiyaBox.Utils;
using System.Collections.Generic;

namespace HaiyaBox.Settings;

/// <summary>
/// BattleData 是危险区域计算相关的临时数据管理类。
/// 采用单例模式，提供全局唯一的临时数据存储实例，用于危险区域计算和可视化。
/// </summary>
public sealed class BattleData
{
    // 单例实例
    private static BattleData? _instance;

    // 线程安全锁对象，用于确保多线程环境下单例的唯一性
    private static readonly object _lock = new();

    /// <summary>
    /// 获取全局唯一的 BattleData 实例
    /// </summary>
    public static BattleData Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    if (_instance is null)
                    {
                        _instance = new BattleData();
                    }
                }
            }

            return _instance;
        }
    }

    /// <summary>
    /// 私有构造函数，确保只能通过单例模式获取实例
    /// </summary>
    private BattleData()
    {
    }

    // 危险区域临时参数
    public List<DangerArea> TempDangerAreas { get; set; } = new();
    public Vector3? ReferencePoint { get; set; } = null;
    public int CloseToRefCount = 3;
    public double MaxFarDistance = 25.0;
    public double MinSafePointDistance = 3.0;

    // 计算结果缓存
    public List<Point> SafePoints { get; set; } = new();
    public bool IsCalculated { get; set; } = false;

    /// <summary>
    /// 清除所有临时数据和计算结果
    /// </summary>
    public void Clear()
    {
        TempDangerAreas.Clear();
        ReferencePoint = null;
        SafePoints.Clear();
        IsCalculated = false;
    }
}