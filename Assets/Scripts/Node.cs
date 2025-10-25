using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField] private List<Way> ways;

    public List<Way> Ways => ways;
    
    private void OnValidate()
    {
        foreach (var way in ways)
        {
            if (way == null)
                continue;

            if (way.StartNode == null)
            {
                way.SetStartNode(this);
                return;
            }
            
            if (way.EndNode == null && way.StartNode != this)
                way.SetEndNode(this);
        }
    }
}

