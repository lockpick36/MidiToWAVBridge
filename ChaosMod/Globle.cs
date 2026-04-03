using UnityEngine;
using UnityEngine.AI; // Для NavMesh, если он используется в твоей сборке

public class Globle : NPC
{
    [Header("Globle Visuals")]
    public Sprite idleSprite;
    public Sprite angrySprite;

    public override void Initialize()
    {
        base.Initialize();

        // Стандартный способ Unity получить спрайт NPC
        if (spriteRenderer != null && spriteRenderer.Length > 0)
        {
            spriteRenderer[0].sprite = idleSprite;
        }

        Wander();
        Debug.Log("Globle: Инициализирован.");
    }

    public void Wander()
    {
        // 1. Установка скорости через Entity (стандартный метод Unity GetComponent)
        // В Baldi's Basics Plus у NPC обычно есть компонент Entity
        Entity entity = GetComponent<Entity>();
        if (entity != null)
        {
            // Устанавливаем базовую скорость перемещения
            navigator.maxSpeed = 15f;
            navigator.SetSpeed(15f);
        }

        // 2. Вместо GetNewTarget используем прямое обращение к случайной точке
        WanderRandomly();
    }

    // Реализация случайного блуждания через стандартный навигатор
    protected void WanderRandomly()
    {
        // В большинстве версий API для Baldi, у навигатора есть метод для выбора 
        // случайного свободного узла (Node) на карте.
        // Если GetNewTarget не работает, пробуем стандартный способ:

        if (navigator != null)
        {
            // Используем существующий метод WanderRandom вместо несуществующего FindBestAvailableDestination
            navigator.WanderRandom();
        }
    }

    public override void DestinationEmpty()
    {
        base.DestinationEmpty();
        // Когда NPC закончил путь, снова отправляем его бродить
        WanderRandomly();
    }
}