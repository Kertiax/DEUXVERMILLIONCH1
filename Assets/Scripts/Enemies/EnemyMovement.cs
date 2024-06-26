using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class EnemyMovement : NetworkBehaviour
{
    [SerializeField] NavMeshAgent aiNav;
    public PlayerMovement[] players;
    [SerializeField] float attackRange = 2.0f;
    public Animator enemyAnim;
    [SerializeField] float speed = 2f;
    
    [SerializeField] float waitintgBeforeHuntingTime = 2f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }
        aiNav.speed = speed;
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
        players = FindObjectsOfType<PlayerMovement>();
        InvokeRepeating("UpdateNearestPlayer", 0.5f, 2f);
    }

    private void ClientDisconnected(ulong u)
    {
        players = null;
    }
    private void Update() {
        aiNav.speed = speed;
    }
    public void UpdateNearestPlayer()
    {
        if (players.Length == 0)
            return;

        float minDistance = float.MaxValue;
        PlayerMovement nearestPlayer = null;

        foreach (PlayerMovement player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPlayer = player;
            }
        }

        if (nearestPlayer != null && aiNav != null)
        {
            aiNav.SetDestination(nearestPlayer.transform.position);
            enemyAnim.SetBool("isWalking", true);
        }
    }

    public void OnPlayerKilled()
    {
        enemyAnim.SetBool("isWalking", false);
        StartCoroutine(UpdatePlayersAfterDelay());
    }

    public IEnumerator UpdatePlayersAfterDelay()
    {
        yield return new WaitForSeconds(waitintgBeforeHuntingTime); 
        players = FindObjectsOfType<PlayerMovement>();
        UpdateNearestPlayer();
    }
}

  
