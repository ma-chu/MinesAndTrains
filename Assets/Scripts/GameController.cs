using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct TrainData
{
    public float MovingSpeed;
    public float MiningTime;
}

public struct TrainRoutesTable
{
    public Route[,] RoutsTable;
}

public class GameController : MonoBehaviour
{
    private const int TrainsCount = 3;
    private const int MaxNodesInRoute = 8;
    
    [SerializeField] private TrainData[] trainsStartData = new TrainData[TrainsCount];
    [SerializeField] private Train trainPrefab;
    [SerializeField] private TextMeshPro totalScoreTMP;

    private readonly Train[] _trains = new Train[TrainsCount];
    private readonly TrainRoutesTable[] _trainsRoutesTables = new TrainRoutesTable[TrainsCount];
    private readonly Route[] _optimalTrainRoutes = new Route[TrainsCount];
    
    private Base[] _bases;
    private Mine[] _mines;
    private Node[] _nodes;
    private Way[] _ways;

    private float _totalScore;
    
    private void Awake()
    {
        GetElements();
        SpawnTrains();
        BuildTrainTables();
        CalculateOptimalRoutes();
        StartRunTrains();
        SubscribeToTrainEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromTrainEvents();
    }
    
    /// <summary>
    /// впоследствии должно замениться на парсинг элементов с карты или что-то еще
    /// </summary>
    private void GetElements()
    {
        _bases = FindObjectsOfType<Base>();
        _mines = FindObjectsOfType<Mine>();
        _nodes = FindObjectsOfType<Node>();
        _ways = FindObjectsOfType<Way>();
    }
    
    private void SpawnTrains()
    {
        for (var i = 0; i < trainsStartData.Length; i++)
        {
            var train = Instantiate(trainPrefab, Vector3.zero, Quaternion.identity).GetComponent<Train>();
            train.Initialize(trainsStartData[i], i);
            _trains[i] = train;
        }
    }
    
    /// <summary>
    /// Graph is not saved, only TrainsRoutesTables
    /// </summary>
    private void BuildTrainTables()
    {
        for (var index = 0; index < _trainsRoutesTables.Length; index++)
            _trainsRoutesTables[index].RoutsTable = new Route[_mines.Length, _bases.Length];
        
        for (var i = 0; i < _mines.Length; i++)
        {
            var mine = _mines[i];
            var tempRoutes = new List<Route>();
            var levelRoutes = new List<Route>();
            
            //1-level routes
            foreach (var way in mine.Ways)
            {
                var route = new Route
                {
                    Nodes = new List<Node>(),
                    Ways = new List<Way>()
                };
                route.Nodes.Add(mine);
                route.Ways.Add(way);
                route.Length += way.Length;
                route.Nodes.Add(way.GetOppositeNode(mine));
                if (route.Nodes[^1] is Base)
                    TryToFillRouteIntoTrainTables(route);
                
                tempRoutes.Add(route);
            }
            
            //2..n-level routes
            for (var j = 2; j < MaxNodesInRoute; j++)
            {
                foreach (var route in tempRoutes)
                {
                    var routeLastNode = route.Nodes[^1];
                    foreach (var way in routeLastNode.Ways)
                    {
                        var addingNode = way.GetOppositeNode(routeLastNode);
                        if (route.Nodes.Contains(addingNode))
                            continue; //way back

                        var newRoute = new Route(route);
                        newRoute.Ways.Add(way);
                        newRoute.Length += way.Length;
                        newRoute.Nodes.Add(addingNode);
                        if (addingNode is Base)
                            TryToFillRouteIntoTrainTables(newRoute);

                        levelRoutes.Add(newRoute);
                    }
                }

                tempRoutes.Clear();
                tempRoutes.AddRange(levelRoutes);
                levelRoutes.Clear();
            }
        }
    }

    private void TryToFillRouteIntoTrainTables(Route route)
    {
        var x = Array.IndexOf(_mines, route.Mine);
        var y = Array.IndexOf(_bases, route.Base);

        for (var i = 0; i < TrainsCount; i++)
        {
            var train = _trains[i];
            var routeTime = 2 * route.Length / train.MovingSpeed + route.Mine.MiningFactor * train.MiningTime;
            route.Value = route.Base.ResourceFactor / routeTime;
            if (_trainsRoutesTables[i].RoutsTable[x, y].Equals(default(Route))
                || _trainsRoutesTables[i].RoutsTable[x, y].Value < route.Value)
            {
                _trainsRoutesTables[i].RoutsTable[x, y] = route;
            }
        }
    }

    private void CalculateOptimalRoutes()
    {
        for (var i = 0; i < TrainsCount; i++)
            _optimalTrainRoutes[i] = GetOptimalTrainRoute(_trains[i]);
    }
    
