// =================================================={By Qcbf|qcbf@qq.com|2024-09-18}==================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cysharp.Threading.Tasks;

namespace FLib.Unity
{
    public static class ModuleStage
    {
        public static ReadOnlyDictionary<uint, IModuleStageable[]> AllStages;
        public static Action<float> OnStageProgress;
        public static event Action<bool, uint> OnGotoEvent;
        private static long _nextGoto;
        public static uint StageId { get; private set; }
        public static HashSet<IModuleStageable> StageInstances { get; private set; } = new();

        /// <summary>
        /// 
        /// </summary>
        public static void Goto(uint stageId) => GotoAsync(stageId).Forget();

        /// <summary>
        /// 
        /// </summary>
        public static async UniTask GotoAsync(uint stageId)
        {
            if (_nextGoto != 0)
            {
                _nextGoto = stageId;
                return;
            }
            _nextGoto = -1;
            Log.Debug?.Write($"goto stage: {StageId}>{stageId}");
            OnGotoEvent?.Invoke(false, stageId);
            StageId = stageId;
            var group = 0;
            using var tasks = new PooledList<UniTask>();
            using var removeStages = new PooledList<IModuleStageable>();
            OnStageProgress?.Invoke(0.1f);
            foreach (var stage in StageInstances)
            {
                if ((stage.StageIdMask & stageId) == 0)
                {
                    if (stage.StageOrderGroup != group)
                    {
                        if (!tasks.IsEmpty)
                            await tasks.Array;
                        group = stage.StageOrderGroup;
                        tasks.Clear();
                    }
                    _ = tasks.Add(stage.OnExitStage());
                    removeStages.Add(stage);
                }
            }
            if (!tasks.IsEmpty)
                await tasks.Array;
            tasks.Clear();
            foreach (var stage in removeStages)
                StageInstances.Remove(stage);
            OnStageProgress?.Invoke(0.5f);
            group = 0;
            if (AllStages.TryGetValue(stageId, out var stages))
            {
                foreach (var stage in stages)
                {
                    if (StageInstances.Add(stage))
                    {
                        if (stage.StageOrderGroup != group)
                        {
                            await tasks.Array;
                            group = stage.StageOrderGroup;
                            tasks.Clear();
                            if (TryNextGoto()) return;
                        }
                        _ = tasks.Add(stage.OnEnterStage());
                        if (TryNextGoto()) return;
                    }
                }
            }
            if (!tasks.IsEmpty)
                await tasks.Array;
            if (TryNextGoto()) return;
            _nextGoto = 0;
            OnStageProgress?.Invoke(1f);
            OnGotoEvent?.Invoke(true, stageId);
        }

        /// <summary>
        /// 
        /// </summary>
        private static bool TryNextGoto()
        {
            if (_nextGoto <= 0) return false;
            var temp = (uint)_nextGoto;
            _nextGoto = 0;
            Goto(temp);
            return true;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public interface IModuleStageable
    {
        int StageOrderGroup { get; }
        uint StageIdMask { get; }
        UniTask OnEnterStage();
        UniTask OnExitStage();
    }
}
