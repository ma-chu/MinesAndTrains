using TMPro;
using UnityEngine;

public class Base : Node
{
    [SerializeField] private float resourceFactor;
    [SerializeField] private TextMeshPro resourceFactorTMP;
    
    public float ResourceFactor => resourceFactor;

    private void Awake()
    {
        resourceFactorTMP.text = $"{resourceFactor}x";
    }

    private void Update()
    {
        if (Time.frameCount % 30 == 0)
            return;
        
        resourceFactorTMP.text = $"{resourceFactor}x";
    }
}






