// ==================== qcbf@qq.com | 2025-09-08 ====================

#if SERVER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Datas;
using DBDatas;
using FLib;
using FLib.Server;
using FLib.Unity;
using GameServers;
using Modules;
using Modules.Login;
using UnityEngine;

namespace Launcher
{
    public class UnityServer : MonoBehaviour
    {
        private void Awake()
        {
            GameSetting.Port = 19800;
            GameSetting.Ip = IPAddress.Loopback;
            PlayerDB.Operator = new PlayerDBOperator();
            MailDB.Operator = new MailDBOperator();
            TypeAssistant.AddAssemblies(typeof(Services).Assembly, typeof(GameSetting).Assembly);
            enabled = false;
            ModuleStage.OnGotoEvent += (b, u) =>
            {
                if (b || u != (uint)EModuleStage.Login) return;
                enabled = true;
                Services.CallMethods(EServiceMethod.Awake);
                Services.CallMethods(EServiceMethod.Start);
                GameServer.Server.Heartbeat = null;
            };
        }

        private void OnDestroy()
        {
            TryFlush();
            GameServer.Server.Close("destroy");
            GameServer.Server.Update();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                TryFlush();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                TryFlush();
        }

        private void Update()
        {
            Services.Update();
        }

        private void TryFlush()
        {
            if (GameServer.Server.LoginedClients.FirstOrDefault().Value is PlayerClient client)
            {
                Log.Debug?.Write("try flush true");
                PlayerDB.Operator.Flush(client.Pdb);
            }
            else
            {
                Log.Debug?.Write("try flush false");
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PlayerDBOperator : IPlayerDBOperable
    {
        public string BaseDir = Path.Combine(Application.persistentDataPath, "db");

        public PlayerDBOperator()
        {
            FIO.CreateDirectory(BaseDir);
        }

        public void Flush(PlayerDB db)
        {
            File.WriteAllBytes(Path.Combine(BaseDir, db.Token), BytesPack.Pack(db).ToArray());
        }

        public PlayerDB Get(string token)
        {
            var path = Path.Combine(BaseDir, token);
            if (File.Exists(path))
                return BytesPack.Unpack<PlayerDB>(File.ReadAllBytes(path));
            return null;
        }

        public bool Insert(PlayerDB db)
        {
            var path = Path.Combine(BaseDir, db.Token);
            File.WriteAllBytes(path, BytesPack.Pack(db).ToArray());
            return true;
        }

        public void FlushAll(IEnumerable<PlayerDB> players)
        {
            foreach (var playerDB in players)
                Flush(playerDB);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class MailDBOperator : IMailDBOperable
    {
        public string BaseDir = Path.Combine(Application.persistentDataPath, "db", "mails");

        public MailDBOperator()
        {
            FIO.CreateDirectory(BaseDir);
        }

        public Task<List<MailLittleData>> GetList(long playerUid)
        {
            var datas = new ConcurrentBag<MailLittleData>();
            Directory.GetFiles(BaseDir).AsParallel().ForAll(filePath =>
            {
                var mailDB = BytesPack.Unpack<MailDB>(File.ReadAllBytes(filePath));
                if (mailDB.PlayerUid == playerUid)
                    datas.Add(mailDB.LittleData);
            });
            return Task.FromResult(datas.ToList());
        }

        public async Task<MailData> GetInfo(long id)
        {
            var bytes = await File.ReadAllBytesAsync(Path.Combine(BaseDir, id.ToString()));
            return BytesPack.Unpack<MailDB>(bytes).Data;
        }

        public void Insert(MailDB mail)
        {
            File.WriteAllBytes(Path.Combine(BaseDir, mail._id.ToString()), BytesPack.Pack(mail).ToArray());
        }

        public void Delete(long id)
        {
            File.Delete(Path.Combine(BaseDir, id.ToString()));
        }
    }
}
#endif
