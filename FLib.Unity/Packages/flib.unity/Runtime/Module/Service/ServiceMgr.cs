using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace FLib.Unity
{
    [ObjectInjectionReceiver(nameof(ServiceMgr), nameof(RegisterServices))]
    public static class ServiceMgr
    {
        public static Meta[] AllServices = Array.Empty<Meta>();

        public struct Meta
        {
            public Type ServiceType;
            public object Service;
        }

        /// <summary>
        /// 处理全部模块和类型
        /// </summary>
        public static void RegisterServices(List<(object, ObjectInjectToAttribute)> list)
        {
            AllServices = new Meta[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var type = (Type)item.Item1;
                // var attr = (ServiceAttribute)item.Item2;
                AllServices[i] = new Meta { ServiceType = type };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Initialize()
        {
            var stages = new SlimDictionary<uint, List<IModuleStageable>>(64);
            for (var i = 0; i < AllServices.Length; i++)
            {
                var s = AllServices[i].Service = TypeAssistant.New(AllServices[i].ServiceType);
                if (s is IModuleStageable stage)
                {
                    var mask = stage.StageIdMask;
                    var idBit = 1u;
                    while (mask != 0)
                    {
                        var stageId = stage.StageIdMask & idBit;
                        if (stageId != 0)
                            (stages.GetOrAddValueRef(stageId) ??= new List<IModuleStageable>()).Add(stage);
                        mask >>= 1;
                        idBit <<= 1;
                    }
                }
            }

            for (var i = 0; i < AllServices.Length; i++)
            {
                if (AllServices[i].Service is ServiceBase temp)
                    temp.Begin();
            }
            ModuleStage.AllStages = new ReadOnlyDictionary<uint, IModuleStageable[]>(stages.ToDictionary(k => k.Key, v => v.Value.OrderBy(stage => stage.StageOrderGroup).ToArray()));
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Uninitialize()
        {
            ModuleStage.Goto(0);
            for (var i = 0; i < AllServices.Length; i++)
            {
                if (AllServices[i].Service is ServiceBase temp)
                    temp.End();
            }
            ModuleStage.AllStages = null;
        }
    }
}
