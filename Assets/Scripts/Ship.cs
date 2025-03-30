using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class Ship : MonoBehaviour
{ 
    public float xOffset;
    public float zOffset;
    public float xOffsetHorizontal;
    public float zOffsetHorizontal;
    public int size;
    
    public bool isHorizontal;
    public Vector2Int[] positions; /* Store the grid positions occupied by this ship*/

    public Vector3 initialPosition;/*store the inital positon to return the ship to it*/
    private int hitCount;
    private bool isSelected = false;
    private bool isPlaced;
    public bool IsPlaced { get => isPlaced; set => isPlaced = value; } /*get set for isPlaced*/
    [SerializeField]  private GameBoardManager gameBoardManager;

    #region Ship lambda function
    public bool IsSunk => hitCount == size;/*return if the hit count equal to size that mean the ship has been sunked*/
    public bool IsAtPosition(Vector2Int position) => positions.Contains(position);/*return if ship at this position it used when we want to chick if tile empty*/
    public void UpdateGridPositions(Vector2Int[] positions) => this.positions = positions;/*to save the location of the ship*/
    public void ResetToInitialPosition() => transform.position = initialPosition;/*return to wooden dock*/
    public void Hit() => hitCount += !IsSunk ? 1 : 0;/*increse the hit count if the ship not sunked*/
    public int RemaningSize() => size - hitCount;
    #endregion

    #region Ship placment function
    private void Start()
    {
        initialPosition = transform.position;
        positions =new Vector2Int[size];
        hitCount = 0;
        isHorizontal = false;
        this.IsPlaced = false;
        if (gameBoardManager == null)
            Debug.LogError("GameBoardManager is not assigned to the ship! Make sure to set it in the Inspector.");
    }

    private void Update()
    {
        if (isSelected&& Input.GetMouseButtonDown(0))
            isSelected = false;
    }

    public void OnMouseUpAsButton()
    {
        if (!gameBoardManager.IsSelectedShip())
        {
            gameBoardManager.SelectShip(this);

            isSelected = true;
            if (isPlaced)
            {
                this.IsPlaced = false;
                if(isHorizontal)
                        this.Rotate();
                this.ResetToInitialPosition();
                gameBoardManager.ResetTileShipOccupied(positions);
                gameBoardManager.DecShipPlacedNum();
            }
        }
    }

    public void Rotate()
    {
        isHorizontal = !isHorizontal;
        if (isHorizontal)
            transform.Rotate(0, 0, 90); /* Rotate 90 degrees around the X axis*/
        else
            transform.Rotate(0, 0, -90);
        
    }
    #endregion

}