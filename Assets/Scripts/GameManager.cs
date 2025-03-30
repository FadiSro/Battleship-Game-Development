#if UNITY_EDITOR
using Unity.Profiling.Editor;
using Unity.VisualScripting;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
public enum GameMode { PvP, PvC }
public enum GameState{ WaitingForPlayers, PlayerTurn,EnemyTurn,GameOver}
public class GameManager : MonoBehaviourPunCallbacks
{
    #region Varibles and Get/Set
    public static GameManager Instance;
    /*Missle objects*/
    public GameObject missilePrefab;
    public GameObject firePrefab;
    /* Stores references to the player's GameBoardManager and UIManager*/
    private GameBoardManager playerBoardManager;
    public GameBoardManager PlayerBoardManager { get => playerBoardManager; }
    private UIManager playerUIManager;
    /* Stores an empty matrix for the enemy's board (could also be a reference to a GameBoardManager for the enemy)*/
    private GameBoardManager enemyBoardManager;  /* If using a separate GameBoardManager for the enemy*/
    public GameBoardManager EnemyBoardManager { get => enemyBoardManager; }

    private AIManager aIManager;
    public AIManager AiManager { get => aIManager; }

    private Tile[,] enemyBoard;  /* Or use a matrix of Tiles for the enemy*/
    private Dictionary<Vector2Int, int> enemyColor = new Dictionary<Vector2Int, int>();
    public Dictionary<Vector2Int, int> EnemyColor { get => enemyColor; }
    private Dictionary<Vector2Int, int> playerColor = new Dictionary<Vector2Int, int>();
    public Dictionary<Vector2Int, int> PlayerColor { get => playerColor; }
   
    private List<GameObject> enemyFire = new List<GameObject>();
    public List<GameObject> EnemyFire { get => enemyFire; }
    private List<GameObject> playerFire = new List<GameObject>();
    public List<GameObject> PlayerFire { get => playerFire; }

    private bool setupComplete=false;
    public bool SetupComplete { get => setupComplete; set => setupComplete = value; }
    private bool playerTurn;
    public bool PlayerTurn { get => playerTurn; set => playerTurn = value; }

