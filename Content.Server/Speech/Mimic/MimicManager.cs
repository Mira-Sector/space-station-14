using Content.Server.Database;
using Content.Shared.CCVar;
using Robust.Shared.Asynchronous;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Exceptions;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.Speech.Mimic;

public delegate void UpdateLearnedCallback(EntProtoId prototype, Dictionary<string, float?> phrases);

public sealed class MimicManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ITaskManager _task = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRuntimeLog _runtimeLog = default!;

    private ISawmill _sawmill = default!;

    private ValueList<EntProtoId> _entProtosDirty;

    private readonly List<Task> _pendingSaveTasks = new();

    private TimeSpan _saveInterval;
    private TimeSpan _lastSave;

    private readonly Dictionary<EntProtoId, MimicLearnedData> _mimicLearnedData = new();

    public event UpdateLearnedCallback? UpdateLearned;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("mimic_learning");

        _cfg.OnValueChanged(CCVars.MimicPhraseSaveInterval, f => _saveInterval = TimeSpan.FromSeconds(f), true);
    }

    public void Shutdown()
    {
        Save();

        _task.BlockWaitOnTask(Task.WhenAll(_pendingSaveTasks));
    }

    public void Update()
    {
        UpdateDirtyPrototypes();

        if (_timing.RealTime < _lastSave + _saveInterval)
            return;

        Save();
    }

    private void UpdateDirtyPrototypes()
    {
        if (_entProtosDirty.Count == 0)
            return;

        var time = _timing.RealTime;

        foreach (var prototype in _entProtosDirty)
        {
            if (!_mimicLearnedData.TryGetValue(prototype, out var data))
                continue;

            DebugTools.Assert(data.IsDirty);

            RefreshSinglePrototype(prototype, data, time);

            data.IsDirty = false;
        }

        _entProtosDirty.Clear();
    }

    public void RefreshSinglePrototype(EntProtoId prototype)
    {
        var time = _timing.RealTime;

        if (!_mimicLearnedData.TryGetValue(prototype, out var data))
            return;

        RefreshSinglePrototype(prototype, data, time);
        data.IsDirty = false;
    }

    private void RefreshSinglePrototype(EntProtoId prototype, MimicLearnedData data, TimeSpan time)
    {
        DebugTools.Assert(data.Initialized);

        FlushSinglePrototype(data, time);

        data.PhrasesToUpdate.Clear();

        try
        {
            UpdateLearned?.Invoke(prototype, data.PhrasesToUpdate);
        }
        catch (Exception e)
        {
            _runtimeLog.LogException(e, "Mimic UpdateLearned");
        }
    }

    public void FlushAllPrototypes()
    {
        var time = _timing.RealTime;

        foreach (var data in _mimicLearnedData.Values)
        {
            FlushSinglePrototype(data, time);
        }
    }

    public void FlushPrototype(EntProtoId prototype)
    {
        var time = _timing.RealTime;
        var data = _mimicLearnedData[prototype];

        FlushSinglePrototype(data, time);
    }

    private static void FlushSinglePrototype(MimicLearnedData data, TimeSpan time)
    {
        data.LastUpdate = time;

        foreach (var (phrase, prob) in data.PhrasesToUpdate)
            AddProbToPhrase(data, phrase, prob);
    }

    public async void Save()
    {
        FlushAllPrototypes();

        TrackPending(DoSaveAsync());
        _lastSave = _timing.RealTime;
    }

    public async void SavePrototype(EntProtoId prototype)
    {
        FlushAllPrototypes();

        TrackPending(DoSavePrototypeAsync(prototype));
    }

    private async void TrackPending(Task task)
    {
        _pendingSaveTasks.Add(task);

        try
        {
            await task;
        }
        finally
        {
            _pendingSaveTasks.Remove(task);
        }
    }

    private async Task DoSaveAsync()
    {
        var log = new List<MimicPhrasesUpdate>();

        foreach (var (prototype, data) in _mimicLearnedData)
        {
            foreach (var phrase in data.DbPhrasesDirty)
            {
                log.Add(new MimicPhrasesUpdate(prototype, phrase, data.LearnedPhrases[phrase]));
            }

            data.DbPhrasesDirty.Clear();
        }

        if (log.Count == 0)
            return;

        await _db.UpdateMimicProbs(log);

        _sawmill.Debug($"Saved {log.Count} mimic phrases");
    }

    private async Task DoSavePrototypeAsync(EntProtoId prototype)
    {
        var log = new List<MimicPhrasesUpdate>();

        var data = _mimicLearnedData[prototype];

        foreach (var phrase in data.DbPhrasesDirty)
        {
            log.Add(new MimicPhrasesUpdate(prototype, phrase, data.LearnedPhrases[phrase]));
        }

        data.DbPhrasesDirty.Clear();

        await _db.UpdateMimicProbs(log);

        _sawmill.Debug($"Saved {log.Count} mimicked phrases for {prototype.Id}");
    }

    public async Task<bool> LoadData(EntProtoId prototype, CancellationToken cancel)
    {
        if (_mimicLearnedData.ContainsKey(prototype))
            return true;

        var data = new MimicLearnedData();
        _mimicLearnedData.Add(prototype, data);

        var phrases = await _db.GetMimicPhraseProbs(prototype, cancel);
        if (cancel.IsCancellationRequested)
            return false;

        foreach (var phraseProb in phrases)
            data.LearnedPhrases.Add(phraseProb.Phrase, phraseProb.Prob);

        data.Initialized = true;
        return true;
    }

    private static void AddProbToPhrase(MimicLearnedData data, string phrase, float? prob)
    {
        ref var probability = ref CollectionsMarshal.GetValueRefOrAddDefault(data.LearnedPhrases, phrase, out var found);

        if (found)
        {
            if (prob != null)
                probability = Math.Min(probability + prob.Value, 1f);
        }

        data.DbPhrasesDirty.Add(phrase);
        data.IsDirty = true;
    }

    public bool TryGetPhraseProbs(EntProtoId prototype, [NotNullWhen(true)] out Dictionary<string, float>? prob)
    {
        prob = null;

        if (!_mimicLearnedData.TryGetValue(prototype, out var data) || !data.Initialized)
        {
            return false;
        }

        prob = data.LearnedPhrases;
        return true;
    }

    public bool TryGetPhraseProb(EntProtoId prototype, string phrase, [NotNullWhen(true)] out float? prob)
    {
        prob = null;

        if (!TryGetPhraseProbs(prototype, out var phrases))
            return false;

        if (!phrases.TryGetValue(phrase, out var probability))
            return false;

        prob = probability;

        return true;
    }

    public Dictionary<string, float> GetPhraseProbs(EntProtoId prototype)
    {
        if (!_mimicLearnedData.TryGetValue(prototype, out var data) || !data.Initialized)
            throw new InvalidOperationException($"Mimicked phases are not yet loaded for {prototype.Id}!");

        return data.LearnedPhrases;
    }

    public float GetProbForPhrase(EntProtoId prototype, string phrase)
    {
        if (!_mimicLearnedData.TryGetValue(prototype, out var data) || !data.Initialized)
            throw new InvalidOperationException($"Mimicked phases are not yet loaded for {prototype.Id}!");

        return data.LearnedPhrases.GetValueOrDefault(phrase);
    }

    private sealed class MimicLearnedData
    {
        // Queued update flags
        public bool IsDirty;

        // Active tracking info
        public readonly Dictionary<string, float?> PhrasesToUpdate = new();
        public TimeSpan LastUpdate;

        /// <summary>
        /// Have we finished retrieving our data from the DB?
        /// </summary>
        public bool Initialized;

        public readonly Dictionary<string, float> LearnedPhrases = new();

        public readonly HashSet<string> DbPhrasesDirty = new();
    }
}
