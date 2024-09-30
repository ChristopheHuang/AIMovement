using System.Collections;
using System.Collections.Generic;
using Navigation;
using UnityEngine;

public enum EAStarNodeType
{
    Walkable,
    UnWalkable
}

public class AStarNode : Navigation.Node
{
    public EAStarNodeType nodeType;
    
    public AStarNode parent;    // ParentNode
    public int gCost;           // Cost of CurrentNode to StartNode
    public int hCost;           // Cost of CurrentNode to EndNode with Manhattan Distance
    public int fCost { get { return gCost + hCost; } } // All cost f = g + h
    
    public AStarNode(Node node, AStarNode parent, int gCost, int hCost)
    {
        this.Position = node.Position;
        this.Weight = node.Weight;
        this.nodeType = (node.Weight >= int.MaxValue) ? EAStarNodeType.UnWalkable : EAStarNodeType.Walkable;
        this.parent = parent;
        this.gCost = gCost;
        this.hCost = hCost;
        nodeType = EAStarNodeType.Walkable;
    }
}


public class AStar
{
    private static AStarNode GetNodeWithLowestFCost(List<AStarNode> openList) // Compare the lowest cost
    {
        AStarNode lowestFCostNode = openList[0];
        for (int i = 1; i < openList.Count; i++)
        {
            if (openList[i].fCost < lowestFCostNode.fCost)
            {
                lowestFCostNode = openList[i];
            }
        }
        return lowestFCostNode;
    }

    private static int GetHeuristicCost(Node fromNode, Node toNode) // Get the cost to the start node
    {
        Vector3 fromPos = fromNode.Position;
        Vector3 toPos = toNode.Position;
        return Mathf.Abs((int)(fromPos.x - toPos.x)) + Mathf.Abs((int)(fromPos.z - toPos.z));
    }

    private static List<Node> RetracePath(AStarNode endNode)
    {
        List<Node> path = new List<Node>();
        AStarNode currentNode = endNode;

        while (currentNode != null)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }
    
    public static List<Node> FindPath(Node startNode, Node targetNode, TileNavGraph graph)
    {
        List<AStarNode> openList = new List<AStarNode>();
        List<AStarNode> closedList = new List<AStarNode>();

        AStarNode startAStarNode = new AStarNode(startNode, null, 0, GetHeuristicCost(startNode, targetNode));
        if (graph.IsNodeWalkable(startAStarNode))
            openList.Add(startAStarNode);
        
        int securityCount = 5000;
        while (openList.Count > 0 && securityCount > 0)
        {
            securityCount--;

            AStarNode currentNode = GetNodeWithLowestFCost(openList);

            // Check if we have reached the target node
            if (currentNode.Position == targetNode.Position)
            {
                return RetracePath(currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);
            currentNode.nodeType = EAStarNodeType.UnWalkable;

            foreach (Node neighbour in graph.GetNeighbours(currentNode))
            {
                if (!graph.IsNodeWalkable(neighbour) || closedList.Exists(node => node.Position == neighbour.Position))
                    continue;

                int newGCost = currentNode.gCost + graph.ComputeConnectionCost(currentNode, neighbour);

                // Find if the neighbour is already in the open list
                AStarNode neighbourAStarNode = openList.Find(node => node.Position == neighbour.Position);
                if (neighbourAStarNode == null)
                {
                    int hCost = GetHeuristicCost(neighbour, targetNode);
                    neighbourAStarNode = new AStarNode(neighbour, currentNode, newGCost, hCost);
                    openList.Add(neighbourAStarNode);
                }
                else if (newGCost < neighbourAStarNode.gCost)
                {
                    neighbourAStarNode.gCost = newGCost;
                    neighbourAStarNode.parent = currentNode;
                }
            }
        }
        return null; // No path found
    }
}
