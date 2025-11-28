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

        float scaled = StatPipelineScaling.ApplyScaling(definition.targetStat, rawValue, definition.pipelineScale);
        return definition.ClampPipelineValue(scaled);
    }

    public static float ClampPipelineValue(this ChipDefinition definition, float pipelineValue)
    {
        if (definition == null || !definition.HasStatMapping)
            return pipelineValue;

        return StatPipelineScaling.ApplyClamping(
            pipelineValue,
            definition.pipelineMin,
            definition.pipelineMax
        );
    }

    public static float FromPipelineToDisplay(this ChipDefinition definition, float pipelineValue)
    {
        if (definition == null || !definition.HasStatMapping)
            return pipelineValue;

        return pipelineValue * definition.displayScale;
    }
}
