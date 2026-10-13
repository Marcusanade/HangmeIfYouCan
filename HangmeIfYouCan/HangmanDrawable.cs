namespace HangmeIfYouCan;

public class HangmanDrawable : IDrawable
{
    public int WrongGuesses { get; set; }

    // SVG viewBox de referencia: 180 × 200 (igual que el proyecto web)
    const float SW = 180f, SH = 200f;

    public void Draw(ICanvas canvas, RectF rect)
    {
        if (rect.Width < 1 || rect.Height < 1) return;

        float sc = Math.Min(rect.Width / SW, rect.Height / SH);
        float ox  = rect.X + (rect.Width  - SW * sc) / 2f;
        float oy  = rect.Y + (rect.Height - SH * sc) / 2f;

        float X(float x) => ox + x * sc;
        float Y(float y) => oy + y * sc;
        float S(float s) => s * sc;

        canvas.StrokeLineCap = LineCap.Round;

        // ── Horca (siempre visible) ─────────────────────────────────
        canvas.StrokeColor = Color.FromArgb("#1a1a1a");
        canvas.StrokeSize  = S(5);
        canvas.DrawLine(X(10),  Y(190), X(155), Y(190)); // base
        canvas.DrawLine(X(40),  Y(190), X(40),  Y(15));  // palo
        canvas.DrawLine(X(40),  Y(15),  X(120), Y(15));  // travesaño
        canvas.StrokeSize = S(4);
        canvas.DrawLine(X(40),  Y(52),  X(73),  Y(15));  // diagonal refuerzo

        canvas.StrokeColor = Color.FromArgb("#8B4513"); // cuerda marrón
        canvas.StrokeSize  = S(3);
        canvas.DrawLine(X(120), Y(15),  X(120), Y(31));

        if (WrongGuesses == 0) return;

        // ── Cabeza (error 1+) ───────────────────────────────────────
        string headFill = WrongGuesses switch
        {
            1 or 2 => "#fff0f0",
            3 or 4 => "#ffcccc",
            _      => "#ffaaaa"
        };
        canvas.FillColor   = Color.FromArgb(headFill);
        canvas.StrokeColor = Color.FromArgb("#e74c3c");
        canvas.StrokeSize  = S(2);
        // cx=120, cy=47, r=16  →  rect x=104, y=31, w=32, h=32
        canvas.FillEllipse(X(104), Y(31), S(32), S(32));
        canvas.DrawEllipse(X(104), Y(31), S(32), S(32));

        // Cara progresiva
        DrawFace(canvas, WrongGuesses, ox, oy, sc);

        // ── Cuerpo (error 2+) ───────────────────────────────────────
        canvas.StrokeColor = Color.FromArgb("#1a1a1a");
        canvas.StrokeSize  = S(3);
        if (WrongGuesses >= 2) canvas.DrawLine(X(120), Y(63),  X(120), Y(115));
        if (WrongGuesses >= 3) canvas.DrawLine(X(120), Y(80),  X(93),  Y(103));
        if (WrongGuesses >= 4) canvas.DrawLine(X(120), Y(80),  X(147), Y(103));
        if (WrongGuesses >= 5) canvas.DrawLine(X(120), Y(115), X(93),  Y(148));
        if (WrongGuesses >= 6) canvas.DrawLine(X(120), Y(115), X(147), Y(148));
    }

    private static void DrawFace(ICanvas canvas, int errors, float ox, float oy, float sc)
    {
        float X(float x) => ox + x * sc;
        float Y(float y) => oy + y * sc;
        float S(float s) => s * sc;

        canvas.StrokeColor  = Color.FromArgb("#e74c3c");
        canvas.StrokeLineCap = LineCap.Round;

        // ── Cejas ──────────────────────────────────────────────────
        canvas.StrokeSize = S(1.8f);
        switch (errors)
        {
            case 1 or 2: // planas
                canvas.DrawLine(X(112), Y(41), X(117), Y(41));
                canvas.DrawLine(X(123), Y(41), X(128), Y(41));
                break;
            case 3: // ligeramente preocupadas
                canvas.DrawLine(X(112), Y(42), X(117), Y(39));
                canvas.DrawLine(X(123), Y(39), X(128), Y(42));
                break;
            case 4: // preocupadas
                canvas.DrawLine(X(112), Y(44), X(117), Y(39));
                canvas.DrawLine(X(123), Y(39), X(128), Y(44));
                break;
            case 5: // terror (V muy pronunciada)
                canvas.DrawLine(X(112), Y(46), X(117), Y(38));
                canvas.DrawLine(X(123), Y(38), X(128), Y(46));
                break;
            default: // muerto: X
                canvas.DrawLine(X(112), Y(39), X(117), Y(44));
                canvas.DrawLine(X(117), Y(39), X(112), Y(44));
                canvas.DrawLine(X(123), Y(39), X(128), Y(44));
                canvas.DrawLine(X(128), Y(39), X(123), Y(44));
                break;
        }

        // ── Ojos ───────────────────────────────────────────────────
        canvas.FillColor = Color.FromArgb("#e74c3c");
        if (errors <= 4)
        {
            float r = errors <= 2 ? S(2.5f) : S(3.2f);
            canvas.FillEllipse(X(115) - r, Y(46) - r, r * 2, r * 2);
            canvas.FillEllipse(X(125) - r, Y(46) - r, r * 2, r * 2);
        }
        else if (errors == 5) // terror: óvalos
        {
            canvas.FillEllipse(X(111), Y(42), S(8), S(10));
            canvas.FillEllipse(X(121), Y(42), S(8), S(10));
        }
        else // muerto: ojos en X
        {
            canvas.StrokeSize = S(2f);
            canvas.DrawLine(X(112), Y(43), X(118), Y(49));
            canvas.DrawLine(X(118), Y(43), X(112), Y(49));
            canvas.DrawLine(X(122), Y(43), X(128), Y(49));
            canvas.DrawLine(X(128), Y(43), X(122), Y(49));
        }

        // ── Boca ───────────────────────────────────────────────────
        // Nota: círculo cabeza cx=120, cy=47, r=16 → x∈[104,136], y∈[31,63]
        // Para x=110..130 → max y≈58 para quedar dentro del círculo.
        canvas.FillColor  = Colors.Transparent;
        canvas.StrokeSize = S(2f);
        var path = new PathF();
        switch (errors)
        {
            case 1: // sonrisa (curva hacia abajo)
                path.MoveTo(X(110), Y(53));
                path.QuadTo(X(120), Y(59), X(130), Y(53));
                break;
            case 2: // recta
                path.MoveTo(X(110), Y(55));
                path.LineTo(X(130), Y(55));
                break;
            case 3: // leve fruncimiento
                path.MoveTo(X(110), Y(57));
                path.QuadTo(X(120), Y(53), X(130), Y(57));
                break;
            case 4 or 5: // fruncida
                path.MoveTo(X(110), Y(58));
                path.QuadTo(X(120), Y(52), X(130), Y(58));
                break;
            default: // muerto: arco muy triste
                path.MoveTo(X(113), Y(58));
                path.QuadTo(X(120), Y(49), X(127), Y(58));
                break;
        }
        canvas.DrawPath(path);
    }
}
