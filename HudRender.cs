using System.Drawing;
using System.Numerics;

namespace Raycaster
{
    public class HudRenderer
    {
        private readonly int _screenWidth;
        private readonly int _screenHeight;
        private readonly int _statusBarHeight;
        private readonly Font _font;


        public HudRenderer(int screenWidth, int screenHeight, int statusBarHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _statusBarHeight = statusBarHeight;
            _font = new Font("Consolas", 12, FontStyle.Bold);
        }

        private int ViewHeight => _screenHeight - _statusBarHeight;

        // ----------------------------
        // MAIN ENTRY POINT
        // ----------------------------
        public void Render(Graphics g, int[,] map, Player player)
        {
            DrawStatusBarBackground(g);
            DrawMiniMap(g, map, player);
            DrawStats(g, player);
            DrawInventory(g);
        }

        // ----------------------------
        // STATUS BAR BACKGROUND
        // ----------------------------
        private void DrawStatusBarBackground(Graphics g)
        {
            Rectangle bar = new Rectangle(
                0,
                ViewHeight,
                _screenWidth,
                _statusBarHeight
            );

            g.FillRectangle(Brushes.DarkSlateGray, bar);
            g.DrawRectangle(Pens.Black, bar);
        }

        // ----------------------------
        // MINI MAP
        // ----------------------------
        private void DrawMiniMap(Graphics g, int[,] map, Player player)
        {
            const int scale = 3;
            const int padding = 5;

            int mapHeight = map.GetLength(0);
            int mapWidth = map.GetLength(1);

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    Brush b = map[y, x] == 1 ? Brushes.Black : Brushes.LightGray;

                    g.FillRectangle(
                        b,
                        padding + x * scale,
                        ViewHeight + padding + y * scale,
                        scale,
                        scale
                    );
                }
            }

            // Player dot
            g.FillEllipse(
                Brushes.Red,
                padding + (float)player.posX * scale - 3,
                ViewHeight + padding + (float)player.posY * scale - 3,
                6,
                6
            );

            // Player direction
            g.DrawLine(
                new Pen(Color.Red, 2),
                padding + (float)player.posX * scale,
                ViewHeight + padding + (float)player.posY * scale,
                padding + (float)(player.posX + player.dirX * 2) * scale,
                ViewHeight + padding + (float)(player.posY + player.dirY) * scale
            );
        }

        // ----------------------------
        // PLAYER STATS
        // ----------------------------
        private void DrawStats(Graphics g, Player player)
        {
            int x = 200;
            int y = ViewHeight + 20;

            g.DrawString($"HP: {player.Health}", _font, Brushes.White, x, y);
            g.DrawString($"AMMO: {player.Ammo}", _font, Brushes.White, x, y + 25);
        }

        // ----------------------------
        // INVENTORY SLOTS
        // ----------------------------
        private void DrawInventory(Graphics g)
        {
            int slotSize = 32;
            int spacing = 8;
            int startX = 360;
            int y = ViewHeight + 20;

            for (int i = 0; i < 5; i++)
            {
                Rectangle slot = new Rectangle(
                    startX + i * (slotSize + spacing),
                    y,
                    slotSize,
                    slotSize
                );

                g.DrawRectangle(Pens.White, slot);
            }
        }
    }
}
