using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;

namespace FishingExpanded.Services
{
    /// <summary>BATCH-070: 本 Mod 自绘 HUD 消息的换行版本。
    /// 原生 <see cref="HUDMessage"/> 对非 noIcon 消息只按单行绘制且背景高度固定；
    /// 本子类按 UI 视口 1/3 宽度换行，并让背景高度随行数增加。</summary>
    public class WrappingHUDMessage : HUDMessage
    {
        private readonly float _maxWidth;

        public WrappingHUDMessage(string message, int whatType, float maxWidth)
            : base(message, whatType)
        {
            _maxWidth = Math.Max(1f, maxWidth);
        }

        public WrappingHUDMessage(string message, float maxWidth)
            : base(message)
        {
            _maxWidth = Math.Max(1f, maxWidth);
        }

        public override void draw(SpriteBatch b, int i, ref int heightUsed)
        {
            if (noIcon || messageSubject != null || string.IsNullOrEmpty(message))
            {
                base.draw(b, i, ref heightUsed);
                return;
            }

            string wrapped = Game1.parseText(message, Game1.smallFont, (int)Math.Ceiling(_maxWidth));
            string[] lines = wrapped.Replace("\r\n", "\n").Split('\n');
            float textWidth = 0f;
            foreach (string line in lines)
                textWidth = Math.Max(textWidth, Game1.smallFont.MeasureString(line).X);

            // 原生单行 HUD 背景高 96（source 24 × 4）：文字上边距约 34px、下边距约 30px。
            // 多行时背景高度 = 文本高度 + 上下边距，避免换行后文字超出气泡。
            // 注意：不能用 lines.Length * LineSpacing 代替实际测量高度。
            // SpriteFont.MeasureString 会取“换行间距之和 + 本行字形裁剪最大高度”，
            // 当中文字形高度大于 LineSpacing 时前者会更高，直接乘行数会低估气泡高度。
            float textHeight = Game1.smallFont.MeasureString(wrapped).Y;
            float boxHeight = Math.Max(96f, textHeight + 64f);
            float num2 = boxHeight + 16f;

            Rectangle titleSafeArea = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
            Vector2 vector = new Vector2(titleSafeArea.Left + 16, titleSafeArea.Bottom - (int)num2 - heightUsed - 64);
            heightUsed += (int)num2;

            if (Game1.isOutdoorMapSmallerThanViewport())
                vector.X = Math.Max(titleSafeArea.Left + 16, -Game1.uiViewport.X + 16);
            if (Game1.uiViewport.Width < 1400)
                vector.Y -= 48f;

            b.Draw(Game1.mouseCursors, vector, new Rectangle(293, 360, 26, 24), Color.White * transparency, 0f, Vector2.Zero, new Vector2(4f, boxHeight / 24f), SpriteEffects.None, 1f);
            b.Draw(Game1.mouseCursors, new Vector2(vector.X + 104f, vector.Y), new Rectangle(319, 360, 1, 24), Color.White * transparency, 0f, Vector2.Zero, new Vector2(textWidth, boxHeight / 24f), SpriteEffects.None, 1f);
            b.Draw(Game1.mouseCursors, new Vector2(vector.X + 104f + textWidth, vector.Y), new Rectangle(323, 360, 6, 24), Color.White * transparency, 0f, Vector2.Zero, new Vector2(4f, boxHeight / 24f), SpriteEffects.None, 1f);

            Vector2 iconVector = vector + new Vector2(16f, 16f);
            switch (whatType)
            {
                case HUDMessage.achievement_type:
                    b.Draw(Game1.mouseCursors, iconVector + new Vector2(8f, 8f) * 4f, new Rectangle(294, 392, 16, 16), Color.White * transparency, 0f, new Vector2(8f, 8f), 4f + Math.Max(0f, (timeLeft - 3000f) / 900f), SpriteEffects.None, 1f);
                    break;
                case HUDMessage.newQuest_type:
                    b.Draw(Game1.mouseCursors, iconVector + new Vector2(8f, 8f) * 4f, new Rectangle(403, 496, 5, 14), Color.White * transparency, 0f, new Vector2(3f, 7f), 4f + Math.Max(0f, (timeLeft - 3000f) / 900f), SpriteEffects.None, 1f);
                    break;
                case HUDMessage.error_type:
                    b.Draw(Game1.mouseCursors, iconVector + new Vector2(8f, 8f) * 4f, new Rectangle(268, 470, 16, 16), Color.White * transparency, 0f, new Vector2(8f, 8f), 4f + Math.Max(0f, (timeLeft - 3000f) / 900f), SpriteEffects.None, 1f);
                    break;
                case HUDMessage.stamina_type:
                    b.Draw(Game1.mouseCursors, iconVector + new Vector2(8f, 8f) * 4f, new Rectangle(0, 411, 16, 16), Color.White * transparency, 0f, new Vector2(8f, 8f), 4f + Math.Max(0f, (timeLeft - 3000f) / 900f), SpriteEffects.None, 1f);
                    break;
                case HUDMessage.health_type:
                    b.Draw(Game1.mouseCursors, iconVector + new Vector2(8f, 8f) * 4f, new Rectangle(16, 411, 16, 16), Color.White * transparency, 0f, new Vector2(8f, 8f), 4f + Math.Max(0f, (timeLeft - 3000f) / 900f), SpriteEffects.None, 1f);
                    break;
                case HUDMessage.screenshot_type:
                    b.Draw(Game1.mouseCursors2, iconVector + new Vector2(8f, 8f) * 4f, new Rectangle(96, 32, 16, 16), Color.White * transparency, 0f, new Vector2(8f, 8f), 4f + Math.Max(0f, (timeLeft - 3000f) / 900f), SpriteEffects.None, 1f);
                    break;
            }

            Vector2 textVector = iconVector + new Vector2(51f, 51f);
            if (number > 1)
                Utility.drawTinyDigits(number, b, textVector, 3f, 1f, Color.White * transparency);
            textVector += new Vector2(32f, -33f);
            Utility.drawTextWithShadow(b, wrapped, Game1.smallFont, textVector, Game1.textColor * transparency, 1f, 1f, -1, -1, transparency);
        }
    }
}
