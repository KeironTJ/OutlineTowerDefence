using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerStatPipeline : SingletonMonoBehaviour<TowerStatPipeline>
{
    public TowerStatBundle CurrentBundle { get; private set; } = TowerStatBundle.Empty;

    public event Action<TowerStatBundle> StatsRebuilt;

    private readonly List<IStatContributor> contributors = new();
    private bool dirty = true;
    private bool skillServiceHooked;
    private bool chipServiceHooked;
    private bool loadoutServiceHooked;
    private bool researchServiceHooked;
    private ChipService chipServiceRef;
    private ResearchService researchServiceRef;

    protected override void OnAwakeAfterInit()
    {
        // Base class handles singleton setup
    }

    protected override void OnDestroy()
    {
        UnhookSkillService();
        UnhookChipService();
        UnhookLoadoutService();
        UnhookResearchService();
        base.OnDestroy();
    }

    private void Update()
    {
        TryHookSkillService();
        TryHookChipService();
        TryHookLoadoutService();
        TryHookResearchService();
        if (dirty)
            RebuildImmediate();
    }

    public void EnsureServiceHooks()
    {
        TryHookSkillService();
        TryHookChipService();
        TryHookLoadoutService();
        TryHookResearchService();
    }

    public void RegisterContributor(IStatContributor contributor)
    {
        if (contributor == null) return;
        if (contributors.Contains(contributor)) return;

        contributors.Add(contributor);
        MarkDirty();
    }

    public void UnregisterContributor(IStatContributor contributor)
    {
        if (contributor == null) return;
        if (!contributors.Remove(contributor)) return;
        MarkDirty();
    }

    public void MarkDirty()
    {
        dirty = true;
    }

    public void RebuildImmediate()
    {
        dirty = false;
        var collector = new StatCollector();

        for (int i = 0; i < contributors.Count; i++)
        {
            var contributor = contributors[i];
            if (contributor == null)
                continue;

            try
            {
                contributor.Contribute(collector);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[TowerStatPipeline] Contributor '{contributor}' threw exception: {exception}");
            }
        }

        CurrentBundle = collector.BuildBundle();
        StatsRebuilt?.Invoke(CurrentBundle);
    }

    public static void SignalDirty()
    {
        if (Instance != null)
            Instance.MarkDirty();
    }

    private void TryHookSkillService()
    {
        if (skillServiceHooked)
        {
            if (SkillService.Instance == null)
            {
                UnhookSkillService();
            }
            return;
        }

        var skillService = SkillService.Instance;
        if (skillService == null)
            return;

        RegisterContributor(skillService);
        skillService.SkillUpgraded += OnSkillServiceChanged;
        skillService.SkillValueChanged += OnSkillServiceChanged;
        skillServiceHooked = true;
        MarkDirty();
    }

    private void UnhookSkillService()
    {
        if (!skillServiceHooked)
            return;

        var skillService = SkillService.Instance;
        if (skillService != null)
        {
            skillService.SkillUpgraded -= OnSkillServiceChanged;
            skillService.SkillValueChanged -= OnSkillServiceChanged;
        }

        skillServiceHooked = false;
    }

    private void OnSkillServiceChanged(string _)
    {
        MarkDirty();
    }

    private void TryHookChipService()
    {
        if (chipServiceHooked)
        {
            if (ChipService.Instance == null)
            {
                UnhookChipService();
            }
            return;
        }

        var chipService = ChipService.Instance;
        if (chipService == null)
            return;

        RegisterContributor(chipService);
        chipService.ChipEquipped += OnChipEquipped;
        chipService.ChipUnequipped += OnChipUnequipped;
        chipService.ChipUpgraded += OnChipUpgraded;
        chipService.ChipUnlocked += OnChipUnlocked;
        chipService.SlotUnlocked += OnChipSlotUnlocked;
        chipServiceRef = chipService;
        chipServiceHooked = true;
        MarkDirty();
    }

    private void UnhookChipService()
    {
        if (!chipServiceHooked)
            return;

        var chipService = chipServiceRef != null ? chipServiceRef : ChipService.Instance;
        if (chipService != null)
        {
            chipService.ChipEquipped -= OnChipEquipped;
            chipService.ChipUnequipped -= OnChipUnequipped;
            chipService.ChipUpgraded -= OnChipUpgraded;
            chipService.ChipUnlocked -= OnChipUnlocked;
            chipService.SlotUnlocked -= OnChipSlotUnlocked;
            UnregisterContributor(chipService);
        }

        chipServiceRef = null;
        chipServiceHooked = false;
    }

    private void OnChipEquipped(int _, string __) => MarkDirty();
    private void OnChipUnequipped(int _) => MarkDirty();
    private void OnChipUpgraded(string __, int ___) => MarkDirty();
    private void OnChipUnlocked(string __) => MarkDirty();
    private void OnChipSlotUnlocked(int _) => MarkDirty();
    
    private void TryHookLoadoutService()
    {
        if (loadoutServiceHooked)
        {
            if (TowerLoadoutService.Instance == null)
            {
                UnhookLoadoutService();
            }
            return;
        }

        var loadoutService = TowerLoadoutService.Instance;
        if (loadoutService == null)
            return;

        RegisterContributor(loadoutService);
        loadoutServiceHooked = true;
        MarkDirty();
    }

    private void UnhookLoadoutService()
    {
        if (!loadoutServiceHooked)
            return;

        var loadoutService = TowerLoadoutService.Instance;
        if (loadoutService != null)
        {
            UnregisterContributor(loadoutService);
        }

        loadoutServiceHooked = false;
    }

    private void TryHookResearchService()
    {
        if (researchServiceHooked)
        {
            if (ResearchService.Instance == null)
            {
                UnhookResearchService();
            }
            return;
        }

        var researchService = ResearchService.Instance;
        if (researchService == null)
            return;

        RegisterContributor(researchService);
        researchService.ResearchCompleted += OnResearchCompleted;
        researchServiceHooked = true;
        researchServiceRef = researchService;
        MarkDirty();
    }

    private void UnhookResearchService()
    {
        if (!researchServiceHooked)
            return;

        var researchService = researchServiceRef != null ? researchServiceRef : ResearchService.Instance;
        if (researchService != null)
        {
            researchService.ResearchCompleted -= OnResearchCompleted;
            UnregisterContributor(researchService);
        }

        researchServiceRef = null;
        researchServiceHooked = false;
    }

    private void OnResearchCompleted(string _, int __)
    {
        MarkDirty();
    }
}
