using UnityEngine;
using UnityEngine.InputSystem;

public class TowerShopUI : MonoBehaviour
{
    public TowerPlacer placer;
    public Tower ballistaPrefab;
    public Tower cannonPrefab;
    public Tower magicPrefab;
    public Tower airPrefab;

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

    public void BuyMagicAttack()
    {
        if (magicPrefab) placer.BeginPlacement(magicPrefab);
    }

    public void BuyAirAttack()
    {
        if (airPrefab) placer.BeginPlacement(airPrefab);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) BuyLightAttack();
        if (Keyboard.current.digit2Key.wasPressedThisFrame) BuyHeavyAttack();
        if (Keyboard.current.digit3Key.wasPressedThisFrame) BuyMagicAttack();
        if (Keyboard.current.digit4Key.wasPressedThisFrame) BuyAirAttack();
    }
}
