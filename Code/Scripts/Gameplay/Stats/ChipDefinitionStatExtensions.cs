using UnityEngine;

public static class ChipDefinitionStatExtensions
{
    public static bool TryGetStatMapping(this ChipDefinition definition, out StatId statId, out SkillContributionKind kind)
    {
        if (definition != null && definition.HasStatMapping)
        {
            statId = definition.targetStat;
            kind = definition.contributionKind;
            return true;
        }

        statId = StatId.Count;
        kind = SkillContributionKind.None;
        return false;
    }

    public static float ToPipelineValue(this ChipDefinition definition, float rawValue)
    {
        if (definition == null || !definition.HasStatMapping)
            return rawValue;

        if (float.IsNaN(rawValue) || float.IsInfinity(rawValue))
            rawValue = 0f;

        float scaled = rawValue * definition.pipelineScale;
        return definition.ClampPipelineValue(scaled);
    }

    public static float ClampPipelineValue(this ChipDefinition definition, float pipelineValue)
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

    public static float FromPipelineToDisplay(this ChipDefinition definition, float pipelineValue)
    {
        if (definition == null || !definition.HasStatMapping)
            return pipelineValue;

        return pipelineValue * definition.displayScale;
    }
}
