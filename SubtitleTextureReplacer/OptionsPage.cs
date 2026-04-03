using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.IO; // Исправит ошибку с "Path"
using UnityEngine.UI;

namespace Subtitles
{
    public class SubtitleOptionsPage : CustomOptionsCategory
    {
        private List<Image> barImages = new List<Image>();
        private Sprite barEmpty;
        private Sprite barFilled;
        private TMP_Text textureNameText; // Ссылка на текст между стрелками
        private int currentTextureIndex = 0;

        public override void Build()
        {
            // Загружаем ресурсы (Убедись, что PPU в LoadCustomSprite = 1.0f!)
            Sprite arrowL = Subtitles.LoadCustomSprite("MenuArrowLeft.png");
            Sprite arrowLH = Subtitles.LoadCustomSprite("MenuArrowLeft_H.png");
            Sprite arrowR = Subtitles.LoadCustomSprite("MenuArrowRight.png");
            Sprite arrowRH = Subtitles.LoadCustomSprite("MenuArrowRight_H.png");
            barEmpty = Subtitles.LoadCustomSprite("AdjustmentBar.png");
            barFilled = Subtitles.LoadCustomSprite("AdjustmentBarFilled.png");

            // --- СЕКЦИЯ TEXTURE ---
            CreateText("Label1", "TEXTURE", new Vector3(-120, 40, 0), BaldiFonts.ComicSans18, TextAlignmentOptions.Left, new Vector2(100, 25), Color.black, false);

            // Левая стрелка
            CreateButton(OnPressLeft, arrowL, arrowLH, "BtnL", new Vector3(-20, 40, 0), new Vector2(17, 32));

            // ТЕКСТ МЕЖДУ СТРЕЛКАМИ (сохраняем в переменную)
            textureNameText = CreateText("FileName", "NONE", new Vector3(30, 40, 0), BaldiFonts.ComicSans18, TextAlignmentOptions.Center, new Vector2(80, 25), Color.blue, false);

            // Правая стрелка
            CreateButton(OnPressRight, arrowR, arrowRH, "BtnR", new Vector3(80, 40, 0), new Vector2(17, 32));

            // --- СЕКЦИЯ OPACITY ---
            CreateText("Label2", "OPACITY", new Vector3(-120, -10, 0), BaldiFonts.ComicSans18, TextAlignmentOptions.Left, new Vector2(100, 25), Color.black, false);

            // Отрисовка баров (смещаем правее, чтобы не наезжали на текст)
            CreateTransparencyBars(new Vector3(-10, -10, 0), Subtitles.transparency.Value);

            UpdateTextureName(); // Сразу пишем имя файла при открытии
        }

        private void CreateTransparencyBars(Vector3 startPos, float currentAlpha)
        {
            barImages.Clear();
            for (int i = 0; i < 10; i++)
            {
                int barIndex = i + 1;
                GameObject barObj = new GameObject("Bar_" + i);
                barObj.transform.SetParent(this.transform, false); // false важен для сохранения координат
                barObj.transform.localPosition = startPos + new Vector3(i * 12, 0, 0);

                Image img = barObj.AddComponent<Image>();
                img.sprite = (i < (currentAlpha * 10f)) ? barFilled : barEmpty;

                // Принудительно ставим размер через RectTransform
                RectTransform rt = img.rectTransform;
                rt.sizeDelta = new Vector2(8, 32);

                barImages.Add(img);

                Button btn = barObj.AddComponent<Button>();
                btn.onClick.AddListener(() => { SetOpacity(barIndex); });
            }
        }

        // Логика при нажатии на деление бара
        private void SetOpacity(int count)
        {
            float newAlpha = count / 10f; // Превращаем 1..10 в 0.1..1.0
            Subtitles.transparency.Value = newAlpha;

            // Обновляем визуально палочки
            for (int i = 0; i < barImages.Count; i++)
            {
                barImages[i].sprite = (i < count) ? barFilled : barEmpty;
            }

            // Применяем изменения (сохранение и обновление текстуры в игре)
            Subtitles.Instance.Config.Save();
            Subtitles.Instance.LoadTexture();
        }

        // Обработчики стрелочек (пример)
        private void UpdateTextureName()
        {
            if (textureNameText != null && Subtitles.textureFiles.Count > 0)
            {
                // Берем имя файла без расширения для красоты
                string name = Path.GetFileNameWithoutExtension(Subtitles.textureFiles[currentTextureIndex]);
                textureNameText.text = name.ToUpper();
            }
        }

        void OnPressLeft()
        {
            currentTextureIndex--;
            if (currentTextureIndex < 0) currentTextureIndex = Subtitles.textureFiles.Count - 1;
            ApplyTexture();
        }

        void OnPressRight()
        {
            currentTextureIndex++;
            if (currentTextureIndex >= Subtitles.textureFiles.Count) currentTextureIndex = 0;
            ApplyTexture();
        }

        private void ApplyTexture()
        {
            Subtitles.currentTexture.Value = Subtitles.textureFiles[currentTextureIndex];
            Subtitles.Instance.LoadTexture(); // Метод в твоем главном классе
            UpdateTextureName();
        }
    }
}