using UnityEngine;

public class TowerShopUI : MonoBehaviour
{
    public TowerPlacer placer;
    public Tower ballistaPrefab;
    public Tower cannonPrefab;

    public void BuyBallista() { placer.BeginPlacement(ballistaPrefab); }
    public void BuyCannon()   { placer.BeginPlacement(cannonPrefab); }
}
