using Photon.Pun;
using UnityEngine;

public class BlueHealth : Health
{
    protected override void Die(int killerActorId)
    {
        base.Die(killerActorId);
        if (photonView.IsMine)
        {
            photonView.RPC(nameof(RPC_HidePlayer), RpcTarget.All);
            photonView.RPC(nameof(NotifyPlayerDeath), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    [PunRPC]
    private void RPC_HidePlayer()
    {
        gameObject.SetActive(false);
    }

    [PunRPC]
    private void NotifyPlayerDeath(int actorNumber)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PlayerSpawnManager.Instance.PlayerDied(actorNumber);
        }
    }
    
}
