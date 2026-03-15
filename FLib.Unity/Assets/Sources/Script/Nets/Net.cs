// ==================== qcbf@qq.com | 2025-09-12 ====================

using System;
using System.Net;
using System.Threading.Tasks;
using Configs;
using Cysharp.Threading.Tasks;
using FLib;
using FLib.Net;
using FLib.Unity;
using Modules;
using Modules.Dialog;
using UnityEngine;

namespace Nets
{
    /// <summary>
    /// 游戏网络管理
    /// </summary>
    public class Net : MonoBehaviour
    {
        public static NetRequester Game = NetHelper.Create("Game", ProjectSetting.Inst.Address, ProjectSetting.Inst.Port);

        internal NetRequester Requester;

        /// <summary>
        /// 链接通道
        /// </summary>
        public class NetChannel : FNetTcpClientChannel
        {
            
            public override async Task Connect()
            {
                InputBlocker.Open(nameof(Connect));
                try
                {
                    await base.Connect();
                }
                catch (Exception err)
                {
                    Log.Error?.Write(err);
                }
                InputBlocker.Close(nameof(Connect));
                if (Invalid)
                    DisconnectTips();
            }

            public async UniTaskVoid Reconnect()
            {
                for (var i = 0; i < 3 && Invalid; i++)
                    await Connect();
                if (Invalid)
                {
                    DisconnectTips();
                    return;
                }
            }

            protected override void CloseProcess()
            {
                if (Socket != null)
                    DisconnectTips();
                base.CloseProcess();
            }

            public void DisconnectTips()
            {
                var dialogCtx = TextDialogUI.Open("连接失败", "请稍候重试");
                if ((ModuleStage.StageId & (uint)EModuleStage.Login) != 0)
                    return;
#if SERVER
                Reconnect().Forget();
#else
                dialogCtx.SetButtons("重新登录", "重新连接").SetCloseCallback(btnIdx =>
                {
                    if (btnIdx == 1)
                        Reconnect().Forget();
                    else if (btnIdx == 0)
                        ModuleStage.Goto((uint)EModuleStage.Login);
                });
#endif
            }
        }

        /// <summary>
        /// 请求器
        /// </summary>
        public class NetRequester : FNetRequester
        {
            public long Uid;
            public new NetChannel Channel => (NetChannel)base.Channel;

            public NetRequester(FNetChannel channel) : base(channel)
            {
            }

            protected override void OnReceiveError(int cmd, int code, string msg)
            {
                NetHelper.OpenErrorDialog((EErrorCode)code, msg);
                base.OnReceiveError(cmd, code, msg);
            }

            protected override FNetRequestingBase AddRequesting(int cmd, FNetRequestingBase requesting)
            {
                InputBlocker.Open(nameof(Net));
                return base.AddRequesting(cmd, requesting);
            }

            protected override void OnRemoveRequesting(FNetRequestingBase requesting)
            {
                InputBlocker.Close(nameof(Net));
                base.OnRemoveRequesting(requesting);
            }

            protected override void ClearAll()
            {
                base.ClearAll();
                InputBlocker.Close(nameof(Net), true);
            }
        }

        private void Update() => Requester.Update();
    }

    public static class NetHelper
    {
        private static readonly SlimDictionary<int, string> CmdLogCache = new();
        private static readonly SlimDictionary<int, string> ErrorCodeLogCache = new();
        public static SlimDictionary<int, string> ErrorLangTextCache = new();

        static NetHelper()
        {
            FNetChannel.LogCmdHandler = i => CmdLogCache.GetOrAddValueRef(i) ??= ((ENetCmd)i).ToString();
            FNetChannel.LogErrorCodeHandler = i => ErrorCodeLogCache.GetOrAddValueRef(i) ??= ((EErrorCode)i).ToString();
        }

        public static Net.NetRequester Create(string name, string address, int port)
        {
            var net = new GameObject($"[Net-{name}]").AddComponent<Net>();
            var channel = new Net.NetChannel() { AddressPoint = new IPEndPoint(IPAddress.Parse(address), port) };
#if !SERVER
            channel.Heartbeat = new FNetHeartbeatClient(channel, (int)ENetCmd.Heartbeat);
#endif
            channel.ReceiveCallbacks.Add((int)ENetCmd.Dialog, new FNetResponseState(static res => TextDialogUI.Open(res.Code > 0 ? res.Code.ToString() : "提示", res.Text)));
            return net.Requester = new Net.NetRequester(channel);
        }

        public static void OpenErrorDialog(EErrorCode error, string text = "")
        {
            var errorText = ErrorLangTextCache.GetOrAddValueRef((int)error) ??= Lang.Get(error + "Error");
            TextDialogUI.Open(Lang.Get("Failure"), $"{errorText}\n{text}");
        }
    }
}
