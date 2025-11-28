// filepath: c:\Users\keiro\Outline - Tower Defence\Assets\Code\Scripts\MainGame\RoundLogic\RoundManager\TierShareConfig.cs
using UnityEngine;

[CreateAssetMenu(menuName = "TD/Tier Share Config")]
public class TierShareConfig : ScriptableObject
{
    public AnimationCurve basicShare    = AnimationCurve.Linear(1, 0.85f, 100, 0.40f);
    public AnimationCurve advancedShare = AnimationCurve.Linear(1, 0.15f, 100, 0.35f);
    public AnimationCurve eliteShare    = AnimationCurve.Linear(1, 0.00f, 100, 0.20f);

    public (float basic, float advanced, float elite) GetShares(int wave)
    {
        float b = Mathf.Clamp01(basicShare.Evaluate(wave));
        float a = Mathf.Clamp01(advancedShare.Evaluate(wave));
        float e = Mathf.Clamp01(eliteShare.Evaluate(wave));
        float sum = b + a + e;
        if (sum > 1f && sum > 0f)
        {
            b /= sum; a /= sum; e /= sum;
        }
        return (b, a, e);
    }
}