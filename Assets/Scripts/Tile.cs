using UnityEngine;

public class Tile : MonoBehaviour
{
    public Material normalMaterial;//Material.002 under assets->objects->misc
    public Vector2Int position; /*tile position*/
    private bool isMissileHit=false;
    public bool IsMissileHit { get => isMissileHit; set => isMissileHit = value; }
    private Renderer tileRenderer;/*Renderer is for drawing a GameObject on the screen*/
    private bool isOccupied;
    public bool IsOccupied{get => isOccupied; set => isOccupied = value; } /*get set for isOccupied*/
    private Ship occupyingShip;
    public Ship OccupyingShip { get => occupyingShip; set => occupyingShip = value; } /*get set for isOccupied*/
    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
    }
    #region tile lambda function
    public void HighlightTile() => tileRenderer.material.color = Color.green; /* Change the color when presse on ship */
    public void ResetHighlight() => tileRenderer.material = normalMaterial; /* Reset to the original color*/
    public void hitHighlightTile(Color color) => tileRenderer.material.color = color;/*Change the color when changing the turns*/
    #endregion
    public void OnMouseUpAsButton()
    {
        if (GameManager.Instance.CurrentGameMode == GameMode.PvP)
        {
            if (GameManager.Instance.PlayerTurn && GameManager.Instance.SetupComplete && !IsMissileHit)
            {
                Cursor.lockState = CursorLockMode.Locked;
                GameManager.Instance.LaunchMissile(transform.position);
            }
        }
        else if (GameManager.Instance.CurrentGameMode == GameMode.PvC)
        {
            bool isMissileHit = false;
            string numberPart = name.Replace("tile (", "").Replace(")", "");
            if (int.TryParse(numberPart, out int tileNumber))
                isMissileHit = GameManager.Instance.AiManager.IsMissileHit(tileNumber);
            if (GameManager.Instance.PlayerTurn && GameManager.Instance.SetupComplete && !isMissileHit)
            {
                Cursor.lockState = CursorLockMode.Locked;
                GameManager.Instance.LaunchMissile(transform.position);
            }
        }
    }
}
