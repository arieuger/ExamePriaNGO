using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{

    [SerializeField] private int maxPlayersPerTeam = 2;

    public static GameManager Instance;
    public List<Material> materials; // 0 -> blanco | 1 a 3 -> Left | 4 a 6 -> Right

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
            SubmitNewPositionButton();
        }

        GUILayout.EndArea();
    }

    static void StartButtons()
    {
        if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
        if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
        if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
    }

    static void StatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
        
    }

    private static void SubmitNewPositionButton()
    {
        if (GUILayout.Button("Mover ao inicio"))
        {
            if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
            {
                foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>().ServerMove();
                }

            }
            else
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                playerObject.GetComponent<Player>().MoveToStartServerRpc();
            }
        }
    }


    public void CheckMovementActivation()
    {
        // Recorremos todos os clientes para comprobar cantos hai de cada equipo
        List<Player> leftPlayers = new List<Player>();
        List<Player> rightPlayers = new List<Player>();
        List<Player> whitePlayers = new List<Player>();
        foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
        {
            Player player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>();
            if (player.ColorIndex.Value > 0 && player.ColorIndex.Value <= 3)
            {
                leftPlayers.Add(player);
            }
            else if (player.ColorIndex.Value > 3 && player.ColorIndex.Value <= 6)
            {
                rightPlayers.Add(player);
            }
            else if (player.ColorIndex.Value == 0)
            {
                whitePlayers.Add(player);
            }
        }


        // Se hai dous de calquera dos equipos, desactivamos o movemento do contrario e, 
        // por se acaso non estivese activo xa, activamos o propio
        if (leftPlayers.Count == maxPlayersPerTeam || rightPlayers.Count == maxPlayersPerTeam)
        {
            foreach (Player player in leftPlayers)
                player.EnableOrDisableMovementClientRpc(leftPlayers.Count == maxPlayersPerTeam);

            foreach (Player player in rightPlayers)
                player.EnableOrDisableMovementClientRpc(rightPlayers.Count == maxPlayersPerTeam);
            
            foreach (Player player in whitePlayers)
                player.EnableOrDisableMovementClientRpc(false);
        }
        else
        // No caso contrario (non hai 2 xogadores en ningún equipo) unificamos as listas
        // e activamos o movemento de todos
        {
            leftPlayers.AddRange(rightPlayers);
            leftPlayers.AddRange(whitePlayers);
            foreach (Player player in leftPlayers)
            {
                player.EnableOrDisableMovementClientRpc(true);
            }
        }

    }

}
