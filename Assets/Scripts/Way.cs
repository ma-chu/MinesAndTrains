using TMPro;
using UnityEngine;

public class Way : MonoBehaviour
{
    [SerializeField] private float length;
    [SerializeField] private TextMeshPro lengthTMP;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color defaultColor = Color.black;
    [SerializeField] private Color selectedColor = Color.green;
    
    [SerializeField] private Node startNode;
    [SerializeField] private Node endNode;

    private bool _isSelected;
    
    public Node StartNode
    {
        get => startNode;
        private set => startNode = value;
    }

    public Node EndNode
    {
        get => endNode;
        private set => endNode = value;
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            var color = value ? selectedColor : defaultColor;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
    
    public float Length => length;
    
    private void Update()
    {
        if (Time.frameCount % 30 == 0)
            return;
        
        lengthTMP.text = $"{length}";
    }

    public Node GetOppositeNode(Node node)
    {
        if (startNode == null || endNode == null)
        {
            Debug.LogError("Trying to get not set opposite node ", gameObject);
            return null;
        }

        if (node == startNode)
            return endNode;
        
        if (node == endNode)
            return startNode;

        Debug.LogError("Trying to get opposite node by not valid node ", gameObject);
        return null;
    }

    public void SetStartNode(Node node)
    {
        StartNode = node;
        if (StartNode != null)
            lineRenderer.SetPosition(0, StartNode.transform.position);

        TrySetPosition();
    }
    
    public void SetEndNode(Node node)
    {
        EndNode = node;
        if (EndNode != null)
            lineRenderer.SetPosition(1, EndNode.transform.position);
        
        TrySetPosition();
    }

    private void TrySetPosition()
    {
        if (startNode == null || endNode == null)
            return;

        transform.position = Vector3.Lerp(lineRenderer.GetPosition(0), lineRenderer.GetPosition(1), 0.5f);
        lengthTMP.text = $"{length}";
    }

    [ContextMenu("ResetNodes")]
    private void ResetNodesContext()
    {
        startNode = null;
        endNode = null;
        
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, Vector3.one);
    }
}




