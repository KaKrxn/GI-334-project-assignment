using UnityEngine;

// Week 7's base terrain rules. Profession/expert modifiers are later milestones.
public static class TerrainYield
{
    public static int Calculate(Hex hex, int id)
    {
        if (hex == null || id < 0 || id >= 8 || hex.ResourceYield == null || id >= hex.ResourceYield.Length) return 0;
        int amount = hex.ResourceYield[id];
        if (id == 4 || id == 5)
        {
            if (!hex.HasForest) amount = 0;
        }
        else if (hex.HasForest) amount -= 2;
        if (hex.HasTown) amount = Mathf.CeilToInt(amount / 2f);
        return Mathf.Max(0, amount);
    }
}
