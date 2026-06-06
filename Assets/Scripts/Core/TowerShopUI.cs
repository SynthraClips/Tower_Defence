using UnityEngine;

public class TowerShopUI : MonoBehaviour
{
    public TowerPlacer placer;
    public Tower ballistaPrefab;
    public Tower cannonPrefab;

    public void BuyBallista() { BuyLightAttack(); }
    public void BuyCannon()   { BuyHeavyAttack(); }

    public void BuyLightAttack()
    {
        placer.BeginPlacement(ballistaPrefab);
    }

    public void BuyHeavyAttack()
    {
        placer.BeginPlacement(cannonPrefab);
    }
}
