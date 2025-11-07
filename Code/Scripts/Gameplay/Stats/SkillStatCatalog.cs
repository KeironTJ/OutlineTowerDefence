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

        float scaled = StatPipelineScaling.ApplyScaling(definition.primaryStat, rawValue, definition.pipelineScale);
        return definition.ClampPipelineValue(scaled);
    }

    public static float ClampPipelineValue(this SkillDefinition definition, float pipelineValue)
    {
        if (definition == null || !definition.HasStatMapping)
            return pipelineValue;

        return StatPipelineScaling.ApplyClamping(
            pipelineValue,
            definition.pipelineMin,
            definition.pipelineMax
        );
    }

    public static float FromPipelineToDisplay(this SkillDefinition definition, float pipelineValue)
    {
        if (definition == null || !definition.HasStatMapping)
            return pipelineValue;

        return pipelineValue * definition.displayScale;
    }
}