    private Route GetOptimalTrainRoute(Train train)
    {
        var maxvalue = 0f;
        Route optimalRoute = default;
        for (var i = 0; i < _mines.Length; i++)
        for (var j = 0; j < _bases.Length; j++)
        {
            var route = _trainsRoutesTables[Array.IndexOf(_trains, train)].RoutsTable[i, j];
            if (route.Value > maxvalue)
            {
                optimalRoute = route;
                maxvalue = route.Value;
            }
        }

        return optimalRoute;
    }

    private void StartRunTrains()
    {
        for (var i = 0; i < TrainsCount; i++)
        {
            var train = _trains[i]; 
            var startNode = _nodes[Random.Range(0, _nodes.Length)];
            train.transform.position = startNode.transform.position;

            if (startNode == _optimalTrainRoutes[i].Mine)
            {
                train.Route = new Route(_optimalTrainRoutes[i]);
                train.MineResource();
                continue;
            }
            
            train.Route = new Route(FindWayFromNodeToOptimalMine(startNode, train));
            train.OptimalMiningRoute = new Route(_optimalTrainRoutes[i]);
            train.MoveToAnotherMine();
        }
    }
    
    private void RunTrain(Train train)
    {
        BuildTrainTables();
        var optimalRoute = GetOptimalTrainRoute(train);
        _optimalTrainRoutes[Array.IndexOf(_trains, train)] = optimalRoute;
        
        if (train.Route.Equals(optimalRoute))
        {
            train.MineResource();
            return;
        }

        var currentNode = train.Route.Base;
        train.Route = new Route(FindWayFromNodeToOptimalMine(currentNode, train));
        train.OptimalMiningRoute = new Route(optimalRoute);
        train.MoveToAnotherMine();
    }
    
    /// <summary>
    /// Не будет работать, в случае, если нода (не шахта и не база) будет расположена вне хотя бы одного из маршрутов
    /// </summary>
    private Route FindWayFromNodeToOptimalMine(Node node, Train train)
    {
        var trainIndex = Array.IndexOf(_trains, train);
        var miningRoute = _optimalTrainRoutes[trainIndex];
        var x  = Array.IndexOf(_mines, miningRoute.Mine);
        
        // there is routes in table from optimal mine with node,
        for (var i = 0; i < _bases.Length; i++)
        {
            var route = new Route(_trainsRoutesTables[trainIndex].RoutsTable[x, i]);
            var nodeIndex = route.Nodes.IndexOf(node);
            if (nodeIndex == -1)
                continue;

            var removeCount = route.Nodes.Count - 1 - nodeIndex;
            route.Nodes.RemoveRange(nodeIndex + 1, removeCount);
            return route;
        }
        
        // there are no routes in table from optimal mine with node,
        // therefore check all mines and go to some base
        Route routeToAnyBaseReverse = default;
        var baseIndex = 0;
        for (var i = 0; i < _mines.Length; i++)
        for (var j = 0; j < _bases.Length; j++)   
        {
            var route = new Route(_trainsRoutesTables[trainIndex].RoutsTable[i, j]);
            var nodeIndex = route.Nodes.IndexOf(node);
            if (nodeIndex == -1)
                continue;
            
            route.Nodes.RemoveRange(0, nodeIndex);
            route.Ways.RemoveRange(0, nodeIndex);
            routeToAnyBaseReverse = new Route(route);
            baseIndex = j;
            break;
        }

        if (routeToAnyBaseReverse.Equals(default(Route)))
        {
            // there are no base routes with node at all!
            Debug.LogError("Route from node to optimal mine is not found ", gameObject);
            return default;
        }

        // From some base go to optimal mine
        {
            var route = new Route(_trainsRoutesTables[trainIndex].RoutsTable[x, baseIndex]);
            if (route.Base != routeToAnyBaseReverse.Nodes[^1])
            {
                Debug.LogError("Base of any route doesn't equal optimal route base", gameObject);
                return default;
            }

            for (var j = routeToAnyBaseReverse.Nodes.Count - 2; j >= 0; j--)
                route.Nodes.Add(routeToAnyBaseReverse.Nodes[j]);

            for (var j = routeToAnyBaseReverse.Ways.Count - 1; j >= 0; j--)
                route.Ways.Add(routeToAnyBaseReverse.Ways[j]);

            return route;
        }
    }
    
    private void SubscribeToTrainEvents()
    {
        foreach (var train in _trains)
        {
            train.BaseReached += (points) =>
            {
                _totalScore += points;
                totalScoreTMP.text = $"{_totalScore}";
                RunTrain(train);
            };
            train.RouteChanged += UpdateWaysColors;
        }
    }
    
    private void UnsubscribeFromTrainEvents()
    {
        foreach (var train in _trains)
        {
            train.BaseReached = null;
            train.RouteChanged -= UpdateWaysColors;
        }
    }
    
    private void UpdateWaysColors()
    {
        foreach (var way in _ways)
            way.IsSelected = false;

        foreach (var train in _trains)
        foreach (var way in train.Route.Ways)
            way.IsSelected = true;
    }
}
