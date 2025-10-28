using UnityEngine;

public static class SkillDefinitionStatExtensions
{
    public static bool TryGetStatMapping(this SkillDefinition definition, out StatId statId, out SkillContributionKind contributionKind)
    {
        if (definition != null && definition.HasStatMapping)
        {
            statId = definition.primaryStat;
            contributionKind = definition.statContributionKind;
            return true;
        }

        statId = StatId.Count;
        contributionKind = SkillContributionKind.None;
        return false;
    }

    public static float ToPipelineValue(this SkillDefinition definition, float rawValue)
    {
        if (definition == null || !definition.HasStatMapping)
            return rawValue;

        if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
            rawValue = 0f;

        float scaled = rawValue * definition.pipelineScale;
        return definition.ClampPipelineValue(scaled);
    }

    public static float ClampPipelineValue(this SkillDefinition definition, float pipelineValue)
    {
        if (definition == null || !definition.HasStatMapping)
            return pipelineValue;

        if (float.IsNaN(pipelineValue) || float.IsInfinity(pipelineValue))
            pipelineValue = 0f;

        if (!float.IsNegativeInfinity(definition.pipelineMin))
            pipelineValue = Mathf.Max(definition.pipelineMin, pipelineValue);
        if (!float.IsPositiveInfinity(definition.pipelineMax))
            pipelineValue = Mathf.Min(definition.pipelineMax, pipelineValue);
        return pipelineValue;
    }

    public static float FromPipelineToDisplay(this SkillDefinition definition, float pipelineValue)
    {
        if (definition == null || !definition.HasStatMapping)
            return pipelineValue;

        return pipelineValue * definition.displayScale;
    }
}
