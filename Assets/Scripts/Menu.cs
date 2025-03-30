using Photon.Pun;
using UnityEngine;

public class Menu : MonoBehaviour
{
    private NetworkControlar networkControlar;

    public void OnplayPvPButton()
    {
        networkControlar = GameManager.Instance.gameObject.AddComponent<NetworkControlar>(); // Add NetworkControlar to GameManager if playing online
        networkControlar.ConnectToServer();// Connect to Photon server
    }
    private void DisablePhotonViewsForPvC()/*need to disable the photonView*/
    {
        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
        foreach (var pv in photonViews)
            pv.enabled = false;
    }
    public void OnplayPvCButton()
    {
        GameManager.Instance.CurrentGameState = GameState.EnemyTurn;//
        GameManager.Instance.CurrentGameMode=GameMode.PvC;
        GameManager.Instance.PlayerTurn = true;
        DisablePhotonViewsForPvC();
        GameManager.Instance.StartCoroutine(GameManager.Instance.SetupGameManagersCoroutine());
        GameManager.Instance.LoadGameBoardScene();
    }

    public void onquitButton()
    {
        Application.Quit();
    }

    void OnDisable()
    {
        // Clean up when exiting the menu scene
        /*if (networkControlar != null)
        {
            networkControlar.DisconnectFromServer();
            Destroy(networkControlar);
        }*/
    }
}
