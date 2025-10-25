using System.Collections.Generic;

public struct Route
{
    public List<Node> Nodes;
    public List<Way> Ways;
    public float Length;
    public float Value;
    
    public Mine Mine => Nodes[0] as Mine;
    public Base Base => Nodes[^1] as Base;

    public Route(Route route)
    {
        Nodes = new List<Node>();
        Nodes.AddRange(route.Nodes);
        Ways = new List<Way>();
        Ways.AddRange(route.Ways);
        Length = route.Length;
        Value = route.Value;
    }
    
    public override bool Equals(object obj) 
    {
        if (obj is not Route route)
            return false;

        if (Nodes == null)
            return route.Nodes == null;

        if (route.Nodes == null)
            return false; 
        
        if (Nodes.Count != route.Nodes.Count)
            return false;

        for (var i = 0; i < Nodes.Count; i++)
        {
            if (Nodes[i] != route.Nodes[i])
                return false;
        }

        return true;
    }
}
