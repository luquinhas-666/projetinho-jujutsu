using System;
using System.Drawing;

namespace movimento;

// Fundo consistente entre os modos clássicos e a Arena Avançada.
// Usa apenas a API portátil do projeto (Canvas), sem Windows Forms.
internal static class ArenaVisual
{
    internal static void Desenhar(Graphics g, int largura, int altura, string modo)
    {
        g.Clear(Color.FromArgb(12, 20, 34));
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (Brush piso = new SolidBrush(Color.FromArgb(20, 31, 49)))
            g.FillRectangle(piso, 16, 101, largura - 32, altura - 148);

        using (Pen grade = new Pen(Color.FromArgb(55, 90, 124, 149), 1f))
        {
            for (int i = 16; i < largura; i += 48)
                g.DrawLine(grade, i, 101, i, altura - 47);
            for (int i = 101; i < altura - 47; i += 48)
                g.DrawLine(grade, 16, i, largura - 16, i);
        }

        float cx = largura / 2f, cy = altura * .53f;
        using (Pen anel = new Pen(Color.FromArgb(70, 115, 155, 199), 2f))
        using (Pen acento = new Pen(Color.FromArgb(64, 213, 139, 188), 1.5f))
        {
            g.DrawEllipse(anel, cx - 145, cy - 145, 290, 290);
            g.DrawEllipse(acento, cx - 95, cy - 95, 190, 190);
            g.DrawLine(acento, cx - 160, cy, cx + 160, cy);
            g.DrawLine(acento, cx, cy - 160, cx, cy + 160);
        }

        using (Brush topo = new SolidBrush(Color.FromArgb(234, 8, 13, 26)))
        using (Brush rodape = new SolidBrush(Color.FromArgb(239, 8, 13, 25)))
        {
            g.FillRectangle(topo, 0, 0, largura, 101);
            g.FillRectangle(rodape, 0, altura - 47, largura, 47);
        }
        using (Pen borda = new Pen(Color.FromArgb(121, 85, 132, 160), 1.5f))
        {
            g.DrawRectangle(borda, 11, 101, largura - 22, altura - 148);
            g.DrawLine(borda, 0, 101, largura, 101);
        }
        using (Font fonte = new Font("Segoe UI", 9, FontStyle.Bold))
        using (StringFormat centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        using (Brush texto = new SolidBrush(Color.FromArgb(215, 176, 197, 224)))
            g.DrawString("JUJUTSU ARENA  /  " + modo, fonte, texto,
                new RectangleF(0, altura - 42, largura, 30), centro);
    }
}
