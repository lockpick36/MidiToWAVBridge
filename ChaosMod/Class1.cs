using BepInEx;
using BepInEx.Bootstrap;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[BepInPlugin("brt.lockpick36.horrormod", "RTX Globle Mod", "1.0.0")]
[BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
public class Class1 : BaseUnityPlugin
{
    public static ItemObject rtxItem;
    public static GameObject globlePrefab;

    void Awake()
    {
        GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorModifications);
        // Регистрируем PostLoad в очередь загрузки
        LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoad(), LoadingEventOrder.Post);
    }

    void GeneratorModifications(string name, int floor, SceneObject sceneObj)
    {
        if (rtxItem == null || globlePrefab == null) return;

        // Добавляем NPC
        if (sceneObj.forcedNpcs != null)
        {
            List<NPC> npcs = sceneObj.forcedNpcs.ToList();
            npcs.Add(globlePrefab.GetComponent<Globle>());
            sceneObj.forcedNpcs = npcs.ToArray();
        }

        // Добавляем предмет в магазин
        if (sceneObj.shopItems != null)
        {
            List<WeightedItemObject> items = sceneObj.shopItems.ToList();
            items.Add(new WeightedItemObject() { selection = rtxItem, weight = 100 });
            sceneObj.shopItems = items.ToArray();
        }
    }

    IEnumerator PostLoad()
    {
        yield return 2;
        yield return "Checking existing assets...";

        // 1. ПРОВЕРКА: Если предмет уже создан, выходим из загрузки
        // Это предотвратит ошибку "An item with the same key has already been added"
        if (rtxItem != null)
        {
            Debug.Log("[RTX LOG] Assets already loaded, skipping...");
            yield break;
        }

        yield return "Loading Horror Content...";

        try
        {
            // Загрузка текстур (путь с подпапкой Textures)
            Texture2D rtxTex = AssetLoader.TextureFromMod(this, "Textures", "rtx_glasses.png");
            Texture2D globleTex = AssetLoader.TextureFromMod(this, "Textures", "globle.png");
            Sprite rtxSprite = AssetLoader.SpriteFromTexture2D(rtxTex, 100f);
            Sprite globleIdle = AssetLoader.SpriteFromTexture2D(globleTex, 100f);

            // 2. СОЗДАНИЕ ПРЕДМЕТА
            // ВНИМАНИЕ: ItemBuilder сам регистрирует предмет в глобальный список по Enum.
            rtxItem = new ItemBuilder(Info)
                .SetEnum("RTX_Glasses")
                .SetSprites(rtxSprite, rtxSprite)
                .SetItemComponent<ITM_RTXGlasses>()
                .SetNameAndDescription("Itm_RTX", "Desc_RTX")
                .Build();

            // 3. СОЗДАНИЕ NPC
            globlePrefab = new NPCBuilder<Globle>(Info)
                .SetName("Char_Globle")
                .SetEnum("Globle")
                .Build().gameObject;

            globlePrefab.GetComponent<Globle>().idleSprite = globleIdle;

            // 4. РЕГИСТРАЦИЯ МЕТАДАННЫХ
            // Если ошибка повторится, попробуй закомментировать строку ниже, 
            // так как Builder иногда делает это сам в новых версиях API.
            if (ItemMetaStorage.Instance.Find(x => x.value == rtxItem) == null)
            {
                ItemMetaStorage.Instance.Add(new ItemMetaData(Info, rtxItem));
            }

            NPCMetaStorage.Instance.Add(new NPCMetadata(Info, new NPC[] { globlePrefab.GetComponent<Globle>() }, "Char_Globle", NPCFlags.None));

            Debug.Log("[RTX LOG] Load Successful!");
        }
        catch (Exception e)
        {
            Debug.LogError("[RTX LOG] Critical Error during creation: " + e.Message);
        }

        yield break;
    }
}