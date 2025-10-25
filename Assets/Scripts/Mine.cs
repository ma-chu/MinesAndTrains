using TMPro;
using UnityEngine;

public class Mine : Node
{
    [SerializeField] private float miningFactor;
    [SerializeField] private TextMeshPro miningTimeTMP;
    
    public float MiningFactor => miningFactor;

    private void Awake()
    {
        miningTimeTMP.text = $"{miningFactor}x";
    }
    
    private void Update()
    {
        if (Time.frameCount % 30 == 0)
            return;
        
        miningTimeTMP.text = $"{miningFactor}x";
    }
}