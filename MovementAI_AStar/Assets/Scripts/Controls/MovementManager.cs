using System.Collections;
using System.Collections.Generic;
using Navigation;
using UnityEngine;
using UnityEngine.UIElements;

public class MovementManager : MonoBehaviour
{
    TileNavGraph tileNavGraphInstance;

    public List<Vector3> obstacles = new List<Vector3>();
    
    void GetAllObstaclePos()
    {
        GameObject[] detectedObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        
        foreach (GameObject obstacle in detectedObstacles)
        {
            obstacles.Add(obstacle.transform.position);
        }
    }
    
    void Start()
    {
        tileNavGraphInstance = TileNavGraph.Instance;
        GetAllObstaclePos();
        
        if (tileNavGraphInstance)
        {
            foreach (var obstaclePos in obstacles)
            {
                foreach (var node in tileNavGraphInstance.LNode)
                {
                    if (Vector3.Distance(obstaclePos, node.Position) < 5) 
                    {
                        tileNavGraphInstance.GetNode(node.Position).Weight = tileNavGraphInstance.UnreachableCost;
                    }
                }
            }
        }
    }

    void Update()
    {
    }
}
