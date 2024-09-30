using UnityEngine;
using System.Collections.Generic;
using Navigation;

public class PlayerController : MonoBehaviour
{
    public TileNavGraph TileNavGraphInstance;
    
    [SerializeField]
    private GameObject TargetCursorPrefab = null;
    private GameObject targetCursor = null;
    
    [SerializeField]
    private GameObject UnitPrefab = null;
    
    [SerializeField]
    private Transform PlayerStart = null;
    
    [SerializeField]
    private Unit unit;
    
    [SerializeField]
    private List<Unit> units = new List<Unit>();

    delegate void InputEventHandler();
    event InputEventHandler OnMouseClicked;
    
    private Unit selectedUnit = null;
    
    private GameObject GetTargetCursor()
    {
        if (targetCursor == null)
            targetCursor = Instantiate(TargetCursorPrefab);
        return targetCursor;
    }

    private void Start ()
    {
        if (UnitPrefab)
        {
            GameObject unitInst = Instantiate(UnitPrefab, PlayerStart, false);
            unitInst.transform.parent = null;
            unit = unitInst.GetComponent<Unit>();

            RaycastHit raycastInfo;
            Ray ray = new Ray(unitInst.transform.position, Vector3.down);
            if (Physics.Raycast(ray, out raycastInfo, 10f, 1 << LayerMask.NameToLayer("Floor")))
            {
                unitInst.transform.position = raycastInfo.point;
            }

            unit.SetSelected(true);
        }

        OnMouseClicked += () =>
        {
            int floorLayer = 1 << LayerMask.NameToLayer("Floor");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit raycastInfo;
            // unit move target
            if (Physics.Raycast(ray, out raycastInfo, Mathf.Infinity, floorLayer))
            {
                Vector3 newPos = raycastInfo.point;
                Vector3 targetPos = newPos;
                targetPos.y += 0.1f;
                GetTargetCursor().transform.position = targetPos;

                if (TileNavGraph.Instance.IsPosValid(newPos) && TileNavGraph.Instance.IsNodeWalkable(TileNavGraph.Instance.GetNode(newPos)))
                {
                    unit.SetTargetPos(newPos);
                    unit.movement.FindPathToTarget();
                }
            }
            
            
        };
    }

    public void SwitchMoveState()
    {
        unit.SwicthState();
    }

    public void AddNewUnit()
    {
        Vector3 newUnitPosition = PlayerStart.position;

        foreach (Unit existingUnit in units)
        {
            Vector3 offset = existingUnit.transform.position - newUnitPosition;
            if (offset.magnitude < 1.0f) 
            {
                newUnitPosition.x += 1.0f;
            }
        }

        GameObject newUnitInst = Instantiate(UnitPrefab, newUnitPosition, Quaternion.identity);
        newUnitInst.transform.parent = null; 

        Unit newUnit = newUnitInst.GetComponent<Unit>();
        units.Add(newUnit);
        unit.movement.followCount += 1;
        newUnit.movement.followNumber = units.IndexOf(newUnit);
        
        if (unit != null)
        {
            newUnit.SetLeader(unit);
        }

        RaycastHit raycastInfo;
        Ray ray = new Ray(newUnitInst.transform.position, Vector3.down);
        if (Physics.Raycast(ray, out raycastInfo, 10f, 1 << LayerMask.NameToLayer("Floor")))
        {
            newUnitInst.transform.position = raycastInfo.point;
        }
    }

    void SwitchFormat()
    {
        if (unit)
        {
            if (unit.movement.format == Movement.Format.Circle)
                unit.movement.format = Movement.Format.Cube;
            else if (unit.movement.format == Movement.Format.Cube)
                unit.movement.format = Movement.Format.Circle;
        }
    }
    
    // Update is called once per frame
    private void Update ()
    {
        if (Input.GetMouseButtonDown(0))
            OnMouseClicked();
        
        if (Input.GetKeyDown(KeyCode.Q))
            SwitchMoveState();

        if (Input.GetKeyDown(KeyCode.E))
            AddNewUnit();
        
        if (Input.GetKeyDown(KeyCode.F))
            SwitchFormat();
    }
}
