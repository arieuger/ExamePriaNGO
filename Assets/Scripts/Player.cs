using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{

    private float baseSpeed = 5f;
    private MeshRenderer mr;

    public NetworkVariable<int> ColorIndex = new NetworkVariable<int>();
    public NetworkVariable<bool> CanMove = new NetworkVariable<bool>();

    public override void OnNetworkSpawn()
    {
        ColorIndex.OnValueChanged += OnColorChanged;
        CanMove.Value = true;

        mr = GetComponent<MeshRenderer>();

        if (IsOwner) InitializeServerRpc();
        else mr.material = GameManager.Instance.materials[ColorIndex.Value];

        GameManager.Instance.CheckMovementActivation();
    }
    public override void OnNetworkDespawn()
    {
        ColorIndex.OnValueChanged -= OnColorChanged;
    }

    void Update()
    {
        if (IsOwner && CanMove.Value)
        {
            if (Input.GetKey(KeyCode.W)) MoveRequestServerRpc(Vector3.forward);
            if (Input.GetKey(KeyCode.S)) MoveRequestServerRpc(Vector3.back);
            if (Input.GetKey(KeyCode.A)) MoveRequestServerRpc(Vector3.left);
            if (Input.GetKey(KeyCode.D)) MoveRequestServerRpc(Vector3.right);
            if (Input.GetKeyDown(KeyCode.M))
            {
                MoveToStartServerRpc();
            }
        }
    }

    [ServerRpc]
    private void MoveRequestServerRpc(Vector3 direction)
    {
        transform.position += direction * baseSpeed * Time.deltaTime;
    }


    [ServerRpc]
    void InitializeServerRpc()
    {
        ColorIndex.Value = 0;  // Así sempre se executará o método "OnColorChanged" no inicio
        transform.position = new Vector3(Random.Range(-2.5f, 2.5f), 1.2f, Random.Range(-3f, 3f));
    }

    [ServerRpc]
    internal void MoveToStartServerRpc()
    {
        transform.position = GetRandomPosition();
    }

    [ClientRpc]
    internal void EnableOrDisableMovementClientRpc(bool isEnabled)
    {
        CanMove.Value = isEnabled;
    }

    public void OnColorChanged(int previous, int current)
    {
        mr.material = GameManager.Instance.materials[current];
        GameManager.Instance.CheckMovementActivation();
    }

    public void ServerMove()
    {
        transform.position = GetRandomPosition();
    }

    public static Vector3 GetRandomPosition()
    {
        bool toLeft = Random.Range(0, 2) == 0;
        float x = toLeft ? Random.Range(-9.5f, -3f) : Random.Range(3f, 9.5f);
        return new Vector3(x, 1.2f, Random.Range(-3f, 3f));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Left"))
            ColorIndex.Value = SetUnrepeatedRandomColor(1,3);
        else if (other.CompareTag("Right"))
            ColorIndex.Value = SetUnrepeatedRandomColor(4, 6);
    }

    private void OnTriggerExit(Collider other)
    {
        ColorIndex.Value = 0;
    }

    private int SetUnrepeatedRandomColor(int min, int max)
    {
        bool isRepeated = false;
        int randomIndexColor;

        do
        {
            randomIndexColor = Random.Range(min, max + 1);
            foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
            {
                isRepeated = false;
                if (randomIndexColor == NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid).GetComponent<Player>().ColorIndex.Value)
                {
                    isRepeated = true;
                    break;
                }
            }
        } while (isRepeated);

        return randomIndexColor;
    }

}