    private GameMode currentGameMode;
    public GameMode CurrentGameMode { get => currentGameMode; set => currentGameMode = value; }
    private GameState currentGameState;
    public GameState CurrentGameState { get => currentGameState; set => currentGameState = value; }
    #endregion
    #region GameManager lambda function
    public void LoadGameBoardScene() => SceneManager.LoadScene(1);
    public bool AreAllShipsPlaced() => playerBoardManager.ReturnshipPlacedNum()==5;
    #endregion
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    /*function to start player and enemy */
    public IEnumerator SetupGameManagersCoroutine()
    {
        yield return new WaitForSeconds(1f); /* Wait for all objects to be instantiated properly*/

        SetupGameManagers(); /* Call setup function*/
    }
    private void SetupGameManagers()
    {
        /* Use PhotonView to determine ownership and assign the correct board/UI for each player*/
        GameBoardManager[] boardManagers = FindObjectsOfType<GameBoardManager>();
        UIManager uiManagers = FindObjectOfType<UIManager>();
        AIManager aiManagers = FindObjectOfType<AIManager>();
        if (GameManager.Instance.currentGameMode == GameMode.PvP)
        {
            if (boardManagers[0].gameObject.layer == LayerMask.NameToLayer("PlayerBoard"))
            {
                playerBoardManager = boardManagers[0];
                enemyBoardManager = boardManagers[1];
            }
            else
            {
                playerBoardManager = boardManagers[1];
                enemyBoardManager = boardManagers[0];
            }
        }
        if (GameManager.Instance.currentGameMode == GameMode.PvC)/*PVC mode to set the PC ships*/
        {
            playerBoardManager = boardManagers[1];
            if (boardManagers[0].gameObject.layer == LayerMask.NameToLayer("PlayerBoard"))
                playerBoardManager= boardManagers[0];
            aIManager = aiManagers;
        }
        playerUIManager = uiManagers;
    }
    #region Set Turn
    private void AITurn()
    {
        int tileNum = AiManager.ExecuteTurn();
        string tileName = $"tile ({tileNum})";
        GameObject tileObject = GameObject.Find(tileName);
        LaunchMissile(tileObject.GetComponent<Tile>().transform.position);
    }
    private void SetUpPlayerTurn()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        playerUIManager.SetText(playerUIManager.msg, "Your Turn!");
        foreach (var ship in playerBoardManager.ships) ship.SetActive(false);
        foreach (var fire in playerFire) fire.SetActive(false);
        if (GameManager.Instance.CurrentGameMode == GameMode.PvP)
            enemyBoardManager.GetCurrentTileColor(enemyColor);
        else
            playerBoardManager.GetCurrentTileColor(enemyColor);
        foreach (var fire in enemyFire) fire.SetActive(true);
    }
    private void SetUpEnemyTurn()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        playerUIManager.SetText(playerUIManager.msg, "Enemy's Turn!");
        foreach (var ship in playerBoardManager.ships) ship.SetActive(true);
        foreach (var fire in enemyFire) fire.SetActive(false);
        playerBoardManager.GetCurrentTileColor(playerColor);
        foreach (var fire in playerFire) fire.SetActive(true);
        if (GameManager.Instance.CurrentGameMode == GameMode.PvC)
            AITurn();
    }
    private void SetTurnUI(bool isPlayerTurn)
    {
        if (isPlayerTurn)
            SetUpPlayerTurn();
        else
            SetUpEnemyTurn();
        if (GameManager.Instance.CurrentGameMode == GameMode.PvP)
        {
            playerUIManager.SetText(playerUIManager.playerShipNumTXT, playerBoardManager.ReturnshipPlacedNum().ToString());
            photonView.RPC("RequestEnemyShipNum", RpcTarget.All);
        }
        else
        {
            playerUIManager.SetText(playerUIManager.playerShipNumTXT, playerBoardManager.ReturnshipPlacedNum().ToString());
            playerUIManager.SetText(playerUIManager.enemyShipNumTXT, AiManager.shipPlacedNum.ToString());

        }

    }
    [PunRPC]
    public IEnumerator SwitchTurn()
    {
        yield return new WaitForSeconds(5f);
        playerTurn = !playerTurn;
        SetTurnUI(playerTurn);
    }
    [PunRPC]
    private void RespondEnemyShipNum(int shipPlacedNum)
    {
        playerUIManager.SetText(playerUIManager.enemyShipNumTXT, shipPlacedNum.ToString());
    }
    [PunRPC]
    private void RequestEnemyShipNum()
    {
        photonView.RPC("RespondEnemyShipNum", RpcTarget.All, playerBoardManager.ReturnshipPlacedNum());
    }
    #endregion

    public void LaunchMissile(Vector3 targetPosition)
    {
        targetPosition.y += 30;// Adjust missile position
        if (GameManager.Instance.CurrentGameMode == GameMode.PvP)
        {
            if (!playerTurn) return; // Prevent missile launch if it's not the player's turn
            PhotonNetwork.Instantiate(missilePrefab.name, targetPosition, Quaternion.Euler(0, 0, 180));
            photonView.RPC("SwitchTurn", RpcTarget.All);
        }
        else
        {
            Instantiate(missilePrefab, targetPosition, missilePrefab.transform.rotation);
            StartCoroutine(GameManager.Instance.SwitchTurn());
        }
    }

    private void SetDictionaryColor()/*Dictionary<Vector2Int, int> colorDictionary*/
    {
        foreach (Tile tile in playerBoardManager.tiles.Values)
        {
            enemyColor.Add(tile.position, 0);
            playerColor.Add(tile.position, 0);
        }
    }
    
    public void OnPlayerSetupComplete()
    {
        if(currentGameMode==GameMode.PvP)
            enemyBoardManager.woodDock.SetActive(false);
        playerBoardManager.woodDock.SetActive(false);
        setupComplete = true;
        foreach (var ship in playerBoardManager.ships)/*to make the ships not moving when the missile hit them*/
        {
            ship.GetComponent<Rigidbody>().isKinematic = true;
            if (GameManager.Instance.CurrentGameMode == GameMode.PvC)
                ship.GetComponent<BoxCollider>().isTrigger= true;
        }
        // Set initial UI and board visibility based on whose turn it is
        SetDictionaryColor();
        SetTurnUI(playerTurn);
    }
}