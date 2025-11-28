using UnityEngine;

public static class SkillStatDisplayUtility
{
    public static bool TryGetEffectiveValue(string skillId, out float value)
    {
        value = 0f;
        if (string.IsNullOrEmpty(skillId))
            return false;

        var service = SkillService.Instance;
        if (service == null)
            return false;

        if (!service.TryGetDefinition(skillId, out var definition) || !definition.HasStatMapping)
            return false;

        var pipeline = TowerStatPipeline.Instance;
        if (pipeline == null)
            return false;

        pipeline.EnsureServiceHooks();

        var bundle = pipeline.CurrentBundle;
        float pipelineValue = definition.ClampPipelineValue(bundle[definition.primaryStat]);
        value = definition.FromPipelineToDisplay(pipelineValue);
        return true;
    }

    public static bool TryProjectEffectiveValue(string skillId, float currentBase, float projectedBase, out float value)
    {
        value = projectedBase;
        if (string.IsNullOrEmpty(skillId))
            return false;

        var service = SkillService.Instance;
        if (service == null)
            return false;

        if (!service.TryGetDefinition(skillId, out var definition) || !definition.HasStatMapping)
            return false;

        var pipeline = TowerStatPipeline.Instance;
        if (pipeline == null)
            return false;

        pipeline.EnsureServiceHooks();

        float pipelineValue = pipeline.CurrentBundle[definition.primaryStat];
        float currentPipelineFromSkill = definition.ToPipelineValue(currentBase);
        float projectedPipelineFromSkill = definition.ToPipelineValue(projectedBase);

        float projectedPipeline = projectedPipelineFromSkill;

        switch (definition.projectionMode)
        {
            case SkillProjectionMode.Multiply:
            {
                if (Mathf.Approximately(currentPipelineFromSkill, 0f))
                {
                    // Treat existing pipeline value as additive carry-over when the skill currently contributes nothing.
                    projectedPipeline = projectedPipelineFromSkill + pipelineValue;
                }
                else
                {
                    float multiplier = pipelineValue / currentPipelineFromSkill;
                    projectedPipeline = projectedPipelineFromSkill * multiplier;
                }
                break;
            }
            case SkillProjectionMode.Add:
            {
                float additive = pipelineValue - currentPipelineFromSkill;
                projectedPipeline = projectedPipelineFromSkill + additive;
                break;
            }
            default:
                projectedPipeline = projectedPipelineFromSkill;
                break;
        }

        projectedPipeline = definition.ClampPipelineValue(projectedPipeline);
        value = definition.FromPipelineToDisplay(projectedPipeline);
        return true;
    }
}
