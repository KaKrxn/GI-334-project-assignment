using System.Collections.Generic;

public enum HexDirection { TopLeft, TopRight, Right, BottomRight, BottomLeft, Left }

public static class HexCalculator
{
    // The manual uses three preferred directions, not a full pathfinding search.
    public static Hex FindNextHexToGo(Unit unit, Hex current, Hex destination, Hex[,] hexes)
    {
        if (unit == null || current == null || destination == null || current == destination || unit.MovePoint <= 0) return null;
        HexDirection[] directions;
        if (destination.Y > current.Y) directions = destination.X <= current.X ?
            new[] { HexDirection.TopLeft, HexDirection.TopRight, HexDirection.Left } : new[] { HexDirection.TopRight, HexDirection.TopLeft, HexDirection.Right };
        else if (destination.Y < current.Y) directions = destination.X >= current.X ?
            new[] { HexDirection.BottomRight, HexDirection.Right, HexDirection.BottomLeft } : new[] { HexDirection.BottomLeft, HexDirection.BottomRight, HexDirection.Left };
        else directions = destination.X > current.X ? new[] { HexDirection.Right, HexDirection.TopRight, HexDirection.BottomRight } : new[] { HexDirection.Left, HexDirection.BottomRight, HexDirection.BottomLeft };
        foreach (HexDirection direction in directions)
        {
            Hex next = FindHexByDir(current, direction, hexes);
            if (next == null || next.MoveCost <= 0 || next.MoveCost > unit.MovePoint) continue;
            if (unit.UnitType == UnitType.Naval ? next.HexType == HexType.Ocean : next.HexType != HexType.Ocean) return next;
        }
        return null;
    }
    public static Faction CheckNeverMetFaction(Hex[,] hexes, Unit playerUnit)
    {
        if (playerUnit == null || playerUnit.CurHex == null) return null;
        foreach (Hex hex in GetHexAround(hexes,playerUnit.CurHex))
            foreach (Unit other in hex.UnitsInHex)
                if (other != null && !other.IsDead && other.Faction != playerUnit.Faction && !other.Faction.MetByPlayer) return other.Faction;
        return null;
    }
    public static Hex FindHexByDir(Hex center, HexDirection dir, Hex[,] hexes)
    {
        if (center == null || hexes == null) return null;
        int x = center.X;
        int y = center.Y;
        bool odd = (y % 2) != 0;
        switch (dir)
        {
            case HexDirection.TopLeft: if (!odd) x--; y++; break;
            case HexDirection.TopRight: if (odd) x++; y++; break;
            case HexDirection.Right: x++; break;
            case HexDirection.BottomRight: if (odd) x++; y--; break;
            case HexDirection.BottomLeft: if (!odd) x--; y--; break;
            case HexDirection.Left: x--; break;
        }
        if (x < 0 || y < 0 || x >= hexes.GetLength(0) || y >= hexes.GetLength(1)) return null;
        return hexes[x, y];
    }

    public static List<Hex> GetHexAround(Hex[,] hexes, Hex center)
    {
        List<Hex> result = new List<Hex>(6);
        for (int i = 0; i < 6; i++)
        {
            Hex hex = FindHexByDir(center, (HexDirection)i, hexes);
            if (hex != null) result.Add(hex);
        }
        return result;
    }

    public static bool CheckIfHexAroundHasTown(Hex[,] hexes, Hex center)
    {
        foreach (Hex hex in GetHexAround(hexes, center))
            if (hex.HasTown) return true;
        return false;
    }

    public static Hex[] GetHexAroundToArray(Hex[,] hexes, Hex center)
    {
        Hex[] result = new Hex[6];
        for (int i = 0; i < result.Length; i++) result[i] = FindHexByDir(center, (HexDirection)i, hexes);
        return result;
    }
}
