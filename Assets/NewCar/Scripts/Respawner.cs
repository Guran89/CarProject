using UnityEngine;

public class Respawner : MonoBehaviour
{
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private GameObject player;

    void Start()
    {
        //spawnPoint = spawnPoint?.GetComponent<GameObject>();
        //player = player?.GetComponent<GameObject>();
    }

    /*void Update()
    {
        if (player.transform.position.y < -5)
        {
            player.
            player.transform.position = spawnPoint.transform.position;
        }
    }*/
}
