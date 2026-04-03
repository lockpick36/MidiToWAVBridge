using UnityEngine;

public class ITM_RTXGlasses : Item
{
    public override bool Use(PlayerManager pm)
    {
        // Пока что очки просто исчезают из инвентаря при использовании
        UnityEngine.Debug.Log("[RTX LOG] Очки использованы игроком!");

        // Здесь мы позже пропишем эффект рентгена

        // Возвращаем true, чтобы предмет удалился из слота
        return true;
    }
}