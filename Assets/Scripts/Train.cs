using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Train : MonoBehaviour
{
    [SerializeField] private float movingSpeed;
    [SerializeField] private float miningTime;
    [SerializeField] private TextMeshPro numberTMP;
    
    private bool _isFull;
    private int _currentNodeIndex;
    private Route _route;
    
    public event Action<float> BaseReached;
    public event Action RouteChanged;

    public Route OptimalMiningRoute;
    
    public Route Route
    {
        get => _route;
        set
        {
            _route = value;
            RouteChanged?.Invoke();
        }
    }
    
    public float MovingSpeed => movingSpeed;
    public float MiningTime => miningTime;

    public void Initialize(TrainData trainData, int number)
    {
        movingSpeed = trainData.MovingSpeed;
        miningTime = trainData.MiningTime;
        numberTMP.text = $"{number}";
    }
    
    public void MineResource()
    {
        if (Route.Equals(default(Route)))
        {
            Debug.LogError("Route is not set ", gameObject);
            return;
        }
        
        StartCoroutine(MineResourceCoroutine());
    }
    
    public void MoveToAnotherMine()
    {
        _currentNodeIndex = Route.Nodes.Count - 1;
        StartCoroutine(MoveToAnotherMineCoroutine());
    }
    
    private IEnumerator MoveToAnotherMineCoroutine()
    {
        while (!Route.Nodes[_currentNodeIndex].Equals(Route.Mine))
            yield return MoveToNextNode();

        Route = OptimalMiningRoute;
        _currentNodeIndex = 0;
        yield return MineResourceCoroutine();
    }

    private IEnumerator MineResourceCoroutine()
    {
        while (!Route.Nodes[_currentNodeIndex].Equals(Route.Mine))
            yield return MoveToNextNode();
        
        yield return DoMining();
        
        while (!Route.Nodes[_currentNodeIndex].Equals(Route.Base))
            yield return MoveToNextNode();
        
        _isFull = false;
        BaseReached?.Invoke(Route.Base.ResourceFactor);
    }
    
    private IEnumerator MoveToNextNode()
    {
        var currentNode = Route.Nodes[_currentNodeIndex];
        var nextNode = _isFull ? Route.Nodes[_currentNodeIndex + 1] : Route.Nodes[_currentNodeIndex - 1];
        var currentNodePosition = currentNode.transform.position;
        var nextNodePosition = nextNode.transform.position;
        
        var currentWay = _isFull ? Route.Ways[_currentNodeIndex]: Route.Ways[_currentNodeIndex - 1];
        
        float timePassed = 0;
        var moveTime = currentWay.Length / MovingSpeed;
        
        while (timePassed < moveTime)
        {
            timePassed += Time.deltaTime;
            transform.position = Vector3.Lerp(currentNodePosition, nextNodePosition, timePassed / moveTime);
            yield return null;
        }

        transform.position = nextNodePosition;
        if (_isFull)
            _currentNodeIndex++;
        else
            _currentNodeIndex--;
    }
    
    private IEnumerator DoMining()
    {
        yield return new WaitForSeconds(miningTime * Route.Mine.MiningFactor);
        _isFull = true;
    }
}