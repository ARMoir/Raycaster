using Raycaster;
using System;
using System.Drawing;
using System.Numerics;

public static class SkeletonRenderer
{
    public static void Render(
        Graphics g,
        SkeletonEnemy enemy,
        Player player,
        double[] zBuffer,
        int screenWidth,
        int screenHeight)
    {
        double spriteX = enemy.X - player.posX;
        double spriteY = enemy.Y - player.posY;

        double invDet =
            1.0 / (player.planeX * player.dirY - player.dirX * player.planeY);

        double transformX = invDet * (player.dirY * spriteX - player.dirX * spriteY);
        double transformY = invDet * (-player.planeY * spriteX + player.planeX * spriteY);

        if (transformY <= 0.05) return;

        int screenX = (int)((screenWidth / 2) * (1 + transformX / transformY));
        if (screenX < 0 || screenX >= screenWidth) return;
        if (transformY >= zBuffer[screenX]) return;

        // Sprite scale
        float spriteHeight = (float)(screenHeight / transformY);
        float spriteWidth = spriteHeight;
        float spriteBottom = screenHeight / 2 + spriteHeight / 2;
        float spriteTop = spriteBottom - spriteHeight;
        float spriteLeft = screenX - spriteWidth / 2;

        foreach (var b in enemy.Bones)
        {
            // Use Local positions if exploding, otherwise normal positions
            Vector2 ls, le;
            if (enemy.Exploding && b.Broken)
            {
                ls = b.LocalStart;
                le = b.LocalEnd;
            }
            else
            {
                ls = b.Broken ? b.WorldStart : b.LocalStart;
                le = b.Broken ? b.WorldEnd : b.LocalEnd;
            }

            PointF a = new(
                spriteLeft + (ls.X + 0.5f) * spriteWidth,
                spriteTop + (ls.Y + 0.5f) * spriteHeight
            );

            PointF e = new(
                spriteLeft + (le.X + 0.5f) * spriteWidth,
                spriteTop + (le.Y + 0.5f) * spriteHeight
            );

            // Shrink thickness if exploding
            float thickness = enemy.Exploding && b.Broken ? b.Thickness * spriteHeight * 0.2f : b.Thickness * spriteHeight * 0.3f;

            DrawGlowLine(g, a, e, thickness, Color.DarkOrange);
        }
    }

    static void DrawGlowLine(Graphics g, PointF a, PointF b, float width, Color c)
    {
        for (int i = 3; i >= 1; i--)
        {
            using var glow = new Pen(Color.FromArgb(30, c), width + i * 2);
            g.DrawLine(glow, a, b);
        }
        using var pen = new Pen(c, width);
        g.DrawLine(pen, a, b);
    }
}
