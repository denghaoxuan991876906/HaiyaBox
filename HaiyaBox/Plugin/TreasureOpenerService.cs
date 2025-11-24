using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using AEAssist.Helper;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using GameVector3 = FFXIVClientStructs.FFXIV.Common.Math.Vector3;

namespace HaiyaBox.Plugin
{
    /// <summary>
    /// Handles teleporting to the nearest treasure object and opening it via packet injection.
    /// </summary>
    internal unsafe class TreasureOpenerService : IDisposable
    {
        public static TreasureOpenerService Instance { get; } = new();

        private delegate* unmanaged<IntPtr, GameObject*, void> openTreasure;
        private delegate* unmanaged<void*, void*, uint, uint, bool> sendPacket;
        private ushort updatePositionOpcode;
        private bool initialized;
        private bool initializationFailed;
        private bool enabled;
        private readonly HashSet<ulong> openedTreasureIds = new();
        private const string CommandName = "/hbtreasure";
        private const float SearchRadius = 200f;

        /// <summary>
        /// Performs all signature scans and command registration.
        /// </summary>
        public bool TryInitialize()
        {
            if (initialized)
                return true;
            if (initializationFailed)
                return false;

            if (!Svc.SigScanner.TryScanText("C7 44 24 ?? ?? ?? ?? ?? 48 8D 54 24 ?? 48 C7 44 24 ?? ?? ?? ?? ?? 0F 11 44 24", out var opcodePtr))
            {
                LogHelper.PrintError("[TreasureOpener] 无法找到 UpdatePositionInstance opcode 签名。");
                initializationFailed = true;
                return false;
            }

            updatePositionOpcode = (ushort)Marshal.ReadInt16(opcodePtr + 4);

            if (!Svc.SigScanner.TryScanText("E8 ?? ?? ?? ?? 48 8B D6 48 8B CF E8 ?? ?? ?? ?? 48 8B 8C 24", out var sendPacketPtr))
            {
                LogHelper.PrintError("[TreasureOpener] 无法找到 SendPacket 签名。");
                initializationFailed = true;
                return false;
            }

            sendPacket = (delegate* unmanaged<void*, void*, uint, uint, bool>)sendPacketPtr;

            if (!Svc.SigScanner.TryScanText("48 89 5C 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B 3D", out var openTreasurePtr))
            {
                LogHelper.PrintError("[TreasureOpener] 无法找到 OpenTreasure 签名。");
                initializationFailed = true;
                return false;
            }

            openTreasure = (delegate* unmanaged<IntPtr, GameObject*, void>)openTreasurePtr;

            Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "开启或关闭自动传送开宝箱功能。"
            });

            initialized = true;
            LogHelper.Print("[TreasureOpener] 初始化完成，使用 /hbtreasure 切换状态。");
            return true;
        }

        public void Dispose()
        {
            if (!initialized)
                return;

            Svc.Commands.RemoveHandler(CommandName);
            initialized = false;
            enabled = false;
            openedTreasureIds.Clear();
        }

        /// <summary>
        /// Called each frame from the plugin update loop.
        /// </summary>
        public void Update()
        {
            if (!initialized || !enabled)
                return;

            TryOpenClosestTreasure();
        }

        private void OnCommand(string command, string args)
        {
            enabled = !enabled;
            LogHelper.Print($"[TreasureOpener] 自动开宝箱已{(enabled ? "开启" : "关闭")}。");
        }

        public bool TryOpenTreasureOnce()
        {
            if (!initialized && !TryInitialize())
                return false;

            return TryOpenClosestTreasure();
        }

        private bool TryOpenClosestTreasure()
        {
            IPlayerCharacter? player = Svc.ClientState.LocalPlayer;
            if (player == null)
                return false;

            PruneOpenedTreasureIds();
            var treasure = FindAvailableTreasure(player.Position);
            if (treasure == null)
                return false;

            OpenTreasure(treasure, player);
            return true;
        }

        private IGameObject? FindAvailableTreasure(Vector3 playerPos)
        {
            IGameObject? result = null;
            float bestDistance = SearchRadius;

            foreach (IGameObject obj in Svc.Objects)
            {
                if (obj.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure || !obj.IsTargetable)
                    continue;

                if (openedTreasureIds.Contains(obj.GameObjectId))
                    continue;

                if (obj.Address == IntPtr.Zero)
                    continue;

                Treasure* treasure = (Treasure*)obj.Address;
                if (treasure == null || (((byte)treasure->Flags) & 3) != 0)
                    continue;

                GameObject* gameObjectPtr = (GameObject*)obj.Address;
                Vector3 treasurePos = ToNumerics(gameObjectPtr->Position);
                float distance = Vector3.Distance(playerPos, treasurePos);
                if (distance < bestDistance)
                {
                    result = obj;
                    bestDistance = distance;
                }
            }

            return result;
        }

        private void OpenTreasure(IGameObject target, IPlayerCharacter player)
        {
            if (target.Address == IntPtr.Zero)
                return;

            GameObject* targetPtr = (GameObject*)target.Address;
            Vector3 originalPosition = player.Position;
            float rotation = player.Rotation;

            SendUpdatePositionInstance(targetPtr->Position, rotation);
            openTreasure(IntPtr.Zero, targetPtr);
            openedTreasureIds.Add(target.GameObjectId);
            SendUpdatePositionInstance(ToGameVector(originalPosition), rotation);
        }

        private void SendUpdatePositionInstance(GameVector3 position, float rotation)
        {
            Framework* framework = Framework.Instance();
            if (framework == null || !framework->IsNetworkModuleInitialized || sendPacket == null)
                return;

            var packet = stackalloc UpdatePositionInstancePacket[1];
            packet->OpCode = updatePositionOpcode;
            packet->Size = 56;
            packet->Position = position;
            packet->PositionNew = position;
            packet->Rotation = rotation;
            packet->RotationNew = rotation;

            sendPacket(framework->NetworkModuleProxy, packet, 101, 1);
        }

        private void PruneOpenedTreasureIds()
        {
            if (openedTreasureIds.Count < 64)
                return;

            HashSet<ulong> alive = Svc.Objects.Select(o => o.GameObjectId).ToHashSet();
            openedTreasureIds.RemoveWhere(id => !alive.Contains(id));
        }

        private static Vector3 ToNumerics(GameVector3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        private static GameVector3 ToGameVector(Vector3 value)
        {
            return new GameVector3
            {
                X = value.X,
                Y = value.Y,
                Z = value.Z
            };
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct UpdatePositionInstancePacket
        {
            [FieldOffset(0)]
            public ushort OpCode;

            [FieldOffset(8)]
            public ushort Size;

            [FieldOffset(32)]
            public float Rotation;

            [FieldOffset(36)]
            public float RotationNew;

            [FieldOffset(44)]
            public GameVector3 Position;

            [FieldOffset(56)]
            public GameVector3 PositionNew;
        }
    }
}
