﻿using AmongUs.GameOptions;
using Hazel;
using System;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;

namespace TOHE;

internal class EAC
{
    public static List<string> Msgs = new();
    public static int MeetingTimes = 0;
    public static int DeNum = 0;
    public static void WarnHost(int denum = 1)
    {
        DeNum += denum;
        ErrorText.Instance.CheatDetected = DeNum > 3;
        ErrorText.Instance.SBDetected = DeNum > 10;
        if (ErrorText.Instance.CheatDetected)
            ErrorText.Instance.AddError(ErrorText.Instance.SBDetected ? ErrorCode.SBDetected : ErrorCode.CheatDetected);
        else
            ErrorText.Instance.Clear();
    }
    public static bool Receive(PlayerControl pc, byte callId, MessageReader reader)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (pc == null || reader == null) return true;
        if (pc.GetClient()?.PlatformData?.Platform is Platforms.Android or Platforms.IPhone or Platforms.Switch or Platforms.Playstation or Platforms.Xbox or Platforms.StandaloneMac) return false;
        try
        {
            MessageReader sr = MessageReader.Get(reader);
            var rpc = (RpcCalls)callId;
            switch (rpc)
            {
                case RpcCalls.SetName:
                    string name = sr.ReadString();
                    if (sr.BytesRemaining > 0 && sr.ReadBoolean()) return false;
                    if (
                        ((name.Contains("<size") || name.Contains("size>")) && name.Contains("?") && !name.Contains("color")) ||
                        name.Length > 160 ||
                        name.Count(f => f.Equals("\"\\n\"")) > 3 ||
                        name.Count(f => f.Equals("\n")) > 3 ||
                        name.Count(f => f.Equals("\r")) > 3 ||
                        name.Contains("░") ||
                        name.Contains("▄") ||
                        name.Contains("█") ||
                        name.Contains("▌") ||
                        name.Contains("▒") ||
                        name.Contains("习近平")
                        )
                    {
                        WarnHost();
                        Report(pc, "非法设置游戏名称");
                        Logger.Fatal($"非法修改玩家【{pc.GetClientId()}:{pc.GetRealName()}】的游戏名称，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.SetRole:
                    var role = (RoleTypes)sr.ReadUInt16();
                    if (GameStates.IsLobby && (role is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost))
                    {
                        WarnHost();
                        Report(pc, "非法设置状态为幽灵");
                        Logger.Fatal($"非法设置玩家【{pc.GetClientId()}:{pc.GetRealName()}】的状态为幽灵，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.SendChat:
                    var text = sr.ReadString();
                    if (text.StartsWith("/")) return false;
                    if (Msgs.Contains(text)) return true;
                    Msgs.Add(text);
                    if (Msgs.Count > 1) Msgs.Remove(Msgs[0]);
                    if (
                        text.Contains("░") ||
                        text.Contains("▄") ||
                        text.Contains("█") ||
                        text.Contains("▌") ||
                        text.Contains("▒") ||
                        text.Contains("习近平")
                        )
                    {
                        Report(pc, "非法消息");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】发送非法消息，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.StartMeeting:
                    MeetingTimes++;
                    if ((GameStates.IsMeeting && MeetingTimes > 3) || GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法召集会议");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法召集会议：【null】，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.ReportDeadBody:
                    var p1 = Utils.GetPlayerById(sr.ReadByte());
                    if (p1 != null && p1.IsAlive() && !p1.Is(CustomRoles.Paranoia) && !p1.Is(CustomRoles.GM))
                    {
                        WarnHost();
                        Report(pc, "非法报告尸体");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法报告尸体：【{p1?.GetNameWithRole() ?? "null"}】，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.SetColor:
                case RpcCalls.CheckColor:
                    var color = sr.ReadByte();
                    if ( pc.Data.DefaultOutfit.ColorId != -1 &&
                        (Main.AllPlayerControls.Where(x => x.Data.DefaultOutfit.ColorId == color).Count() >= 5
                        || !GameStates.IsLobby || color < 0 || color > 18))
                    {
                        WarnHost();
                        Report(pc, "非法设置颜色");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置颜色，已驳回", "EAC");
                        return true;
                    }
                    break;
                case RpcCalls.MurderPlayer:
                    if (GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法击杀");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法击杀，已驳回", "EAC");
                        return true;
                    }
                    break;
            }
            switch (callId)
            {
                case 101:
                    var AUMChat = sr.ReadString();
                    Report(pc, "AUM");
                    Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法使用AUM发送消息", "EAC");
                    return true;
                case 7:
                    if (!GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法设置颜色");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置颜色，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 11:
                    MeetingTimes++;
                    if ((GameStates.IsMeeting && MeetingTimes > 3) || GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法召集会议");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法召集会议：【null】，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 5:
                    string name = sr.ReadString();
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置游戏名称");
                        Logger.Fatal($"非法修改玩家【{pc.GetClientId()}:{pc.GetRealName()}】的游戏名称，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 47:
                    if (GameStates.IsLobby)
                    {
                        WarnHost();
                        Report(pc, "非法击杀");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法击杀，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 41:
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置宠物");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置宠物，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 40:
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置皮肤");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置皮肤，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 42:
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置面部装扮");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置面部装扮，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 39:
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置帽子");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置帽子，已驳回", "EAC");
                        return true;
                    }
                    break;
                case 43:
                    if (sr.BytesRemaining > 0 && sr.ReadBoolean()) return false;
                    if (GameStates.IsInGame)
                    {
                        Report(pc, "非法设置游戏名称");
                        Logger.Fatal($"玩家【{pc.GetClientId()}:{pc.GetRealName()}】非法设置名称，已驳回", "EAC");
                        return true;
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Logger.Exception(e, "EAC");
            throw e;
        }
        return false;
    }
    public static void Report(PlayerControl pc, string reason)
    {
        if (pc == null) return;
        string msg = $"{pc.GetClientId()}|{pc.FriendCode}|{pc.Data.PlayerName}|{reason}";
        Cloud.SendData(msg);
        Logger.Fatal($"EAC报告：{pc.GetRealName()}: {reason}", "EAC Cloud");
    }
    public static bool CheckAUM(byte callId, ref string text)
    {
        switch (callId)
        {
            case 85:
                text = GetString("Cheat.AUM");
                break;
        }
        return text != "";
    }
    public static bool CheckInvalidRpc(PlayerControl __instance, byte callId)
    {
        if (Main.LastRPC.ContainsKey(__instance.PlayerId))
        {
            if (Main.LastRPC[__instance.PlayerId] == byte.MaxValue) return false; //已处理
            string text = "";
            if (Main.LastRPC[__instance.PlayerId] == callId && EAC.CheckAUM(callId, ref text))
            {
                Report(__instance, "AUM");
                switch (Options.CheatResponses.GetInt())
                {
                    case 0:
                        AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), true);
                        Logger.Warn($"检测到 {__instance?.Data?.PlayerName} 正在使用作弊程序，因此将其踢出：{text}", "Kick");
                        Logger.SendInGame(string.Format($"封禁 {__instance?.Data?.PlayerName}，理由：{text}", __instance?.Data?.PlayerName));
                        break;
                    case 1:
                        AmongUsClient.Instance.KickPlayer(__instance.GetClientId(), false);
                        Logger.Warn($"检测到 {__instance?.Data?.PlayerName} 正在使用作弊程序，因此将其踢出：{text}", "Kick");
                        Logger.SendInGame(string.Format($"踢出 {__instance?.Data?.PlayerName}，理由：{text}", __instance?.Data?.PlayerName));
                        break;
                    case 2:
                        Utils.SendMessage($"检测到 {__instance?.Data?.PlayerName}：{text}", PlayerControl.LocalPlayer.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "【 ★ 作弊检测 ★ 】"));
                        break;
                    case 3:
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            if (pc != null && pc.PlayerId != __instance?.Data?.PlayerId)
                            {
                                Utils.SendMessage($"检测到 {__instance?.Data?.PlayerName}：{text}", pc.PlayerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Impostor), "【 ★ 作弊检测 ★ 】"));
                            }
                        }
                        break;
                }
                Main.LastRPC[__instance.PlayerId] = byte.MaxValue;
                return false;
            }
        }
        else
        {
            Main.LastRPC.Add(__instance.PlayerId, callId);
            return false;
        }
        return true;
    }
}
