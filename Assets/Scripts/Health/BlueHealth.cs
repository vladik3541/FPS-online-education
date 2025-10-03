using Photon.Pun;
using UnityEngine;

public class BlueHealth : Health
{
    protected override void Die(int killerActorId, string weaponType)
    {
        // ✅ Повідомляємо PlayerSpawnManager про смерть (тільки ОДИН РАЗ - від власника)
        if (photonView.IsMine)
        {
            photonView.RPC(nameof(NotifyPlayerDeath), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }

        // ✅ Викликаємо base.Die() який нарахує винагороду і деактивує об'єкт
        base.Die(killerActorId, weaponType);
    }

    [PunRPC]
    private void NotifyPlayerDeath(int actorNumber)
    {
        if (PhotonNetwork.IsMasterClient && PlayerSpawnManager.Instance != null)
        {
            PlayerSpawnManager.Instance.PlayerDied(actorNumber);
        }
    }
}